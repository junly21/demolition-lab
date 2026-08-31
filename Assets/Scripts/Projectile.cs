using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemolitionLab
{
    public readonly struct ProjectileImpact
    {
        public ProjectileImpact(
            int projectileInstanceId,
            Vector3 projectilePosition,
            GameObject impactedObject,
            Vector3 contactPosition,
            float impulseMagnitude,
            bool wasPlayerFired,
            string launchSceneName,
            int frame,
            float time)
        {
            ProjectileInstanceId = projectileInstanceId;
            ProjectilePosition = projectilePosition;
            ImpactedObject = impactedObject;
            ContactPosition = contactPosition;
            ImpulseMagnitude = impulseMagnitude;
            WasPlayerFired = wasPlayerFired;
            LaunchSceneName = launchSceneName;
            Frame = frame;
            Time = time;
        }

        public int ProjectileInstanceId { get; }
        public Vector3 ProjectilePosition { get; }
        public GameObject ImpactedObject { get; }
        public Vector3 ContactPosition { get; }
        public float ImpulseMagnitude { get; }
        public bool WasPlayerFired { get; }
        public string LaunchSceneName { get; }
        public int Frame { get; }
        public float Time { get; }
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float postImpactLifetime = 1.25f;

        private Rigidbody projectileRigidbody;
        private static int nextProjectileId;
        private int projectileInstanceId;
        private bool hasReportedImpact;
        private bool wasPlayerFired;
        private string launchSceneName;

        public static event Action<ProjectileImpact> MeaningfulImpact;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            MeaningfulImpact = null;
            nextProjectileId = 0;
        }

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
            projectileInstanceId = ++nextProjectileId;
        }

        public void Launch(Vector3 initialVelocity, float mass, float lifetime, bool playerFired)
        {
            wasPlayerFired = playerFired;
            launchSceneName = SceneManager.GetActiveScene().name;
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

            MeaningfulImpact?.Invoke(new ProjectileImpact(
                projectileInstanceId,
                transform.position,
                collision.gameObject,
                impactPoint,
                collision.impulse.magnitude,
                wasPlayerFired,
                launchSceneName,
                Time.frameCount,
                Time.time));

            Debug.Log(
                $"Projectile impacted {collision.gameObject.name} at "
                + $"{collision.relativeVelocity.magnitude:0.0} m/s with "
                + $"{collision.impulse.magnitude:0.00} N·s collision impulse.",
                this);
        }
    }
}
