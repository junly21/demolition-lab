using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public readonly struct LocalSupportResult
    {
        public LocalSupportResult(
            IReadOnlyList<StructuralBlock> supportedBlocks,
            IReadOnlyList<StructuralBlock> unsupportedBlocks)
        {
            SupportedBlocks = supportedBlocks;
            UnsupportedBlocks = unsupportedBlocks;
        }

        public IReadOnlyList<StructuralBlock> SupportedBlocks { get; }
        public IReadOnlyList<StructuralBlock> UnsupportedBlocks { get; }
    }

    public sealed class LocalSupportEvaluator : MonoBehaviour
    {
        [SerializeField, Min(0)] private int maxCantileverBlocks = 2;
        [SerializeField, Min(0f)] private float contactTolerance = 0.01f;
        [SerializeField] private bool drawLocalSupportGizmos = true;

        private readonly Dictionary<StructuralBlock, int> debugSupportedSpans = new();
        private readonly HashSet<StructuralBlock> debugUnsupported = new();
        private readonly List<SupportLink> debugLinks = new();

        public int MaxCantileverBlocks => maxCantileverBlocks;

        internal void BeginEvaluation()
        {
            debugSupportedSpans.Clear();
            debugUnsupported.Clear();
            debugLinks.Clear();
        }

        internal LocalSupportResult Evaluate(IReadOnlyList<StructuralBlock> component)
        {
            var spans = new Dictionary<StructuralBlock, int>();
            var searchQueue = new Queue<StructuralBlock>();

            foreach (StructuralBlock block in component)
            {
                if (!block.IsGroundAnchor)
                {
                    continue;
                }

                spans[block] = 0;
                searchQueue.Enqueue(block);
            }

            while (searchQueue.Count > 0)
            {
                StructuralBlock current = searchQueue.Dequeue();
                int currentSpan = spans[current];

                foreach (StructuralBlock neighbor in current.Neighbors)
                {
                    if (neighbor == null || !neighbor.ParticipatesInStructure || !Contains(component, neighbor))
                    {
                        continue;
                    }

                    bool isVerticalSupport = IsDirectlyAbove(neighbor, current);
                    bool isHorizontalSupport = !isVerticalSupport && IsHorizontalNeighbor(current, neighbor);
                    if (!isVerticalSupport && !isHorizontalSupport)
                    {
                        continue;
                    }

                    int candidateSpan = isVerticalSupport ? currentSpan : currentSpan + 1;
                    if (candidateSpan > maxCantileverBlocks
                        || spans.TryGetValue(neighbor, out int knownSpan) && knownSpan <= candidateSpan)
                    {
                        continue;
                    }

                    spans[neighbor] = candidateSpan;
                    searchQueue.Enqueue(neighbor);
                    debugLinks.Add(new SupportLink(current, neighbor, isVerticalSupport));
                }
            }

            var supported = new List<StructuralBlock>();
            var unsupported = new List<StructuralBlock>();
            foreach (StructuralBlock block in component)
            {
                if (spans.TryGetValue(block, out int span))
                {
                    supported.Add(block);
                    debugSupportedSpans[block] = span;
                    debugUnsupported.Remove(block);
                }
                else
                {
                    unsupported.Add(block);
                    debugUnsupported.Add(block);
                }
            }

            return new LocalSupportResult(supported, unsupported);
        }

        private bool IsDirectlyAbove(StructuralBlock upper, StructuralBlock lower)
        {
            Bounds upperBounds = upper.ColliderBounds;
            Bounds lowerBounds = lower.ColliderBounds;
            if (upperBounds.center.y <= lowerBounds.center.y
                || Mathf.Abs(upperBounds.min.y - lowerBounds.max.y) > contactTolerance)
            {
                return false;
            }

            return OverlapLength(upperBounds.min.x, upperBounds.max.x, lowerBounds.min.x, lowerBounds.max.x) > contactTolerance
                && OverlapLength(upperBounds.min.z, upperBounds.max.z, lowerBounds.min.z, lowerBounds.max.z) > contactTolerance;
        }

        private bool IsHorizontalNeighbor(StructuralBlock first, StructuralBlock second)
        {
            Bounds firstBounds = first.ColliderBounds;
            Bounds secondBounds = second.ColliderBounds;
            float faceDistance = Mathf.Min(
                Mathf.Abs(firstBounds.max.x - secondBounds.min.x),
                Mathf.Abs(secondBounds.max.x - firstBounds.min.x));

            return faceDistance <= contactTolerance
                && OverlapLength(firstBounds.min.y, firstBounds.max.y, secondBounds.min.y, secondBounds.max.y) > contactTolerance
                && OverlapLength(firstBounds.min.z, firstBounds.max.z, secondBounds.min.z, secondBounds.max.z) > contactTolerance;
        }

        private static float OverlapLength(float firstMin, float firstMax, float secondMin, float secondMax)
        {
            return Mathf.Min(firstMax, secondMax) - Mathf.Max(firstMin, secondMin);
        }

        private static bool Contains(IReadOnlyList<StructuralBlock> component, StructuralBlock target)
        {
            for (int index = 0; index < component.Count; index++)
            {
                if (component[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (!drawLocalSupportGizmos)
            {
                return;
            }

            foreach (KeyValuePair<StructuralBlock, int> supported in debugSupportedSpans)
            {
                if (supported.Key == null)
                {
                    continue;
                }

                Gizmos.color = Color.cyan;
                Bounds bounds = supported.Key.ColliderBounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size * 1.03f);
            }

            foreach (StructuralBlock block in debugUnsupported)
            {
                if (block == null)
                {
                    continue;
                }

                Gizmos.color = Color.magenta;
                Bounds bounds = block.ColliderBounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size * 1.06f);
            }

            foreach (SupportLink link in debugLinks)
            {
                if (link.From == null || link.To == null)
                {
                    continue;
                }

                Gizmos.color = link.IsVertical ? Color.cyan : Color.blue;
                Gizmos.DrawLine(link.From.transform.position, link.To.transform.position);
            }
        }

        private readonly struct SupportLink
        {
            public SupportLink(StructuralBlock from, StructuralBlock to, bool isVertical)
            {
                From = from;
                To = to;
                IsVertical = isVertical;
            }

            public StructuralBlock From { get; }
            public StructuralBlock To { get; }
            public bool IsVertical { get; }
        }
    }
}
