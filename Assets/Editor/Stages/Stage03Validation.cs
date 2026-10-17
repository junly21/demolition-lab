using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static VerticalSliceStage03Builder;

internal static class Stage03Validation
{
    internal static void ValidateController(VerticalSliceStageController controller)
    {
        var serializedController = new SerializedObject(controller);
        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 03"
            || serializedController.FindProperty("startingShots").intValue != 4
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || !Mathf.Approximately(serializedController.FindProperty("postClearPresentationDelay").floatValue, 0f)
            || !Mathf.Approximately(serializedController.FindProperty("projectileSpeedMultiplier").floatValue, 1f)
            || !Mathf.Approximately(serializedController.FindProperty("projectileMassMultiplier").floatValue, 1f)
            || serializedController.FindProperty("nextSceneName").stringValue != "VerticalSlice_Stage04"
            || serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount)
        {
            throw new UnityException("Stage03 controller configuration is invalid.");
        }

        ValidateControllerReferences(serializedController);
    }

    internal static void ValidateControllerReferences(SerializedObject controller)
    {
        StagePresentationValidation.Validate((VerticalSliceStageController)controller.targetObject);
        string[] references =
        {
            "launcher", "hud", "feedback"
        };

        foreach (string propertyName in references)
        {
            if (controller.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new UnityException($"Stage03 controller is missing '{propertyName}'.");
            }
        }

        SerializedProperty targets = controller.FindProperty("targetPieces");
        for (int index = 0; index < targets.arraySize; index++)
        {
            if (targets.GetArrayElementAtIndex(index).objectReferenceValue == null)
            {
                throw new UnityException($"Stage03 target reference {index} is missing.");
            }
        }
    }

    internal static void ValidateNoResearchSystems()
    {
        if (Object.FindAnyObjectByType<StructureManager>() != null
            || Object.FindAnyObjectByType<StructuralBlock>() != null
            || Object.FindAnyObjectByType<StructuralConnection>() != null)
        {
            throw new UnityException("Stage03 must not use the M1-M5 structural simulation stack.");
        }
    }

    internal static void ValidateJoints(IReadOnlyList<CriticalSupportJoint> joints)
    {
        int weakJoints = 0;
        int reinforcedJoints = 0;
        foreach (CriticalSupportJoint joint in joints)
        {
            if (joint.IsWeak)
            {
                weakJoints++;
                bool validWeak = Mathf.Approximately(joint.BreakForce, WeakLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, WeakUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakUpperTorque);
                if (!validWeak)
                {
                    throw new UnityException("Stage03 Weak joint calibration mismatch.");
                }
            }
            else
            {
                reinforcedJoints++;
                bool validReinforced = Mathf.Approximately(joint.BreakForce, ReinforcedLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, ReinforcedLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, ReinforcedUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, ReinforcedUpperTorque);
                if (!validReinforced)
                {
                    throw new UnityException("Stage03 Reinforced joint calibration mismatch.");
                }
            }
        }

        if (weakJoints != 4 || reinforcedJoints != 2)
        {
            throw new UnityException(
                $"Stage03 expected 4 Weak and 2 Reinforced joints, found {weakJoints}/{reinforcedJoints}.");
        }
    }

    internal static void ValidateNoInitialPenetration(IReadOnlyList<GameObject> pieces)
    {
        Physics.SyncTransforms();
        for (int firstIndex = 0; firstIndex < pieces.Count; firstIndex++)
        {
            Collider first = pieces[firstIndex].GetComponent<Collider>();
            for (int secondIndex = firstIndex + 1; secondIndex < pieces.Count; secondIndex++)
            {
                Collider second = pieces[secondIndex].GetComponent<Collider>();
                if (Physics.ComputePenetration(
                        first, first.transform.position, first.transform.rotation,
                        second, second.transform.position, second.transform.rotation,
                        out _, out float distance)
                    && distance > 0.0001f)
                {
                    throw new UnityException(
                        $"Stage03 pieces '{pieces[firstIndex].name}' and '{pieces[secondIndex].name}' "
                        + $"overlap by {distance:0.0000} m.");
                }
            }
        }
    }
}
