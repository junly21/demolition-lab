using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSliceStage05Builder
{
    internal const string SourceScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    internal const string Stage02ScenePath = "Assets/Scenes/VerticalSlice_Stage02.unity";
    internal const string Stage03ScenePath = "Assets/Scenes/VerticalSlice_Stage03.unity";
    internal const string Stage04ScenePath = "Assets/Scenes/VerticalSlice_Stage04.unity";
    internal const string ScenePath = "Assets/Scenes/VerticalSlice_Stage05.unity";
    internal const int ExpectedPieceCount = 45;
    internal const int ExpectedTargetCount = 15;
    internal const int ExpectedJointCount = 10;
    internal const float WeakLowerForce = 300f;
    internal const float WeakLowerTorque = 110f;
    internal const float WeakUpperForce = 350f;
    internal const float WeakUpperTorque = 140f;
    internal const float ReinforcedForce = 1000f;
    internal const float ReinforcedTorque = 420f;
    internal const float ProjectileSpeedMultiplier = 1.1f;
    internal const float ProjectileMassMultiplier = 1.15f;

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 05")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create Stage05 at {ScenePath}.");
        }

        GameObject oldStructure = GameObject.Find("Stage 01 Structure");
        if (oldStructure == null)
        {
            throw new UnityException("Stage05 source is missing the Stage01 structure root.");
        }

        Object.DestroyImmediate(oldStructure);
        StageMaterials materials = LoadMaterials();
        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        var structure = new Dictionary<string, GameObject>();
        GameObject root = new("Stage 05 Industrial Gantry Structure");
        BuildStructure(root.transform, materials, structure, pieces, targets);

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Stage05 source is missing the reusable stage controller.");
        }

        ConfigureController(controller, targets);
        AddJoints(structure, controller, materials.Broken);
        ConfigureCamera();
        VerticalSlicePresentationBuilder.ApplyEnvironmentToOpenScene();
        VerticalSlicePresentationBuilder.ApplyStructuralPresentationToOpenScene();
        VerticalSlicePresentationBuilder.ApplyCameraFramingToOpenScene();
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Built vertical-slice Stage05 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 05")]
    public static void ValidateScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("Destructible");
        StageTargetPiece[] targets = Object.FindObjectsByType<StageTargetPiece>();
        CriticalSupportJoint[] joints = Object.FindObjectsByType<CriticalSupportJoint>();
        VerticalSliceStageController[] controllers = Object.FindObjectsByType<VerticalSliceStageController>();

        if (pieces.Length != ExpectedPieceCount
            || targets.Length != ExpectedTargetCount
            || joints.Length != ExpectedJointCount
            || controllers.Length != 1)
        {
            throw new UnityException(
                $"Stage05 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"{ExpectedJointCount} joints, and one controller.");
        }

        Stage05Validation.ValidateController(controllers[0]);
        VerticalSliceSceneBuilder.ValidateLegacyHudDisabled();
        Stage05Validation.ValidateTargets(targets);
        Stage05Validation.ValidateJoints(joints);
        Stage05Validation.ValidateNoInitialPenetration(pieces);
        Stage05Validation.ValidateNoResearchSystems();
        Stage05Validation.ValidateIdleStability();

        Debug.Log(
            "Stage05 validation passed: 45-piece asymmetric industrial frame, 15 targets, "
            + "10 critical joints, four shots, hidden Next, startup 0%, references, "
            + "10-second idle stability, and collider separation verified.");
    }

    [MenuItem("DemolitionLab/Validate All Vertical Slice Stages 01-05")]
    public static void ValidateAllStages()
    {
        VerticalSliceSceneBuilder.ValidateScene();
        VerticalSliceStage02Builder.ValidateScene();
        VerticalSliceStage03Builder.ValidateScene();
        VerticalSliceStage04Builder.ValidateScene();
        ValidateScene();

        var enabledScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                enabledScenes.Add(scene.path);
            }
        }

        int stageOneIndex = enabledScenes.IndexOf(SourceScenePath);
        int stageTwoIndex = enabledScenes.IndexOf(Stage02ScenePath);
        int stageThreeIndex = enabledScenes.IndexOf(Stage03ScenePath);
        int stageFourIndex = enabledScenes.IndexOf(Stage04ScenePath);
        int stageFiveIndex = enabledScenes.IndexOf(ScenePath);
        if (stageOneIndex < 0
            || stageTwoIndex != stageOneIndex + 1
            || stageThreeIndex != stageTwoIndex + 1
            || stageFourIndex != stageThreeIndex + 1
            || stageFiveIndex != stageFourIndex + 1)
        {
            throw new UnityException(
                "Vertical-slice build order must be Stage01 -> Stage02 -> Stage03 -> Stage04 -> Stage05.");
        }

        Debug.Log("All five vertical-slice stages and progression validated.");
    }

    private static void BuildStructure(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        AddPrimarySupport(root, materials, structure, pieces, targets);
        AddWeakSupport(root, materials, structure, pieces, targets, "Transfer", -0.6f);
        AddWeakSupport(root, materials, structure, pieces, targets, "Secondary", 1.9f);
        AddWeakSupport(root, materials, structure, pieces, targets, "Remote", 5.2f);

        AddFrameSection(
            root,
            materials,
            structure,
            pieces,
            targets,
            "Left",
            -2.075f,
            4.45f,
            -2.075f,
            4.45f,
            new[] { -3.45f, -2.05f, -0.65f },
            new[] { -3.7f, -2.6f, -1.5f, -0.4f });
        AddFrameSection(
            root,
            materials,
            structure,
            pieces,
            targets,
            "Middle",
            1.825f,
            3.25f,
            1.825f,
            3.25f,
            new[] { 0.65f, 1.825f, 3f },
            new[] { 0.55f, 1.4f, 2.25f, 3.1f });
        AddFrameSection(
            root,
            materials,
            structure,
            pieces,
            targets,
            "Right",
            5.2f,
            3.4f,
            5.6f,
            4.2f,
            new[] { 3.95f, 5.2f, 6.45f },
            new[] { 4.15f, 5.15f, 6.15f, 7.15f });

        AddGroundDecor(root, materials, structure, pieces, targets, "Ground_Decor_Left", -5.4f);
        AddGroundDecor(root, materials, structure, pieces, targets, "Ground_Decor_Inner_Left", 0.65f);
        AddGroundDecor(root, materials, structure, pieces, targets, "Ground_Decor_Inner_Right", 3.25f);
        AddGroundDecor(root, materials, structure, pieces, targets, "Ground_Decor_Right", 7.2f);

        AddVisualBrace(root, materials.Reinforced, "Primary_Brace_Left", new Vector3(-3.65f, 3.25f, 3.2f), 28f, new Vector3(2.5f, 0.22f, 0.32f));
        AddVisualBrace(root, materials.Reinforced, "Primary_Brace_Right", new Vector3(-2.25f, 3.25f, 3.2f), -28f, new Vector3(2.5f, 0.22f, 0.32f));
        AddVisualCable(root, materials.Reinforced, "Cantilever_Cable", new Vector3(7.3f, 5.2f, 3.15f), new Vector3(0.12f, 2f, 0.12f));
        AddVisualCable(root, materials.Decor, "Cantilever_Hook", new Vector3(7.3f, 4.15f, 3.15f), new Vector3(0.5f, 0.25f, 0.5f));
    }

    private static void AddPrimarySupport(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        AddPiece(structure, pieces, targets, root, "Primary_Foundation", new Vector3(-3f, 0.4f, 4f), new Vector3(2.2f, 0.8f, 2.2f), 14f, true, false, materials.Reinforced);
        string[] levels = { "Lower", "Mid_Lower", "Mid_Upper", "Upper" };
        for (int index = 0; index < levels.Length; index++)
        {
            GameObject support = AddPiece(
                structure,
                pieces,
                targets,
                root,
                $"Primary_{levels[index]}",
                new Vector3(-3f, 1.2f + index * 0.8f, 4f),
                new Vector3(1.5f, 0.8f, 1.7f),
                4f,
                false,
                false,
                materials.Reinforced);
            if (index == 0 || index == levels.Length - 1)
            {
                AddReinforcementBand(support.transform, materials.Reinforced, $"{levels[index]}_Band");
            }
        }
    }

    private static void AddWeakSupport(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        string supportName,
        float positionX)
    {
        AddPiece(structure, pieces, targets, root, $"{supportName}_Foundation", new Vector3(positionX, 0.4f, 4f), new Vector3(1.3f, 0.8f, 1.7f), 8f, true, false, materials.Concrete);
        GameObject lower = AddPiece(structure, pieces, targets, root, $"{supportName}_Weak_Lower", new Vector3(positionX, 1.6f, 4f), new Vector3(0.65f, 1.6f, 1f), 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, $"{supportName}_Weak_Upper", new Vector3(positionX, 3.2f, 4f), new Vector3(0.85f, 1.6f, 1.15f), 2.5f, false, false, materials.Weak);
        AddWeakPlate(lower.transform, materials.Weak);
    }

    private static void AddFrameSection(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        string sectionName,
        float platformCenterX,
        float platformWidth,
        float roofCenterX,
        float roofWidth,
        IReadOnlyList<float> uprightPositions,
        IReadOnlyList<float> decorPositions)
    {
        AddPiece(structure, pieces, targets, root, $"{sectionName}_Platform", new Vector3(platformCenterX, 4.25f, 4f), new Vector3(platformWidth, 0.5f, 2f), 5f, false, true, materials.Concrete);
        foreach (float positionX in uprightPositions)
        {
            AddPiece(structure, pieces, targets, root, $"{sectionName}_Frame_{positionX:0.00}", new Vector3(positionX, 5.1f, 4f), new Vector3(0.65f, 1.2f, 1.25f), 0.8f, false, true, materials.Concrete);
        }

        AddPiece(structure, pieces, targets, root, $"{sectionName}_Upper_Beam", new Vector3(roofCenterX, 5.95f, 4f), new Vector3(roofWidth, 0.5f, 2f), 4f, false, true, materials.Concrete);
        foreach (float positionX in decorPositions)
        {
            AddPiece(structure, pieces, targets, root, $"{sectionName}_Machinery_{positionX:0.00}", new Vector3(positionX, 6.45f, 4f), new Vector3(0.55f, 0.5f, 0.65f), 0.4f, false, false, materials.Decor);
        }
    }

    private static void AddGroundDecor(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        string name,
        float positionX)
    {
        AddPiece(structure, pieces, targets, root, name, new Vector3(positionX, 0.35f, 4f), new Vector3(0.7f, 0.7f, 0.7f), 0.8f, false, false, materials.Decor);
    }

    private static GameObject AddPiece(
        IDictionary<string, GameObject> structure,
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

        structure.Add(name, piece);
        pieces.Add(piece);
        return piece;
    }

    private static void AddWeakPlate(Transform parent, Material material)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Exposed_Weak_Plate";
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = new Vector3(0f, 0.25f, -0.54f);
        plate.transform.localScale = new Vector3(1.35f, 0.14f, 0.12f);
        plate.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(plate.GetComponent<Collider>());
    }

    private static void AddReinforcementBand(
        Transform parent,
        Material material,
        string name)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = name;
        band.transform.SetParent(parent, false);
        band.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        band.transform.localScale = new Vector3(1.12f, 0.16f, 1.12f);
        band.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(band.GetComponent<Collider>());
    }

    private static void AddVisualBrace(
        Transform root,
        Material material,
        string name,
        Vector3 position,
        float zRotation,
        Vector3 scale)
    {
        GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        brace.name = name;
        brace.transform.SetParent(root, false);
        brace.transform.position = position;
        brace.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        brace.transform.localScale = scale;
        brace.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(brace.GetComponent<Collider>());
    }

    private static void AddVisualCable(
        Transform root,
        Material material,
        string name,
        Vector3 position,
        Vector3 scale)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = name;
        visual.transform.SetParent(root, false);
        visual.transform.position = position;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
    }

    private static void AddJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        CreateJoint(structure["Primary_Lower"], structure["Primary_Foundation"], controller, false, ReinforcedForce, ReinforcedTorque, brokenMaterial);
        CreateJoint(structure["Primary_Mid_Lower"], structure["Primary_Lower"], controller, false, ReinforcedForce, ReinforcedTorque, brokenMaterial);
        CreateJoint(structure["Primary_Mid_Upper"], structure["Primary_Mid_Lower"], controller, false, ReinforcedForce, ReinforcedTorque, brokenMaterial);
        CreateJoint(structure["Primary_Upper"], structure["Primary_Mid_Upper"], controller, false, ReinforcedForce, ReinforcedTorque, brokenMaterial);
        AddWeakSupportJoints(structure, controller, brokenMaterial, "Transfer");
        AddWeakSupportJoints(structure, controller, brokenMaterial, "Secondary");
        AddWeakSupportJoints(structure, controller, brokenMaterial, "Remote");
    }

    private static void AddWeakSupportJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial,
        string supportName)
    {
        CreateJoint(structure[$"{supportName}_Weak_Lower"], structure[$"{supportName}_Foundation"], controller, true, WeakLowerForce, WeakLowerTorque, brokenMaterial);
        CreateJoint(structure[$"{supportName}_Weak_Upper"], structure[$"{supportName}_Weak_Lower"], controller, true, WeakUpperForce, WeakUpperTorque, brokenMaterial);
    }

    private static void CreateJoint(
        GameObject owner,
        GameObject connected,
        VerticalSliceStageController controller,
        bool weak,
        float breakForce,
        float breakTorque,
        Material brokenMaterial)
    {
        FixedJoint joint = owner.AddComponent<FixedJoint>();
        joint.connectedBody = connected.GetComponent<Rigidbody>();
        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;
        joint.enableCollision = false;

        CriticalSupportJoint supportJoint = owner.AddComponent<CriticalSupportJoint>();
        var serializedJoint = new SerializedObject(supportJoint);
        serializedJoint.FindProperty("physicalJoint").objectReferenceValue = joint;
        serializedJoint.FindProperty("stageController").objectReferenceValue = controller;
        serializedJoint.FindProperty("weakConnection").boolValue = weak;
        serializedJoint.FindProperty("indicatorRenderer").objectReferenceValue = owner.GetComponent<Renderer>();
        serializedJoint.FindProperty("brokenMaterial").objectReferenceValue = brokenMaterial;
        serializedJoint.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureController(
        VerticalSliceStageController controller,
        IReadOnlyList<StageTargetPiece> targets)
    {
        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 05";
        serializedController.FindProperty("startingShots").intValue = 4;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 3.5f;
        serializedController.FindProperty("postClearPresentationDelay").floatValue = 0f;
        serializedController.FindProperty("nextSceneName").stringValue = string.Empty;
        serializedController.FindProperty("projectileSpeedMultiplier").floatValue = ProjectileSpeedMultiplier;
        serializedController.FindProperty("projectileMassMultiplier").floatValue = ProjectileMassMultiplier;
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        Text label = (Text)serializedHud.FindProperty("stageLabelText").objectReferenceValue;
        label.text = "STAGE 05";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new UnityException("Stage05 is missing the Main Camera.");
        }

        camera.transform.position = new Vector3(14f, 8f, -21f);
        camera.transform.LookAt(new Vector3(1f, 3.6f, 4f));
        camera.fieldOfView = 54f;
    }

    private static StageMaterials LoadMaterials()
    {
        return new StageMaterials(
            LoadMaterial("Concrete"),
            LoadMaterial("Weak"),
            LoadMaterial("Reinforced"),
            LoadMaterial("Decor"),
            LoadMaterial("Broken"));
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            $"Assets/Materials/VerticalSlice/{name}.mat");
        if (material == null)
        {
            throw new UnityException($"Missing vertical-slice material '{name}'.");
        }

        return material;
    }

    private static void AssignObjectList<T>(
        SerializedProperty property,
        IReadOnlyList<T> values)
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
        int stageFourIndex = scenes.FindIndex(scene => scene.path == Stage04ScenePath);
        scenes.Insert(stageFourIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct StageMaterials
    {
        public StageMaterials(
            Material concrete,
            Material weak,
            Material reinforced,
            Material decor,
            Material broken)
        {
            Concrete = concrete;
            Weak = weak;
            Reinforced = reinforced;
            Decor = decor;
            Broken = broken;
        }

        public Material Concrete { get; }
        public Material Weak { get; }
        public Material Reinforced { get; }
        public Material Decor { get; }
        public Material Broken { get; }
    }
}
