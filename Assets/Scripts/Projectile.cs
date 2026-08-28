using System;
using UnityEngine;

namespace DemolitionLab
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float postImpactLifetime = 1.25f;

        private Rigidbody projectileRigidbody;
        private bool hasReportedImpact;

        public static event Action<GameObject, Vector3, float> MeaningfulImpact;

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 initialVelocity, float mass, float lifetime)
        {
            projectileRigidbody.mass = mass;
            projectileRigidbody.linearVelocity = initialVelocity;
            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasReportedImpact || !collision.gameObject.CompareTag("Destructible"))
            {
                return;
            }

            hasReportedImpact = true;
            Destroy(gameObject, postImpactLifetime);
            Vector3 impactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : collision.transform.position;
            if (collision.gameObject.TryGetComponent(out StructuralBlock structuralBlock))
            {
                Vector3 impactImpulse = -collision.impulse;
                structuralBlock.HandleProjectileImpact(impactImpulse, impactPoint);
            }

            MeaningfulImpact?.Invoke(collision.gameObject, impactPoint, collision.impulse.magnitude);

            Debug.Log(
                $"Projectile impacted {collision.gameObject.name} at "
                + $"{collision.relativeVelocity.magnitude:0.0} m/s with "
                + $"{collision.impulse.magnitude:0.00} N·s collision impulse.",
                this);
        }
    }
}
