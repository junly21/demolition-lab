using UnityEngine;
using UnityEngine.InputSystem;

namespace DemolitionLab
{
    public sealed class LauncherController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform pitchPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LineRenderer aimLine;

        [Header("Projectile")]
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0.1f)] private float projectileMass = 2f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 10f;

        [Header("Aiming")]
        [SerializeField, Min(1f)] private float aimingSensitivity = 35f;
        [SerializeField] private Vector2 yawLimits = new(-25f, 25f);
        [SerializeField] private Vector2 pitchLimits = new(5f, 35f);
        [SerializeField, Range(8, 40)] private int trajectoryPoints = 24;
        [SerializeField, Range(0.03f, 0.2f)] private float trajectoryTimeStep = 0.1f;

        private Quaternion initialYawRotation;
        private Quaternion initialPitchRotation;
        private float yawAngle;
        private float pitchAngle = 15f;
        private bool hasValidReferences;

        private void Awake()
        {
            hasValidReferences = ValidateRequiredReferences();
            if (!hasValidReferences)
            {
                return;
            }

            initialYawRotation = yawPivot.localRotation;
            initialPitchRotation = pitchPivot.localRotation;
            ApplyAim();
        }

        private void Update()
        {
            if (!hasValidReferences)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 aimInput = Vector2.zero;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
            {
                aimInput.x -= 1f;
            }
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
            {
                aimInput.x += 1f;
            }
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
            {
                aimInput.y -= 1f;
            }
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
            {
                aimInput.y += 1f;
            }

            if (aimInput.sqrMagnitude > 0f)
            {
                yawAngle = Mathf.Clamp(
                    yawAngle + aimInput.x * aimingSensitivity * Time.deltaTime,
                    yawLimits.x,
                    yawLimits.y);
                pitchAngle = Mathf.Clamp(
                    pitchAngle + aimInput.y * aimingSensitivity * Time.deltaTime,
                    pitchLimits.x,
                    pitchLimits.y);
                ApplyAim();
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                Fire();
            }

            UpdateAimLine();
        }

        private void ApplyAim()
        {
            yawPivot.localRotation = initialYawRotation * Quaternion.Euler(0f, yawAngle, 0f);
            pitchPivot.localRotation = initialPitchRotation * Quaternion.Euler(-pitchAngle, 0f, 0f);
        }

        private void Fire()
        {
            Projectile projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
            projectile.Launch(muzzle.forward * projectileSpeed, projectileMass, projectileLifetime);
            Debug.Log($"Projectile fired at {projectileSpeed:0.0} m/s.", this);
        }

        private void UpdateAimLine()
        {
            if (aimLine == null)
            {
                return;
            }

            aimLine.positionCount = trajectoryPoints;
            Vector3 startPosition = muzzle.position;
            Vector3 startVelocity = muzzle.forward * projectileSpeed;

            for (int index = 0; index < trajectoryPoints; index++)
            {
                float time = index * trajectoryTimeStep;
                Vector3 point = startPosition
                    + startVelocity * time
                    + 0.5f * Physics.gravity * time * time;
                aimLine.SetPosition(index, point);
            }
        }

        [ContextMenu("Validate Required References")]
        private void ValidateReferencesFromContextMenu()
        {
            ValidateRequiredReferences();
        }

        private bool ValidateRequiredReferences()
        {
            bool isValid = true;
            isValid &= ValidateReference(yawPivot, nameof(yawPivot));
            isValid &= ValidateReference(pitchPivot, nameof(pitchPivot));
            isValid &= ValidateReference(muzzle, nameof(muzzle));
            isValid &= ValidateReference(projectilePrefab, nameof(projectilePrefab));
            isValid &= ValidateReference(aimLine, nameof(aimLine));
            return isValid;
        }

        private bool ValidateReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(LauncherController)} on {name} is missing required reference '{fieldName}'.",
                this);
            return false;
        }
    }
}
