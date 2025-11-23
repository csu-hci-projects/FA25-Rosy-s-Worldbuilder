// Assets/Editor/BatchPrefabIconRenderer.cs
// Batch render prefab icons with seasonal material swaps.
// Renders one PNG per season (Spring, Summer, Fall, Winter) with transparent background.

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BatchPrefabIconRenderer : EditorWindow
{
    // ====== General ======
    [Header("Source & Output")]
    [SerializeField] private DefaultAsset sourceFolder;   // folder with prefabs
    [SerializeField] private DefaultAsset outputFolder;   // where to save PNGs

    [Header("Render Settings")]
    [SerializeField] private int resolution = 512;        // 256/512/1024
    [SerializeField] private float padding = 0.18f;       // frame padding
    [SerializeField] private Vector3 isoAngles = new(30f, 45f, 0f);
    [SerializeField] private Vector3 lightEuler = new(50f, -30f, 0f);
    [SerializeField] private float lightIntensity = 1.35f;
    [SerializeField] private bool autoLayerCull = true;
    [SerializeField] private string filenameSuffix = "";  // appended after prefab name
    [SerializeField] private bool overwrite = true;

    // ====== Seasons ======
    public enum Season { Spring, Summer, Fall, Winter }

    [Header("Seasonal Materials (optional but recommended)")]
    [SerializeField] private Material springMaterial;
    [SerializeField] private Material summerMaterial;
    [SerializeField] private Material fallMaterial;
    [SerializeField] private Material winterMaterial;

    [Header("Season Output Style")]
    [SerializeField] private bool useSeasonSubfolders = false; // Icons/Spring/Prefab.png, etc.
    [SerializeField] private string springSuffix = "_spring";
    [SerializeField] private string summerSuffix = "_summer";
    [SerializeField] private string fallSuffix = "_fall";
    [SerializeField] private string winterSuffix = "_winter";

    [Header("Material Replacement Mode")]
    [Tooltip("-1 replaces ALL slots with the season's material.\n" +
             ">=0 replaces only that material index (if exists).")]
    [SerializeField] private int targetMaterialIndex = -1;

    [Tooltip("If true, ignores target index and forces ALL renderer slots to seasonal material.")]
    [SerializeField] private bool forceReplaceAllSlots = true;

    [MenuItem("Tools/Prefab Icon Renderer")]
    public static void ShowWindow()
    {
        var win = GetWindow<BatchPrefabIconRenderer>();
        win.titleContent = new GUIContent("Prefab Icon Renderer");
        win.minSize = new Vector2(460, 420);
        win.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Source & Output", EditorStyles.boldLabel);
        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefabs Folder", sourceFolder, typeof(DefaultAsset), false);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntPopup("Resolution", resolution,
            new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });
        padding = EditorGUILayout.Slider("Padding", padding, 0f, 0.4f);
        isoAngles = EditorGUILayout.Vector3Field("Camera Angles", isoAngles);
        lightEuler = EditorGUILayout.Vector3Field("Light Angles", lightEuler);
        lightIntensity = EditorGUILayout.Slider("Light Intensity", lightIntensity, 0f, 3f);
        autoLayerCull = EditorGUILayout.Toggle("Auto Layer Cull", autoLayerCull);
        filenameSuffix = EditorGUILayout.TextField("Base Filename Suffix", filenameSuffix);
        overwrite = EditorGUILayout.Toggle("Overwrite Existing", overwrite);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Seasonal Materials", EditorStyles.boldLabel);
        springMaterial = (Material)EditorGUILayout.ObjectField("Spring", springMaterial, typeof(Material), false);
        summerMaterial = (Material)EditorGUILayout.ObjectField("Summer", summerMaterial, typeof(Material), false);
        fallMaterial = (Material)EditorGUILayout.ObjectField("Fall", fallMaterial, typeof(Material), false);
        winterMaterial = (Material)EditorGUILayout.ObjectField("Winter", winterMaterial, typeof(Material), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Season Output Style", EditorStyles.boldLabel);
        useSeasonSubfolders = EditorGUILayout.Toggle("Use Season Subfolders", useSeasonSubfolders);
        using (new EditorGUI.DisabledScope(useSeasonSubfolders))
        {
            springSuffix = EditorGUILayout.TextField("Spring Suffix", springSuffix);
            summerSuffix = EditorGUILayout.TextField("Summer Suffix", summerSuffix);
            fallSuffix = EditorGUILayout.TextField("Fall Suffix", fallSuffix);
            winterSuffix = EditorGUILayout.TextField("Winter Suffix", winterSuffix);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Material Replacement", EditorStyles.boldLabel);
        forceReplaceAllSlots = EditorGUILayout.Toggle("Force Replace All Slots", forceReplaceAllSlots);
        using (new EditorGUI.DisabledScope(forceReplaceAllSlots))
        {
            targetMaterialIndex = EditorGUILayout.IntField("Target Material Index", targetMaterialIndex);
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanRender()))
        {
            if (GUILayout.Button("Render Icons (All Seasons)"))
                RenderAll();
        }

        EditorGUILayout.HelpBox(
            "How it works:\n" +
            "• For each prefab, we swap its materials to your seasonal material(s),\n" +
            "  render a transparent PNG, then restore the originals.\n" +
            "• If you leave a season's material empty, we still render that season\n" +
            "  using the prefab's original materials (useful as a fallback).",
            MessageType.Info);
    }

    bool CanRender()
    {
        return sourceFolder != null && outputFolder != null && resolution > 0;
    }

    void RenderAll()
    {
        string srcPath = AssetDatabase.GetAssetPath(sourceFolder);
        string outPath = AssetDatabase.GetAssetPath(outputFolder);

        if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(outPath))
        {
            EditorUtility.DisplayDialog("Error", "Please set both Prefabs Folder and Output Folder.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { srcPath });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Prefabs", "No prefabs found in the selected folder.", "OK");
            return;
        }

        // Temp scene
        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Layer isolation
        int renderLayer = 0;
        if (autoLayerCull)
            renderLayer = CreateOrGetLayer("IconRenderTemp");

        // Camera
        var camGO = new GameObject("IconCamera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = true;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;
        cam.allowHDR = false;
        camGO.transform.rotation = Quaternion.Euler(isoAngles);

        if (autoLayerCull)
            cam.cullingMask = 1 << renderLayer;

        // Light
        var lightGO = new GameObject("IconLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = lightIntensity;
        lightGO.transform.rotation = Quaternion.Euler(lightEuler);

        // RT
        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;

        var seasons = new[]
        {
            (Season.Spring, springMaterial, springSuffix, "Spring"),
            (Season.Summer, summerMaterial, summerSuffix, "Summer"),
            (Season.Fall,   fallMaterial,   fallSuffix,   "Fall"),
            (Season.Winter, winterMaterial, winterSuffix, "Winter"),
        };

        int done = 0;
        int total = guids.Length * seasons.Length;

        try
        {
            foreach (string guid in guids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                foreach (var (season, mat, suffix, folderName) in seasons)
                {
                    // Instantiate target
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, tempScene);
                    go.name = "ICON_TARGET";
                    if (autoLayerCull) SetLayerRecursively(go, renderLayer);
                    go.transform.position = Vector3.zero;
                    go.transform.rotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    // Swap materials (store originals)
                    var rends = go.GetComponentsInChildren<Renderer>(true);
                    var originals = new Dictionary<Renderer, Material[]>();
                    foreach (var r in rends) originals[r] = r.sharedMaterials.ToArray();

                    ApplySeasonMaterials(rends, mat);

                    // Bounds + camera
                    if (!TryGetWorldBounds(go, out Bounds b))
                    {
                        // Fallback to AssetPreview
                        var ap = AssetPreview.GetAssetPreview(prefab);
                        string outFileAP = BuildOutPath(outPath, useSeasonSubfolders ? folderName : null,
                                                        prefab.name, filenameSuffix, suffix);
                        EnsureDirectory(Path.GetDirectoryName(outFileAP)!);
                        if (ap != null) File.WriteAllBytes(outFileAP, ap.EncodeToPNG());
                        RestoreMaterials(rends, originals);
                        DestroyImmediate(go);
                        done++;
                        EditorUtility.DisplayProgressBar($"Rendering {folderName}", prefab.name, (float)done / total);
                        continue;
                    }

                    float radius = b.extents.magnitude;
                    float orthoSize = Mathf.Max(b.extents.x, b.extents.y) * (1f + padding);
                    cam.orthographicSize = Mathf.Max(0.001f, orthoSize);
                    Vector3 camDir = cam.transform.forward;
                    float distance = radius * 3.0f + 0.1f; // a little zoomed out for UI
                    cam.transform.position = b.center - camDir * distance;

                    // Match light to cam
                    lightGO.transform.rotation = camGO.transform.rotation;

                    cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 3f);
                    cam.farClipPlane = distance + radius * 3f;

                    // Render
                    cam.targetTexture = rt;
                    var prevRT = RenderTexture.active;
                    RenderTexture.active = rt;
                    cam.Render();

                    Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
                    tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0, false);
                    tex.Apply(false, false);

                    string outFile = BuildOutPath(outPath, useSeasonSubfolders ? folderName : null,
                                                  prefab.name, filenameSuffix, suffix);

                    if (!overwrite && File.Exists(outFile))
                    {
                        // skip
                    }
                    else
                    {
                        EnsureDirectory(Path.GetDirectoryName(outFile)!);
                        File.WriteAllBytes(outFile, tex.EncodeToPNG());
                    }

                    // Cleanup + restore
                    RestoreMaterials(rends, originals);
                    DestroyImmediate(tex);
                    RenderTexture.active = prevRT;
                    DestroyImmediate(go);

                    done++;
                    EditorUtility.DisplayProgressBar($"Rendering {folderName}", prefab.name, (float)done / total);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (rt != null) rt.Release();
            DestroyImmediate(rt);
            if (camGO) DestroyImmediate(camGO);
            if (lightGO) DestroyImmediate(lightGO);

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Done",
            $"Rendered {done} seasonal icons to:\n{AssetDatabase.GetAssetPath(outputFolder)}",
            "OK");
    }

    // ====== Helpers ======

    static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }

    static string BuildOutPath(string basePath, string seasonFolder, string prefabName, string baseSuffix, string seasonSuffix)
    {
        string folder = string.IsNullOrEmpty(seasonFolder)
            ? basePath
            : Path.Combine(basePath, seasonFolder).Replace("\\", "/");
        string name = prefabName + baseSuffix + seasonSuffix + ".png";
        return Path.Combine(folder, name).Replace("\\", "/");
    }

    static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return false;

        bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);
        return true;
    }

    void ApplySeasonMaterials(Renderer[] rends, Material seasonMat)
    {
        // If no material provided for this season, keep originals.
        if (seasonMat == null) return;

        foreach (var r in rends)
        {
            var mats = r.sharedMaterials;
            if (forceReplaceAllSlots || targetMaterialIndex < 0)
            {
                for (int i = 0; i < mats.Length; i++) mats[i] = seasonMat;
            }
            else if (targetMaterialIndex < mats.Length)
            {
                mats[targetMaterialIndex] = seasonMat;
            }
            // else: index out of range -> leave as is

            r.sharedMaterials = mats;
        }
    }

    static void RestoreMaterials(Renderer[] rends, Dictionary<Renderer, Material[]> originals)
    {
        foreach (var r in rends)
        {
            if (r == null) continue;
            if (originals.TryGetValue(r, out var mats))
                r.sharedMaterials = mats;
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    static int CreateOrGetLayer(string layerName)
    {
        for (int i = 0; i < 32; i++)
        {
            string ln = LayerMask.LayerToName(i);
            if (ln == layerName) return i;
        }
        var tagMan = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagMan.FindProperty("layers");
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagMan.ApplyModifiedProperties();
                return i;
            }
        }
        return 0; // default
    }
}
