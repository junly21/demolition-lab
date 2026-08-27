using UnityEngine;

namespace DemolitionLab
{
    public sealed class StructuralConnection : MonoBehaviour
    {
        [SerializeField] private StructuralBlock blockA;
        [SerializeField] private StructuralBlock blockB;
        [SerializeField, Min(0.01f)] private float connectionStrength = 75f;
        [SerializeField] private bool isActive = true;
        [SerializeField] private float currentEstimatedLoad;
        [SerializeField] private bool drawConnectionGizmo = true;

        public StructuralBlock BlockA => blockA;
        public StructuralBlock BlockB => blockB;
        public float ConnectionStrength => connectionStrength;
        public bool IsActive => isActive;
        public float CurrentEstimatedLoad => currentEstimatedLoad;

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

        internal void SetEstimatedLoad(float load)
        {
            currentEstimatedLoad = Mathf.Max(0f, load);
        }

        internal bool Break(float load)
        {
            SetEstimatedLoad(load);
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
                float normalizedLoad = connectionStrength > 0f
                    ? Mathf.Clamp01(currentEstimatedLoad / connectionStrength)
                    : 1f;
                Gizmos.color = Color.Lerp(Color.green, Color.red, normalizedLoad);
            }

            Vector3 start = blockA.transform.position;
            Vector3 end = blockB.transform.position;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(Vector3.Lerp(start, end, 0.5f), 0.08f);
        }
    }
}
