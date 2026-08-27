using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public readonly struct StructuralLoadEvaluationResult
    {
        public StructuralLoadEvaluationResult(int evaluatedConnections, int brokenConnections)
        {
            EvaluatedConnections = evaluatedConnections;
            BrokenConnections = brokenConnections;
        }

        public int EvaluatedConnections { get; }
        public int BrokenConnections { get; }
    }

    public sealed class StructuralLoadEvaluator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float impactLoadScale = 1f;
        [SerializeField, Min(0f)] private float leverArmMultiplier = 0.5f;

        internal StructuralLoadEvaluationResult Evaluate(
            IReadOnlyDictionary<StructuralBlock, List<StructuralBlock>> adjacency,
            IReadOnlyList<StructuralConnection> connections,
            StructuralBlock impactedBlock,
            float impactImpulseMagnitude)
        {
            int evaluatedConnections = 0;
            var loads = new Dictionary<StructuralConnection, float>();

            foreach (StructuralConnection connection in connections)
            {
                if (connection == null || !connection.IsValidForStaticGraph())
                {
                    continue;
                }

                float gravityLoad = CalculateGravityLoad(adjacency, connection);
                float impactLoad = impactedBlock != null && connection.Connects(impactedBlock)
                    ? impactImpulseMagnitude * impactLoadScale
                    : 0f;
                loads[connection] = gravityLoad + impactLoad;
                evaluatedConnections++;
            }

            int brokenConnections = 0;
            foreach (KeyValuePair<StructuralConnection, float> connectionLoad in loads)
            {
                StructuralConnection connection = connectionLoad.Key;
                float load = connectionLoad.Value;
                connection.SetEstimatedLoad(load);
                if (load > connection.ConnectionStrength && connection.Break(load))
                {
                    brokenConnections++;
                    Debug.Log(
                        $"Structural connection broke: '{connection.BlockA.NodeId}' <-> "
                        + $"'{connection.BlockB.NodeId}', load={load:0.00}, "
                        + $"strength={connection.ConnectionStrength:0.00}.",
                        connection);
                }
            }

            return new StructuralLoadEvaluationResult(evaluatedConnections, brokenConnections);
        }

        private float CalculateGravityLoad(
            IReadOnlyDictionary<StructuralBlock, List<StructuralBlock>> adjacency,
            StructuralConnection connection)
        {
            HashSet<StructuralBlock> firstSide = TraverseWithoutConnection(
                connection.BlockA,
                connection.BlockA,
                connection.BlockB,
                adjacency);
            if (firstSide.Contains(connection.BlockB))
            {
                return 0f;
            }

            HashSet<StructuralBlock> secondSide = TraverseWithoutConnection(
                connection.BlockB,
                connection.BlockA,
                connection.BlockB,
                adjacency);

            bool firstHasAnchor = HasAnchor(firstSide);
            bool secondHasAnchor = HasAnchor(secondSide);
            if (firstHasAnchor == secondHasAnchor)
            {
                return 0f;
            }

            IReadOnlyCollection<StructuralBlock> dependentBlocks = firstHasAnchor
                ? secondSide
                : firstSide;
            float totalMass = 0f;
            float weightedX = 0f;
            foreach (StructuralBlock block in dependentBlocks)
            {
                totalMass += block.Mass;
                weightedX += block.transform.position.x * block.Mass;
            }

            if (totalMass <= 0f)
            {
                return 0f;
            }

            float centerOfMassX = weightedX / totalMass;
            float connectionX = (connection.BlockA.transform.position.x + connection.BlockB.transform.position.x) * 0.5f;
            float leverDistance = Mathf.Abs(centerOfMassX - connectionX);
            return CalculateLoad(totalMass, leverDistance, Physics.gravity.magnitude, leverArmMultiplier);
        }

        public static float CalculateLoad(
            float dependentMass,
            float horizontalLeverDistance,
            float gravityMagnitude,
            float leverMultiplier)
        {
            float leverFactor = 1f + Mathf.Max(0f, horizontalLeverDistance) * Mathf.Max(0f, leverMultiplier);
            return Mathf.Max(0f, dependentMass) * Mathf.Max(0f, gravityMagnitude) * leverFactor;
        }

        private static HashSet<StructuralBlock> TraverseWithoutConnection(
            StructuralBlock start,
            StructuralBlock excludedA,
            StructuralBlock excludedB,
            IReadOnlyDictionary<StructuralBlock, List<StructuralBlock>> adjacency)
        {
            var visited = new HashSet<StructuralBlock> { start };
            var queue = new Queue<StructuralBlock>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                StructuralBlock current = queue.Dequeue();
                foreach (StructuralBlock neighbor in adjacency[current])
                {
                    bool isExcludedEdge = current == excludedA && neighbor == excludedB
                        || current == excludedB && neighbor == excludedA;
                    if (!isExcludedEdge && neighbor.ParticipatesInStructure && visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited;
        }

        private static bool HasAnchor(IEnumerable<StructuralBlock> blocks)
        {
            foreach (StructuralBlock block in blocks)
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
