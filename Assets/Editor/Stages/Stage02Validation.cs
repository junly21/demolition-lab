using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static VerticalSliceStage02Builder;

internal static class Stage02Validation
{
    internal static void ValidateController(VerticalSliceStageController controller)
    {
        var serializedController = new SerializedObject(controller);
        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 02"
            || serializedController.FindProperty("startingShots").intValue != 3
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || !Mathf.Approximately(serializedController.FindProperty("postClearPresentationDelay").floatValue, 0f)
            || !Mathf.Approximately(serializedController.FindProperty("projectileSpeedMultiplier").floatValue, 1f)
            || !Mathf.Approximately(serializedController.FindProperty("projectileMassMultiplier").floatValue, 1f)
            || serializedController.FindProperty("nextSceneName").stringValue != "VerticalSlice_Stage03"
            || serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount
            || !new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes).Exists(
                scene => scene.enabled && scene.path == "Assets/Scenes/VerticalSlice_Stage03.unity"))
        {
            throw new UnityException("Stage02 controller configuration is invalid.");
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
                throw new UnityException($"Stage02 controller is missing '{propertyName}'.");
            }
        }

        SerializedProperty targets = controller.FindProperty("targetPieces");
        for (int index = 0; index < targets.arraySize; index++)
        {
            if (targets.GetArrayElementAtIndex(index).objectReferenceValue == null)
            {
                throw new UnityException($"Stage02 target reference {index} is missing.");
            }
        }
    }

    internal static void ValidateNoResearchSystems()
    {
        if (Object.FindAnyObjectByType<StructureManager>() != null
            || Object.FindAnyObjectByType<StructuralBlock>() != null
            || Object.FindAnyObjectByType<StructuralConnection>() != null)
        {
            throw new UnityException("Stage02 must not use the M1-M5 structural simulation stack.");
        }
    }

    internal static void ValidateJoints(IReadOnlyList<CriticalSupportJoint> joints)
    {
        int lowerJoints = 0;
        int upperJoints = 0;
        foreach (CriticalSupportJoint joint in joints)
        {
            if (!joint.IsWeak)
            {
                throw new UnityException("Stage02 critical joints must use the visible Weak supports.");
            }

            if (Mathf.Approximately(joint.BreakForce, LowerBreakForce)
                && Mathf.Approximately(joint.BreakTorque, LowerBreakTorque))
            {
                lowerJoints++;
            }
            else if (Mathf.Approximately(joint.BreakForce, UpperBreakForce)
                && Mathf.Approximately(joint.BreakTorque, UpperBreakTorque))
            {
                upperJoints++;
            }
        }

        if (lowerJoints != 2 || upperJoints != 2)
        {
            throw new UnityException("Stage02 joint calibration mismatch.");
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
                        $"Stage02 pieces '{pieces[firstIndex].name}' and '{pieces[secondIndex].name}' "
                        + $"overlap by {distance:0.0000} m.");
                }
            }
        }
    }

    internal static void ValidateIdleStability()
    {
        GameObject[] corePieces = GameObject.FindGameObjectsWithTag("Destructible");
        var initialPositions = new Dictionary<Rigidbody, Vector3>();
        var initialRotations = new Dictionary<Rigidbody, Quaternion>();
        foreach (GameObject piece in corePieces)
        {
            if (piece.name.Contains("Decor"))
            {
                continue;
            }

            Rigidbody body = piece.GetComponent<Rigidbody>();
            initialPositions.Add(body, body.position);
            initialRotations.Add(body, body.rotation);
        }

        SimulationMode previousMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        Physics.SyncTransforms();
        try
        {
            for (int step = 0; step < 500; step++)
            {
                Physics.Simulate(0.02f);
            }
        }
        finally
        {
            Physics.simulationMode = previousMode;
        }

        float maximumDisplacement = 0f;
        float maximumRotation = 0f;
        string leastStablePiece = string.Empty;
        foreach (KeyValuePair<Rigidbody, Vector3> pair in initialPositions)
        {
            float displacement = Vector3.Distance(pair.Value, pair.Key.position);
            float rotation = Quaternion.Angle(initialRotations[pair.Key], pair.Key.rotation);
            if (displacement > maximumDisplacement || rotation > maximumRotation)
            {
                maximumDisplacement = Mathf.Max(maximumDisplacement, displacement);
                maximumRotation = Mathf.Max(maximumRotation, rotation);
                leastStablePiece = pair.Key.name;
            }
        }

        int survivingJoints = Object.FindObjectsByType<FixedJoint>(FindObjectsSortMode.None).Length;
        if (maximumDisplacement > 0.05f || maximumRotation > 2f || survivingJoints != ExpectedJointCount)
        {
            throw new UnityException(
                $"Stage02 idle stability failed at '{leastStablePiece}': "
                + $"displacement={maximumDisplacement:0.000} m, rotation={maximumRotation:0.0}°, "
                + $"joints={survivingJoints}/{ExpectedJointCount}.");
        }

        Debug.Log(
            $"Stage02 10-second idle stability passed: maximum displacement "
            + $"{maximumDisplacement:0.000} m, rotation {maximumRotation:0.0}°, "
            + $"joints {survivingJoints}/{ExpectedJointCount}.");
    }
}
