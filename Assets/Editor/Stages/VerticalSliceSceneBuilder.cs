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

public static class VerticalSliceSceneBuilder
{
    internal const string SourceScenePath = "Assets/Scenes/M0_DemolitionLab.unity";
    internal const string ScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    internal const string MaterialFolder = "Assets/Materials/VerticalSlice";
    internal const int ExpectedPieceCount = 20;
    internal const int ExpectedTargetCount = 8;
    internal const int ExpectedJointCount = 4;
    internal const float WeakBreakForce = 220f;
    internal const float WeakBreakTorque = 90f;
    internal const float ReinforcedBreakForce = 900f;
    internal const float ReinforcedBreakTorque = 400f;

    internal static void ValidateLegacyHudDisabled()
    {
        Stage01Validation.ValidateLegacyHudDisabled();
    }

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 01")]
    public static void BuildScene()
    {
        string previousScenePath = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create vertical-slice scene at {ScenePath}.");
        }

        RemoveBaselineBlocks();
        StageMaterials materials = CreateMaterials();
        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        Camera camera = Camera.main;
        if (launcher == null || camera == null)
        {
            throw new UnityException("Vertical-slice source scene is missing launcher or Main Camera.");
        }

        ConfigureLegacyHud();

        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        GameObject root = new("Stage 01 Structure");
        Dictionary<string, GameObject> structure = BuildStructure(root.transform, materials, pieces, targets);

        SimpleCameraShake cameraShake = camera.gameObject.GetComponent<SimpleCameraShake>();
        if (cameraShake == null)
        {
            cameraShake = camera.gameObject.AddComponent<SimpleCameraShake>();
        }

