using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static VerticalSliceSceneBuilder;

internal static class Stage01Validation
{
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
                        $"Stage 01 pieces '{pieces[firstIndex].name}' and "
                        + $"'{pieces[secondIndex].name}' overlap by {distance:0.0000} m.");
                }
            }
        }
    }

    internal static void AssertJointThreshold(
        CriticalSupportJoint joint,
        float expectedForce,
        float expectedTorque)
    {
        if (!Mathf.Approximately(joint.BreakForce, expectedForce)
            || !Mathf.Approximately(joint.BreakTorque, expectedTorque))
        {
            throw new UnityException("Stage 01 critical joint threshold mismatch.");
        }
    }

    internal static void ValidateRequiredReferences(
        VerticalSliceStageController controller,
        LauncherController launcher)
    {
        StagePresentationValidation.Validate(controller);
        var serializedController = new SerializedObject(controller);
        string[] controllerReferences =
        {
            "launcher",
            "hud",
            "feedback"
        };

        foreach (string propertyName in controllerReferences)
        {
            if (serializedController.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new UnityException($"Stage controller is missing '{propertyName}'.");
            }
        }

        if (serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount)
        {
            throw new UnityException("Stage controller target-piece wiring is incomplete.");
        }

        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 01"
            || serializedController.FindProperty("startingShots").intValue != 3
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || !Mathf.Approximately(serializedController.FindProperty("postClearPresentationDelay").floatValue, 0f)
            || !Mathf.Approximately(serializedController.FindProperty("projectileSpeedMultiplier").floatValue, 1f)
            || !Mathf.Approximately(serializedController.FindProperty("projectileMassMultiplier").floatValue, 1f)
            || serializedController.FindProperty("nextSceneName").stringValue != "VerticalSlice_Stage02")
        {
            throw new UnityException("Stage 01 reusable configuration is incorrect.");
        }

        var serializedLauncher = new SerializedObject(launcher);
        string[] launcherReferences = { "yawPivot", "pitchPivot", "muzzle", "projectilePrefab", "aimLine" };
        foreach (string propertyName in launcherReferences)
        {
            if (serializedLauncher.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new UnityException($"Stage launcher is missing '{propertyName}'.");
            }
        }
    }

    internal static void ValidateStageFlowRules(VerticalSliceStageController controller)
    {
        var flow = new StageFlowStateMachine();
        flow.Initialize(3);
        if (flow.State != StageState.Initializing || flow.AmmoRemaining != 3)
        {
            throw new UnityException("Stage flow must start Initializing with full ammo.");
        }

        if (!flow.MarkReady() || !flow.Activate() || flow.State != StageState.Active)
        {
            throw new UnityException("Stage flow failed Initializing -> Ready -> Active.");
        }

        if (!flow.RegisterShot() || flow.AmmoRemaining != 2 || flow.ShotsUsed != 1)
        {
            throw new UnityException("One accepted shot must decrement ammo exactly once.");
        }

        if (!flow.BeginSettling() || !flow.Complete(true) || flow.State != StageState.Clear)
        {
            throw new UnityException("Stage flow failed the legitimate clear transition.");
        }

        flow.Initialize(3);
        flow.MarkReady();
        flow.Activate();
        flow.RegisterShot();
        flow.RegisterShot();
        flow.RegisterShot();
        if (flow.AmmoRemaining != 0
            || !flow.BeginSettling()
            || !flow.Complete(false)
            || flow.State != StageState.Failed)
        {
            throw new UnityException("Stage flow failed the exhausted-ammo failure transition.");
        }

        GameObject targetObject = new("Stage Flow Validation Target");
        targetObject.transform.position = new Vector3(0f, 4f, 0f);
        targetObject.AddComponent<Rigidbody>().isKinematic = true;
        StageTargetPiece target = targetObject.AddComponent<StageTargetPiece>();
        target.InitializeReferenceState();
        if (target.EvaluateDestroyed())
        {
            Object.DestroyImmediate(targetObject);
            throw new UnityException("A target must not count during initialization.");
        }

        target.BeginEvaluation();
        if (target.EvaluateDestroyed())
        {
            Object.DestroyImmediate(targetObject);
            throw new UnityException("An unchanged active target must start at 0% destruction.");
        }

        targetObject.transform.position += Vector3.down * 2f;
        if (!target.EvaluateDestroyed())
        {
            Object.DestroyImmediate(targetObject);
            throw new UnityException("A fallen target must count toward destruction.");
        }

        Object.DestroyImmediate(targetObject);

        int requiredTargets = Mathf.CeilToInt(ExpectedTargetCount * 0.7f);
        if (requiredTargets != 6
            || !new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes).Exists(
                scene => scene.enabled && scene.path == "Assets/Scenes/VerticalSlice_Stage02.unity"))
        {
            throw new UnityException("Stage threshold or Stage02 progression behavior is incorrect.");
        }

        if (!new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes).Exists(
                scene => scene.enabled && scene.path == ScenePath))
        {
            throw new UnityException("Stage 01 must be enabled in build settings for Retry.");
        }
    }

    internal static void ValidateDefaultTrajectory(LauncherController launcher)
    {
        var serializedLauncher = new SerializedObject(launcher);
        Transform pitchPivot = (Transform)serializedLauncher.FindProperty("pitchPivot").objectReferenceValue;
        Transform muzzle = (Transform)serializedLauncher.FindProperty("muzzle").objectReferenceValue;
        float speed = serializedLauncher.FindProperty("projectileSpeed").floatValue;
        int pointCount = serializedLauncher.FindProperty("trajectoryPoints").intValue;
        float timeStep = serializedLauncher.FindProperty("trajectoryTimeStep").floatValue;

        float initialPitchAngle = serializedLauncher.FindProperty("initialPitchAngle").floatValue;
        Quaternion runtimePitchRotation = pitchPivot.rotation * Quaternion.Euler(-initialPitchAngle, 0f, 0f);
        Vector3 startPosition = pitchPivot.position + runtimePitchRotation * muzzle.localPosition;
        Vector3 startVelocity = runtimePitchRotation * muzzle.localRotation * Vector3.forward * speed;
        Vector3 previousPoint = startPosition;
        Physics.SyncTransforms();
        Debug.Log(
            $"Stage 01 default trajectory starts at {startPosition} with velocity {startVelocity}.");

        for (int index = 1; index < pointCount; index++)
        {
            float time = index * timeStep;
            Vector3 nextPoint = startPosition
                + startVelocity * time
                + 0.5f * Physics.gravity * time * time;

            if (Physics.Linecast(previousPoint, nextPoint, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Destructible"))
                {
                    throw new UnityException(
                        $"Stage 01 default trajectory is blocked by '{hit.collider.name}' before the structure.");
                }

                Debug.Log($"Stage 01 default trajectory intersects '{hit.collider.name}'.");
                return;
            }

            previousPoint = nextPoint;
        }

        throw new UnityException("Stage 01 default trajectory never intersects the destructible structure.");
    }

    internal static void ValidateLegacyHudDisabled()
    {
        ResetController resetController = Object.FindAnyObjectByType<ResetController>();
        if (resetController == null
            || new SerializedObject(resetController).FindProperty("showControls").boolValue)
        {
            throw new UnityException("Vertical-slice legacy M0 HUD must be disabled.");
        }
    }
}
