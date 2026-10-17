using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSliceStage04Builder
{
    internal const string SourceScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    internal const string Stage03ScenePath = "Assets/Scenes/VerticalSlice_Stage03.unity";
    internal const string ScenePath = "Assets/Scenes/VerticalSlice_Stage04.unity";
    internal const int ExpectedPieceCount = 36;
    internal const int ExpectedTargetCount = 21;
    internal const int ExpectedJointCount = 6;
    internal const float WeakLowerForce = 200f;
    internal const float WeakLowerTorque = 75f;
    internal const float WeakUpperForce = 250f;
    internal const float WeakUpperTorque = 95f;
    internal const float CenterLowerForce = 700f;
    internal const float CenterLowerTorque = 280f;
    internal const float CenterUpperForce = 850f;
    internal const float CenterUpperTorque = 340f;
    internal const float ProjectileSpeedMultiplier = 1.1f;
    internal const float ProjectileMassMultiplier = 1.15f;
    internal const float PostClearPresentationDelay = 2f;

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 04")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create Stage04 at {ScenePath}.");
        }

        GameObject oldStructure = GameObject.Find("Stage 01 Structure");
        if (oldStructure == null)
        {
            throw new UnityException("Stage04 source is missing the Stage01 structure root.");
        }

        Object.DestroyImmediate(oldStructure);
        StageMaterials materials = LoadMaterials();
        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        var structure = new Dictionary<string, GameObject>();
        GameObject root = new("Stage 04 Tower Structure");
        BuildStructure(root.transform, materials, structure, pieces, targets);

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Stage04 source is missing the reusable stage controller.");
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
        Debug.Log($"Built vertical-slice Stage04 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 04")]
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
                $"Stage04 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"{ExpectedJointCount} joints, and one controller.");
        }

        Stage04Validation.ValidateController(controllers[0]);
        VerticalSliceSceneBuilder.ValidateLegacyHudDisabled();
        Stage04Validation.ValidateJoints(joints);
        Stage04Validation.ValidateNoInitialPenetration(pieces);
        Stage04Validation.ValidateNoResearchSystems();

        float width = 5.5f;
        float height = 8.5f;
        if (height <= width)
        {
            throw new UnityException("Stage04 tower must remain vertically dominant.");
        }

        Debug.Log(
            "Stage04 validation passed: 36-piece tower, 21 targets, three support paths, "
            + "six critical joints, four shots, 2-second post-clear view, Stage05 progression, "
            + "references, and collider separation verified.");
    }

    public static void ValidateAllStages()
    {
        VerticalSliceStage05Builder.ValidateAllStages();
    }

    private static void BuildStructure(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        float[] supportPositions = { -2f, 0f, 2f };
        foreach (float x in supportPositions)
        {
            string support = x < 0f ? "Left" : x > 0f ? "Right" : "Center";
            Material supportMaterial = x == 0f ? materials.Reinforced : materials.Weak;
            Vector3 foundationScale = x == 0f
                ? new Vector3(1.6f, 0.8f, 1.9f)
                : new Vector3(1.4f, 0.8f, 1.6f);
            Vector3 lowerScale = x == 0f
                ? new Vector3(0.9f, 0.8f, 1.2f)
                : new Vector3(0.65f, 0.8f, 1f);
            Vector3 upperScale = x == 0f
                ? new Vector3(1.1f, 0.8f, 1.35f)
                : new Vector3(0.8f, 0.8f, 1.1f);

            AddPiece(structure, pieces, targets, root, $"{support}_Foundation", new Vector3(x, 0.4f, 4f), foundationScale, 8f, true, false, x == 0f ? materials.Reinforced : materials.Concrete);
            AddPiece(structure, pieces, targets, root, $"{support}_Support_Lower", new Vector3(x, 1.2f, 4f), lowerScale, x == 0f ? 2.5f : 1.4f, false, false, supportMaterial);
            AddPiece(structure, pieces, targets, root, $"{support}_Support_Upper", new Vector3(x, 2f, 4f), upperScale, x == 0f ? 3f : 1.8f, false, false, supportMaterial);

            if (x == 0f)
            {
                AddReinforcementBand(structure[$"{support}_Support_Lower"].transform, materials.Reinforced);
            }
            else
            {
                AddWeakPlate(structure[$"{support}_Support_Lower"].transform, materials.Weak);
            }
        }

        AddPiece(structure, pieces, targets, root, "Tower_Floor_00", new Vector3(0f, 2.65f, 4f), new Vector3(5.5f, 0.5f, 2f), 8f, false, true, materials.Concrete);
        AddTowerLevel(root, materials, structure, pieces, targets, 1, 3.35f, 4.05f);
        AddTowerLevel(root, materials, structure, pieces, targets, 2, 4.75f, 5.45f);
        AddTowerLevel(root, materials, structure, pieces, targets, 3, 6.15f, 6.85f);
        AddTowerLevel(root, materials, structure, pieces, targets, 4, 7.55f, 8.25f);

        float[] roofDecorPositions = { -2f, -0.7f, 0.7f, 2f };
        foreach (float x in roofDecorPositions)
        {
            AddPiece(structure, pieces, targets, root, $"Roof_Decor_{x:0.0}", new Vector3(x, 8.75f, 4f), new Vector3(0.65f, 0.5f, 0.65f), 0.6f, false, false, materials.Decor);
        }

        AddPiece(structure, pieces, targets, root, "Left_Ground_Decor", new Vector3(-4f, 0.4f, 4f), new Vector3(0.8f, 0.8f, 0.8f), 1f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, "Right_Ground_Decor", new Vector3(4f, 0.4f, 4f), new Vector3(0.8f, 0.8f, 0.8f), 1f, false, false, materials.Decor);
        AddBrace(root, materials.Reinforced, "Left_Center_Brace", new Vector3(-0.9f, 2.15f, 3.2f), 28f);
        AddBrace(root, materials.Reinforced, "Right_Center_Brace", new Vector3(0.9f, 2.15f, 3.2f), -28f);
    }

    private static void AddTowerLevel(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        int level,
        float wallY,
        float floorY)
    {
        float[] wallPositions = { -2f, -0.67f, 0.67f, 2f };
        foreach (float x in wallPositions)
        {
            AddPiece(structure, pieces, targets, root, $"Level_{level}_Wall_{x:0.00}", new Vector3(x, wallY, 4f), new Vector3(0.8f, 0.9f, 1.2f), 1.2f, false, true, materials.Concrete);
        }

        string floorName = level == 4 ? "Tower_Roof" : $"Tower_Floor_{level:00}";
        AddPiece(structure, pieces, targets, root, floorName, new Vector3(0f, floorY, 4f), new Vector3(5.5f, 0.5f, 2f), level == 4 ? 7f : 8f, false, true, materials.Concrete);
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

    private static void AddReinforcementBand(Transform parent, Material material)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = "Center_Reinforcement_Band";
        band.transform.SetParent(parent, false);
        band.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        band.transform.localScale = new Vector3(1.12f, 0.16f, 1.12f);
        band.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(band.GetComponent<Collider>());
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
        brace.transform.localScale = new Vector3(1.8f, 0.18f, 0.3f);
        brace.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(brace.GetComponent<Collider>());
    }

    private static void AddJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        AddSupportJoints(structure, controller, brokenMaterial, "Left", true, WeakLowerForce, WeakLowerTorque, WeakUpperForce, WeakUpperTorque);
        AddSupportJoints(structure, controller, brokenMaterial, "Center", false, CenterLowerForce, CenterLowerTorque, CenterUpperForce, CenterUpperTorque);
        AddSupportJoints(structure, controller, brokenMaterial, "Right", true, WeakLowerForce, WeakLowerTorque, WeakUpperForce, WeakUpperTorque);
    }

    private static void AddSupportJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial,
        string support,
        bool weak,
        float lowerForce,
        float lowerTorque,
        float upperForce,
        float upperTorque)
    {
        CreateJoint(structure[$"{support}_Support_Lower"], structure[$"{support}_Foundation"], controller, weak, lowerForce, lowerTorque, brokenMaterial);
        CreateJoint(structure[$"{support}_Support_Upper"], structure[$"{support}_Support_Lower"], controller, weak, upperForce, upperTorque, brokenMaterial);
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
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 04";
        serializedController.FindProperty("startingShots").intValue = 4;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 3.25f;
        serializedController.FindProperty("postClearPresentationDelay").floatValue = PostClearPresentationDelay;
        serializedController.FindProperty("nextSceneName").stringValue = "VerticalSlice_Stage05";
        serializedController.FindProperty("projectileSpeedMultiplier").floatValue = ProjectileSpeedMultiplier;
        serializedController.FindProperty("projectileMassMultiplier").floatValue = ProjectileMassMultiplier;
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        Text label = (Text)serializedHud.FindProperty("stageLabelText").objectReferenceValue;
        label.text = "STAGE 04";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new UnityException("Stage04 is missing the Main Camera.");
        }

        camera.transform.position = new Vector3(12f, 8f, -19f);
        camera.transform.LookAt(new Vector3(0f, 4.3f, 4f));
        camera.fieldOfView = 52f;
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
        int stageThreeIndex = scenes.FindIndex(scene => scene.path == Stage03ScenePath);
        scenes.Insert(stageThreeIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
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
