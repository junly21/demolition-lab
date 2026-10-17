using UnityEngine;

namespace DemolitionLab
{
    [DisallowMultipleComponent]
    public sealed class StageFeedbackController : MonoBehaviour
    {
        [SerializeField] private SimpleCameraShake cameraShake;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private ParticleSystem muzzleSmoke;
        [SerializeField] private ParticleSystem impactDust;
        [SerializeField] private ParticleSystem impactSparks;
        [SerializeField] private ParticleSystem impactDebris;
        [SerializeField] private ParticleSystem structuralImpactDust;
        [SerializeField] private ParticleSystem collapseDust;
        [SerializeField] private PresentationAudioHooks audioHooks;

        private float lastCollapseFeedbackTime = -10f;
        private int lastObservedDestroyedTargets;
        private int destroyedInCollapseWindow;
        private float collapseWindowStartTime = -10f;
        private Vector3 lastImpactPosition;
        private float lastImpactFeedbackTime = -10f;
        private float lastGroundImpactFeedbackTime = -10f;

        public void Initialize()
        {
            PrepareParticleSystem(muzzleFlash);
            PrepareParticleSystem(muzzleSmoke);
            PrepareParticleSystem(impactDust);
            PrepareParticleSystem(impactSparks);
            PrepareParticleSystem(impactDebris);
            PrepareParticleSystem(structuralImpactDust);
            PrepareParticleSystem(collapseDust);
        }

        public void PlayCriticalBreak(Vector3 position, bool weakConnection)
        {
            bool overlapsImpact = Time.time - lastImpactFeedbackTime < 0.18f
                && Vector3.Distance(position, lastImpactPosition) < 0.8f;
            EmitParticle(impactSparks, position, overlapsImpact ? 6 : 18);
            EmitParticle(impactDust, position, overlapsImpact ? 8 : 12);
            EmitParticle(impactDebris, position, weakConnection ? 6 : 4);

            cameraShake?.Shake(weakConnection ? 0.085f : 0.1f, 0.18f);
            audioHooks?.PlayCriticalBreak(position);
        }

        public void PlayFire(Vector3 position, Quaternion rotation)
        {
            EmitParticle(muzzleFlash, position, rotation, 1);
            EmitParticle(muzzleSmoke, position, rotation, 10);
            cameraShake?.Shake(0.022f, 0.08f);
            audioHooks?.PlayFire(position);
        }

        public void PlayImpact(ProjectileImpact impact)
        {
            float shakeIntensity = Mathf.Lerp(
                0.018f,
                0.055f,
                Mathf.InverseLerp(2f, 18f, impact.ImpulseMagnitude));
            cameraShake?.Shake(shakeIntensity, 0.12f);

            PresentationVisual visual = impact.ImpactedObject.GetComponentInChildren<PresentationVisual>();
            PresentationCategory category = visual != null
                ? visual.Category
                : PresentationCategory.Concrete;
            float audioSeverity = Mathf.Lerp(
                0.72f,
                1f,
                Mathf.InverseLerp(4f, 24f, impact.ImpulseMagnitude));
            if (category is PresentationCategory.Concrete or PresentationCategory.Foundation)
            {
                EmitParticle(impactDust, impact.ContactPosition, 18);
                EmitParticle(impactDebris, impact.ContactPosition, 6);
                audioHooks?.PlayConcreteImpact(impact.ContactPosition, audioSeverity);
            }
            else if (category == PresentationCategory.Weak)
            {
                EmitParticle(impactSparks, impact.ContactPosition, 18);
                EmitParticle(impactDust, impact.ContactPosition, 5);
                EmitParticle(impactDebris, impact.ContactPosition, 4);
                audioHooks?.PlaySteelImpact(impact.ContactPosition, audioSeverity * 1.08f);
            }
            else
            {
                EmitParticle(impactSparks, impact.ContactPosition, 14);
                EmitParticle(impactDebris, impact.ContactPosition, 3);
                audioHooks?.PlaySteelImpact(impact.ContactPosition, audioSeverity);
            }

            lastImpactPosition = impact.ContactPosition;
            lastImpactFeedbackTime = Time.time;
        }

        public void PlayGroundImpact(StructuralGroundImpact impact)
        {
            if (Time.time - lastGroundImpactFeedbackTime < 0.18f)
            {
                return;
            }

            lastGroundImpactFeedbackTime = Time.time;
            bool largeImpact = impact.ImpulseMagnitude >= 14f;
            EmitParticle(structuralImpactDust, impact.Position, largeImpact ? 22 : 12);
            audioHooks?.PlayConcreteImpact(impact.Position, largeImpact ? 1f : 0.65f);
            if (largeImpact)
            {
                cameraShake?.Shake(0.045f, 0.12f);
            }
        }

        public bool RegisterDestruction(int destroyed)
        {
            int newlyDestroyed = Mathf.Max(0, destroyed - lastObservedDestroyedTargets);
            lastObservedDestroyedTargets = destroyed;
            if (newlyDestroyed == 0)
            {
                return false;
            }

            if (Time.time - collapseWindowStartTime > 1.35f)
            {
                collapseWindowStartTime = Time.time;
                destroyedInCollapseWindow = 0;
            }

            destroyedInCollapseWindow += newlyDestroyed;
            if (destroyedInCollapseWindow < 3 || Time.time - lastCollapseFeedbackTime < 1.2f)
            {
                return false;
            }

            return true;
        }

        public void PlayCollapse(Vector3 center)
        {
            center.y = 0.18f;
            destroyedInCollapseWindow = 0;
            lastCollapseFeedbackTime = Time.time;
            EmitParticle(collapseDust, center, 38);
            cameraShake?.Shake(0.12f, 0.22f);
            audioHooks?.PlayLargeCollapse(center);
        }

        public void PlayResult(bool cleared)
        {
            if (cleared)
            {
                audioHooks?.PlayClear();
            }
            else
            {
                audioHooks?.PlayFail();
            }
        }

        private static void PrepareParticleSystem(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void EmitParticle(ParticleSystem particles, Vector3 position, int count)
        {
            particles.transform.position = position;
            particles.Emit(count);
        }

        private static void EmitParticle(
            ParticleSystem particles,
            Vector3 position,
            Quaternion rotation,
            int count)
        {
            particles.transform.SetPositionAndRotation(position, rotation);
            particles.Emit(count);
        }

        public bool ValidateRequiredReferences()
        {
            bool isValid = true;
            isValid &= ValidateReference(cameraShake, nameof(cameraShake));
            isValid &= ValidateReference(muzzleFlash, nameof(muzzleFlash));
            isValid &= ValidateReference(muzzleSmoke, nameof(muzzleSmoke));
            isValid &= ValidateReference(impactDust, nameof(impactDust));
            isValid &= ValidateReference(impactSparks, nameof(impactSparks));
            isValid &= ValidateReference(impactDebris, nameof(impactDebris));
            isValid &= ValidateReference(structuralImpactDust, nameof(structuralImpactDust));
            isValid &= ValidateReference(collapseDust, nameof(collapseDust));
            isValid &= ValidateReference(audioHooks, nameof(audioHooks));
            return isValid;
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{GetType().Name} on {name} is missing '{fieldName}'.", this);
            return false;
        }
    }
}
