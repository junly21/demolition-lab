using UnityEngine;

namespace DemolitionLab
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        private Rigidbody projectileRigidbody;
        private bool hasReportedImpact;

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
            if (collision.gameObject.TryGetComponent(out StructuralBlock structuralBlock))
            {
                Vector3 impactImpulse = -collision.impulse;
                Vector3 impactPoint = collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : structuralBlock.transform.position;
                structuralBlock.HandleProjectileImpact(impactImpulse, impactPoint);
            }

            Debug.Log(
                $"Projectile impacted {collision.gameObject.name} at "
                + $"{collision.relativeVelocity.magnitude:0.0} m/s with "
                + $"{collision.impulse.magnitude:0.00} N·s collision impulse.",
                this);
        }
    }
}
