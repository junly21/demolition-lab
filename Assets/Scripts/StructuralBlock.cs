using System.Collections.Generic;
using UnityEngine;

namespace DemolitionLab
{
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

        private StructureManager structureManager;
        private Rigidbody blockRigidbody;
        private Collider blockCollider;
        private Renderer blockRenderer;

        public string NodeId => nodeId;
        public bool IsGroundAnchor => isGroundAnchor;
        public bool IsActive { get; private set; }
        public bool IsSupported { get; private set; }
        public IReadOnlyList<StructuralBlock> Neighbors => neighbors;

        internal void Initialize(StructureManager manager)
        {
            structureManager = manager;
            blockRigidbody = GetComponent<Rigidbody>();
            blockCollider = GetComponent<Collider>();
            blockRenderer = GetComponent<Renderer>();
            IsActive = true;
            SetSupported(true);
        }

        public void HandleProjectileImpact()
        {
            if (IsActive)
            {
                structureManager.DestroyBlock(this);
            }
        }

        internal void SetSupported(bool isSupported)
        {
            if (!IsActive)
            {
                return;
            }

            IsSupported = isSupported;
            blockRigidbody.isKinematic = isSupported;
            blockRenderer.sharedMaterial = isSupported ? supportedMaterial : unsupportedMaterial;
        }

        internal void MarkDestroyed()
        {
            IsActive = false;
            IsSupported = false;
            blockRigidbody.isKinematic = true;
            blockRigidbody.linearVelocity = Vector3.zero;
            blockRigidbody.angularVelocity = Vector3.zero;
            blockCollider.enabled = false;
            blockRenderer.enabled = false;
        }
    }
}
