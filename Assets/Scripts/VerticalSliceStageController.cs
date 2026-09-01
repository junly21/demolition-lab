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
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDetailsText;
        [SerializeField] private Button nextButton;

        [Header("Feedback")]
        [SerializeField] private SimpleCameraShake cameraShake;
        [SerializeField] private ParticleSystem impactDust;
        [SerializeField] private ParticleSystem impactSparks;
        [SerializeField] private ParticleSystem collapseDust;
        [SerializeField] private PresentationAudioHooks audioHooks;
        [SerializeField] private bool logImpactDiagnostics = true;

        private readonly StageFlowStateMachine flow = new();
        private bool hasValidReferences;
        private bool clearLockedIn;
        private int destroyedAtLastCollapseFeedback;
        private float lastCollapseFeedbackTime = -10f;

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
            PrepareParticleSystem(impactDust);
            PrepareParticleSystem(impactSparks);
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
            launcher.ProjectileFired += HandleProjectileFired;
            Projectile.MeaningfulImpact += HandleImpact;
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
            PlayParticle(impactSparks, position);
            if (weakConnection)
            {
                PlayParticle(impactDust, position);
            }

            cameraShake?.Shake(weakConnection ? 0.085f : 0.11f, 0.2f);
            audioHooks?.PlayCriticalBreak();
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
            audioHooks?.PlayFire();
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
            bool concreteImpact = visual == null
                || visual.Category is PresentationCategory.Concrete or PresentationCategory.Foundation;
            if (concreteImpact)
            {
                PlayParticle(impactDust, impact.ContactPosition);
                audioHooks?.PlayConcreteImpact();
            }
            else
            {
                PlayParticle(impactSparks, impact.ContactPosition);
                audioHooks?.PlaySteelImpact();
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
            resultTitleText.text = cleared ? "STAGE CLEAR" : "STAGE FAILED";
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
            shotsText.text = $"Shots: {flow.AmmoRemaining}";
            destructionText.text = $"Destruction: {DestructionPercentage:0}%";
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
            if (flow.ShotsUsed == 0 || Time.time - lastCollapseFeedbackTime < 0.8f)
            {
                return;
            }

            int destroyed = CountDestroyedTargets();
            if (destroyed - destroyedAtLastCollapseFeedback < 3)
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
            destroyedAtLastCollapseFeedback = destroyed;
            lastCollapseFeedbackTime = Time.time;
            PlayParticle(collapseDust, center);
            cameraShake?.Shake(0.13f, 0.24f);
            audioHooks?.PlayLargeCollapse();
        }

        private static void PrepareParticleSystem(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void PlayParticle(ParticleSystem particles, Vector3 position)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.transform.position = position;
            particles.Play(true);
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
            isValid &= ValidateReference(endPanel, nameof(endPanel));
            isValid &= ValidateReference(resultTitleText, nameof(resultTitleText));
            isValid &= ValidateReference(resultDetailsText, nameof(resultDetailsText));
            isValid &= ValidateReference(nextButton, nameof(nextButton));
            isValid &= ValidateReference(cameraShake, nameof(cameraShake));
            isValid &= ValidateReference(impactDust, nameof(impactDust));
            isValid &= ValidateReference(impactSparks, nameof(impactSparks));
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
