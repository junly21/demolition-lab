using System;
using System.Collections.Generic;
using System.IO;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class IndustrialAssetFactory
{
    private static bool assetsReady;
    private const string MaterialFolder = "Assets/Art/Materials";
    internal const string PrefabFolder = "Assets/Art/Prefabs/Industrial";
    private const string CityRoot = "Assets/Art/ThirdParty/kenney_city-kit-industrial_1.0";
    private const string FactoryRoot = "Assets/Art/ThirdParty/kenney_factory-kit_3.0";
    private const string AsphaltRoot = "Assets/Art/ThirdParty/asphalt_01_4k.blend/textures";
    private const string ConcreteMottlePath = "Assets/Art/Materials/DL_ConcreteMottle.png";
    private static readonly ModelDefinition[] ModelDefinitions =
    {
        new("IndustrialWarehouse", $"{CityRoot}/Models/FBX format/building-f.fbx", MaterialKind.City),
        new("IndustrialPlant", $"{CityRoot}/Models/FBX format/building-l.fbx", MaterialKind.City),
        new("CoolingChimney", $"{CityRoot}/Models/FBX format/chimney-large.fbx", MaterialKind.City),
        new("StorageTank", $"{CityRoot}/Models/FBX format/detail-tank.fbx", MaterialKind.City),
        new("CargoCrate", $"{FactoryRoot}/Models/FBX format/box-large.fbx", MaterialKind.Factory),
        new("FactoryMachine", $"{FactoryRoot}/Models/FBX format/machine-fortified.fbx", MaterialKind.Factory),
        new("IndustrialPipe", $"{FactoryRoot}/Models/FBX format/pipe-large-long.fbx", MaterialKind.Factory),
        new("YardCrane", $"{FactoryRoot}/Models/FBX format/crane.fbx", MaterialKind.Factory)
    };


    internal static void ValidateStructuralMaterials()
    {
        Texture2D mottle = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteMottlePath);
        string[] concreteMaterials =
        {
            "DL_Concrete",
            "DL_ConcreteDark",
            "DL_ConcreteWorn",
            "DL_FoundationConcrete"
        };

        if (mottle == null)
        {
            throw new UnityException("Project-owned concrete mottle texture is missing.");
        }

        foreach (string materialName in concreteMaterials)
        {
            Material material = LoadMaterial(materialName);
            if (material.GetTexture("_BaseMap") != mottle
                || material.GetTexture("_BumpMap") != null
                || material.GetFloat("_Metallic") > 0.01f)
            {
                throw new UnityException($"{materialName} is not using the non-directional concrete treatment.");
            }
        }
    }

    internal static void EnsurePresentationAssets()
    {
        if (assetsReady)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/DL_Asphalt.mat") != null
            && AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/IndustrialWarehouse.prefab") != null)
        {
            assetsReady = true;
            return;
        }

        RebuildPresentationAssets();
    }

    internal static void RebuildPresentationAssets()
    {
        assetsReady = false;

        EnsureFolder("Assets/Art", "Materials");
        EnsureFolder("Assets/Art", "Prefabs");
        EnsureFolder("Assets/Art/Prefabs", "Industrial");

        ConfigureTexture($"{AsphaltRoot}/asphalt_01_diff_4k.jpg", false, true);
        ConfigureTexture($"{AsphaltRoot}/asphalt_01_nor_gl_4k.exr", true, false);
        ConfigureTexture($"{AsphaltRoot}/asphalt_01_rough_4k.exr", false, false);
        CreateConcreteMottleTexture();

        string asphaltMask = $"{MaterialFolder}/Asphalt_MetallicSmoothness.png";
        CreateSmoothnessMask($"{AsphaltRoot}/asphalt_01_rough_4k.exr", asphaltMask);

        CreateSurfaceMaterial(
            "DL_Asphalt",
            $"{AsphaltRoot}/asphalt_01_diff_4k.jpg",
            $"{AsphaltRoot}/asphalt_01_nor_gl_4k.exr",
            asphaltMask,
            new Color(0.48f, 0.5f, 0.5f),
            new Vector2(4f, 4f),
            0.42f);
        CreateProceduralConcreteMaterial(
            "DL_Concrete",
            new Color(0.76f, 0.8f, 0.82f),
            new Vector2(1.45f, 1.45f),
            0.16f);
        CreateProceduralConcreteMaterial(
            "DL_ConcreteDark",
            new Color(0.62f, 0.67f, 0.7f),
            new Vector2(1.7f, 1.7f),
            0.14f);
        CreateProceduralConcreteMaterial(
            "DL_ConcreteWorn",
            new Color(0.68f, 0.7f, 0.69f),
            new Vector2(1.9f, 1.9f),
            0.12f);
        CreateProceduralConcreteMaterial(
            "DL_FoundationConcrete",
            new Color(0.49f, 0.53f, 0.55f),
            new Vector2(1.3f, 1.3f),
            0.11f);

        CreateSimpleMaterial("DL_Steel", new Color(0.2f, 0.25f, 0.29f), 0.78f, 0.3f);
        CreateSimpleMaterial("DL_WeakSteel", new Color(0.24f, 0.25f, 0.24f), 0.7f, 0.23f);
        CreateSimpleMaterial("DL_ReinforcedSteel", new Color(0.13f, 0.21f, 0.27f), 0.86f, 0.34f);
        CreateSimpleMaterial("DL_LauncherMetal", new Color(0.14f, 0.17f, 0.19f), 0.84f, 0.36f);
        CreateColorMapMaterial(
            "DL_KenneyCity",
            $"{CityRoot}/Models/FBX format/Textures/colormap.png",
            new Color(0.58f, 0.62f, 0.68f));
        CreateColorMapMaterial(
            "DL_KenneyFactory",
            $"{FactoryRoot}/Models/FBX format/Textures/colormap.png",
            new Color(0.5f, 0.54f, 0.62f));

        foreach (ModelDefinition definition in ModelDefinitions)
        {
            CreateProjectPrefab(definition);
        }

        AssetDatabase.SaveAssets();
        assetsReady = true;
    }

    private static void CreateProjectPrefab(ModelDefinition definition)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(definition.SourcePath);
        if (model == null)
        {
            throw new UnityException($"Missing imported model '{definition.SourcePath}'.");
        }

        GameObject root = new(definition.PrefabName);
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        PrefabUtility.UnpackPrefabInstance(
            modelInstance,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);
        modelInstance.name = "Visual";
        modelInstance.transform.SetParent(root.transform, false);

        StripNonVisualComponents(root);
        Material material = definition.MaterialKind == MaterialKind.City
            ? LoadMaterial("DL_KenneyCity")
            : LoadMaterial("DL_KenneyFactory");
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = material;
        }

        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{definition.PrefabName}.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void StripNonVisualComponents(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component is Transform or MeshFilter or MeshRenderer)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    internal static void ValidateProjectPrefabs()
    {
        foreach (ModelDefinition definition in ModelDefinitions)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/{definition.PrefabName}.prefab");
            if (prefab == null)
            {
                throw new UnityException($"Missing project-owned prefab '{definition.PrefabName}'.");
            }

            ValidateVisualOnlyHierarchy(prefab.transform, $"Prefab {definition.PrefabName}");
        }
    }

    internal static void ValidateVisualOnlyHierarchy(Transform root, string context)
    {
        if (root.GetComponentInChildren<Rigidbody>(true) != null
            || root.GetComponentInChildren<Collider>(true) != null
            || root.GetComponentInChildren<Joint>(true) != null
            || root.GetComponentInChildren<MonoBehaviour>(true) != null)
        {
            throw new UnityException($"{context} contains a physics or gameplay component.");
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.sharedMaterial == null
                || renderer.sharedMaterial.shader == null
                || !renderer.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            {
                throw new UnityException($"{context} contains an incompatible renderer material.");
            }
        }
    }

    private static void ConfigureTexture(string path, bool isNormalMap, bool sRgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new UnityException($"Missing imported texture '{path}'.");
        }

        importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = sRgb;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void CreateSmoothnessMask(string roughnessPath, string outputPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(roughnessPath) as TextureImporter;
        if (importer == null)
        {
            throw new UnityException($"Missing roughness texture '{roughnessPath}'.");
        }

        importer.isReadable = true;
        importer.sRGBTexture = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);
        Color[] sourcePixels = roughness.GetPixels();
        var mask = new Texture2D(roughness.width, roughness.height, TextureFormat.RGBA32, false, true);
        var maskPixels = new Color[sourcePixels.Length];
        for (int index = 0; index < sourcePixels.Length; index++)
        {
            float smoothness = 1f - sourcePixels[index].r;
            maskPixels[index] = new Color(0f, 0f, 0f, smoothness);
        }

        mask.SetPixels(maskPixels);
        mask.Apply();
        File.WriteAllBytes(outputPath, mask.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(mask);
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

        TextureImporter maskImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        maskImporter.sRGBTexture = false;
        maskImporter.maxTextureSize = 2048;
        maskImporter.textureCompression = TextureImporterCompression.CompressedHQ;
        maskImporter.SaveAndReimport();

        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static void CreateSurfaceMaterial(
        string name,
        string baseMapPath,
        string normalMapPath,
        string maskMapPath,
        Color tint,
        Vector2 tiling,
        float bumpScale)
    {
        Material material = GetOrCreateMaterial(name);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(maskMapPath));
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTextureScale("_BumpMap", tiling);
        material.SetTextureScale("_MetallicGlossMap", tiling);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 1f);
        material.SetFloat("_BumpScale", bumpScale);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
    }

    private static void CreateConcreteMottleTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x / 47f + 3.1f, y / 47f + 8.7f);
                float fine = Mathf.PerlinNoise(x / 13f + 17.4f, y / 13f + 2.6f);
                float value = Mathf.Lerp(0.78f, 0.98f, broad * 0.72f + fine * 0.28f);
                pixels[y * size + x] = new Color(value, value, value);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(ConcreteMottlePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(ConcreteMottlePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(ConcreteMottlePath) as TextureImporter;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.maxTextureSize = size;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void CreateProceduralConcreteMaterial(
        string name,
        Color tint,
        Vector2 tiling,
        float smoothness)
    {
        Material material = GetOrCreateMaterial(name);
        Texture2D mottle = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteMottlePath);
        material.SetTexture("_BaseMap", mottle);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTexture("_BumpMap", null);
        material.SetTexture("_MetallicGlossMap", null);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_BumpScale", 0f);
        material.DisableKeyword("_NORMALMAP");
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
    }

    private static void CreateSimpleMaterial(
        string name,
        Color color,
        float metallic,
        float smoothness)
    {
        Material material = GetOrCreateMaterial(name);
        material.SetTexture("_BaseMap", null);
        material.SetTexture("_BumpMap", null);
        material.SetTexture("_MetallicGlossMap", null);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.DisableKeyword("_NORMALMAP");
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
    }

    private static void CreateColorMapMaterial(string name, string colorMapPath, Color tint)
    {
        ConfigureTexture(colorMapPath, false, true);
        Material material = GetOrCreateMaterial(name);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colorMapPath));
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", 0.08f);
        material.SetFloat("_Smoothness", 0.28f);
        EditorUtility.SetDirty(material);
    }

    private static Material GetOrCreateMaterial(string name)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            throw new UnityException("URP Lit shader is unavailable.");
        }

        material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    internal static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        if (material == null)
        {
            throw new UnityException($"Missing project presentation material '{name}'.");
        }

        return material;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
    private readonly struct ModelDefinition
    {
        public ModelDefinition(string prefabName, string sourcePath, MaterialKind materialKind)
        {
            PrefabName = prefabName;
            SourcePath = sourcePath;
            MaterialKind = materialKind;
        }

        public string PrefabName { get; }
        public string SourcePath { get; }
        public MaterialKind MaterialKind { get; }
    }

    private enum MaterialKind
    {
        City,
        Factory
    }
}
