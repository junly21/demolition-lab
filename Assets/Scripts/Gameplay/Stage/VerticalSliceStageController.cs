using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemolitionLab
{
    [RequireComponent(typeof(StageHudView), typeof(StageFeedbackController))]
    public sealed class VerticalSliceStageController : MonoBehaviour
    {
        [Header("Stage Configuration")]
        [SerializeField] private string stageLabel = "STAGE 01";
        [SerializeField, Min(1)] private int startingShots = 3;
        [SerializeField, Range(0.1f, 1f)] private float clearThreshold = 0.7f;
        [SerializeField, Min(0f)] private float collapseSettleDelay = 2.75f;
        [SerializeField, Min(0f)] private float postClearPresentationDelay;
        [SerializeField] private string nextSceneName;
        [SerializeField] private string completionSceneName;
        [SerializeField, Min(0.1f)] private float projectileSpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float projectileMassMultiplier = 1f;
        [SerializeField] private LauncherController launcher;
        [SerializeField] private List<StageTargetPiece> targetPieces = new();

        [Header("Presentation")]
        [SerializeField] private StageHudView hud;
        [SerializeField] private StageFeedbackController feedback;
        [SerializeField] private bool logImpactDiagnostics = true;

        private readonly StageFlowStateMachine flow = new();
        private bool hasValidReferences;
        private bool clearLockedIn;

        public StageHudView Hud => hud;
        public StageFeedbackController Feedback => feedback;
        public StageState CurrentState => flow.State;
        public int ShotsRemaining => flow.AmmoRemaining;
        public int ShotsUsed => flow.ShotsUsed;
        public int DestroyedTargetCount => CountDestroyedTargets();
        public float DestructionPercentage => CalculateDestructionPercentage();
        public bool HasNextScene => !string.IsNullOrWhiteSpace(nextSceneName)
            && Application.CanStreamedLevelBeLoaded(nextSceneName);
        public bool HasCompletionScene => !string.IsNullOrWhiteSpace(completionSceneName)
            && Application.CanStreamedLevelBeLoaded(completionSceneName);

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
            feedback.Initialize();
            hud.Initialize(stageLabel);
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
            if (logImpactDiagnostics)
            {
                LogInitialTargetState();
            }
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
            feedback.PlayCriticalBreak(position, weakConnection);
            Debug.Log(
                $"{stageLabel} {(weakConnection ? "Weak" : "Reinforced")} critical joint broke; "
                + $"shot={flow.ShotsUsed}, remainingAmmo={flow.AmmoRemaining}, "
                + $"destruction={DestructionPercentage:0}%.",
                this);
        }

        public void RetryStage()
        {
            if (flow.State == StageState.Clear && !HasNextScene && HasCompletionScene)
            {
                SceneManager.LoadScene(completionSceneName);
                return;
            }

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
            feedback.PlayFire(launcher.MuzzlePosition, launcher.MuzzleRotation);
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

            feedback.PlayImpact(impact);
        }

        private void HandleGroundImpact(StructuralGroundImpact impact)
        {
            bool validState = flow.State == StageState.Active || flow.State == StageState.Settling;
            if (validState && flow.ShotsUsed > 0)
            {
                feedback.PlayGroundImpact(impact);
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
            hud.ShowResult(cleared, HasNextScene, flow.ShotsUsed, destruction);
            feedback.PlayResult(cleared);
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
            hud.SetShots(flow.AmmoRemaining);
            hud.SetDestruction(DestructionPercentage);
            hud.SetProgress(DestructionPercentage);
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
            if (!feedback.RegisterDestruction(destroyed))
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
            feedback.PlayCollapse(center);
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
            isValid &= ValidateReference(hud, nameof(hud));
            isValid &= ValidateReference(feedback, nameof(feedback));
            if (hud != null)
            {
                isValid &= hud.ValidateRequiredReferences();
            }
            if (feedback != null)
            {
                isValid &= feedback.ValidateRequiredReferences();
            }

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
