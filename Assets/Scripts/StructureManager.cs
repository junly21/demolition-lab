using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public sealed class StructureManager : MonoBehaviour
    {
        [SerializeField] private List<StructuralBlock> blocks = new();
        [SerializeField] private StructuralStabilityEvaluator stabilityEvaluator;
        [SerializeField] private LocalSupportEvaluator localSupportEvaluator;
        [SerializeField] private bool keepDestroyedBlocksAsDebris;
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

            block.MarkDestroyed(keepDestroyedBlocksAsDebris, impulse, impactPoint);
            RecalculateSupport($"Destroyed structural node '{block.NodeId}'.");
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
                block.Initialize(this);
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
                    bool isConnected = HasGroundAnchor(component);
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
                        LocalSupportResult localSupport = localSupportEvaluator.Evaluate(component);
                        if (localSupport.UnsupportedBlocks.Count > 0)
                        {
                            int componentReleased = ReleaseBlocks(
                                localSupport.UnsupportedBlocks,
                                StructuralBlockState.Unstable);
                            releasedCount += componentReleased;
                            repeatEvaluation |= componentReleased > 0;
                            LogLocalSupportResult(component, localSupport, componentReleased);
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
            LogPhysicsHandoff(blocksToRelease);
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
            var unvisited = new HashSet<StructuralBlock>();
            foreach (StructuralBlock block in adjacency.Keys)
            {
                if (block.ParticipatesInStructure)
                {
                    unvisited.Add(block);
                }
            }

            var components = new List<List<StructuralBlock>>();
            var searchQueue = new Queue<StructuralBlock>();

            while (unvisited.Count > 0)
            {
                StructuralBlock start = null;
                foreach (StructuralBlock candidate in unvisited)
                {
                    start = candidate;
                    break;
                }

                var component = new List<StructuralBlock>();
                unvisited.Remove(start);
                searchQueue.Enqueue(start);

                while (searchQueue.Count > 0)
                {
                    StructuralBlock current = searchQueue.Dequeue();
                    component.Add(current);

                    foreach (StructuralBlock neighbor in adjacency[current])
                    {
                        if (neighbor.ParticipatesInStructure && unvisited.Remove(neighbor))
                        {
                            searchQueue.Enqueue(neighbor);
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }

        private static bool HasGroundAnchor(IReadOnlyList<StructuralBlock> component)
        {
            foreach (StructuralBlock block in component)
            {
                if (block.IsGroundAnchor)
                {
                    return true;
                }
            }

            return false;
        }

        private void LogPhysicsHandoff(IReadOnlyList<StructuralBlock> component)
        {
            for (int firstIndex = 0; firstIndex < component.Count; firstIndex++)
            {
                StructuralBlock first = component[firstIndex];
                Collider firstCollider = first.GetComponent<Collider>();

                for (int secondIndex = firstIndex + 1; secondIndex < component.Count; secondIndex++)
                {
                    StructuralBlock second = component[secondIndex];
                    Collider secondCollider = second.GetComponent<Collider>();
                    if (Physics.ComputePenetration(
                            firstCollider,
                            first.transform.position,
                            first.transform.rotation,
                            secondCollider,
                            second.transform.position,
                            second.transform.rotation,
                            out Vector3 direction,
                            out float distance)
                        && distance > 0.0001f)
                    {
                        Debug.LogError(
                            $"Physics handoff penetration: '{first.NodeId}' and '{second.NodeId}' "
                            + $"overlap by {distance:0.0000} m along {direction}.",
                            this);
                    }
                }
            }

            foreach (StructuralBlock block in component)
            {
                Rigidbody body = block.GetComponent<Rigidbody>();
                Debug.Log(
                    $"Release snapshot '{block.NodeId}': mass={body.mass:0.00}, "
                    + $"position={body.position}, linearVelocity={body.linearVelocity}, "
                    + $"angularVelocity={body.angularVelocity}.",
                    this);
            }
        }

        private void LogStabilityResult(
            IReadOnlyList<StructuralBlock> component,
            StructuralStabilityResult result,
            bool isConnected,
            bool isStable,
            int releasedCount)
        {
            string supportInterval = result.HasSupport
                ? $"[{result.SupportMinX:0.00}, {result.SupportMaxX:0.00}]"
                : "none";

            Debug.Log(
                $"Component '{component[0].NodeId}': nodes={component.Count}, "
                + $"mass={result.TotalMass:0.00}, COM.x={result.CenterOfMass.x:0.00}, "
                + $"support={supportInterval}, connected={isConnected}, stable={isStable}, "
                + $"released={releasedCount}.",
                this);
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
                LogStabilityResult(component, result, isConnected, isStable, releasedCount);
            }
        }

        private void LogLocalSupportResult(
            IReadOnlyList<StructuralBlock> component,
            LocalSupportResult result,
            int releasedCount)
        {
            var unsupportedIds = new List<string>();
            foreach (StructuralBlock block in result.UnsupportedBlocks)
            {
                unsupportedIds.Add(block.NodeId);
            }

            Debug.Log(
                $"Local support '{component[0].NodeId}': supported={result.SupportedBlocks.Count}, "
                + $"unsupported=[{string.Join(", ", unsupportedIds)}], "
                + $"maxCantileverBlocks={localSupportEvaluator.MaxCantileverBlocks}, "
                + $"released={releasedCount}.",
                this);
        }

        private void OnDrawGizmos()
        {
            if (!drawGraphGizmos)
            {
                return;
            }

            foreach (StructuralBlock block in blocks)
            {
                if (block == null)
                {
                    continue;
                }

                foreach (StructuralBlock neighbor in block.Neighbors)
                {
                    if (neighbor == null || blocks.IndexOf(block) >= blocks.IndexOf(neighbor))
                    {
                        continue;
                    }

                    Gizmos.color = GetStateColor(block, neighbor);
                    Gizmos.DrawLine(block.transform.position, neighbor.transform.position);
                }
            }
        }

        private static Color GetStateColor(StructuralBlock first, StructuralBlock second)
        {
            if (!Application.isPlaying)
            {
                return Color.green;
            }

            if (first.State == StructuralBlockState.Destroyed
                || second.State == StructuralBlockState.Destroyed)
            {
                return Color.gray;
            }

            if (first.State == StructuralBlockState.Disconnected
                || second.State == StructuralBlockState.Disconnected)
            {
                return Color.red;
            }

            if (first.State == StructuralBlockState.Unstable
                || second.State == StructuralBlockState.Unstable)
            {
                return Color.yellow;
            }

            return Color.green;
        }
    }
}
