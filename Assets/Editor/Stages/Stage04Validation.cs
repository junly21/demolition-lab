using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static VerticalSliceStage04Builder;

internal static class Stage04Validation
{
    internal static void ValidateController(VerticalSliceStageController controller)
    {
        StagePresentationValidation.Validate(controller);
        var serializedController = new SerializedObject(controller);
        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 04"
            || serializedController.FindProperty("startingShots").intValue != 4
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || !Mathf.Approximately(
                serializedController.FindProperty("postClearPresentationDelay").floatValue,
                PostClearPresentationDelay)
            || serializedController.FindProperty("nextSceneName").stringValue != "VerticalSlice_Stage05"
            || !Mathf.Approximately(
                serializedController.FindProperty("projectileSpeedMultiplier").floatValue,
                ProjectileSpeedMultiplier)
            || !Mathf.Approximately(
                serializedController.FindProperty("projectileMassMultiplier").floatValue,
                ProjectileMassMultiplier)
            || serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount)
        {
            throw new UnityException("Stage04 controller configuration is invalid.");
        }

        LauncherController launcher = (LauncherController)serializedController
            .FindProperty("launcher").objectReferenceValue;
        if (launcher == null)
        {
            throw new UnityException("Stage04 controller is missing 'launcher'.");
        }

        float configuredSpeed = launcher.ProjectileSpeed * ProjectileSpeedMultiplier;
        float configuredMass = launcher.ProjectileMass * ProjectileMassMultiplier;
        if (!Mathf.Approximately(configuredSpeed, 19.8f)
            || !Mathf.Approximately(configuredMass, 2.3f))
        {
            throw new UnityException("Stage04 projectile multiplier configuration is invalid.");
        }

        string[] references =
        {
            "launcher", "hud", "feedback"
        };
        foreach (string propertyName in references)
        {
            if (serializedController.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new UnityException($"Stage04 controller is missing '{propertyName}'.");
            }
        }

        SerializedProperty targets = serializedController.FindProperty("targetPieces");
        for (int index = 0; index < targets.arraySize; index++)
        {
            if (targets.GetArrayElementAtIndex(index).objectReferenceValue == null)
            {
                throw new UnityException($"Stage04 target reference {index} is missing.");
            }
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
                bool valid = Mathf.Approximately(joint.BreakForce, WeakLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, WeakUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakUpperTorque);
                if (!valid)
                {
                    throw new UnityException("Stage04 Weak joint calibration mismatch.");
                }
            }
            else
            {
                reinforcedJoints++;
                bool valid = Mathf.Approximately(joint.BreakForce, CenterLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, CenterLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, CenterUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, CenterUpperTorque);
                if (!valid)
                {
                    throw new UnityException("Stage04 center joint calibration mismatch.");
                }
            }
        }

        if (weakJoints != 4 || reinforcedJoints != 2)
        {
            throw new UnityException(
                $"Stage04 expected 4 Weak and 2 center joints, found {weakJoints}/{reinforcedJoints}.");
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
                        $"Stage04 pieces '{pieces[firstIndex].name}' and '{pieces[secondIndex].name}' "
                        + $"overlap by {distance:0.0000} m.");
                }
            }
        }
    }

    internal static void ValidateNoResearchSystems()
    {
        if (Object.FindAnyObjectByType<StructureManager>() != null
            || Object.FindAnyObjectByType<StructuralBlock>() != null
            || Object.FindAnyObjectByType<StructuralConnection>() != null)
        {
            throw new UnityException("Stage04 must not use the M1-M5 structural simulation stack.");
        }
    }
}
