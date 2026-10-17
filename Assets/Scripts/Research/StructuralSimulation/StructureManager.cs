using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public sealed class StructureManager : MonoBehaviour
    {
        [SerializeField] private List<StructuralBlock> blocks = new();
        [SerializeField] private List<StructuralConnection> connections = new();
        [SerializeField] private StructuralStabilityEvaluator stabilityEvaluator;
        [SerializeField] private LocalSupportEvaluator localSupportEvaluator;
        [SerializeField] private StructuralLoadEvaluator loadEvaluator;
        [SerializeField] private bool keepDestroyedBlocksAsDebris;
        [SerializeField] private bool destroyImpactedBlocks = true;
        [SerializeField] private bool usePhysicalJoints;
        [SerializeField] private bool drawGraphGizmos = true;

        private readonly Dictionary<StructuralBlock, List<StructuralBlock>> adjacency = new();

        private void Awake()
        {
            InitializeGraph();
        }

        public void DestroyBlock(StructuralBlock block, Vector3 impulse, Vector3 impactPoint)
        {
            if (block == null || !block.ParticipatesInStructure)
            {
                return;
            }

            if (usePhysicalJoints)
            {
                Debug.Log(
                    $"Physical impact registered on '{block.NodeId}' with impulse "
                    + $"{impulse.magnitude:0.00} N·s; ConfigurableJoint physics remains authoritative.",
                    this);
                return;
            }

            int connectionEvaluations = EvaluateConnections(block, impulse.magnitude);
            string reason = $"Impacted structural node '{block.NodeId}'.";

            if (destroyImpactedBlocks)
            {
                block.MarkDestroyed(keepDestroyedBlocksAsDebris, impulse, impactPoint);
                DeactivateConnectionsFor(block);
                RebuildActiveAdjacency();
                connectionEvaluations += EvaluateConnections(null, 0f);
                reason = $"Destroyed structural node '{block.NodeId}'.";
            }

            RecalculateSupport(reason);
            LogEvaluationCounts(connectionEvaluations);
        }

        private void InitializeGraph()
        {
            adjacency.Clear();

            foreach (StructuralBlock block in blocks)
            {
                if (block == null || adjacency.ContainsKey(block))
                {
                    continue;
                }

                adjacency.Add(block, new List<StructuralBlock>());
                block.Initialize(this, usePhysicalJoints);
            }

            foreach (StructuralConnection connection in connections)
            {
                connection?.Initialize(this, usePhysicalJoints);
            }

            RebuildActiveAdjacency();
            int connectionEvaluations = usePhysicalJoints ? 0 : EvaluateConnections(null, 0f);

            int anchorCount = 0;
            foreach (StructuralBlock block in adjacency.Keys)
            {
                if (block.IsGroundAnchor)
                {
                    anchorCount++;
                }
            }

            if (anchorCount == 0)
            {
                Debug.LogError("StructureManager found no ground-anchored nodes.", this);
            }

            RecalculateSupport($"Initialized structural graph with {adjacency.Count} nodes and {anchorCount} anchors.");
            LogEvaluationCounts(connectionEvaluations);
        }

        internal void HandlePhysicalConnectionBreak(
            StructuralConnection connection,
            float measuredBreakForce)
        {
            if (!usePhysicalJoints || connection == null || !connections.Contains(connection))
            {
                return;
            }

            RebuildActiveAdjacency();
            RecalculateSupport(
                $"Physical joint broke between '{connection.BlockA.NodeId}' and "
                + $"'{connection.BlockB.NodeId}' at {measuredBreakForce:0.00} N; "
                + $"sampled peaks were {connection.PeakJointForce:0.00} N and "
                + $"{connection.PeakJointTorque:0.00} N·m.");
            LogEvaluationCounts(0);
        }

        private void RebuildActiveAdjacency()
        {
            foreach (List<StructuralBlock> neighbors in adjacency.Values)
            {
                neighbors.Clear();
            }

            if (connections.Count > 0)
            {
                foreach (StructuralConnection connection in connections)
                {
                    if (connection == null || !connection.IsValidForStaticGraph())
                    {
                        continue;
                    }

                    AddEdge(connection.BlockA, connection.BlockB);
                    AddEdge(connection.BlockB, connection.BlockA);
                }

                return;
            }

            foreach (StructuralBlock block in adjacency.Keys)
            {
                foreach (StructuralBlock neighbor in block.Neighbors)
                {
                    if (neighbor == null || neighbor == block || !adjacency.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    AddEdge(block, neighbor);
                    AddEdge(neighbor, block);
                }
            }
        }

        private int EvaluateConnections(StructuralBlock impactedBlock, float impactImpulseMagnitude)
        {
            if (loadEvaluator == null || connections.Count == 0)
            {
                return 0;
            }

            int totalEvaluations = 0;
            bool applyImpact = impactedBlock != null;
            StructuralLoadEvaluationResult result;

            do
            {
                result = loadEvaluator.Evaluate(
                    adjacency,
                    connections,
                    applyImpact ? impactedBlock : null,
                    applyImpact ? impactImpulseMagnitude : 0f);
                totalEvaluations += result.EvaluatedConnections;
                applyImpact = false;

                if (result.BrokenConnections > 0)
                {
                    RebuildActiveAdjacency();
                }
            }
            while (result.BrokenConnections > 0);

            return totalEvaluations;
        }

        private void DeactivateConnectionsFor(StructuralBlock block)
        {
            foreach (StructuralConnection connection in connections)
            {
                if (connection != null && connection.Connects(block))
                {
                    connection.Deactivate();
                }
            }
        }

        private void LogEvaluationCounts(int connectionEvaluations)
        {
            StructuralDiagnostics.LogEvaluationCounts(blocks, connections, connectionEvaluations, this);
        }

        private void AddEdge(StructuralBlock from, StructuralBlock to)
        {
            if (!adjacency[from].Contains(to))
            {
                adjacency[from].Add(to);
            }
        }

        private void RecalculateSupport(string reason)
        {
            stabilityEvaluator?.BeginEvaluation();
            localSupportEvaluator?.BeginEvaluation();

            int releasedCount = 0;
            bool repeatEvaluation;

            do
            {
                repeatEvaluation = false;
                List<List<StructuralBlock>> components = FindActiveComponents();

                foreach (List<StructuralBlock> component in components)
                {
                    bool isConnected = StructuralGraphTraversal.HasGroundAnchor(component);
                    bool isStable = isConnected;
                    StructuralStabilityResult stability = default;

                    if (stabilityEvaluator != null)
                    {
                        stability = stabilityEvaluator.Evaluate(component);
                        isStable = isConnected && stability.IsStable;
                    }

                    if (!isStable)
                    {
                        StructuralBlockState releaseState = isConnected
                            ? StructuralBlockState.Unstable
                            : StructuralBlockState.Disconnected;
                        int componentReleased = ReleaseBlocks(component, releaseState);
                        releasedCount += componentReleased;
                        LogStabilityResultIfAvailable(
                            component,
                            stability,
                            isConnected,
                            false,
                            componentReleased);
                        continue;
                    }

                    if (localSupportEvaluator != null)
                    {
                        LocalSupportResult localSupport = localSupportEvaluator.Evaluate(component, adjacency);
                        if (localSupport.UnsupportedBlocks.Count > 0)
                        {
                            int componentReleased = ReleaseBlocks(
                                localSupport.UnsupportedBlocks,
                                StructuralBlockState.Unstable);
                            releasedCount += componentReleased;
                            repeatEvaluation |= componentReleased > 0;
                            StructuralDiagnostics.LogLocalSupportResult(
                                component, localSupport, componentReleased,
                                localSupportEvaluator.MaxCantileverBlocks, this);
                            LogStabilityResultIfAvailable(
                                component,
                                stability,
                                true,
                                true,
                                componentReleased);
                            continue;
                        }
                    }

                    foreach (StructuralBlock block in component)
                    {
                        block.KeepSupported();
                    }

                    LogStabilityResultIfAvailable(component, stability, true, true, 0);
                }
            }
            while (repeatEvaluation);

            int supportedCount = 0;
            foreach (StructuralBlock block in adjacency.Keys)
            {
                if (block.ParticipatesInStructure)
                {
                    supportedCount++;
                }
            }

            Debug.Log(
                $"{reason} Traversal visited {supportedCount} nodes: "
                + $"{supportedCount} supported, {releasedCount} released.",
                this);
        }

        private int ReleaseBlocks(
            IReadOnlyList<StructuralBlock> blocksToRelease,
            StructuralBlockState releaseState)
        {
            StructuralDiagnostics.LogPhysicsHandoff(blocksToRelease, this);
            int releasedCount = 0;

            foreach (StructuralBlock block in blocksToRelease)
            {
                if (block.ReleaseToPhysics(releaseState))
                {
                    releasedCount++;
                }
            }

            return releasedCount;
        }

        private List<List<StructuralBlock>> FindActiveComponents()
        {
            return StructuralGraphTraversal.FindActiveComponents(adjacency);
        }

        private void LogStabilityResultIfAvailable(
            IReadOnlyList<StructuralBlock> component,
            StructuralStabilityResult result,
            bool isConnected,
            bool isStable,
            int releasedCount)
        {
            if (stabilityEvaluator != null)
            {
                StructuralDiagnostics.LogStabilityResult(component, result, isConnected, isStable, releasedCount, this);
            }
        }

        private void OnDrawGizmos()
        {
            if (drawGraphGizmos)
            {
                StructuralDiagnostics.DrawGraph(blocks);
            }
        }
    }
}
