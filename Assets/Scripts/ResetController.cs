using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DemolitionLab
{
    public sealed class ResetController : MonoBehaviour
    {
        [SerializeField] private bool showControls = true;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnGUI()
        {
            if (!showControls)
            {
                return;
            }

            GUI.Box(new Rect(16f, 16f, 255f, 70f), "DemolitionLab — M0");
            GUI.Label(new Rect(28f, 43f, 230f, 20f), "Aim: WASD / Arrow Keys");
            GUI.Label(new Rect(28f, 63f, 230f, 20f), "Fire: Space    Reset: R");
        }
    }
}
