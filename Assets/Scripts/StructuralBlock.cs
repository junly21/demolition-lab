using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
    public enum StructuralBlockState
    {
        Supported,
        Unstable,
        Disconnected,
        Destroyed
    }

    [RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(MeshRenderer))]
    public sealed class StructuralBlock : MonoBehaviour
    {
        [Header("Structural Node")]
        [SerializeField] private string nodeId;
        [SerializeField] private bool isGroundAnchor;
        [SerializeField] private List<StructuralBlock> neighbors = new();

        [Header("Debug Materials")]
        [SerializeField] private Material supportedMaterial;
        [SerializeField] private Material unsupportedMaterial;
        [SerializeField] private Material disconnectedMaterial;
        [SerializeField] private Material destroyedMaterial;

        private StructureManager structureManager;
        private Rigidbody blockRigidbody;
        private Collider blockCollider;
        private Renderer blockRenderer;

        public string NodeId => nodeId;
        public bool IsGroundAnchor => isGroundAnchor;
        public bool IsActive { get; private set; }
        public bool IsSupported => State == StructuralBlockState.Supported;
        public bool IsReleased { get; private set; }
        public StructuralBlockState State { get; private set; }
        public IReadOnlyList<StructuralBlock> Neighbors => neighbors;
        public float Mass => blockRigidbody.mass;
        public Bounds ColliderBounds => blockCollider.bounds;

        internal void Initialize(StructureManager manager)
        {
            structureManager = manager;
            blockRigidbody = GetComponent<Rigidbody>();
            blockCollider = GetComponent<Collider>();
            blockRenderer = GetComponent<Renderer>();
            IsActive = true;
            IsReleased = false;
            State = StructuralBlockState.Supported;
            blockCollider.enabled = true;
            blockRenderer.enabled = true;
            blockRigidbody.isKinematic = true;
            blockRenderer.sharedMaterial = supportedMaterial;
        }

        public void HandleProjectileImpact()
        {
            HandleProjectileImpact(Vector3.zero, transform.position);
        }

        public void HandleProjectileImpact(Vector3 impulse, Vector3 impactPoint)
        {
            if (IsActive && structureManager != null)
            {
                structureManager.DestroyBlock(this, impulse, impactPoint);
            }
        }

        internal void KeepSupported()
        {
            if (!IsActive || IsReleased)
            {
                return;
            }

            State = StructuralBlockState.Supported;
            blockRigidbody.isKinematic = true;
            blockRenderer.sharedMaterial = supportedMaterial;
        }

        internal bool ReleaseToPhysics(StructuralBlockState releaseState)
        {
            if (!IsActive)
            {
                return false;
            }

            if (IsReleased)
            {
                if (releaseState == StructuralBlockState.Disconnected)
                {
                    State = StructuralBlockState.Disconnected;
                    blockRenderer.sharedMaterial = disconnectedMaterial != null
                        ? disconnectedMaterial
                        : unsupportedMaterial;
                }
                return false;
            }

            State = releaseState;
            IsReleased = true;
            blockRigidbody.isKinematic = false;
            blockRenderer.sharedMaterial = releaseState == StructuralBlockState.Disconnected
                && disconnectedMaterial != null
                    ? disconnectedMaterial
                    : unsupportedMaterial;
            return true;
        }

        internal void MarkDestroyed(bool keepAsDebris, Vector3 impulse, Vector3 impactPoint)
        {
            IsActive = false;
            IsReleased = keepAsDebris;
            State = StructuralBlockState.Destroyed;

            if (keepAsDebris)
            {
                blockRigidbody.isKinematic = false;
                blockRenderer.sharedMaterial = destroyedMaterial != null
                    ? destroyedMaterial
                    : unsupportedMaterial;
                blockRigidbody.AddForceAtPosition(impulse, impactPoint, ForceMode.Impulse);
                return;
            }

            blockRigidbody.isKinematic = true;
            blockRigidbody.linearVelocity = Vector3.zero;
            blockRigidbody.angularVelocity = Vector3.zero;
            blockCollider.enabled = false;
            blockRenderer.enabled = false;
        }
    }
}
