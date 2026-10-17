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
        private bool isInitialized;
        private bool evaluationEnabled;

        public bool IsDestroyed { get; private set; }
        public bool IsInitialized => isInitialized;
        public bool EvaluationEnabled => evaluationEnabled;
        public Vector3 InitialPosition => initialPosition;
        public Vector3 CurrentPosition => transform.position;
        public bool IsBelowHeight => transform.position.y <= destroyedBelowHeight;
        public bool HasFallen => initialPosition.y - transform.position.y >= requiredDrop;
        public bool HasMoved => Vector3.Distance(initialPosition, transform.position) >= requiredDisplacement;
        public bool HasRotated => Quaternion.Angle(initialRotation, transform.rotation) >= requiredRotation;

        private void Awake()
        {
            InitializeReferenceState();
        }

        public void InitializeReferenceState()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            IsDestroyed = false;
            evaluationEnabled = false;
            isInitialized = true;
        }

        public void BeginEvaluation()
        {
            if (!isInitialized)
            {
                InitializeReferenceState();
            }

            evaluationEnabled = true;
        }

        public bool EvaluateDestroyed()
        {
            if (!evaluationEnabled)
            {
                return false;
            }

            if (IsDestroyed)
            {
                return true;
            }

            IsDestroyed = IsBelowHeight || HasFallen || HasMoved || HasRotated;
            return IsDestroyed;
        }
    }
}
