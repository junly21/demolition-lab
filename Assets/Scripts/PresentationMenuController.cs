using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemolitionLab
{
    public sealed class PresentationMenuController : MonoBehaviour
    {
        [SerializeField] private string startSceneName = "VerticalSlice_Stage01";

        public string StartSceneName => startSceneName;

        public void StartTest()
        {
            if (!Application.CanStreamedLevelBeLoaded(startSceneName))
            {
                Debug.LogError($"Cannot load configured start scene '{startSceneName}'.", this);
                return;
            }

            SceneManager.LoadScene(startSceneName);
        }
    }
}
