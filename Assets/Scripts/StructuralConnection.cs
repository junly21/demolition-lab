using UnityEngine;

namespace DemolitionLab
{
    public enum StructuralConnectionType
    {
        Weak,
        Reinforced
    }

    public sealed class StructuralConnection : MonoBehaviour
    {
        [Header("Logical Connection")]
        [SerializeField] private StructuralBlock blockA;
        [SerializeField] private StructuralBlock blockB;
        [SerializeField, Min(0.01f)] private float gravityCapacity = 120f;
        [SerializeField, Min(0.01f)] private float impactCapacity = 28f;
        [SerializeField] private bool isActive = true;

        [Header("Physical Joint")]
        [SerializeField] private StructuralConnectionType connectionType;
        [SerializeField] private ConfigurableJoint physicalJoint;
        [SerializeField, Min(0.01f)] private float physicalBreakForce = 900f;
        [SerializeField, Min(0.01f)] private float physicalBreakTorque = 450f;

        [Header("Debug State")]
        [SerializeField] private float currentGravityLoad;
        [SerializeField] private float currentImpactImpulse;
        [SerializeField] private float gravityUtilization;
        [SerializeField] private float impactUtilization;
        [SerializeField] private float failureScore;
        [SerializeField] private float currentJointForce;
        [SerializeField] private float currentJointTorque;
        [SerializeField] private float peakJointForce;
        [SerializeField] private float peakJointTorque;
        [SerializeField] private bool drawConnectionGizmo = true;

        private StructureManager structureManager;
        private bool physicalJointConfigured;
        private bool jointBreakSignalPending;
        private float reportedJointBreakForce;

        public StructuralBlock BlockA => blockA;
        public StructuralBlock BlockB => blockB;
        public float GravityCapacity => gravityCapacity;
        public float ImpactCapacity => impactCapacity;
        public bool IsActive => isActive;
        public bool IsBroken => !isActive;
        public StructuralConnectionType ConnectionType => connectionType;
        public bool HasPhysicalJoint => physicalJoint != null;
        public float PhysicalBreakForce => physicalBreakForce;
        public float PhysicalBreakTorque => physicalBreakTorque;
        public float CurrentGravityLoad => currentGravityLoad;
        public float CurrentImpactImpulse => currentImpactImpulse;
        public float GravityUtilization => gravityUtilization;
        public float ImpactUtilization => impactUtilization;
        public float FailureScore => failureScore;
        public float CurrentJointForce => currentJointForce;
        public float CurrentJointTorque => currentJointTorque;
        public float PeakJointForce => peakJointForce;
        public float PeakJointTorque => peakJointTorque;

        public bool Connects(StructuralBlock block)
        {
            return blockA == block || blockB == block;
        }

        internal void Initialize(StructureManager manager, bool physicalJointMode)
        {
            structureManager = manager;
            if (!physicalJointMode)
            {
                return;
            }

            physicalJointConfigured = physicalJoint != null;
            if (physicalJoint == null)
            {
                Debug.LogError(
                    $"Physical connection '{name}' is missing its ConfigurableJoint reference.",
                    this);
                return;
            }

            physicalJoint.breakForce = physicalBreakForce;
            physicalJoint.breakTorque = physicalBreakTorque;
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
            RemovePhysicalJoint();
            return true;
        }

        internal void Deactivate()
        {
            isActive = false;
            RemovePhysicalJoint();
        }

        private void OnJointBreak(float breakForce)
        {
            jointBreakSignalPending = true;
            reportedJointBreakForce = breakForce;
            ResolvePhysicalJointBreak();
        }

        private void LateUpdate()
        {
            ResolvePhysicalJointBreak();
        }

        private void FixedUpdate()
        {
            UpdatePhysicalJointDebugState();
            ResolvePhysicalJointBreak();
        }

        private void UpdatePhysicalJointDebugState()
        {
            if (physicalJoint == null)
            {
                currentJointForce = 0f;
                currentJointTorque = 0f;
                return;
            }

            currentJointForce = physicalJoint.currentForce.magnitude;
            currentJointTorque = physicalJoint.currentTorque.magnitude;
            peakJointForce = Mathf.Max(peakJointForce, currentJointForce);
            peakJointTorque = Mathf.Max(peakJointTorque, currentJointTorque);
        }

        private void ResolvePhysicalJointBreak()
        {
            if (!physicalJointConfigured || !isActive || physicalJoint != null)
            {
                return;
            }

            jointBreakSignalPending = false;
            isActive = false;
            structureManager?.HandlePhysicalConnectionBreak(this, reportedJointBreakForce);
        }

        private void RemovePhysicalJoint()
        {
            if (physicalJoint == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(physicalJoint);
            }
            else
            {
                DestroyImmediate(physicalJoint);
            }

            physicalJoint = null;
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
                Gizmos.color = physicalJoint != null
                    ? connectionType == StructuralConnectionType.Weak
                        ? new Color(1f, 0.55f, 0.1f)
                        : Color.cyan
                    : Color.Lerp(Color.green, Color.red, Mathf.Clamp01(failureScore));
            }

            Vector3 start = blockA.transform.position;
            Vector3 end = blockB.transform.position;
            Gizmos.DrawLine(start, end);
            float markerRadius = connectionType == StructuralConnectionType.Reinforced ? 0.11f : 0.08f;
            Gizmos.DrawWireSphere(Vector3.Lerp(start, end, 0.5f), markerRadius);
        }
    }
}
