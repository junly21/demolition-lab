using UnityEngine;

namespace DemolitionLab
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class StageTargetPiece : MonoBehaviour
    {
        [SerializeField] private float destroyedBelowHeight = 1.25f;
        [SerializeField, Min(0.1f)] private float requiredDrop = 1f;
        [SerializeField, Min(0.1f)] private float requiredDisplacement = 1f;
        [SerializeField, Range(1f, 180f)] private float requiredRotation = 35f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public bool EvaluateDestroyed()
        {
            if (IsDestroyed)
            {
                return true;
            }

            float verticalDrop = initialPosition.y - transform.position.y;
            IsDestroyed = transform.position.y <= destroyedBelowHeight
                || verticalDrop >= requiredDrop
                || Vector3.Distance(initialPosition, transform.position) >= requiredDisplacement
                || Quaternion.Angle(initialRotation, transform.rotation) >= requiredRotation;
            return IsDestroyed;
        }
    }
}
