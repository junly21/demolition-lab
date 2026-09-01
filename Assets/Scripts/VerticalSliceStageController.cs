using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemolitionLab
{
    public sealed class VerticalSliceStageController : MonoBehaviour
    {
        [Header("Stage Configuration")]
        [SerializeField] private string stageLabel = "STAGE 01";
        [SerializeField, Min(1)] private int startingShots = 3;
        [SerializeField, Range(0.1f, 1f)] private float clearThreshold = 0.7f;
        [SerializeField, Min(0f)] private float collapseSettleDelay = 2.75f;
        [SerializeField, Min(0f)] private float postClearPresentationDelay;
        [SerializeField] private string nextSceneName;
        [SerializeField, Min(0.1f)] private float projectileSpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float projectileMassMultiplier = 1f;
        [SerializeField] private LauncherController launcher;
        [SerializeField] private List<StageTargetPiece> targetPieces = new();

        [Header("HUD")]
        [SerializeField] private Text stageLabelText;
        [SerializeField] private Text shotsText;
        [SerializeField] private Text destructionText;
        [SerializeField] private Image destructionFill;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDetailsText;
        [SerializeField] private Button nextButton;

        [Header("Feedback")]
        [SerializeField] private SimpleCameraShake cameraShake;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private ParticleSystem muzzleSmoke;
        [SerializeField] private ParticleSystem impactDust;
        [SerializeField] private ParticleSystem impactSparks;
        [SerializeField] private ParticleSystem impactDebris;
        [SerializeField] private ParticleSystem structuralImpactDust;
        [SerializeField] private ParticleSystem collapseDust;
        [SerializeField] private PresentationAudioHooks audioHooks;
        [SerializeField] private bool logImpactDiagnostics = true;

        private readonly StageFlowStateMachine flow = new();
        private bool hasValidReferences;
        private bool clearLockedIn;
        private float lastCollapseFeedbackTime = -10f;
        private int lastObservedDestroyedTargets;
        private int destroyedInCollapseWindow;
        private float collapseWindowStartTime = -10f;
        private Vector3 lastImpactPosition;
        private float lastImpactFeedbackTime = -10f;
        private float lastGroundImpactFeedbackTime = -10f;

        public StageState CurrentState => flow.State;
        public int ShotsRemaining => flow.AmmoRemaining;
        public int ShotsUsed => flow.ShotsUsed;
        public int DestroyedTargetCount => CountDestroyedTargets();
        public float DestructionPercentage => CalculateDestructionPercentage();
        public bool HasNextScene => !string.IsNullOrWhiteSpace(nextSceneName)
            && Application.CanStreamedLevelBeLoaded(nextSceneName);

        private void Awake()
        {
            flow.Initialize(startingShots);
            hasValidReferences = ValidateRequiredReferences();
            if (!hasValidReferences)
            {
                enabled = false;
                return;
            }

            launcher.ConfigureProjectileMultipliers(
                projectileSpeedMultiplier,
                projectileMassMultiplier);
            launcher.SetGameplayEnabled(false);
            PrepareParticleSystem(muzzleFlash);
            PrepareParticleSystem(muzzleSmoke);
            PrepareParticleSystem(impactDust);
            PrepareParticleSystem(impactSparks);
            PrepareParticleSystem(impactDebris);
            PrepareParticleSystem(structuralImpactDust);
            PrepareParticleSystem(collapseDust);
            endPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
            stageLabelText.text = stageLabel;
            RefreshHud();
        }

        private void OnEnable()
        {
            if (!hasValidReferences)
            {
                return;
            }

            launcher.ProjectileFired -= HandleProjectileFired;
            Projectile.MeaningfulImpact -= HandleImpact;
            StructuralGroundImpactFeedback.MeaningfulGroundImpact -= HandleGroundImpact;
            launcher.ProjectileFired += HandleProjectileFired;
            Projectile.MeaningfulImpact += HandleImpact;
            StructuralGroundImpactFeedback.MeaningfulGroundImpact += HandleGroundImpact;
        }

        private void Start()
        {
            if (!hasValidReferences)
            {
                return;
            }

            InitializeTargets();
            flow.MarkReady();
            LogInitialTargetState();
            RefreshHud();
            StartCoroutine(ActivateGameplayNextFrame());
        }

        private void OnDisable()
        {
            if (!hasValidReferences)
            {
                return;
            }

            launcher.ProjectileFired -= HandleProjectileFired;
            Projectile.MeaningfulImpact -= HandleImpact;
            StructuralGroundImpactFeedback.MeaningfulGroundImpact -= HandleGroundImpact;
        }

        private void Update()
        {
            bool canEvaluate = flow.State == StageState.Active || flow.State == StageState.Settling;
            if (!canEvaluate)
            {
                return;
            }

            RefreshHud();
            TryPlayCollapseFeedback();
            if (!clearLockedIn
                && flow.ShotsUsed > 0
                && DestructionPercentage >= clearThreshold * 100f)
            {
                LockInClear();
                if (flow.State == StageState.Active)
                {
                    BeginSettling();
                }
            }
        }

        public void NotifyCriticalSupportBroken(Vector3 position, bool weakConnection)
        {
            bool overlapsImpact = Time.time - lastImpactFeedbackTime < 0.18f
                && Vector3.Distance(position, lastImpactPosition) < 0.8f;
            EmitParticle(impactSparks, position, overlapsImpact ? 6 : 18);
            EmitParticle(impactDust, position, overlapsImpact ? 8 : 12);
            EmitParticle(impactDebris, position, weakConnection ? 6 : 4);

            cameraShake?.Shake(weakConnection ? 0.085f : 0.1f, 0.18f);
            audioHooks?.PlayCriticalBreak(position);
            Debug.Log(
                $"{stageLabel} {(weakConnection ? "Weak" : "Reinforced")} critical joint broke; "
                + $"shot={flow.ShotsUsed}, remainingAmmo={flow.AmmoRemaining}, "
                + $"destruction={DestructionPercentage:0}%.",
                this);
        }

        public void RetryStage()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        public void LoadNextStage()
        {
            if (flow.State != StageState.Clear || !HasNextScene)
            {
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator ActivateGameplayNextFrame()
        {
            yield return null;
            if (!flow.Activate())
            {
                yield break;
            }

            foreach (StageTargetPiece target in targetPieces)
            {
                target.BeginEvaluation();
            }

            launcher.SetGameplayEnabled(true);
            Debug.Log($"{stageLabel} active with {flow.AmmoRemaining} shots and 0% destruction.", this);
        }

        private void InitializeTargets()
        {
            foreach (StageTargetPiece target in targetPieces)
            {
                target.InitializeReferenceState();
            }
        }

        private void HandleProjectileFired()
        {
            if (!flow.RegisterShot())
            {
                return;
            }

            Debug.Log($"{stageLabel} shot {flow.ShotsUsed}/{flow.StartingAmmo} fired.", this);
            EmitParticle(muzzleFlash, launcher.MuzzlePosition, launcher.MuzzleRotation, 1);
            EmitParticle(muzzleSmoke, launcher.MuzzlePosition, launcher.MuzzleRotation, 10);
            cameraShake?.Shake(0.022f, 0.08f);
            audioHooks?.PlayFire(launcher.MuzzlePosition);
            RefreshHud();
            if (flow.AmmoRemaining == 0)
            {
                BeginSettling();
            }
        }

        private void HandleImpact(ProjectileImpact impact)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool validState = flow.State == StageState.Active || flow.State == StageState.Settling;
            bool accepted = impact.WasPlayerFired
                && impact.LaunchSceneName == activeScene.name
                && validState
                && impact.ImpulseMagnitude >= 1f;

            if (logImpactDiagnostics)
            {
                Debug.Log(
                    $"Impact feedback {(accepted ? "ACCEPTED" : "REJECTED")}: "
                    + $"scene={activeScene.name}, launchScene={impact.LaunchSceneName}, "
                    + $"frame={impact.Frame}, time={impact.Time:0.000}, "
                    + $"projectileId={impact.ProjectileInstanceId}, projectilePosition={impact.ProjectilePosition}, "
                    + $"collider={impact.ImpactedObject.name}, contact={impact.ContactPosition}, "
                    + $"particle={(accepted ? impact.ContactPosition.ToString() : "none")}, "
                    + $"shakeSource={(accepted ? impact.ContactPosition.ToString() : "none")}, "
                    + $"state={flow.State}, playerFired={impact.WasPlayerFired}, "
                    + $"shot={flow.ShotsUsed}, remainingAmmo={flow.AmmoRemaining}, "
                    + $"destruction={DestructionPercentage:0}%, "
                    + $"impulse={impact.ImpulseMagnitude:0.00} N·s.",
                    this);
            }

            if (!accepted)
            {
                return;
            }

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

        private void HandleGroundImpact(StructuralGroundImpact impact)
        {
            bool validState = flow.State == StageState.Active || flow.State == StageState.Settling;
            if (!validState
                || flow.ShotsUsed == 0
                || Time.time - lastGroundImpactFeedbackTime < 0.18f)
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

        private void BeginSettling()
        {
            if (!flow.BeginSettling())
            {
                return;
            }

            launcher.SetGameplayEnabled(false);
            StartCoroutine(ResolveAfterCollapse());
        }

        private IEnumerator ResolveAfterCollapse()
        {
            yield return new WaitForSeconds(collapseSettleDelay);
            float destruction = DestructionPercentage;
            bool cleared = clearLockedIn || destruction >= clearThreshold * 100f;
            if (cleared && !clearLockedIn)
            {
                LockInClear();
            }

            if (cleared && postClearPresentationDelay > 0f)
            {
                yield return new WaitForSeconds(postClearPresentationDelay);
                destruction = DestructionPercentage;
            }

            flow.Complete(cleared);
            resultTitleText.text = cleared
                ? HasNextScene ? "STAGE CLEAR" : "TEST PROGRAM COMPLETE"
                : "STAGE FAILED";
            resultDetailsText.text = $"Shots Used: {flow.ShotsUsed}\nDestruction: {destruction:0}%";
            nextButton.gameObject.SetActive(cleared && HasNextScene);
            endPanel.SetActive(true);
            if (cleared)
            {
                audioHooks?.PlayClear();
            }
            else
            {
                audioHooks?.PlayFail();
            }
            Debug.Log(
                $"{stageLabel} {(cleared ? "CLEAR" : "FAILED")}: "
                + $"destruction {DestroyedTargetCount}/{targetPieces.Count} ({destruction:0}%), "
                + $"shots used {flow.ShotsUsed}.",
                this);
        }

        private void LockInClear()
        {
            clearLockedIn = true;
            Debug.Log(
                $"{stageLabel} clear locked at {DestructionPercentage:0}% destruction; "
                + $"result presentation delay {postClearPresentationDelay:0.00}s.",
                this);
        }

        private int CountDestroyedTargets()
        {
            int destroyed = 0;
            foreach (StageTargetPiece target in targetPieces)
            {
                if (target != null && target.EvaluateDestroyed())
                {
                    destroyed++;
                }
            }

            return destroyed;
        }

        private float CalculateDestructionPercentage()
        {
            return targetPieces.Count == 0
                ? 0f
                : CountDestroyedTargets() * 100f / targetPieces.Count;
        }

        private void RefreshHud()
        {
            shotsText.text = $"SHOTS  {flow.AmmoRemaining}";
            destructionText.text = $"DESTRUCTION  {DestructionPercentage:0}%";
            destructionFill.fillAmount = DestructionPercentage / 100f;
        }

        private void LogInitialTargetState()
        {
            var message = new StringBuilder();
            message.AppendLine($"{stageLabel} initialized targets:");
            foreach (StageTargetPiece target in targetPieces)
            {
                message.AppendLine(
                    $"- {target.name}: initial={target.InitialPosition}, current={target.CurrentPosition}, "
                    + $"fallen={target.HasFallen}, destroyed={target.IsDestroyed}, "
                    + $"evaluationEnabled={target.EvaluationEnabled}, counts=false");
            }

            message.Append(
                $"Destruction: {CountDestroyedTargets()}/{targetPieces.Count} "
                + $"({CalculateDestructionPercentage():0}%)");
            Debug.Log(message.ToString(), this);
        }

        private void TryPlayCollapseFeedback()
        {
            if (flow.ShotsUsed == 0)
            {
                return;
            }

            int destroyed = CountDestroyedTargets();
            int newlyDestroyed = Mathf.Max(0, destroyed - lastObservedDestroyedTargets);
            lastObservedDestroyedTargets = destroyed;
            if (newlyDestroyed == 0)
            {
                return;
            }

            if (Time.time - collapseWindowStartTime > 1.35f)
            {
                collapseWindowStartTime = Time.time;
                destroyedInCollapseWindow = 0;
            }

            destroyedInCollapseWindow += newlyDestroyed;
            if (destroyedInCollapseWindow < 3 || Time.time - lastCollapseFeedbackTime < 1.2f)
            {
                return;
            }

            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (StageTargetPiece target in targetPieces)
            {
                if (target != null && target.IsDestroyed)
                {
                    center += target.CurrentPosition;
                    count++;
                }
            }

            if (count == 0)
            {
                return;
            }

            center /= count;
            center.y = 0.18f;
            destroyedInCollapseWindow = 0;
            lastCollapseFeedbackTime = Time.time;
            EmitParticle(collapseDust, center, 38);
            cameraShake?.Shake(0.12f, 0.22f);
            audioHooks?.PlayLargeCollapse(center);
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

        [ContextMenu("Validate Required References")]
        private void ValidateReferencesFromContextMenu()
        {
            ValidateRequiredReferences();
        }

        private bool ValidateRequiredReferences()
        {
            bool isValid = true;
            isValid &= ValidateReference(launcher, nameof(launcher));
            isValid &= ValidateReference(stageLabelText, nameof(stageLabelText));
            isValid &= ValidateReference(shotsText, nameof(shotsText));
            isValid &= ValidateReference(destructionText, nameof(destructionText));
            isValid &= ValidateReference(destructionFill, nameof(destructionFill));
            isValid &= ValidateReference(endPanel, nameof(endPanel));
            isValid &= ValidateReference(resultTitleText, nameof(resultTitleText));
            isValid &= ValidateReference(resultDetailsText, nameof(resultDetailsText));
            isValid &= ValidateReference(nextButton, nameof(nextButton));
            isValid &= ValidateReference(cameraShake, nameof(cameraShake));
            isValid &= ValidateReference(muzzleFlash, nameof(muzzleFlash));
            isValid &= ValidateReference(muzzleSmoke, nameof(muzzleSmoke));
            isValid &= ValidateReference(impactDust, nameof(impactDust));
            isValid &= ValidateReference(impactSparks, nameof(impactSparks));
            isValid &= ValidateReference(impactDebris, nameof(impactDebris));
            isValid &= ValidateReference(structuralImpactDust, nameof(structuralImpactDust));
            isValid &= ValidateReference(collapseDust, nameof(collapseDust));
            isValid &= ValidateReference(audioHooks, nameof(audioHooks));

            if (targetPieces.Count == 0 || targetPieces.Exists(target => target == null))
            {
                Debug.LogError(
                    $"{nameof(VerticalSliceStageController)} requires non-null target pieces.",
                    this);
                isValid = false;
            }

            return isValid;
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(VerticalSliceStageController)} on {name} is missing '{fieldName}'.",
                this);
            return false;
        }
    }
}
