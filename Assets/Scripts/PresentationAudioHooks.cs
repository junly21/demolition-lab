using UnityEngine;

namespace DemolitionLab
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PresentationAudioHooks : MonoBehaviour
    {
        [SerializeField] private AudioClip fire;
        [SerializeField] private AudioClip impactConcrete;
        [SerializeField] private AudioClip impactSteel;
        [SerializeField] private AudioClip criticalJointBreak;
        [SerializeField] private AudioClip largeCollapse;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip clear;
        [SerializeField] private AudioClip fail;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        public void PlayFire() => Play(fire);
        public void PlayConcreteImpact() => Play(impactConcrete);
        public void PlaySteelImpact() => Play(impactSteel);
        public void PlayCriticalBreak() => Play(criticalJointBreak);
        public void PlayLargeCollapse() => Play(largeCollapse);
        public void PlayUiClick() => Play(uiClick);
        public void PlayClear() => Play(clear);
        public void PlayFail() => Play(fail);

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }
}
