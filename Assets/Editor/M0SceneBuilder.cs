using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class M0SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/M0_DemolitionLab.unity";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";

    [MenuItem("DemolitionLab/Build M0 Scene")]
    public static void BuildScene()
    {
        EnsureDestructibleTag();

        Material groundMaterial = CreateMaterial(
            "Assets/Materials/Ground.mat",
            new Color(0.19f, 0.23f, 0.20f));
        Material blockMaterial = CreateMaterial(
            "Assets/Materials/Block.mat",
            new Color(0.72f, 0.48f, 0.24f));
        Material launcherMaterial = CreateMaterial(
            "Assets/Materials/Launcher.mat",
            new Color(0.12f, 0.17f, 0.21f));
        Material projectileMaterial = CreateMaterial(
            "Assets/Materials/Projectile.mat",
            new Color(0.82f, 0.18f, 0.12f));
        Material aimMaterial = CreateMaterial(
            "Assets/Materials/AimLine.mat",
            new Color(1f, 0.75f, 0.12f));

        Projectile projectilePrefab = CreateProjectilePrefab(projectileMaterial);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLighting();
        CreateGround(groundMaterial);
        CreateBlockStructure(blockMaterial);
        CreateLauncher(projectilePrefab, launcherMaterial, aimMaterial);
        CreateCamera();
        new GameObject("Game Manager").AddComponent<ResetController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Built Milestone 0 scene at {ScenePath}");
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.58f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.27f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.1f);
    }

    private static void CreateGround(Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(24f, 1f, 14f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateBlockStructure(Material material)
    {
        GameObject structure = new("Block Structure");
        const int columns = 5;
        const int rows = 5;
        const float blockWidth = 1.45f;
        const float blockHeight = 0.75f;
        const float blockDepth = 1.05f;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Block_{row + 1}_{column + 1}";
                block.tag = "Destructible";
                block.transform.SetParent(structure.transform);
                block.transform.position = new Vector3(
                    5.5f,
                    blockHeight * 0.5f + row * blockHeight,
                    (column - (columns - 1) * 0.5f) * blockDepth);
                block.transform.localScale = new Vector3(blockWidth, blockHeight, blockDepth);
                block.GetComponent<MeshRenderer>().sharedMaterial = material;

                Rigidbody rigidbody = block.AddComponent<Rigidbody>();
                rigidbody.mass = 2f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }
    }

    private static void CreateLauncher(
        Projectile projectilePrefab,
        Material launcherMaterial,
        Material aimMaterial)
    {
        GameObject launcher = new("Launcher");
        launcher.transform.position = new Vector3(-8f, 0f, 0f);

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "Base";
        baseObject.transform.SetParent(launcher.transform, false);
        baseObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        baseObject.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
        baseObject.GetComponent<MeshRenderer>().sharedMaterial = launcherMaterial;

        GameObject yawPivot = new("Yaw Pivot");
        yawPivot.transform.SetParent(launcher.transform, false);
        yawPivot.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        yawPivot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject pitchPivot = new("Pitch Pivot");
        pitchPivot.transform.SetParent(yawPivot.transform, false);

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(pitchPivot.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0f, 1.15f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.32f, 1.15f, 0.32f);
        barrel.GetComponent<MeshRenderer>().sharedMaterial = launcherMaterial;

        GameObject muzzle = new("Muzzle");
        muzzle.transform.SetParent(pitchPivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 2.4f);

        LineRenderer aimLine = launcher.AddComponent<LineRenderer>();
        aimLine.name = "Aim Trajectory";
        aimLine.useWorldSpace = true;
        aimLine.startWidth = 0.055f;
        aimLine.endWidth = 0.025f;
        aimLine.sharedMaterial = aimMaterial;
        aimLine.shadowCastingMode = ShadowCastingMode.Off;
        aimLine.receiveShadows = false;

        LauncherController controller = launcher.AddComponent<LauncherController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("yawPivot").objectReferenceValue = yawPivot.transform;
        serializedController.FindProperty("pitchPivot").objectReferenceValue = pitchPivot.transform;
        serializedController.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        serializedController.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serializedController.FindProperty("aimLine").objectReferenceValue = aimLine;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(-1f, 7.2f, -16f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.7f, 0f));
    }

    private static Projectile CreateProjectilePrefab(Material material)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "Projectile";
        projectileObject.transform.localScale = Vector3.one * 0.6f;
        projectileObject.GetComponent<MeshRenderer>().sharedMaterial = material;

        Rigidbody rigidbody = projectileObject.AddComponent<Rigidbody>();
        rigidbody.mass = 2f;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Projectile projectile = projectileObject.AddComponent<Projectile>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ProjectilePrefabPath);
        Object.DestroyImmediate(projectileObject);
        return prefab.GetComponent<Projectile>();
    }

    private static Material CreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }

    private static void EnsureDestructibleTag()
    {
        SerializedObject tagManager = new(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        for (int index = 0; index < tags.arraySize; index++)
        {
            if (tags.GetArrayElementAtIndex(index).stringValue == "Destructible")
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Destructible";
        tagManager.ApplyModifiedProperties();
    }
}
