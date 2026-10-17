using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public readonly struct StructuralStabilityResult
    {
        public StructuralStabilityResult(
            float totalMass,
            Vector3 centerOfMass,
            bool hasSupport,
            float supportMinX,
            float supportMaxX,
            bool isStable)
        {
            TotalMass = totalMass;
            CenterOfMass = centerOfMass;
            HasSupport = hasSupport;
            SupportMinX = supportMinX;
            SupportMaxX = supportMaxX;
            IsStable = isStable;
        }

        public float TotalMass { get; }
        public Vector3 CenterOfMass { get; }
        public bool HasSupport { get; }
        public float SupportMinX { get; }
        public float SupportMaxX { get; }
        public bool IsStable { get; }
    }

    public sealed class StructuralStabilityEvaluator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float supportTolerance = 0.05f;
        [SerializeField] private bool drawStabilityGizmos = true;
        [SerializeField, Min(0.01f)] private float centerOfMassGizmoRadius = 0.16f;

        private readonly List<StructuralStabilityResult> debugResults = new();

        public float SupportTolerance => supportTolerance;

        internal void BeginEvaluation()
        {
            debugResults.Clear();
        }

        internal StructuralStabilityResult Evaluate(IReadOnlyList<StructuralBlock> component)
        {
            var positions = new Vector3[component.Count];
            var masses = new float[component.Count];
            float supportMinX = float.PositiveInfinity;
            float supportMaxX = float.NegativeInfinity;
            bool hasSupport = false;

            for (int index = 0; index < component.Count; index++)
            {
                StructuralBlock block = component[index];
                positions[index] = block.transform.position;
                masses[index] = block.Mass;

                if (block.IsGroundAnchor)
                {
                    Bounds bounds = block.ColliderBounds;
                    supportMinX = Mathf.Min(supportMinX, bounds.min.x);
                    supportMaxX = Mathf.Max(supportMaxX, bounds.max.x);
                    hasSupport = true;
                }
            }

            Vector3 centerOfMass = CalculateCenterOfMass(positions, masses, out float totalMass);
            bool isStable = hasSupport
                && IsWithinSupportInterval(centerOfMass.x, supportMinX, supportMaxX, supportTolerance);
            var result = new StructuralStabilityResult(
                totalMass,
                centerOfMass,
                hasSupport,
                supportMinX,
                supportMaxX,
                isStable);
            debugResults.Add(result);
            return result;
        }

        public static Vector3 CalculateCenterOfMass(
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<float> masses,
            out float totalMass)
        {
            if (positions == null || masses == null || positions.Count != masses.Count || positions.Count == 0)
            {
                throw new ArgumentException("Positions and masses must be non-empty lists of equal length.");
            }

            Vector3 weightedPosition = Vector3.zero;
            totalMass = 0f;

            for (int index = 0; index < positions.Count; index++)
            {
                if (masses[index] <= 0f)
                {
                    throw new ArgumentException("Every structural mass must be greater than zero.");
                }

                weightedPosition += positions[index] * masses[index];
                totalMass += masses[index];
            }

            return weightedPosition / totalMass;
        }

        public static bool IsWithinSupportInterval(
            float centerOfMassX,
            float supportMinX,
            float supportMaxX,
            float tolerance)
        {
            return centerOfMassX >= supportMinX - tolerance
                && centerOfMassX <= supportMaxX + tolerance;
        }

        private void OnDrawGizmos()
        {
            if (!drawStabilityGizmos)
            {
                return;
            }

            foreach (StructuralStabilityResult result in debugResults)
            {
                Gizmos.color = result.IsStable
                    ? Color.green
                    : result.HasSupport ? Color.yellow : Color.red;
                Gizmos.DrawSphere(result.CenterOfMass, centerOfMassGizmoRadius);
                Gizmos.DrawLine(
                    result.CenterOfMass,
                    new Vector3(result.CenterOfMass.x, 0.05f, result.CenterOfMass.z));

                if (result.HasSupport)
                {
                    float z = result.CenterOfMass.z;
                    Gizmos.DrawLine(
                        new Vector3(result.SupportMinX, 0.05f, z),
                        new Vector3(result.SupportMaxX, 0.05f, z));
                }
            }
        }
    }
}
