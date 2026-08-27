using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public sealed class StructureManager : MonoBehaviour
    {
        [SerializeField] private List<StructuralBlock> blocks = new();
        [SerializeField] private bool drawGraphGizmos = true;

        private readonly Dictionary<StructuralBlock, List<StructuralBlock>> adjacency = new();

        private void Awake()
        {
            InitializeGraph();
        }

        public void DestroyBlock(StructuralBlock block)
        {
            if (block == null || !block.IsActive)
            {
                return;
            }

            block.MarkDestroyed();
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
            var supported = new HashSet<StructuralBlock>();
            var searchQueue = new Queue<StructuralBlock>();

            foreach (StructuralBlock block in adjacency.Keys)
            {
                if (block.IsActive && block.IsGroundAnchor)
                {
                    supported.Add(block);
                    searchQueue.Enqueue(block);
                }
            }

            while (searchQueue.Count > 0)
            {
                StructuralBlock current = searchQueue.Dequeue();
                foreach (StructuralBlock neighbor in adjacency[current])
                {
                    if (neighbor.IsActive && supported.Add(neighbor))
                    {
                        searchQueue.Enqueue(neighbor);
                    }
                }
            }

            int unsupportedCount = 0;
            foreach (StructuralBlock block in adjacency.Keys)
            {
                if (!block.IsActive)
                {
                    continue;
                }

                bool isSupported = supported.Contains(block);
                block.SetSupported(isSupported);
                if (!isSupported)
                {
                    unsupportedCount++;
                }
            }

            Debug.Log(
                $"{reason} Traversal visited {supported.Count} nodes: "
                + $"{supported.Count} supported, {unsupportedCount} unsupported.",
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

                if (!block.IsActive && Application.isPlaying)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(block.transform.position, block.transform.lossyScale * 1.05f);
                }

                foreach (StructuralBlock neighbor in block.Neighbors)
                {
                    if (neighbor == null || blocks.IndexOf(block) >= blocks.IndexOf(neighbor))
                    {
                        continue;
                    }

                    if (Application.isPlaying && (!block.IsActive || !neighbor.IsActive))
                    {
                        Gizmos.color = Color.gray;
                    }
                    else if (Application.isPlaying && (!block.IsSupported || !neighbor.IsSupported))
                    {
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                    }

                    Gizmos.DrawLine(block.transform.position, neighbor.transform.position);
                }
            }
        }
    }
}
