using UnityEngine;

namespace DemolitionLab
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PresentationAudioHooks : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource worldSource;

        [Header("Clips")]
        [SerializeField] private AudioClip fire;
        [SerializeField] private AudioClip impactConcrete;
        [SerializeField] private AudioClip impactSteel;
        [SerializeField] private AudioClip criticalJointBreak;
        [SerializeField] private AudioClip largeCollapse;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip clear;
        [SerializeField] private AudioClip fail;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float fireVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float impactVolume = 0.62f;
        [SerializeField, Range(0f, 1f)] private float criticalBreakVolume = 0.78f;
        [SerializeField, Range(0f, 1f)] private float collapseVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float resultVolume = 0.68f;

        private AudioSource uiSource;

        private void Awake()
        {
            uiSource = GetComponent<AudioSource>();
        }

        public void PlayFire(Vector3 position) => PlayWorld(fire, position, fireVolume);
        public void PlayConcreteImpact(Vector3 position, float severity = 1f) =>
            PlayWorld(impactConcrete, position, impactVolume * severity);
        public void PlaySteelImpact(Vector3 position, float severity = 1f) =>
            PlayWorld(impactSteel, position, impactVolume * severity);
        public void PlayCriticalBreak(Vector3 position) =>
            PlayWorld(criticalJointBreak, position, criticalBreakVolume);
        public void PlayLargeCollapse(Vector3 position) =>
            PlayWorld(largeCollapse, position, collapseVolume);
        public void PlayUiClick() => PlayUi(uiClick, uiVolume);
        public void PlayClear() => PlayUi(clear, resultVolume);
        public void PlayFail() => PlayUi(fail, resultVolume);

        private void PlayWorld(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null || worldSource == null)
            {
                return;
            }

            worldSource.transform.position = position;
            worldSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void PlayUi(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                uiSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }
    }
}
