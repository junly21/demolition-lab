using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSliceStage03Builder
{
    internal const string SourceScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    internal const string Stage02ScenePath = "Assets/Scenes/VerticalSlice_Stage02.unity";
    internal const string ScenePath = "Assets/Scenes/VerticalSlice_Stage03.unity";
    internal const int ExpectedPieceCount = 31;
    internal const int ExpectedTargetCount = 16;
    internal const int ExpectedJointCount = 6;
    internal const float WeakLowerForce = 185f;
    internal const float WeakLowerTorque = 70f;
    internal const float WeakUpperForce = 230f;
    internal const float WeakUpperTorque = 90f;
    internal const float ReinforcedLowerForce = 1200f;
    internal const float ReinforcedLowerTorque = 500f;
    internal const float ReinforcedUpperForce = 1400f;
    internal const float ReinforcedUpperTorque = 600f;

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 03")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create Stage03 at {ScenePath}.");
        }

        GameObject oldStructure = GameObject.Find("Stage 01 Structure");
        if (oldStructure == null)
        {
            throw new UnityException("Stage03 source is missing the Stage01 structure root.");
        }

        Object.DestroyImmediate(oldStructure);
        StageMaterials materials = LoadMaterials();
        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        var structure = new Dictionary<string, GameObject>();
        GameObject root = new("Stage 03 Reinforced Bunker Structure");
        BuildStructure(root.transform, materials, structure, pieces, targets);

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Stage03 source is missing the reusable stage controller.");
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
        Debug.Log($"Built vertical-slice Stage03 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 03")]
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
                $"Stage03 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"{ExpectedJointCount} joints, and one controller.");
        }

        Stage03Validation.ValidateController(controllers[0]);
        VerticalSliceSceneBuilder.ValidateLegacyHudDisabled();
        Stage03Validation.ValidateJoints(joints);
        Stage03Validation.ValidateNoInitialPenetration(pieces);
        Stage03Validation.ValidateNoResearchSystems();

        int leftTargets = 0;
        int rightTargets = 0;
        foreach (StageTargetPiece target in targets)
        {
            if (target.name.StartsWith("Left_"))
            {
                leftTargets++;
            }
            else if (target.name.StartsWith("Right_"))
            {
                rightTargets++;
            }
        }

        if (leftTargets != 8 || rightTargets != 8)
        {
            throw new UnityException(
                $"Stage03 must split its objective evenly, found {leftTargets}/{rightTargets}.");
        }

        Debug.Log(
            "Stage03 validation passed: 31-piece low reinforced bunker, two eight-target halves, four Weak and two "
            + "Reinforced joints, forgiving Weak calibration, four shots, Stage04 progression, references, "
            + "and collider separation verified.");
    }

    [MenuItem("DemolitionLab/Validate All Vertical Slice Stages")]
    public static void ValidateAllStages()
    {
        VerticalSliceStage04Builder.ValidateAllStages();
    }

    private static void BuildStructure(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        AddPiece(structure, pieces, targets, root, "Left_Foundation", new Vector3(-3.8f, 0.4f, 4f), new Vector3(1.6f, 0.8f, 1.8f), 8f, true, false, materials.Concrete);
        AddPiece(structure, pieces, targets, root, "Center_Foundation", new Vector3(0f, 0.4f, 4f), new Vector3(2.4f, 0.8f, 2.1f), 12f, true, false, materials.Reinforced);
        AddPiece(structure, pieces, targets, root, "Right_Foundation", new Vector3(3.8f, 0.4f, 4f), new Vector3(1.6f, 0.8f, 1.8f), 8f, true, false, materials.Concrete);

        AddPiece(structure, pieces, targets, root, "Left_Weak_Lower", new Vector3(-3.8f, 1.2f, 4f), new Vector3(0.8f, 0.8f, 1f), 1.5f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Left_Weak_Upper", new Vector3(-3.8f, 2.25f, 4f), new Vector3(0.95f, 1.3f, 1.15f), 2.1f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Lower", new Vector3(3.8f, 1.2f, 4f), new Vector3(0.8f, 0.8f, 1f), 1.5f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Upper", new Vector3(3.8f, 2.25f, 4f), new Vector3(0.95f, 1.3f, 1.15f), 2.1f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Center_Reinforced_Lower", new Vector3(0f, 1.5f, 4f), new Vector3(1.8f, 1.4f, 1.7f), 5f, false, false, materials.Reinforced);
        AddPiece(structure, pieces, targets, root, "Center_Reinforced_Upper", new Vector3(0f, 2.55f, 4f), new Vector3(1.8f, 0.7f, 1.7f), 4f, false, false, materials.Reinforced);
        AddReinforcementBand(structure["Center_Reinforced_Lower"].transform, materials.Reinforced, "Lower_Band");
        AddReinforcementBand(structure["Center_Reinforced_Upper"].transform, materials.Reinforced, "Upper_Band");
        AddWeakPlate(structure["Left_Weak_Lower"].transform, materials.Weak);
        AddWeakPlate(structure["Right_Weak_Lower"].transform, materials.Weak);
        AddBrace(root, materials.Reinforced, "Left_Center_Brace", new Vector3(-1.25f, 2.15f, 3.25f), 30f);
        AddBrace(root, materials.Reinforced, "Right_Center_Brace", new Vector3(1.25f, 2.15f, 3.25f), -30f);

        AddSide(root, materials, structure, pieces, targets, "Left", -2.25f, new[] { -4.05f, -3.35f, -2.65f, -1.95f, -1.25f, -0.55f });
        AddSide(root, materials, structure, pieces, targets, "Right", 2.25f, new[] { 0.55f, 1.25f, 1.95f, 2.65f, 3.35f, 4.05f });

        AddPiece(structure, pieces, targets, root, "Left_Ground_Decor", new Vector3(-5.3f, 0.4f, 4f), new Vector3(0.8f, 0.8f, 0.8f), 1f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, "Right_Ground_Decor", new Vector3(5.3f, 0.4f, 4f), new Vector3(0.8f, 0.8f, 0.8f), 1f, false, false, materials.Decor);
    }

    private static void AddSide(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        string side,
        float centerX,
        IReadOnlyList<float> wallPositions)
    {
        AddPiece(structure, pieces, targets, root, $"{side}_Platform", new Vector3(centerX, 3.15f, 4f), new Vector3(4.5f, 0.5f, 2f), 7f, false, true, materials.Concrete);
        foreach (float wallX in wallPositions)
        {
            AddPiece(structure, pieces, targets, root, $"{side}_Armor_{wallX:0.00}", new Vector3(wallX, 3.75f, 4f), new Vector3(0.58f, 0.7f, 1.4f), 1f, false, true, materials.Concrete);
        }

        AddPiece(structure, pieces, targets, root, $"{side}_Roof", new Vector3(centerX, 4.35f, 4f), new Vector3(4.5f, 0.5f, 2f), 5f, false, true, materials.Concrete);
        AddPiece(structure, pieces, targets, root, $"{side}_Roof_Decor_A", new Vector3(centerX - 0.75f, 4.8f, 4f), new Vector3(0.6f, 0.4f, 0.6f), 0.5f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, $"{side}_Roof_Decor_B", new Vector3(centerX + 0.75f, 4.8f, 4f), new Vector3(0.6f, 0.4f, 0.6f), 0.5f, false, false, materials.Decor);
    }

    private static void AddPiece(
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
    }

    private static void AddReinforcementBand(Transform parent, Material material, string name)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = name;
        band.transform.SetParent(parent, false);
        band.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        band.transform.localScale = new Vector3(1.12f, 0.16f, 1.12f);
        band.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(band.GetComponent<Collider>());
    }

    private static void AddWeakPlate(Transform parent, Material material)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Exposed_Weak_Plate";
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = new Vector3(0f, 0.28f, -0.54f);
        plate.transform.localScale = new Vector3(1.4f, 0.16f, 0.12f);
        plate.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(plate.GetComponent<Collider>());
    }

    private static void AddBrace(
        Transform root,
        Material material,
        string name,
        Vector3 position,
        float zRotation)
    {
        GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        brace.name = name;
        brace.transform.SetParent(root, false);
        brace.transform.position = position;
        brace.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        brace.transform.localScale = new Vector3(2.4f, 0.22f, 0.3f);
        brace.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(brace.GetComponent<Collider>());
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new UnityException("Stage03 is missing the Main Camera.");
        }

        camera.transform.position = new Vector3(11f, 6.3f, -16.5f);
        camera.transform.LookAt(new Vector3(0f, 2.5f, 4f));
        camera.fieldOfView = 54f;
    }

    private static void AddJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        CreateJoint(structure["Left_Weak_Lower"], structure["Left_Foundation"], controller, true, WeakLowerForce, WeakLowerTorque, brokenMaterial);
        CreateJoint(structure["Left_Weak_Upper"], structure["Left_Weak_Lower"], controller, true, WeakUpperForce, WeakUpperTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Lower"], structure["Right_Foundation"], controller, true, WeakLowerForce, WeakLowerTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Upper"], structure["Right_Weak_Lower"], controller, true, WeakUpperForce, WeakUpperTorque, brokenMaterial);
        CreateJoint(structure["Center_Reinforced_Lower"], structure["Center_Foundation"], controller, false, ReinforcedLowerForce, ReinforcedLowerTorque, brokenMaterial);
        CreateJoint(structure["Center_Reinforced_Upper"], structure["Center_Reinforced_Lower"], controller, false, ReinforcedUpperForce, ReinforcedUpperTorque, brokenMaterial);
    }

    private static void CreateJoint(
        GameObject owner,
        GameObject connected,
        VerticalSliceStageController controller,
        bool isWeak,
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
        serializedJoint.FindProperty("weakConnection").boolValue = isWeak;
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
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 03";
        serializedController.FindProperty("startingShots").intValue = 4;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 2.75f;
        serializedController.FindProperty("postClearPresentationDelay").floatValue = 0f;
        serializedController.FindProperty("nextSceneName").stringValue = "VerticalSlice_Stage04";
        serializedController.FindProperty("projectileSpeedMultiplier").floatValue = 1f;
        serializedController.FindProperty("projectileMassMultiplier").floatValue = 1f;
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        Text label = (Text)serializedHud.FindProperty("stageLabelText").objectReferenceValue;
        label.text = "STAGE 03";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
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
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/VerticalSlice/{name}.mat");
        if (material == null)
        {
            throw new UnityException($"Missing vertical-slice material '{name}'.");
        }

        return material;
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
        int stageTwoIndex = scenes.FindIndex(scene => scene.path == Stage02ScenePath);
        scenes.Insert(stageTwoIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
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
