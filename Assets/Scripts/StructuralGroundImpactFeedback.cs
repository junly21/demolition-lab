using System;
using UnityEngine;

namespace DemolitionLab
{
    public readonly struct StructuralGroundImpact
    {
        public StructuralGroundImpact(Vector3 position, float impulseMagnitude)
        {
            Position = position;
            ImpulseMagnitude = impulseMagnitude;
        }

        public Vector3 Position { get; }
        public float ImpulseMagnitude { get; }
    }

    [DisallowMultipleComponent]
    public sealed class StructuralGroundImpactFeedback : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float minimumImpulse = 5f;
        [SerializeField, Min(0.05f)] private float cooldown = 0.35f;

        private float lastImpactTime = -10f;

        public static event Action<StructuralGroundImpact> MeaningfulGroundImpact;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            MeaningfulGroundImpact = null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name != "Ground"
                || collision.impulse.magnitude < minimumImpulse
                || Time.time - lastImpactTime < cooldown)
            {
                return;
            }

            lastImpactTime = Time.time;
            Vector3 position = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            MeaningfulGroundImpact?.Invoke(new StructuralGroundImpact(
                position,
                collision.impulse.magnitude));
        }
    }
}
