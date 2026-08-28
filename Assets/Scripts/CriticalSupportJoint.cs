using UnityEngine;

namespace DemolitionLab
{
    public sealed class CriticalSupportJoint : MonoBehaviour
    {
        [SerializeField] private Joint physicalJoint;
        [SerializeField] private VerticalSliceStageController stageController;
        [SerializeField] private bool weakConnection = true;
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Material brokenMaterial;

        public bool IsBroken { get; private set; }
        public bool IsWeak => weakConnection;
        public float BreakForce => physicalJoint != null ? physicalJoint.breakForce : 0f;
        public float BreakTorque => physicalJoint != null ? physicalJoint.breakTorque : 0f;

        private void OnJointBreak(float breakForce)
        {
            if (IsBroken)
            {
                return;
            }

            IsBroken = true;
            if (indicatorRenderer != null && brokenMaterial != null)
            {
                indicatorRenderer.sharedMaterial = brokenMaterial;
            }

            stageController?.NotifyCriticalSupportBroken(transform.position, weakConnection);
            Debug.Log(
                $"Stage critical {(weakConnection ? "Weak" : "Reinforced")} joint broke "
                + $"at reported force {breakForce:0.00} N.",
                this);
        }

        private void OnDrawGizmos()
        {
            if (physicalJoint == null || physicalJoint.connectedBody == null)
            {
                return;
            }

            Gizmos.color = IsBroken
                ? Color.magenta
                : weakConnection ? new Color(1f, 0.65f, 0.05f) : Color.cyan;
            Gizmos.DrawLine(transform.position, physicalJoint.connectedBody.position);
            Gizmos.DrawWireSphere(
                Vector3.Lerp(transform.position, physicalJoint.connectedBody.position, 0.5f),
                weakConnection ? 0.09f : 0.12f);
        }
    }
}
