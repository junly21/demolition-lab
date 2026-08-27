using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public sealed class StructureManager : MonoBehaviour
    {
        [SerializeField] private List<StructuralBlock> blocks = new();
        [SerializeField] private StructuralStabilityEvaluator stabilityEvaluator;
        [SerializeField] private bool keepDestroyedBlocksAsDebris;
        [SerializeField] private bool drawGraphGizmos = true;

        private readonly Dictionary<StructuralBlock, List<StructuralBlock>> adjacency = new();

        private void Awake()
        {
            InitializeGraph();
        }

        public void DestroyBlock(StructuralBlock block, Vector3 impulse, Vector3 impactPoint)
        {
            if (block == null || !block.IsActive)
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
            List<List<StructuralBlock>> components = FindActiveComponents();
            stabilityEvaluator?.BeginEvaluation();

            int supportedCount = 0;
            int unsupportedCount = 0;

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

                StructuralBlockState releaseState = isConnected
                    ? StructuralBlockState.Unstable
                    : StructuralBlockState.Disconnected;
                int releasedCount = 0;

                foreach (StructuralBlock block in component)
                {
                    if (isStable)
                    {
                        block.KeepSupported();
                        supportedCount++;
                    }
                    else
                    {
                        if (block.ReleaseToPhysics(releaseState))
                        {
                            releasedCount++;
                        }
                        unsupportedCount++;
                    }
                }

                if (stabilityEvaluator != null)
                {
                    LogStabilityResult(component, stability, isConnected, isStable, releasedCount);
                }
            }

            Debug.Log(
                $"{reason} Traversal visited {supportedCount} nodes: "
                + $"{supportedCount} supported, {unsupportedCount} unsupported.",
                this);
        }

        private List<List<StructuralBlock>> FindActiveComponents()
        {
            var unvisited = new HashSet<StructuralBlock>();
            foreach (StructuralBlock block in adjacency.Keys)
            {
                if (block.IsActive)
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
                        if (neighbor.IsActive && unvisited.Remove(neighbor))
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
