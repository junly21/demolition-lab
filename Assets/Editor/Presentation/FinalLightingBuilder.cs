using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal static class FinalLightingBuilder
{
    private const string ProfilePath = "Assets/Art/Materials/DemolitionLab_PostProcess.asset";
    private const string VolumeName = "Presentation Post Processing";
    private const string FillLightName = "Presentation Fill Light";

    internal static void ApplyLightingAndPostToOpenScene()
    {
        VolumeProfile profile = CreatePostProcessProfile();
        PresentationEditorUtilities.RemoveRootObject(VolumeName);
        PresentationEditorUtilities.RemoveRootObject(FillLightName);

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

    internal static VolumeProfile CreatePostProcessProfile()
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

    internal static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
        {
            return component;
        }

        component = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }
}
