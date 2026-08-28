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
        [SerializeField] private string nextSceneName;
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

        private readonly StageFlowStateMachine flow = new();
        private bool hasValidReferences;

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

            launcher.SetGameplayEnabled(false);
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
            if (flow.State != StageState.Active)
            {
                return;
            }

            RefreshHud();
            if (flow.ShotsUsed > 0 && DestructionPercentage >= clearThreshold * 100f)
            {
                BeginSettling();
            }
        }

        public void NotifyCriticalSupportBroken(Vector3 position, bool weakConnection)
        {
            cameraShake?.Shake(weakConnection ? 0.18f : 0.24f, 0.3f);
            PlayDust(position);
            Debug.Log(
                $"{stageLabel} {(weakConnection ? "Weak" : "Reinforced")} critical joint broke.",
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
            RefreshHud();
            if (flow.AmmoRemaining == 0)
            {
                BeginSettling();
            }
        }

        private void HandleImpact(GameObject impactedObject, Vector3 position, float impulseMagnitude)
        {
            if (impulseMagnitude < 1f)
            {
                return;
            }

            float shakeIntensity = Mathf.Lerp(0.025f, 0.12f, Mathf.InverseLerp(2f, 18f, impulseMagnitude));
            cameraShake?.Shake(shakeIntensity, 0.16f);
            PlayDust(position);
            Debug.Log(
                $"{stageLabel} shot {flow.ShotsUsed} impacted {impactedObject.name} "
                + $"with {impulseMagnitude:0.00} N·s impulse.",
                this);
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
            bool cleared = destruction >= clearThreshold * 100f;
            flow.Complete(cleared);
            resultTitleText.text = cleared ? "STAGE CLEAR" : "STAGE FAILED";
            resultDetailsText.text = $"Shots Used: {flow.ShotsUsed}\nDestruction: {destruction:0}%";
            nextButton.gameObject.SetActive(cleared && HasNextScene);
            endPanel.SetActive(true);
            Debug.Log(
                $"{stageLabel} {(cleared ? "CLEAR" : "FAILED")}: "
                + $"destruction {DestroyedTargetCount}/{targetPieces.Count} ({destruction:0}%), "
                + $"shots used {flow.ShotsUsed}.",
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

        private void PlayDust(Vector3 position)
        {
            if (impactDust == null)
            {
                return;
            }

            impactDust.transform.position = position;
            impactDust.Play(true);
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
