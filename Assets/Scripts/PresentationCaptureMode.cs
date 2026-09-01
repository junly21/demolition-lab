using UnityEngine;
using UnityEngine.InputSystem;

namespace DemolitionLab
{
    public sealed class PresentationCaptureMode : MonoBehaviour
    {
        [SerializeField] private LineRenderer trajectory;

        private bool trajectoryVisible;

        private void Awake()
        {
            if (trajectory == null)
            {
                Debug.LogError($"{nameof(PresentationCaptureMode)} is missing its trajectory reference.", this);
                enabled = false;
                return;
            }

            trajectoryVisible = trajectory.enabled;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f10Key.wasPressedThisFrame)
            {
                return;
            }

            trajectoryVisible = !trajectoryVisible;
            trajectory.enabled = trajectoryVisible;
        }
    }
}
