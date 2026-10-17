using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSliceStage02Builder
{
    internal const string SourceScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    internal const string ScenePath = "Assets/Scenes/VerticalSlice_Stage02.unity";
    internal const int ExpectedPieceCount = 24;
    internal const int ExpectedTargetCount = 12;
    internal const int ExpectedJointCount = 4;
    internal const float LowerBreakForce = 240f;
    internal const float LowerBreakTorque = 95f;
    internal const float UpperBreakForce = 300f;
    internal const float UpperBreakTorque = 120f;

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 02")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create Stage02 at {ScenePath}.");
        }

        GameObject oldStructure = GameObject.Find("Stage 01 Structure");
        if (oldStructure == null)
        {
            throw new UnityException("Stage02 source is missing the Stage01 structure root.");
        }

        Object.DestroyImmediate(oldStructure);
        StageMaterials materials = LoadMaterials();
        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        var structure = new Dictionary<string, GameObject>();
        GameObject root = new("Stage 02 Bridge Structure");
        BuildStructure(root.transform, materials, structure, pieces, targets);

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Stage02 source is missing the reusable stage controller.");
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
        Debug.Log($"Built vertical-slice Stage02 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 02")]
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
                $"Stage02 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"{ExpectedJointCount} joints, and one controller.");
        }

        Stage02Validation.ValidateController(controllers[0]);
        VerticalSliceSceneBuilder.ValidateLegacyHudDisabled();
        Stage02Validation.ValidateJoints(joints);
        Stage02Validation.ValidateNoInitialPenetration(pieces);
        Stage02Validation.ValidateNoResearchSystems();
        Stage02Validation.ValidateIdleStability();

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

        if (leftTargets != 6 || rightTargets != 6)
        {
            throw new UnityException(
                $"Stage02 must split its objective evenly, found {leftTargets}/{rightTargets}.");
        }

        Debug.Log(
            "Stage02 validation passed: 24-piece bridge, two six-target deck halves, four critical joints, "
            + "three shots, Stage03 progression, references, and collider separation verified.");
    }

    private static void BuildStructure(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        AddPiece(structure, pieces, targets, root, "Left_Foundation", new Vector3(-2f, 0.4f, 4f), new Vector3(1.6f, 0.8f, 1.8f), 8f, true, false, materials.Concrete);
        AddPiece(structure, pieces, targets, root, "Right_Foundation", new Vector3(2f, 0.4f, 4f), new Vector3(1.6f, 0.8f, 1.8f), 8f, true, false, materials.Concrete);
        AddPiece(structure, pieces, targets, root, "Left_Weak_Lower", new Vector3(-2f, 1.2f, 4f), new Vector3(0.65f, 0.8f, 1f), 1.4f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Left_Weak_Upper", new Vector3(-2f, 2f, 4f), new Vector3(0.85f, 0.8f, 1.15f), 1.8f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Lower", new Vector3(2f, 1.2f, 4f), new Vector3(0.65f, 0.8f, 1f), 1.4f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Upper", new Vector3(2f, 2f, 4f), new Vector3(0.85f, 0.8f, 1.15f), 1.8f, false, false, materials.Weak);
        AddWeakPlate(structure["Left_Weak_Lower"].transform, materials.Weak);
        AddWeakPlate(structure["Right_Weak_Lower"].transform, materials.Weak);

        AddBridgeHalf(root, materials, structure, pieces, targets, "Left", -2f, new[] { -3.5f, -2.5f, -1.5f, -0.5f });
        AddBridgeHalf(root, materials, structure, pieces, targets, "Right", 2f, new[] { 0.5f, 1.5f, 2.5f, 3.5f });

        AddPiece(structure, pieces, targets, root, "Left_Ground_Decor", new Vector3(-5.8f, 0.4f, 4f), new Vector3(0.8f, 0.8f, 0.8f), 1f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, "Right_Ground_Decor", new Vector3(5.8f, 0.4f, 4f), new Vector3(0.8f, 0.8f, 0.8f), 1f, false, false, materials.Decor);
    }

    private static void AddBridgeHalf(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        string side,
        float centerX,
        IReadOnlyList<float> barrierPositions)
    {
        AddPiece(structure, pieces, targets, root, $"{side}_Deck", new Vector3(centerX, 2.65f, 4f), new Vector3(4f, 0.5f, 1.8f), 6f, false, true, materials.Concrete);
        AddPiece(structure, pieces, targets, root, $"{side}_Back_Rail", new Vector3(centerX, 3.075f, 4.75f), new Vector3(4f, 0.35f, 0.3f), 1.5f, false, true, materials.Concrete);
        foreach (float barrierX in barrierPositions)
        {
            AddPiece(structure, pieces, targets, root, $"{side}_Barrier_{barrierX:0.00}", new Vector3(barrierX, 3.125f, 3.3f), new Vector3(0.65f, 0.45f, 0.35f), 0.7f, false, true, materials.Concrete);
        }

        float direction = side == "Left" ? -1f : 1f;
        AddPiece(structure, pieces, targets, root, $"{side}_Approach_Decor", new Vector3(direction * 4.7f, 0.3f, 3.2f), new Vector3(0.6f, 0.6f, 0.6f), 0.6f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, $"{side}_Underpass_Decor", new Vector3(direction * 3.6f, 0.35f, 4.2f), new Vector3(0.7f, 0.7f, 0.7f), 0.7f, false, false, materials.Decor);
    }

    private static void AddWeakPlate(Transform parent, Material material)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Exposed_Weak_Plate";
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = new Vector3(0f, 0.28f, -0.54f);
        plate.transform.localScale = new Vector3(1.25f, 0.16f, 0.12f);
        plate.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(plate.GetComponent<Collider>());
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new UnityException("Stage02 is missing the Main Camera.");
        }

        camera.transform.position = new Vector3(10.5f, 5.8f, -15.5f);
        camera.transform.LookAt(new Vector3(0f, 2f, 4f));
        camera.fieldOfView = 54f;
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

    private static void AddJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        CreateJoint(structure["Left_Weak_Lower"], structure["Left_Foundation"], controller, LowerBreakForce, LowerBreakTorque, brokenMaterial);
        CreateJoint(structure["Left_Weak_Upper"], structure["Left_Weak_Lower"], controller, UpperBreakForce, UpperBreakTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Lower"], structure["Right_Foundation"], controller, LowerBreakForce, LowerBreakTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Upper"], structure["Right_Weak_Lower"], controller, UpperBreakForce, UpperBreakTorque, brokenMaterial);
    }

    private static void CreateJoint(
        GameObject owner,
        GameObject connected,
        VerticalSliceStageController controller,
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
        serializedJoint.FindProperty("weakConnection").boolValue = true;
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
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 02";
        serializedController.FindProperty("startingShots").intValue = 3;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 2.75f;
        serializedController.FindProperty("postClearPresentationDelay").floatValue = 0f;
        serializedController.FindProperty("nextSceneName").stringValue = "VerticalSlice_Stage03";
        serializedController.FindProperty("projectileSpeedMultiplier").floatValue = 1f;
        serializedController.FindProperty("projectileMassMultiplier").floatValue = 1f;
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        Text label = (Text)serializedHud.FindProperty("stageLabelText").objectReferenceValue;
        label.text = "STAGE 02";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StageMaterials LoadMaterials()
    {
        return new StageMaterials(
            LoadMaterial("Concrete"),
            LoadMaterial("Weak"),
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
        int stageOneIndex = scenes.FindIndex(scene => scene.path == SourceScenePath);
        scenes.Insert(stageOneIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct StageMaterials
    {
        public StageMaterials(Material concrete, Material weak, Material decor, Material broken)
        {
            Concrete = concrete;
            Weak = weak;
            Decor = decor;
            Broken = broken;
        }

        public Material Concrete { get; }
        public Material Weak { get; }
        public Material Decor { get; }
        public Material Broken { get; }
    }
}
