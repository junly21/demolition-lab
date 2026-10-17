using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static VerticalSliceStage05Builder;

internal static class Stage05Validation
{
    internal static void ValidateController(VerticalSliceStageController controller)
    {
        StagePresentationValidation.Validate(controller);
        var serializedController = new SerializedObject(controller);
        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 05"
            || serializedController.FindProperty("startingShots").intValue != 4
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || !Mathf.Approximately(serializedController.FindProperty("postClearPresentationDelay").floatValue, 0f)
            || !string.IsNullOrEmpty(serializedController.FindProperty("nextSceneName").stringValue)
            || !Mathf.Approximately(serializedController.FindProperty("projectileSpeedMultiplier").floatValue, ProjectileSpeedMultiplier)
            || !Mathf.Approximately(serializedController.FindProperty("projectileMassMultiplier").floatValue, ProjectileMassMultiplier)
            || serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount
            || controller.HasNextScene)
        {
            throw new UnityException("Stage05 controller configuration is invalid.");
        }

        string[] references =
        {
            "launcher", "hud", "feedback"
        };
        foreach (string propertyName in references)
        {
            if (serializedController.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new UnityException($"Stage05 controller is missing '{propertyName}'.");
            }
        }

        LauncherController launcher = (LauncherController)serializedController
            .FindProperty("launcher").objectReferenceValue;
        if (!Mathf.Approximately(launcher.ProjectileSpeed * ProjectileSpeedMultiplier, 19.8f)
            || !Mathf.Approximately(launcher.ProjectileMass * ProjectileMassMultiplier, 2.3f))
        {
            throw new UnityException("Stage05 projectile configuration must match Stage04.");
        }

        SerializedProperty targetReferences = serializedController.FindProperty("targetPieces");
        for (int index = 0; index < targetReferences.arraySize; index++)
        {
            if (targetReferences.GetArrayElementAtIndex(index).objectReferenceValue == null)
            {
                throw new UnityException($"Stage05 target reference {index} is missing.");
            }
        }
    }

    internal static void ValidateTargets(IReadOnlyList<StageTargetPiece> targets)
    {
        int leftTargets = 0;
        int middleTargets = 0;
        int rightTargets = 0;
        foreach (StageTargetPiece target in targets)
        {
            target.InitializeReferenceState();
            if (target.EvaluateDestroyed())
            {
                throw new UnityException($"Stage05 target '{target.name}' counts during initialization.");
            }

            if (target.name.StartsWith("Left_"))
            {
                leftTargets++;
            }
            else if (target.name.StartsWith("Middle_"))
            {
                middleTargets++;
            }
            else if (target.name.StartsWith("Right_"))
            {
                rightTargets++;
            }
        }

        int requiredTargets = Mathf.CeilToInt(ExpectedTargetCount * 0.7f);
        if (leftTargets != 5
            || middleTargets != 5
            || rightTargets != 5
            || requiredTargets != 11
            || leftTargets + middleTargets >= requiredTargets)
        {
            throw new UnityException(
                $"Stage05 target zoning is invalid: {leftTargets}/{middleTargets}/{rightTargets}, "
                + $"required={requiredTargets}.");
        }
    }

    internal static void ValidateJoints(IReadOnlyList<CriticalSupportJoint> joints)
    {
        int weakJoints = 0;
        int reinforcedJoints = 0;
        foreach (CriticalSupportJoint joint in joints)
        {
            var serializedJoint = new SerializedObject(joint);
            FixedJoint physicalJoint = joint.GetComponent<FixedJoint>();
            if (physicalJoint == null
                || physicalJoint.connectedBody == null
                || serializedJoint.FindProperty("physicalJoint").objectReferenceValue != physicalJoint
                || serializedJoint.FindProperty("stageController").objectReferenceValue == null
                || serializedJoint.FindProperty("indicatorRenderer").objectReferenceValue == null
                || serializedJoint.FindProperty("brokenMaterial").objectReferenceValue == null)
            {
                throw new UnityException($"Stage05 joint '{joint.name}' has a missing runtime reference.");
            }

            if (joint.IsWeak)
            {
                weakJoints++;
                bool validWeak = Mathf.Approximately(joint.BreakForce, WeakLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, WeakUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakUpperTorque);
                if (!validWeak)
                {
                    throw new UnityException("Stage05 Weak joint calibration mismatch.");
                }
            }
            else
            {
                reinforcedJoints++;
                if (!Mathf.Approximately(joint.BreakForce, ReinforcedForce)
                    || !Mathf.Approximately(joint.BreakTorque, ReinforcedTorque))
                {
                    throw new UnityException("Stage05 Reinforced joint calibration mismatch.");
                }
            }
        }

        if (weakJoints != 6 || reinforcedJoints != 4 || WeakUpperForce >= ReinforcedForce)
        {
            throw new UnityException(
                $"Stage05 expected 6 Weak and 4 Reinforced joints, found "
                + $"{weakJoints}/{reinforcedJoints}.");
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
                        first,
                        first.transform.position,
                        first.transform.rotation,
                        second,
                        second.transform.position,
                        second.transform.rotation,
                        out _,
                        out float distance)
                    && distance > 0.0001f)
                {
                    throw new UnityException(
                        $"Stage05 pieces '{pieces[firstIndex].name}' and "
                        + $"'{pieces[secondIndex].name}' overlap by {distance:0.0000} m.");
                }
            }
        }
    }

    internal static void ValidateIdleStability()
    {
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("Destructible");
        var initialPositions = new Dictionary<Rigidbody, Vector3>();
        var initialRotations = new Dictionary<Rigidbody, Quaternion>();
        foreach (GameObject piece in pieces)
        {
            if (piece.name.StartsWith("Ground_Decor"))
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
            if (displacement > maximumDisplacement)
            {
                maximumDisplacement = displacement;
                leastStablePiece = pair.Key.name;
            }

            maximumRotation = Mathf.Max(maximumRotation, rotation);
        }

        int survivingJoints = Object.FindObjectsByType<FixedJoint>(FindObjectsSortMode.None).Length;
        if (maximumDisplacement > 0.05f
            || maximumRotation > 2f
            || survivingJoints != ExpectedJointCount)
        {
            throw new UnityException(
                $"Stage05 idle stability failed at '{leastStablePiece}': "
                + $"displacement={maximumDisplacement:0.000} m, rotation={maximumRotation:0.0}°, "
                + $"joints={survivingJoints}/{ExpectedJointCount}.");
        }

        Debug.Log(
            $"Stage05 10-second idle stability passed: maximum displacement "
            + $"{maximumDisplacement:0.000} m, rotation {maximumRotation:0.0}°, "
            + $"joints {survivingJoints}/{ExpectedJointCount}.");
    }

    internal static void ValidateNoResearchSystems()
    {
        if (Object.FindAnyObjectByType<StructureManager>() != null
            || Object.FindAnyObjectByType<StructuralBlock>() != null
            || Object.FindAnyObjectByType<StructuralConnection>() != null)
        {
            throw new UnityException("Stage05 must not use the M1-M5 structural simulation stack.");
        }
    }
}