        ParticleSystem dust = CreateImpactDust(materials.Dust);
        VerticalSliceStageController stageController = CreateStageController(
            launcher,
            targets,
            cameraShake,
            dust);
        AddCriticalJoints(structure, stageController, materials.Broken);
        ConfigureCameraAndLauncher(camera, launcher);
        VerticalSlicePresentationBuilder.ApplyEnvironmentToOpenScene();
        VerticalSlicePresentationBuilder.ApplyStructuralPresentationToOpenScene();
        VerticalSlicePresentationBuilder.ApplyCameraFramingToOpenScene();

        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != ScenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"Built vertical-slice Stage 01 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 01")]
    public static void ValidateScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] destructiblePieces = GameObject.FindGameObjectsWithTag("Destructible");
        StageTargetPiece[] targets = Object.FindObjectsByType<StageTargetPiece>();
        CriticalSupportJoint[] joints = Object.FindObjectsByType<CriticalSupportJoint>();
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();

        if (destructiblePieces.Length != ExpectedPieceCount
            || targets.Length != ExpectedTargetCount
            || joints.Length != ExpectedJointCount
            || controller == null
            || launcher == null)
        {
            throw new UnityException(
                $"Stage 01 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"and {ExpectedJointCount} critical joints.");
        }

        if (Object.FindObjectsByType<VerticalSliceStageController>().Length != 1)
        {
            throw new UnityException("Stage 01 must contain exactly one stage controller.");
        }

        if (Object.FindAnyObjectByType<StructureManager>() != null
            || Object.FindAnyObjectByType<StructuralBlock>() != null
            || Object.FindAnyObjectByType<StructuralConnection>() != null)
        {
            throw new UnityException("Stage 01 must not reuse the M1-M5 structural simulation stack.");
        }

        Stage01Validation.ValidateRequiredReferences(controller, launcher);
        Stage01Validation.ValidateLegacyHudDisabled();
        Stage01Validation.ValidateStageFlowRules(controller);
        Stage01Validation.ValidateDefaultTrajectory(launcher);

        int weakJoints = 0;
        int reinforcedJoints = 0;
        foreach (CriticalSupportJoint joint in joints)
        {
            if (joint.IsWeak)
            {
                weakJoints++;
                Stage01Validation.AssertJointThreshold(joint, WeakBreakForce, WeakBreakTorque);
            }
            else
            {
                reinforcedJoints++;
                Stage01Validation.AssertJointThreshold(joint, ReinforcedBreakForce, ReinforcedBreakTorque);
            }
        }

        if (weakJoints != 2 || reinforcedJoints != 2)
        {
            throw new UnityException(
                $"Stage 01 expected 2 Weak and 2 Reinforced joints, found {weakJoints}/{reinforcedJoints}.");
        }

        Stage01Validation.ValidateNoInitialPenetration(destructiblePieces);
        if (!Mathf.Approximately(launcher.ProjectileMass, 2f)
            || !Mathf.Approximately(launcher.ProjectileSpeed, 18f))
        {
            throw new UnityException("Stage 01 projectile baseline must remain 2 kg at 18 m/s.");
        }

        Debug.Log(
            "Vertical-slice Stage 01 validation passed: initial 0%, ammo decrement, state transitions, "
            + "clear/fail rules, Retry/Next configuration, serialized references, critical joints, "
            + "projectile tuning, and collider separation verified.");
    }

    private static Dictionary<string, GameObject> BuildStructure(
        Transform root,
        StageMaterials materials,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        var result = new Dictionary<string, GameObject>();
        AddPiece(result, pieces, targets, root, "Left_Foundation", new Vector3(-2f, 0.5f, 4f), new Vector3(1.4f, 1f, 1.5f), 8f, true, false, materials.Concrete);
        AddPiece(result, pieces, targets, root, "Right_Foundation", new Vector3(2f, 0.5f, 4f), new Vector3(1.4f, 1f, 1.5f), 8f, true, false, materials.Concrete);
        AddPiece(result, pieces, targets, root, "Left_Weak_Lower", new Vector3(-2f, 1.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(result, pieces, targets, root, "Right_Reinforced_Lower", new Vector3(2f, 1.5f, 4f), Vector3.one, 2f, false, false, materials.Reinforced);
        AddPiece(result, pieces, targets, root, "Left_Weak_Upper", new Vector3(-2f, 2.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(result, pieces, targets, root, "Right_Reinforced_Upper", new Vector3(2f, 2.5f, 4f), Vector3.one, 2f, false, false, materials.Reinforced);
        AddPiece(result, pieces, targets, root, "Upper_Platform", new Vector3(0f, 3.25f, 4f), new Vector3(7f, 0.5f, 1.5f), 8f, false, true, materials.Concrete);

        float[] wallX = { -2.5f, -1.5f, -0.5f, 0.5f, 1.5f, 2.5f };
        foreach (float x in wallX)
        {
            AddPiece(result, pieces, targets, root, $"Upper_Wall_{x:0.0}", new Vector3(x, 4.25f, 4f), new Vector3(0.8f, 1.5f, 1f), 1f, false, true, materials.Concrete);
        }

        AddPiece(result, pieces, targets, root, "Roof", new Vector3(0f, 5.25f, 4f), new Vector3(7f, 0.5f, 1.5f), 6f, false, true, materials.Concrete);
        float[] crateX = { -2.4f, -0.8f, 0.8f, 2.4f };
        foreach (float x in crateX)
        {
            AddPiece(result, pieces, targets, root, $"Roof_Decor_{x:0.0}", new Vector3(x, 5.8f, 4f), Vector3.one * 0.6f, 0.8f, false, false, materials.Decor);
        }

        AddPiece(result, pieces, targets, root, "Left_NonCritical", new Vector3(-4.5f, 0.5f, 4f), Vector3.one, 1.5f, false, false, materials.Decor);
        AddPiece(result, pieces, targets, root, "Right_NonCritical", new Vector3(4.5f, 0.5f, 4f), Vector3.one, 1.5f, false, false, materials.Decor);
        return result;
    }

    private static void AddPiece(
        IDictionary<string, GameObject> lookup,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        Transform root,
        string name,
        Vector3 position,
        Vector3 scale,
        float mass,
        bool isKinematic,
        bool isTarget,
        Material material)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.tag = "Destructible";
        piece.transform.SetParent(root, false);
        piece.transform.position = position;
        piece.transform.localScale = scale;
        piece.GetComponent<Renderer>().sharedMaterial = material;

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.mass = mass;
        body.isKinematic = isKinematic;
        body.linearDamping = 0.08f;
        body.angularDamping = 0.18f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.solverIterations = 10;
        body.solverVelocityIterations = 4;

        if (isTarget)
        {
            targets.Add(piece.AddComponent<StageTargetPiece>());
        }

        lookup.Add(name, piece);
        pieces.Add(piece);
    }

    private static void AddCriticalJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        CreateCriticalJoint(structure["Left_Weak_Lower"], structure["Left_Foundation"], controller, true, brokenMaterial);
        CreateCriticalJoint(structure["Left_Weak_Upper"], structure["Left_Weak_Lower"], controller, true, brokenMaterial);
        CreateCriticalJoint(structure["Right_Reinforced_Lower"], structure["Right_Foundation"], controller, false, brokenMaterial);
        CreateCriticalJoint(structure["Right_Reinforced_Upper"], structure["Right_Reinforced_Lower"], controller, false, brokenMaterial);
    }

    private static void CreateCriticalJoint(
        GameObject owner,
        GameObject connected,
        VerticalSliceStageController controller,
        bool isWeak,
        Material brokenMaterial)
    {
        FixedJoint joint = owner.AddComponent<FixedJoint>();
        joint.connectedBody = connected.GetComponent<Rigidbody>();
        joint.breakForce = isWeak ? WeakBreakForce : ReinforcedBreakForce;
        joint.breakTorque = isWeak ? WeakBreakTorque : ReinforcedBreakTorque;
        joint.enableCollision = false;
        joint.enablePreprocessing = true;

        CriticalSupportJoint supportJoint = owner.AddComponent<CriticalSupportJoint>();
        var serializedJoint = new SerializedObject(supportJoint);
        serializedJoint.FindProperty("physicalJoint").objectReferenceValue = joint;
        serializedJoint.FindProperty("stageController").objectReferenceValue = controller;
        serializedJoint.FindProperty("weakConnection").boolValue = isWeak;
        serializedJoint.FindProperty("indicatorRenderer").objectReferenceValue = owner.GetComponent<Renderer>();
        serializedJoint.FindProperty("brokenMaterial").objectReferenceValue = brokenMaterial;
        serializedJoint.ApplyModifiedPropertiesWithoutUndo();
    }

    private static VerticalSliceStageController CreateStageController(
        LauncherController launcher,
        IReadOnlyList<StageTargetPiece> targets,
        SimpleCameraShake cameraShake,
        ParticleSystem dust)
    {
        GameObject controllerObject = new("Vertical Slice Stage Controller");
        VerticalSliceStageController controller = controllerObject.AddComponent<VerticalSliceStageController>();
        StageHudView hud = controller.GetComponent<StageHudView>();
        StageFeedbackController feedback = controller.GetComponent<StageFeedbackController>();
        StageHudFactory.UiReferences ui = StageHudFactory.CreateUi(controller);

        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        serializedController.FindProperty("hud").objectReferenceValue = hud;
        serializedController.FindProperty("feedback").objectReferenceValue = feedback;
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 01";
        serializedController.FindProperty("startingShots").intValue = 3;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 2.75f;
        serializedController.FindProperty("postClearPresentationDelay").floatValue = 0f;
        serializedController.FindProperty("nextSceneName").stringValue = "VerticalSlice_Stage02";
        serializedController.FindProperty("projectileSpeedMultiplier").floatValue = 1f;
        serializedController.FindProperty("projectileMassMultiplier").floatValue = 1f;
        serializedController.FindProperty("launcher").objectReferenceValue = launcher;
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        serializedHud.FindProperty("stageLabelText").objectReferenceValue = ui.StageLabel;
        serializedHud.FindProperty("shotsText").objectReferenceValue = ui.Shots;
        serializedHud.FindProperty("destructionText").objectReferenceValue = ui.Destruction;
        serializedHud.FindProperty("endPanel").objectReferenceValue = ui.EndPanel;
        serializedHud.FindProperty("resultTitleText").objectReferenceValue = ui.ResultTitle;
        serializedHud.FindProperty("resultDetailsText").objectReferenceValue = ui.ResultDetails;
        serializedHud.FindProperty("nextButton").objectReferenceValue = ui.NextButton;
        serializedFeedback.FindProperty("cameraShake").objectReferenceValue = cameraShake;
        serializedFeedback.FindProperty("impactDust").objectReferenceValue = dust;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(ui.RetryButton.onClick, controller.RetryStage);
        UnityEventTools.AddPersistentListener(ui.NextButton.onClick, controller.LoadNextStage);
        return controller;
    }

    private static ParticleSystem CreateImpactDust(Material material)
    {
        GameObject dustObject = new("Impact Dust");
        ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 2.2f;
        main.startSize = 0.22f;
        main.startColor = new Color(0.64f, 0.55f, 0.44f, 0.85f);
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static void ConfigureCameraAndLauncher(Camera camera, LauncherController launcher)
    {
        camera.transform.position = new Vector3(9f, 7f, -14f);
        camera.transform.LookAt(new Vector3(0f, 2.8f, 4f));
        camera.fieldOfView = 52f;
        launcher.transform.position = new Vector3(0f, 0f, -8f);
        launcher.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        var serializedLauncher = new SerializedObject(launcher);
        serializedLauncher.FindProperty("projectileSpeed").floatValue = 18f;
        serializedLauncher.FindProperty("projectileMass").floatValue = 2f;
        serializedLauncher.FindProperty("initialPitchAngle").floatValue = 18f;
        serializedLauncher.FindProperty("yawLimits").vector2Value = new Vector2(-28f, 28f);
        serializedLauncher.FindProperty("pitchLimits").vector2Value = new Vector2(2f, 30f);
        serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StageMaterials CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "VerticalSlice");
        }

        return new StageMaterials(
            CreateMaterial("Concrete", new Color(0.48f, 0.52f, 0.56f), "Universal Render Pipeline/Lit"),
            CreateMaterial("Weak", new Color(1f, 0.48f, 0.06f), "Universal Render Pipeline/Lit"),
            CreateMaterial("Reinforced", new Color(0.14f, 0.38f, 0.58f), "Universal Render Pipeline/Lit"),
            CreateMaterial("Decor", new Color(0.35f, 0.24f, 0.14f), "Universal Render Pipeline/Lit"),
            CreateMaterial("Broken", new Color(0.2f, 0.08f, 0.08f), "Universal Render Pipeline/Lit"),
            CreateMaterial("Dust", new Color(0.64f, 0.55f, 0.44f, 0.8f), "Universal Render Pipeline/Particles/Unlit"));
    }

    private static Material CreateMaterial(string name, Color color, string shaderName)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new UnityException($"Missing shader '{shaderName}' for vertical-slice material.");
        }

        material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void RemoveBaselineBlocks()
    {
        foreach (GameObject destructible in GameObject.FindGameObjectsWithTag("Destructible"))
        {
            Object.DestroyImmediate(destructible);
        }
    }

    private static void ConfigureLegacyHud()
    {
        ResetController resetController = Object.FindAnyObjectByType<ResetController>();
        if (resetController == null)
        {
            throw new UnityException("Vertical-slice source scene is missing the reset controller.");
        }

        var serializedReset = new SerializedObject(resetController);
        serializedReset.FindProperty("showControls").boolValue = false;
        serializedReset.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignObjectList<T>(SerializedProperty property, IReadOnlyList<T> values)
        where T : Object
    {
        property.arraySize = values.Count;
        for (int index = 0; index < values.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct StageMaterials
    {
        public StageMaterials(
            Material concrete,
            Material weak,
            Material reinforced,
            Material decor,
            Material broken,
            Material dust)
        {
            Concrete = concrete;
            Weak = weak;
            Reinforced = reinforced;
            Decor = decor;
            Broken = broken;
            Dust = dust;
        }

        public Material Concrete { get; }
        public Material Weak { get; }
        public Material Reinforced { get; }
        public Material Decor { get; }
        public Material Broken { get; }
        public Material Dust { get; }
    }
}
