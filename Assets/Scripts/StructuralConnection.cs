using UnityEngine;

namespace DemolitionLab
{
    public sealed class StructuralConnection : MonoBehaviour
    {
        [SerializeField] private StructuralBlock blockA;
        [SerializeField] private StructuralBlock blockB;
        [SerializeField, Min(0.01f)] private float gravityCapacity = 120f;
        [SerializeField, Min(0.01f)] private float impactCapacity = 28f;
        [SerializeField] private bool isActive = true;
        [SerializeField] private float currentGravityLoad;
        [SerializeField] private float currentImpactImpulse;
        [SerializeField] private float gravityUtilization;
        [SerializeField] private float impactUtilization;
        [SerializeField] private float failureScore;
        [SerializeField] private bool drawConnectionGizmo = true;

        public StructuralBlock BlockA => blockA;
        public StructuralBlock BlockB => blockB;
        public float GravityCapacity => gravityCapacity;
        public float ImpactCapacity => impactCapacity;
        public bool IsActive => isActive;
        public bool IsBroken => !isActive;
        public float CurrentGravityLoad => currentGravityLoad;
        public float CurrentImpactImpulse => currentImpactImpulse;
        public float GravityUtilization => gravityUtilization;
        public float ImpactUtilization => impactUtilization;
        public float FailureScore => failureScore;

        public bool Connects(StructuralBlock block)
        {
            return blockA == block || blockB == block;
        }

        internal bool IsValidForStaticGraph()
        {
            return isActive
                && blockA != null
                && blockB != null
                && blockA != blockB
                && blockA.ParticipatesInStructure
                && blockB.ParticipatesInStructure;
        }

        internal void SetLoadEvaluation(float gravityLoad, float impactImpulse)
        {
            currentGravityLoad = Mathf.Max(0f, gravityLoad);
            currentImpactImpulse = Mathf.Max(0f, impactImpulse);
            gravityUtilization = CalculateGravityUtilization(currentGravityLoad);
            impactUtilization = CalculateImpactUtilization(currentImpactImpulse);
            failureScore = gravityUtilization + impactUtilization;
        }

        public float CalculateFailureScore(float gravityLoad, float impactImpulse)
        {
            return CalculateGravityUtilization(gravityLoad) + CalculateImpactUtilization(impactImpulse);
        }

        internal bool Break()
        {
            if (!isActive)
            {
                return false;
            }

            isActive = false;
            return true;
        }

        internal void Deactivate()
        {
            isActive = false;
        }

        private float CalculateGravityUtilization(float gravityLoad)
        {
            return Mathf.Max(0f, gravityLoad) / Mathf.Max(0.01f, gravityCapacity);
        }

        private float CalculateImpactUtilization(float impactImpulse)
        {
            return Mathf.Max(0f, impactImpulse) / Mathf.Max(0.01f, impactCapacity);
        }

        private void OnDrawGizmos()
        {
            if (!drawConnectionGizmo || blockA == null || blockB == null)
            {
                return;
            }

            if (!isActive)
            {
                Gizmos.color = Color.magenta;
            }
            else if (!blockA.ParticipatesInStructure || !blockB.ParticipatesInStructure)
            {
                Gizmos.color = Color.gray;
            }
            else
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(failureScore));
            }

            Vector3 start = blockA.transform.position;
            Vector3 end = blockB.transform.position;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(Vector3.Lerp(start, end, 0.5f), 0.08f);
        }
    }
}
