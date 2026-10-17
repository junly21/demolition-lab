using System.Collections.Generic;

namespace DemolitionLab
{
    internal static class StructuralGraphTraversal
    {
        internal static List<List<StructuralBlock>> FindActiveComponents(
            IReadOnlyDictionary<StructuralBlock, List<StructuralBlock>> adjacency)
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

        internal static bool HasGroundAnchor(IReadOnlyList<StructuralBlock> component)
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
    }
}
