using DemolitionLab;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FinalVisualPolishBuilder
{
    private const string ProfilePath = "Assets/Art/Materials/DemolitionLab_PostProcess.asset";
    private const string VolumeName = "Presentation Post Processing";
    private const string FillLightName = "Presentation Fill Light";
    private const string MuzzleFlashName = "Muzzle Flash";
    private const string MuzzleSmokeName = "Muzzle Smoke";
    private const string SparkName = "Impact Sparks";
    private const string DebrisName = "Impact Debris";
    private const string StructuralDustName = "Structural Impact Dust";
    private const string CollapseDustName = "Collapse Dust";
    private const string WorldAudioName = "World Feedback Audio";
    private const string HudPresentationName = "Final HUD Presentation";
    private const string TitleScenePath = "Assets/Scenes/VerticalSlice_Title.unity";
    private const string StageOneSceneName = "VerticalSlice_Stage01";

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
            VerticalSlicePresentationBuilder.ApplyLightingToOpenScene();
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

    [MenuItem("DemolitionLab/Presentation/Apply Final HUD And Flow")]
    public static void ApplyHudAndFlow()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyHudToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        CreateTitleScene();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Applied final HUD, title, result, and screenshot presentation flow.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Final HUD And Flow")]
    public static void ValidateHudAndFlow()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateHudInOpenScene(scenePath);
        }

        ValidateTitleScene();
        Debug.Log("Final HUD and presentation flow validation passed.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Final Portfolio Build")]
    public static void ValidateFinalPortfolioBuild()
    {
        M1SceneBuilder.ValidateScene();
        M2SceneBuilder.ValidateScene();
        M3SceneBuilder.ValidateScene();
        M4SceneBuilder.ValidateScene();
        M5SceneBuilder.ValidateScene();
        VerticalSliceStage05Builder.ValidateAllStages();
        PhysicsPresentationSeparationBuilder.ValidateAllStages();
        ThirdPartyPresentationBuilder.ValidateAllStages();
        VerticalSlicePresentationBuilder.ValidateIndustrialEnvironment();
        VerticalSlicePresentationBuilder.ValidateStructuralPresentation();
        VerticalSlicePresentationBuilder.ValidateCameraFraming();
        ValidateMaterialsAndLighting();
        ValidateImpactFeedback();
        ValidateHudAndFlow();
        Debug.Log("Final portfolio build validation passed.");
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
        fill.intensity = 0.48f;
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

        RemoveRootObject(MuzzleFlashName);
        RemoveRootObject(MuzzleSmokeName);
        RemoveRootObject(SparkName);
        RemoveRootObject(DebrisName);
        RemoveRootObject(StructuralDustName);
        RemoveRootObject(CollapseDustName);
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

        serializedController.FindProperty("muzzleFlash").objectReferenceValue = muzzleFlash;
        serializedController.FindProperty("muzzleSmoke").objectReferenceValue = muzzleSmoke;
        serializedController.FindProperty("impactSparks").objectReferenceValue = sparks;
        serializedController.FindProperty("impactDebris").objectReferenceValue = debris;
        serializedController.FindProperty("structuralImpactDust").objectReferenceValue = structuralDust;
        serializedController.FindProperty("collapseDust").objectReferenceValue = collapse;
        serializedController.FindProperty("audioHooks").objectReferenceValue = audioHooks;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ApplyHudToOpenScene()
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        Camera camera = Camera.main;
        if (controller == null || launcher == null || camera == null)
        {
            throw new UnityException("Final HUD requires the stage controller, launcher, and Main Camera.");
        }

        var serializedController = new SerializedObject(controller);
        Text stage = (Text)serializedController.FindProperty("stageLabelText").objectReferenceValue;
        Text shots = (Text)serializedController.FindProperty("shotsText").objectReferenceValue;
        Text destruction = (Text)serializedController.FindProperty("destructionText").objectReferenceValue;
        GameObject endPanel = (GameObject)serializedController.FindProperty("endPanel").objectReferenceValue;
        Text resultTitle = (Text)serializedController.FindProperty("resultTitleText").objectReferenceValue;
        Text resultDetails = (Text)serializedController.FindProperty("resultDetailsText").objectReferenceValue;
        Button next = (Button)serializedController.FindProperty("nextButton").objectReferenceValue;
        PresentationAudioHooks audio = (PresentationAudioHooks)serializedController.FindProperty("audioHooks").objectReferenceValue;
        Canvas canvas = stage != null ? stage.GetComponentInParent<Canvas>() : null;
        if (stage == null || shots == null || destruction == null || endPanel == null
            || resultTitle == null || resultDetails == null || next == null || audio == null || canvas == null)
        {
            throw new UnityException("Final HUD requires all existing HUD and audio references.");
        }

        Transform existing = canvas.transform.Find(HudPresentationName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject root = new(HudPresentationName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.transform.SetAsFirstSibling();

        ConfigureCompactHudText(stage, 30, new Vector2(30f, -22f), new Vector2(300f, 44f), new Vector2(0f, 1f), TextAnchor.UpperLeft);
        ConfigureCompactHudText(shots, 28, new Vector2(-30f, -22f), new Vector2(300f, 44f), new Vector2(1f, 1f), TextAnchor.UpperRight);
        ConfigureCompactHudText(destruction, 24, new Vector2(0f, 43f), new Vector2(380f, 36f), new Vector2(0.5f, 0f), TextAnchor.LowerCenter);
        CreateHudAccentLine(root.transform, "Stage Accent", new Vector2(30f, -72f), new Vector2(118f, 3f), new Vector2(0f, 1f));
        CreateHudAccentLine(root.transform, "Shots Accent", new Vector2(-30f, -72f), new Vector2(118f, 3f), new Vector2(1f, 1f));
        Image destructionFill = CreateProgressBar(root.transform);

        Image endPanelImage = endPanel.GetComponent<Image>();
        endPanelImage.color = new Color(0.025f, 0.04f, 0.05f, 0.9f);
        endPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 320f);
        Outline panelOutline = endPanel.GetComponent<Outline>() ?? endPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.95f, 0.5f, 0.08f, 0.62f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);
        resultTitle.color = new Color(1f, 0.66f, 0.18f);
        resultTitle.fontSize = 40;
        resultDetails.color = new Color(0.82f, 0.88f, 0.9f);

        Button retry = endPanel.transform.Find("Retry Button")?.GetComponent<Button>();
        StyleResultButton(retry);
        StyleResultButton(next);
        AddPersistentClick(retry, audio, audio.PlayUiClick);
        AddPersistentClick(next, audio, audio.PlayUiClick);
        if (SceneManager.GetActiveScene().name == "VerticalSlice_Stage05")
        {
            Text retryLabel = retry != null ? retry.GetComponentInChildren<Text>() : null;
            if (retryLabel != null)
            {
                retryLabel.text = "RESTART TEST";
            }
        }

        serializedController.FindProperty("destructionFill").objectReferenceValue = destructionFill;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        var serializedLauncher = new SerializedObject(launcher);
        LineRenderer trajectory = (LineRenderer)serializedLauncher.FindProperty("aimLine").objectReferenceValue;
        PresentationCaptureMode captureMode = camera.GetComponent<PresentationCaptureMode>();
        if (captureMode == null)
        {
            captureMode = camera.gameObject.AddComponent<PresentationCaptureMode>();
        }

        var serializedCapture = new SerializedObject(captureMode);
        serializedCapture.FindProperty("trajectory").objectReferenceValue = trajectory;
        serializedCapture.ApplyModifiedPropertiesWithoutUndo();
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
        ParticleSystem muzzleFlash = (ParticleSystem)serializedController.FindProperty("muzzleFlash").objectReferenceValue;
        ParticleSystem muzzleSmoke = (ParticleSystem)serializedController.FindProperty("muzzleSmoke").objectReferenceValue;
        ParticleSystem sparks = (ParticleSystem)serializedController.FindProperty("impactSparks").objectReferenceValue;
        ParticleSystem debris = (ParticleSystem)serializedController.FindProperty("impactDebris").objectReferenceValue;
        ParticleSystem structuralDust = (ParticleSystem)serializedController.FindProperty("structuralImpactDust").objectReferenceValue;
        ParticleSystem collapse = (ParticleSystem)serializedController.FindProperty("collapseDust").objectReferenceValue;
        PresentationAudioHooks audio = (PresentationAudioHooks)serializedController
            .FindProperty("audioHooks").objectReferenceValue;
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

    internal static void ValidateHudInOpenScene(string scenePath)
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        PresentationCaptureMode capture = Camera.main != null
            ? Camera.main.GetComponent<PresentationCaptureMode>()
            : null;
        if (controller == null || capture == null)
        {
            throw new UnityException($"{scenePath} final HUD or screenshot mode is missing.");
        }

        var serializedController = new SerializedObject(controller);
        Image fill = (Image)serializedController.FindProperty("destructionFill").objectReferenceValue;
        GameObject panel = (GameObject)serializedController.FindProperty("endPanel").objectReferenceValue;
        Text stage = (Text)serializedController.FindProperty("stageLabelText").objectReferenceValue;
        Canvas canvas = stage != null ? stage.GetComponentInParent<Canvas>() : null;
        Transform hudRoot = canvas != null ? canvas.transform.Find(HudPresentationName) : null;
        if (fill == null
            || fill.type != Image.Type.Filled
            || fill.fillMethod != Image.FillMethod.Horizontal
            || panel == null
            || panel.GetComponent<Outline>() == null
            || hudRoot == null
            || hudRoot.Find("Stage Panel") != null
            || hudRoot.Find("Shots Panel") != null
            || hudRoot.Find("Destruction Panel") != null
            || hudRoot.Find("Stage Accent") == null
            || hudRoot.Find("Shots Accent") == null)
        {
            throw new UnityException($"{scenePath} final HUD presentation is incomplete.");
        }

        var serializedCapture = new SerializedObject(capture);
        if (serializedCapture.FindProperty("trajectory").objectReferenceValue == null)
        {
            throw new UnityException($"{scenePath} screenshot mode is missing its trajectory reference.");
        }
    }

    private static void ConfigureCompactHudText(
        Text text,
        int fontSize,
        Vector2 position,
        Vector2 size,
        Vector2 anchor,
        TextAnchor alignment)
    {
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = new Color(0.92f, 0.95f, 0.95f);
        SetRect(text.rectTransform, position, size, anchor);
        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0.015f, 0.025f, 0.03f, 0.78f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }

    private static Image CreateHudAccentLine(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Vector2 anchor)
    {
        GameObject lineObject = new(name, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(parent, false);
        SetRect(lineObject.GetComponent<RectTransform>(), position, size, anchor);
        Image image = lineObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.49f, 0.08f, 0.9f);
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateProgressBar(Transform parent)
    {
        GameObject background = new("Destruction Progress", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(parent, false);
        SetRect(background.GetComponent<RectTransform>(), new Vector2(0f, 28f), new Vector2(380f, 6f), new Vector2(0.5f, 0f));
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.07f, 0.08f, 0.72f);
        backgroundImage.raycastTarget = false;

        GameObject fillObject = new("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(background.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
        Image fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.95f, 0.49f, 0.08f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        return fill;
    }

    private static void StyleResultButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.08f, 0.15f, 0.17f, 0.88f);
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.16f, 0.32f, 0.36f, 1f);
        colors.pressedColor = new Color(0.95f, 0.49f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.49f, 0.08f, 0.7f);
        outline.effectDistance = new Vector2(1f, -1f);
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = new Color(0.94f, 0.96f, 0.94f);
            label.fontStyle = FontStyle.Bold;
        }
    }

    private static void AddPersistentClick(Button button, Object target, UnityEngine.Events.UnityAction action)
    {
        if (button == null || target == null)
        {
            return;
        }

        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentTarget(index) == target
                && button.onClick.GetPersistentMethodName(index) == action.Method.Name)
            {
                return;
            }
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static void CreateTitleScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "VerticalSlice_Title";

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 46f;
        camera.transform.position = new Vector3(-1.5f, 4.4f, -13.5f);
        camera.transform.LookAt(new Vector3(2.8f, 2.4f, 6.5f));
        cameraObject.AddComponent<AudioListener>();

        GameObject keyObject = new("Directional Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.91f, 0.78f);
        key.intensity = 1.5f;
        key.shadows = LightShadows.Soft;
        key.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.45f, 0.5f, 0.53f);
        RenderSettings.fogStartDistance = 22f;
        RenderSettings.fogEndDistance = 55f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.47f, 0.56f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.4f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.25f, 0.28f);
        RenderSettings.ambientIntensity = 1.02f;
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/IndustrialSky.mat");
        ApplyLightingAndPostToOpenScene();

        Material concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/YardConcrete.mat");
        Material steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/LauncherMetal.mat");
        Material accent = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/SafetyYellow.mat");
        CreateTitlePrimitive(PrimitiveType.Cube, "Test Yard", new Vector3(1f, -0.35f, 7f), Quaternion.identity, new Vector3(30f, 0.5f, 32f), concrete);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Foundation", new Vector3(5.5f, 0.15f, 7f), Quaternion.identity, new Vector3(8.5f, 0.3f, 3.6f), concrete);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Column Left", new Vector3(3.1f, 2.7f, 7f), Quaternion.identity, new Vector3(0.48f, 5.2f, 0.55f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Column Right", new Vector3(7.9f, 2.7f, 7f), Quaternion.identity, new Vector3(0.48f, 5.2f, 0.55f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Top Beam", new Vector3(5.5f, 5.15f, 7f), Quaternion.identity, new Vector3(5.4f, 0.5f, 0.62f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Mid Beam", new Vector3(5.5f, 2.65f, 7f), Quaternion.identity, new Vector3(5.1f, 0.34f, 0.5f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Brace A", new Vector3(4.25f, 2.65f, 6.65f), Quaternion.Euler(0f, 0f, 34f), new Vector3(0.24f, 4.4f, 0.28f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Brace B", new Vector3(6.75f, 2.65f, 6.65f), Quaternion.Euler(0f, 0f, -34f), new Vector3(0.24f, 4.4f, 0.28f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Hazard Rail", new Vector3(5.5f, 0.48f, 5.2f), Quaternion.identity, new Vector3(7.2f, 0.16f, 0.18f), accent);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new("Title Screen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image overlay = CreateTitleImage(canvas.transform, "Title Overlay", Vector2.zero, new Vector2(1920f, 1080f));
        overlay.color = new Color(0.015f, 0.028f, 0.035f, 0.32f);
        Text demolition = CreateTitleText(canvas.transform, font, "DEMOLITION", 70, new Vector2(-570f, 190f), new Vector2(700f, 90f));
        demolition.alignment = TextAnchor.MiddleLeft;
        demolition.fontStyle = FontStyle.Bold;
        Text lab = CreateTitleText(canvas.transform, font, "LAB", 96, new Vector2(-570f, 105f), new Vector2(700f, 110f));
        lab.alignment = TextAnchor.MiddleLeft;
        lab.color = new Color(0.96f, 0.58f, 0.1f);
        lab.fontStyle = FontStyle.Bold;
        Image titleAccent = CreateTitleImage(canvas.transform, "Title Accent", new Vector2(-565f, 35f), new Vector2(190f, 4f));
        titleAccent.color = new Color(0.96f, 0.49f, 0.08f, 0.95f);
        Text subtitle = CreateTitleText(canvas.transform, font, "STRUCTURAL DEMOLITION TEST", 25, new Vector2(-570f, -12f), new Vector2(700f, 50f));
        subtitle.alignment = TextAnchor.MiddleLeft;
        subtitle.color = new Color(0.78f, 0.85f, 0.87f);
        Button start = CreateTitleButton(canvas.transform, font, new Vector2(-570f, -115f));
        Text controls = CreateTitleText(canvas.transform, font, "WASD / ARROWS  —  AIM     SPACE  —  FIRE", 19, new Vector2(-570f, -205f), new Vector2(700f, 42f));
        controls.alignment = TextAnchor.MiddleLeft;
        controls.color = new Color(0.66f, 0.73f, 0.75f);

        GameObject menuObject = new("Presentation Menu Controller");
        PresentationMenuController menu = menuObject.AddComponent<PresentationMenuController>();
        PresentationAudioHooks audio = menuObject.AddComponent<PresentationAudioHooks>();
        AudioSource source = menuObject.GetComponent<AudioSource>();
        source.playOnAwake = false;
        AddPersistentClick(start, menu, menu.StartTest);
        AddPersistentClick(start, audio, audio.PlayUiClick);

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
        EditorSceneManager.SaveScene(scene, TitleScenePath);
    }

    private static void ValidateTitleScene()
    {
        EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        PresentationMenuController menu = Object.FindAnyObjectByType<PresentationMenuController>();
        Button start = GameObject.Find("Start Button")?.GetComponent<Button>();
        if (menu == null
            || menu.StartSceneName != StageOneSceneName
            || start == null
            || start.onClick.GetPersistentEventCount() < 1
            || Camera.main == null
            || GameObject.Find("Title Backing") != null
            || GameObject.Find("Title Overlay") == null
            || GameObject.Find("Frame Brace A") == null
            || Object.FindAnyObjectByType<Rigidbody>() != null
            || Object.FindAnyObjectByType<Collider>() != null)
        {
            throw new UnityException("Title scene presentation or START flow is incomplete.");
        }

        bool titleBeforeStageOne = false;
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int index = 0; index < scenes.Length - 1; index++)
        {
            if (scenes[index].path == TitleScenePath
                && scenes[index + 1].path == StageScenePaths[0]
                && scenes[index].enabled)
            {
                titleBeforeStageOne = true;
                break;
            }
        }

        if (!titleBeforeStageOne)
        {
            throw new UnityException("Title scene is not enabled immediately before Stage01 in build settings.");
        }
    }

    private static void ConfigureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == TitleScenePath);
        int stageOneIndex = scenes.FindIndex(scene => scene.path == StageScenePaths[0]);
        if (stageOneIndex < 0)
        {
            throw new UnityException("Stage01 must exist in build settings before adding the title scene.");
        }

        scenes.Insert(stageOneIndex, new EditorBuildSettingsScene(TitleScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void CreateTitlePrimitive(
        PrimitiveType type,
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(type);
        visual.name = name;
        visual.transform.position = position;
        visual.transform.rotation = rotation;
        visual.transform.localScale = scale;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Image CreateTitleImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        SetRect(imageObject.GetComponent<RectTransform>(), position, size, new Vector2(0.5f, 0.5f));
        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateTitleText(
        Transform parent,
        Font font,
        string content,
        int fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = new(content, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        SetRect(textObject.GetComponent<RectTransform>(), position, size, new Vector2(0.5f, 0.5f));
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.9f, 0.93f, 0.92f);
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateTitleButton(Transform parent, Font font, Vector2 position)
    {
        GameObject buttonObject = new("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(250f, 62f), new Vector2(0.5f, 0.5f));
        Button button = buttonObject.GetComponent<Button>();
        StyleResultButton(button);
        Text label = CreateTitleText(buttonObject.transform, font, "START TEST", 26, Vector2.zero, new Vector2(250f, 62f));
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size,
        Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
        color.postExposure.Override(0.2f);
        color.contrast.Override(4f);
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
        vignette.intensity.Override(0.055f);
        vignette.smoothness.Override(0.32f);
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

    private static ParticleSystem CreateMuzzleFlash(Material material)
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

    private static ParticleSystem CreateMuzzleSmoke(Material material)
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

    private static ParticleSystem CreateImpactDebris(Material material)
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

    private static ParticleSystem CreateStructuralImpactDust(Material material)
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
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
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
