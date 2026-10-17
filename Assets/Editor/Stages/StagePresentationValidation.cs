using DemolitionLab;
using UnityEngine;

internal static class StagePresentationValidation
{
    internal static void Validate(VerticalSliceStageController controller)
    {
        if (controller.Hud == null || controller.Feedback == null
            || controller.Hud.gameObject != controller.gameObject
            || controller.Feedback.gameObject != controller.gameObject
            || !controller.Hud.ValidateRequiredReferences()
            || !controller.Feedback.ValidateRequiredReferences())
        {
            throw new UnityException("Stage HUD or feedback wiring is incomplete.");
        }
    }
}
