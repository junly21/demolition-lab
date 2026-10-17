using UnityEngine;
using UnityEngine.UI;

namespace DemolitionLab
{
    [DisallowMultipleComponent]
    public sealed class StageHudView : MonoBehaviour
    {
        [SerializeField] private Text stageLabelText;
        [SerializeField] private Text shotsText;
        [SerializeField] private Text destructionText;
        [SerializeField] private Image destructionFill;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDetailsText;
        [SerializeField] private Button nextButton;

        public void Initialize(string stageLabel)
        {
            endPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
            stageLabelText.text = stageLabel;
        }

        public void SetShots(int remaining)
        {
            shotsText.text = $"SHOTS  {remaining}";
        }

        public void SetDestruction(float percentage)
        {
            destructionText.text = $"DESTRUCTION  {percentage:0}%";
        }

        public void SetProgress(float percentage)
        {
            destructionFill.fillAmount = percentage / 100f;
        }

        public void ShowResult(bool cleared, bool hasNextScene, int shotsUsed, float destruction)
        {
            resultTitleText.text = cleared
                ? hasNextScene ? "STAGE CLEAR" : "TEST PROGRAM COMPLETE"
                : "STAGE FAILED";
            resultDetailsText.text = $"Shots Used: {shotsUsed}\nDestruction: {destruction:0}%";
            nextButton.gameObject.SetActive(cleared && hasNextScene);
            endPanel.SetActive(true);
        }

        public bool ValidateRequiredReferences()
        {
            bool isValid = true;
            isValid &= ValidateReference(stageLabelText, nameof(stageLabelText));
            isValid &= ValidateReference(shotsText, nameof(shotsText));
            isValid &= ValidateReference(destructionText, nameof(destructionText));
            isValid &= ValidateReference(destructionFill, nameof(destructionFill));
            isValid &= ValidateReference(endPanel, nameof(endPanel));
            isValid &= ValidateReference(resultTitleText, nameof(resultTitleText));
            isValid &= ValidateReference(resultDetailsText, nameof(resultDetailsText));
            isValid &= ValidateReference(nextButton, nameof(nextButton));
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
