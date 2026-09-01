using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FinalVisualPolishBuilder
{
    private const string ProfilePath = "Assets/Art/Materials/DemolitionLab_PostProcess.asset";
    private const string VolumeName = "Presentation Post Processing";
    private const string FillLightName = "Presentation Fill Light";
    private const string SparkName = "Impact Sparks";
    private const string CollapseDustName = "Collapse Dust";

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/VerticalSlice_Stage01.unity",
        "Assets/Scenes/VerticalSlice_Stage02.unity",
        "Assets/Scenes/VerticalSlice_Stage03.unity",
        "Assets/Scenes/VerticalSlice_Stage04.unity",
        "Assets/Scenes/VerticalSlice_Stage05.unity"
    };

    [MenuItem("DemolitionLab/Presentation/Apply Final Materials And Lighting")]
    public static void ApplyMaterialsAndLighting()
    {
        ThirdPartyPresentationBuilder.ApplyToAllStages();
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyLightingAndPostToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied final industrial materials, lighting, and post processing to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Final Materials And Lighting")]
    public static void ValidateMaterialsAndLighting()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateLightingAndPostInOpenScene(scenePath);
        }

        Debug.Log("Final materials and lighting validation passed for Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Apply Demolition Impact Feedback")]
    public static void ApplyImpactFeedback()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyFeedbackToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied demolition impact feedback to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Demolition Impact Feedback")]
    public static void ValidateImpactFeedback()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateFeedbackInOpenScene(scenePath);
        }

        Debug.Log("Demolition impact feedback validation passed for Stage01-05.");
    }

    internal static void ApplyLightingAndPostToOpenScene()
    {
        VolumeProfile profile = CreatePostProcessProfile();
        RemoveRootObject(VolumeName);
        RemoveRootObject(FillLightName);

        GameObject volumeObject = new(VolumeName);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;

        GameObject fillObject = new(FillLightName);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.64f, 0.76f, 0.92f);
        fill.intensity = 0.26f;
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(28f, 142f, 0f);

        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new UnityException("Final visual polish requires the Main Camera.");
        }

        camera.allowHDR = true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ApplyFeedbackToOpenScene()
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Impact feedback requires the vertical-slice stage controller.");
        }

        var serializedController = new SerializedObject(controller);
        ParticleSystem dust = (ParticleSystem)serializedController.FindProperty("impactDust").objectReferenceValue;
        if (dust == null)
        {
            throw new UnityException("Impact feedback requires the existing impact dust system.");
        }

        Material dustMaterial = CreateParticleMaterial(
            "DL_DustParticle",
            new Color(0.48f, 0.45f, 0.4f, 0.72f));
        Material sparkMaterial = CreateParticleMaterial(
            "DL_SparkParticle",
            new Color(1f, 0.55f, 0.08f, 0.95f));
        ConfigureImpactDust(dust, dustMaterial);

        RemoveRootObject(SparkName);
        RemoveRootObject(CollapseDustName);
        ParticleSystem sparks = CreateSparks(sparkMaterial);
        ParticleSystem collapse = CreateCollapseDust(dustMaterial);

        PresentationAudioHooks audioHooks = controller.GetComponent<PresentationAudioHooks>();
        if (audioHooks == null)
        {
            audioHooks = controller.gameObject.AddComponent<PresentationAudioHooks>();
        }

        AudioSource audioSource = controller.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        serializedController.FindProperty("impactSparks").objectReferenceValue = sparks;
        serializedController.FindProperty("collapseDust").objectReferenceValue = collapse;
        serializedController.FindProperty("audioHooks").objectReferenceValue = audioHooks;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ValidateLightingAndPostInOpenScene(string scenePath)
    {
        Volume volume = GameObject.Find(VolumeName)?.GetComponent<Volume>();
        Light fill = GameObject.Find(FillLightName)?.GetComponent<Light>();
        Camera camera = Camera.main;
        if (volume == null
            || !volume.isGlobal
            || volume.sharedProfile == null
            || fill == null
            || fill.type != LightType.Directional
            || fill.shadows != LightShadows.None
            || camera == null
            || !camera.GetUniversalAdditionalCameraData().renderPostProcessing)
        {
            throw new UnityException($"{scenePath} final lighting or post processing is incomplete.");
        }

        if (!volume.sharedProfile.TryGet(out ColorAdjustments color)
            || !volume.sharedProfile.TryGet(out Tonemapping tonemapping)
            || !volume.sharedProfile.TryGet(out Bloom bloom)
            || !volume.sharedProfile.TryGet(out Vignette vignette)
            || !color.active
            || !tonemapping.active
            || !bloom.active
            || !vignette.active)
        {
            throw new UnityException($"{scenePath} final post-processing profile is incomplete.");
        }
    }

    internal static void ValidateFeedbackInOpenScene(string scenePath)
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        var serializedController = new SerializedObject(controller);
        ParticleSystem dust = (ParticleSystem)serializedController.FindProperty("impactDust").objectReferenceValue;
        ParticleSystem sparks = (ParticleSystem)serializedController.FindProperty("impactSparks").objectReferenceValue;
        ParticleSystem collapse = (ParticleSystem)serializedController.FindProperty("collapseDust").objectReferenceValue;
        PresentationAudioHooks audio = (PresentationAudioHooks)serializedController
            .FindProperty("audioHooks").objectReferenceValue;
        if (dust == null || sparks == null || collapse == null || audio == null)
        {
            throw new UnityException($"{scenePath} impact feedback references are incomplete.");
        }

        foreach (ParticleSystem particles in new[] { dust, sparks, collapse })
        {
            if (particles.main.simulationSpace != ParticleSystemSimulationSpace.World
                || particles.main.maxParticles > 100
                || particles.GetComponent<Collider>() != null
                || particles.GetComponent<Rigidbody>() != null)
            {
                throw new UnityException($"{scenePath} has unsafe or excessive impact particles.");
            }
        }
    }

    private static VolumeProfile CreatePostProcessProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "DemolitionLab Post Processing";
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        profile.components.RemoveAll(component => component == null);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(0.08f);
        color.contrast.Override(12f);
        color.colorFilter.Override(new Color(0.96f, 0.985f, 1f));
        color.hueShift.Override(0f);
        color.saturation.Override(-8f);

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(1.15f);
        bloom.intensity.Override(0.12f);
        bloom.scatter.Override(0.45f);
        bloom.clamp.Override(4f);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.color.Override(new Color(0.025f, 0.035f, 0.045f));
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(0.11f);
        vignette.smoothness.Override(0.38f);
        vignette.rounded.Override(false);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static Material CreateParticleMaterial(string name, Color color)
    {
        string path = $"Assets/Art/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureImpactDust(ParticleSystem particles, Material material)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
        main.startColor = new Color(0.48f, 0.45f, 0.4f, 0.72f);
        main.gravityModifier = 0.45f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        ConfigureBurst(particles, 22);
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.14f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static ParticleSystem CreateSparks(Material material)
    {
        ParticleSystem particles = CreateParticleRoot(SparkName);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.38f, 0.04f),
            new Color(1f, 0.82f, 0.28f));
        main.gravityModifier = 0.8f;
        main.maxParticles = 72;
        ConfigureBurst(particles, 18);
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.08f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3.5f;
        renderer.velocityScale = 0.08f;
        renderer.sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static ParticleSystem CreateCollapseDust(Material material)
    {
        ParticleSystem particles = CreateParticleRoot(CollapseDustName);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 1.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.82f);
        main.startColor = new Color(0.42f, 0.4f, 0.37f, 0.55f);
        main.gravityModifier = 0.08f;
        main.maxParticles = 80;
        ConfigureBurst(particles, 34);
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.65f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static ParticleSystem CreateParticleRoot(string objectName)
    {
        GameObject particleObject = new(objectName);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        return particles;
    }

    private static void ConfigureBurst(ParticleSystem particles, short count)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
        {
            return component;
        }

        component = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void RemoveRootObject(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
