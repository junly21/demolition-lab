using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static VerticalSlicePresentationBuilder;

internal static class IndustrialPresentationValidation
{
    internal static void ValidateEnvironmentInOpenScene(string scenePath)
    {
        GameObject root = GameObject.Find(EnvironmentRootName);
        if (root == null || root.GetComponentsInChildren<Renderer>().Length < 20)
        {
            throw new UnityException($"{scenePath} is missing the presentation environment.");
        }

        if (root.GetComponentInChildren<Collider>() != null)
        {
            throw new UnityException($"{scenePath} presentation geometry must remain non-colliding.");
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial == null)
            {
                throw new UnityException($"{scenePath} has a presentation renderer without a material.");
            }
        }

        Renderer ground = GameObject.Find("Ground")?.GetComponent<Renderer>();
        bool hasSoftKeyLight = false;
        foreach (Light light in Object.FindObjectsByType<Light>())
        {
            if (light.shadows == LightShadows.Soft)
            {
                hasSoftKeyLight = true;
                break;
            }
        }

        if (ground == null
            || ground.sharedMaterial == null
            || !hasSoftKeyLight
            || !RenderSettings.fog
            || RenderSettings.skybox == null)
        {
            throw new UnityException($"{scenePath} environment lighting or ground setup is incomplete.");
        }

        ThirdPartyPresentationBuilder.ValidateOpenScene(scenePath);
    }

    internal static void ValidateStructuralPresentationInOpenScene(string scenePath)
    {
        CriticalSupportJoint[] joints = Object.FindObjectsByType<CriticalSupportJoint>();
        if (joints.Length == 0)
        {
            throw new UnityException($"{scenePath} has no critical support joints to present.");
        }

        foreach (CriticalSupportJoint joint in joints)
        {
            Transform detail = PhysicsPresentationSeparationBuilder.FindDescendant(
                joint.transform,
                ConnectionDetailName);
            int expectedRenderers = joint.IsWeak ? 5 : 4;
            if (detail == null
                || detail.GetComponentsInChildren<Renderer>().Length != expectedRenderers
                || detail.GetComponentInChildren<Collider>() != null
                || detail.GetComponentInChildren<Rigidbody>() != null)
            {
                throw new UnityException($"{scenePath} joint '{joint.name}' has invalid visual detailing.");
            }

            foreach (Renderer renderer in detail.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial == null)
                {
                    throw new UnityException($"{scenePath} joint '{joint.name}' has a missing visual material.");
                }
            }
        }

        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        Transform launcherPresentation = launcher != null
            ? launcher.transform.Find(LauncherPresentationName)
            : null;
        Transform pitchPivot = launcher != null
            ? (Transform)new SerializedObject(launcher).FindProperty("pitchPivot").objectReferenceValue
            : null;
        Transform barrelPresentation = pitchPivot != null
            ? pitchPivot.Find(BarrelPresentationName)
            : null;
        if (launcherPresentation == null
            || launcherPresentation.GetComponentsInChildren<Renderer>().Length < 7
            || launcherPresentation.GetComponentInChildren<Collider>() != null
            || launcherPresentation.GetComponentInChildren<Rigidbody>() != null)
        {
            throw new UnityException($"{scenePath} launcher presentation is incomplete or colliding.");
        }

        if (barrelPresentation == null
            || barrelPresentation.GetComponentsInChildren<Renderer>().Length != 4
            || barrelPresentation.GetComponentInChildren<Collider>() != null)
        {
            throw new UnityException($"{scenePath} barrel presentation is incomplete or colliding.");
        }

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        string[] hudProperties = { "stageLabelText", "shotsText", "destructionText" };
        foreach (string propertyName in hudProperties)
        {
            Text text = (Text)serializedHud.FindProperty(propertyName).objectReferenceValue;
            if (text == null || text.GetComponent<Outline>() == null)
            {
                throw new UnityException($"{scenePath} HUD presentation is incomplete.");
            }
        }

        PhysicsPresentationSeparationBuilder.ValidateOpenScene(scenePath);
        ThirdPartyPresentationBuilder.ValidateOpenScene(scenePath);
        FinalVisualPolishBuilder.ValidateLightingAndPostInOpenScene(scenePath);
        FinalVisualPolishBuilder.ValidateFeedbackInOpenScene(scenePath);
        FinalVisualPolishBuilder.ValidateHudInOpenScene(scenePath);
    }

    internal static void ValidateCameraFramingInOpenScene(string scenePath)
    {
        Camera camera = Camera.main;
        CameraFraming framing = GetCameraFraming(SceneManager.GetActiveScene().name);
        Vector3 expectedDirection = (framing.LookAt - framing.Position).normalized;
        if (camera == null
            || Vector3.Distance(camera.transform.position, framing.Position) > 0.001f
            || Vector3.Angle(camera.transform.forward, expectedDirection) > 0.05f
            || !Mathf.Approximately(camera.fieldOfView, framing.FieldOfView))
        {
            throw new UnityException($"{scenePath} presentation camera framing is incorrect.");
        }
    }
}
