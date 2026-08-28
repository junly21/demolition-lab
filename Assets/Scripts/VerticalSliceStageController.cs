using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemolitionLab
{
    public sealed class VerticalSliceStageController : MonoBehaviour
    {
        [Header("Stage Rules")]
        [SerializeField, Min(1)] private int startingShots = 3;
        [SerializeField, Range(0.1f, 1f)] private float clearThreshold = 0.7f;
        [SerializeField, Min(0f)] private float collapseSettleDelay = 2.75f;
        [SerializeField] private LauncherController launcher;
        [SerializeField] private List<StageTargetPiece> targetPieces = new();

        [Header("HUD")]
        [SerializeField] private Text shotsText;
        [SerializeField] private Text destructionText;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDetailsText;
        [SerializeField] private Button nextButton;

        [Header("Feedback")]
        [SerializeField] private SimpleCameraShake cameraShake;
        [SerializeField] private ParticleSystem impactDust;

        private int shotsRemaining;
        private StageState state;
        private bool hasValidReferences;

        public int ShotsRemaining => shotsRemaining;
        public float DestructionPercentage => CalculateDestructionPercentage();

        private void Awake()
        {
            hasValidReferences = ValidateRequiredReferences();
            if (!hasValidReferences)
            {
                enabled = false;
                return;
            }

            shotsRemaining = startingShots;
            state = StageState.Playing;
            endPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
            launcher.SetGameplayEnabled(true);
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
            RefreshHud();
            if (state == StageState.Playing && CalculateDestructionPercentage() >= clearThreshold * 100f)
            {
                BeginResolution();
            }
        }

        public void NotifyCriticalSupportBroken(Vector3 position, bool weakConnection)
        {
            cameraShake?.Shake(weakConnection ? 0.18f : 0.24f, 0.3f);
            PlayDust(position);
        }

        public void RetryStage()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ShowNextPlaceholder()
        {
            resultTitleText.text = "STAGE 02 COMING SOON";
            nextButton.interactable = false;
        }

        private void HandleProjectileFired()
        {
            if (state != StageState.Playing)
            {
                return;
            }

            shotsRemaining = Mathf.Max(0, shotsRemaining - 1);
            RefreshHud();
            if (shotsRemaining == 0)
            {
                BeginResolution();
            }
        }

        private void HandleImpact(Vector3 position, float impulseMagnitude)
        {
            if (impulseMagnitude < 1f)
            {
                return;
            }

            float shakeIntensity = Mathf.Lerp(0.025f, 0.12f, Mathf.InverseLerp(2f, 18f, impulseMagnitude));
            cameraShake?.Shake(shakeIntensity, 0.16f);
            PlayDust(position);
        }

        private void BeginResolution()
        {
            if (state != StageState.Playing)
            {
                return;
            }

            state = StageState.Resolving;
            launcher.SetGameplayEnabled(false);
            StartCoroutine(ResolveAfterCollapse());
        }

        private IEnumerator ResolveAfterCollapse()
        {
            yield return new WaitForSeconds(collapseSettleDelay);
            float destruction = CalculateDestructionPercentage();
            bool cleared = destruction >= clearThreshold * 100f;
            state = StageState.Ended;
            resultTitleText.text = cleared ? "STAGE CLEAR" : "STAGE FAILED";
            resultDetailsText.text = $"Shots Used: {startingShots - shotsRemaining}\nDestruction: {destruction:0}%";
            nextButton.gameObject.SetActive(cleared);
            endPanel.SetActive(true);
        }

        private float CalculateDestructionPercentage()
        {
            if (targetPieces.Count == 0)
            {
                return 0f;
            }

            int destroyed = 0;
            foreach (StageTargetPiece target in targetPieces)
            {
                if (target != null && target.EvaluateDestroyed())
                {
                    destroyed++;
                }
            }

            return destroyed * 100f / targetPieces.Count;
        }

        private void RefreshHud()
        {
            shotsText.text = $"Shots: {shotsRemaining}";
            destructionText.text = $"Destruction: {CalculateDestructionPercentage():0}%";
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
            isValid &= ValidateReference(shotsText, nameof(shotsText));
            isValid &= ValidateReference(destructionText, nameof(destructionText));
            isValid &= ValidateReference(endPanel, nameof(endPanel));
            isValid &= ValidateReference(resultTitleText, nameof(resultTitleText));
            isValid &= ValidateReference(resultDetailsText, nameof(resultDetailsText));
            isValid &= ValidateReference(nextButton, nameof(nextButton));

            if (targetPieces.Count == 0)
            {
                Debug.LogError($"{nameof(VerticalSliceStageController)} requires target pieces.", this);
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

        private enum StageState
        {
            Playing,
            Resolving,
            Ended
        }
    }
}
