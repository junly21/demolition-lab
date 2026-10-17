using UnityEngine;

namespace DemolitionLab
{
    public enum PresentationCategory
    {
        Concrete,
        Steel,
        Weak,
        Reinforced,
        Foundation,
        Decorative
    }

    [DisallowMultipleComponent]
    public sealed class PresentationVisual : MonoBehaviour
    {
        [SerializeField] private PresentationCategory category;

        public PresentationCategory Category => category;

        public void Configure(PresentationCategory presentationCategory)
        {
            category = presentationCategory;
        }
    }
}
