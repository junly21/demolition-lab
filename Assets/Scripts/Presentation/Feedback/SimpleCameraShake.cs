using UnityEngine;

namespace DemolitionLab
{
    public sealed class SimpleCameraShake : MonoBehaviour
    {
        private Vector3 baseLocalPosition;
        private float remainingDuration;
        private float intensity;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
        }

        public void Shake(float shakeIntensity, float duration)
        {
            intensity = Mathf.Max(intensity, shakeIntensity);
            remainingDuration = Mathf.Max(remainingDuration, duration);
        }

        private void LateUpdate()
        {
            if (remainingDuration <= 0f)
            {
                transform.localPosition = baseLocalPosition;
                intensity = 0f;
                return;
            }

            remainingDuration -= Time.deltaTime;
            transform.localPosition = baseLocalPosition + Random.insideUnitSphere * intensity;
        }

        private void OnDisable()
        {
            transform.localPosition = baseLocalPosition;
        }
    }
}
