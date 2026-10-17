using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class FinalImpactFeedbackBuilder
{
    private const string MuzzleFlashName = "Muzzle Flash";
    private const string MuzzleSmokeName = "Muzzle Smoke";
    private const string SparkName = "Impact Sparks";
    private const string DebrisName = "Impact Debris";
    private const string StructuralDustName = "Structural Impact Dust";
    private const string CollapseDustName = "Collapse Dust";
    private const string WorldAudioName = "World Feedback Audio";

    internal static void ApplyFeedbackToOpenScene()
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Impact feedback requires the vertical-slice stage controller.");
        }

        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        ParticleSystem dust = (ParticleSystem)serializedFeedback.FindProperty("impactDust").objectReferenceValue;
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

        PresentationEditorUtilities.RemoveRootObject(MuzzleFlashName);
        PresentationEditorUtilities.RemoveRootObject(MuzzleSmokeName);
        PresentationEditorUtilities.RemoveRootObject(SparkName);
        PresentationEditorUtilities.RemoveRootObject(DebrisName);
        PresentationEditorUtilities.RemoveRootObject(StructuralDustName);
        PresentationEditorUtilities.RemoveRootObject(CollapseDustName);
        ParticleSystem muzzleFlash = CreateMuzzleFlash(sparkMaterial);
        ParticleSystem muzzleSmoke = CreateMuzzleSmoke(dustMaterial);
        ParticleSystem sparks = CreateSparks(sparkMaterial);
        ParticleSystem debris = CreateImpactDebris(dustMaterial);
        ParticleSystem structuralDust = CreateStructuralImpactDust(dustMaterial);
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

        Transform existingWorldAudio = controller.transform.Find(WorldAudioName);
        if (existingWorldAudio != null)
        {
            Object.DestroyImmediate(existingWorldAudio.gameObject);
        }

        GameObject worldAudioObject = new(WorldAudioName);
        worldAudioObject.transform.SetParent(controller.transform, false);
        AudioSource worldAudio = worldAudioObject.AddComponent<AudioSource>();
        worldAudio.playOnAwake = false;
        worldAudio.loop = false;
        worldAudio.spatialBlend = 1f;
        worldAudio.dopplerLevel = 0f;
        worldAudio.rolloffMode = AudioRolloffMode.Linear;
        worldAudio.minDistance = 3f;
        worldAudio.maxDistance = 28f;
        worldAudio.volume = 1f;

        var serializedAudio = new SerializedObject(audioHooks);
        serializedAudio.FindProperty("worldSource").objectReferenceValue = worldAudio;
        serializedAudio.ApplyModifiedPropertiesWithoutUndo();

        foreach (StageTargetPiece target in Object.FindObjectsByType<StageTargetPiece>())
        {
            if (target.GetComponent<StructuralGroundImpactFeedback>() == null)
            {
                target.gameObject.AddComponent<StructuralGroundImpactFeedback>();
            }
        }

        serializedFeedback.FindProperty("muzzleFlash").objectReferenceValue = muzzleFlash;
        serializedFeedback.FindProperty("muzzleSmoke").objectReferenceValue = muzzleSmoke;
        serializedFeedback.FindProperty("impactSparks").objectReferenceValue = sparks;
        serializedFeedback.FindProperty("impactDebris").objectReferenceValue = debris;
        serializedFeedback.FindProperty("structuralImpactDust").objectReferenceValue = structuralDust;
        serializedFeedback.FindProperty("collapseDust").objectReferenceValue = collapse;
        serializedFeedback.FindProperty("audioHooks").objectReferenceValue = audioHooks;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ValidateFeedbackInOpenScene(string scenePath)
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        ParticleSystem dust = (ParticleSystem)serializedFeedback.FindProperty("impactDust").objectReferenceValue;
        ParticleSystem muzzleFlash = (ParticleSystem)serializedFeedback.FindProperty("muzzleFlash").objectReferenceValue;
        ParticleSystem muzzleSmoke = (ParticleSystem)serializedFeedback.FindProperty("muzzleSmoke").objectReferenceValue;
        ParticleSystem sparks = (ParticleSystem)serializedFeedback.FindProperty("impactSparks").objectReferenceValue;
        ParticleSystem debris = (ParticleSystem)serializedFeedback.FindProperty("impactDebris").objectReferenceValue;
        ParticleSystem structuralDust = (ParticleSystem)serializedFeedback.FindProperty("structuralImpactDust").objectReferenceValue;
        ParticleSystem collapse = (ParticleSystem)serializedFeedback.FindProperty("collapseDust").objectReferenceValue;
        PresentationAudioHooks audio = (PresentationAudioHooks)serializedFeedback.FindProperty("audioHooks").objectReferenceValue;
        if (dust == null
            || muzzleFlash == null
            || muzzleSmoke == null
            || sparks == null
            || debris == null
            || structuralDust == null
            || collapse == null
            || audio == null)
        {
            throw new UnityException($"{scenePath} impact feedback references are incomplete.");
        }

        foreach (ParticleSystem particles in new[]
                 {
                     dust, muzzleFlash, muzzleSmoke, sparks, debris, structuralDust, collapse
                 })
        {
            if (particles.main.simulationSpace != ParticleSystemSimulationSpace.World
                || particles.main.playOnAwake
                || particles.main.maxParticles > 100
                || particles.GetComponent<Collider>() != null
                || particles.GetComponent<Rigidbody>() != null)
            {
                throw new UnityException($"{scenePath} has unsafe or excessive impact particles.");
            }
        }

        var serializedAudio = new SerializedObject(audio);
        AudioSource worldSource = (AudioSource)serializedAudio.FindProperty("worldSource").objectReferenceValue;
        int targetCount = Object.FindObjectsByType<StageTargetPiece>().Length;
        int groundFeedbackCount = Object.FindObjectsByType<StructuralGroundImpactFeedback>().Length;
        if (worldSource == null
            || !Mathf.Approximately(worldSource.spatialBlend, 1f)
            || worldSource.playOnAwake
            || targetCount != groundFeedbackCount)
        {
            throw new UnityException($"{scenePath} world audio or structural impact feedback is incomplete.");
        }
    }

    internal static Material CreateParticleMaterial(string name, Color color)
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

    internal static void ConfigureImpactDust(ParticleSystem particles, Material material)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = false;
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

    internal static ParticleSystem CreateMuzzleFlash(Material material)
    {
        ParticleSystem particles = CreateParticleRoot(MuzzleFlashName);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.035f, 0.065f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.36f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.48f, 0.08f, 0.95f),
            new Color(1f, 0.88f, 0.42f, 0.95f));
        main.maxParticles = 4;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.04f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    internal static ParticleSystem CreateMuzzleSmoke(Material material)
    {
        ParticleSystem particles = CreateParticleRoot(MuzzleSmokeName);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.13f, 0.3f);
        main.startColor = new Color(0.48f, 0.5f, 0.5f, 0.42f);
        main.gravityModifier = -0.05f;
        main.maxParticles = 24;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.08f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    internal static ParticleSystem CreateSparks(Material material)
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

    internal static ParticleSystem CreateImpactDebris(Material material)
    {
        ParticleSystem particles = CreateParticleRoot(DebrisName);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.45f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.34f, 0.36f, 0.36f, 0.9f),
            new Color(0.62f, 0.6f, 0.55f, 0.9f));
        main.gravityModifier = 1f;
        main.maxParticles = 40;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.08f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    internal static ParticleSystem CreateStructuralImpactDust(Material material)
    {
        ParticleSystem particles = CreateParticleRoot(StructuralDustName);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.65f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.4f);
        main.startColor = new Color(0.45f, 0.43f, 0.4f, 0.58f);
        main.gravityModifier = 0.18f;
        main.maxParticles = 48;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.22f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    internal static ParticleSystem CreateCollapseDust(Material material)
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

    internal static ParticleSystem CreateParticleRoot(string objectName)
    {
        GameObject particleObject = new(objectName);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
        return particles;
    }

    internal static void ConfigureBurst(ParticleSystem particles, short count)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
    }
}
