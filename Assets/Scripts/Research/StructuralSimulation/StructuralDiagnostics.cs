using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    internal static class StructuralDiagnostics
    {
        internal static void LogEvaluationCounts(
            IReadOnlyList<StructuralBlock> blocks,
            IReadOnlyList<StructuralConnection> connections,
            int connectionEvaluations,
            UnityEngine.Object context)
        {
            int activeBlocks = 0;
            foreach (StructuralBlock block in blocks)
            {
                if (block != null && block.ParticipatesInStructure)
                {
                    activeBlocks++;
                }
            }

            int activeConnections = 0;
            foreach (StructuralConnection connection in connections)
            {
                if (connection != null && connection.IsValidForStaticGraph())
                {
                    activeConnections++;
                }
            }

            Debug.Log(
                $"Structural event metrics: activeBlocks={activeBlocks}, "
                + $"activeConnections={activeConnections}, "
                + $"connectionEvaluations={connectionEvaluations}.",
                context);
        }

        internal static void LogPhysicsHandoff(IReadOnlyList<StructuralBlock> component,
            UnityEngine.Object context)
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
                            context);
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
                    context);
            }
        }

        internal static void LogStabilityResult(
            IReadOnlyList<StructuralBlock> component,
            StructuralStabilityResult result,
            bool isConnected,
            bool isStable,
            int releasedCount,
            UnityEngine.Object context)
        {
            string supportInterval = result.HasSupport
                ? $"[{result.SupportMinX:0.00}, {result.SupportMaxX:0.00}]"
                : "none";

            Debug.Log(
                $"Component '{component[0].NodeId}': nodes={component.Count}, "
                + $"mass={result.TotalMass:0.00}, COM.x={result.CenterOfMass.x:0.00}, "
                + $"support={supportInterval}, connected={isConnected}, stable={isStable}, "
                + $"released={releasedCount}.",
                context);
        }

        internal static void LogLocalSupportResult(
            IReadOnlyList<StructuralBlock> component,
            LocalSupportResult result,
            int releasedCount,
            int maxCantileverBlocks, UnityEngine.Object context)
        {
            var unsupportedIds = new List<string>();
            foreach (StructuralBlock block in result.UnsupportedBlocks)
            {
                unsupportedIds.Add(block.NodeId);
            }

            Debug.Log(
                $"Local support '{component[0].NodeId}': supported={result.SupportedBlocks.Count}, "
                + $"unsupported=[{string.Join(", ", unsupportedIds)}], "
                + $"maxCantileverBlocks={maxCantileverBlocks}, "
                + $"released={releasedCount}.",
                context);
        }

        internal static void DrawGraph(List<StructuralBlock> blocks)
        {
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
