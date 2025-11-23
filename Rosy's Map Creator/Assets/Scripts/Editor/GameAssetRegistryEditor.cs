#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameAssetRegistry))]
public class GameAssetRegistryEditor : Editor
{
    private GameAssetRegistry _registry;

    private const string PrefabRoot = "Assets/Prefabs";
    private const string TextureRoot = "Assets/Textures";
    private const string AutoIconRoot = "Assets/Textures/AutoIcons";

    private void OnEnable()
    {
        _registry = (GameAssetRegistry)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Registry Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan Prefabs Folder and Rebuild Records"))
        {
            ScanPrefabsAndRebuild();
        }

        if (GUILayout.Button("Rebuild Index Only"))
        {
            _registry.BuildIndex();
            EditorUtility.SetDirty(_registry);
        }

        EditorGUILayout.HelpBox(
            "Scan Prefabs Folder and Rebuild Records:\n" +
            "- Scans Assets/Prefabs for all prefabs\n" +
            "- Id from relative path (lowercase, '/' kept)\n" +
            "- Category from top folder: tiles/units/buildings/decoration\n" +
            "- Faction from subfolder: blue/green/red/yellow/neutral\n" +
            "- Seasonal Icons per record:\n" +
            "   1) Uses <id>_spring/summer/fall/winter sprites if present under Assets/Textures\n" +
            "   2) Else uses generic <id> / prefabName sprite for all seasons\n" +
            "   3) Else auto-generates seasonal icons with the 4 materials into Assets/Textures/AutoIcons",
            MessageType.Info);
    }

    private void ScanPrefabsAndRebuild()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });

        var newRecords = guids
      .Select(AssetDatabase.GUIDToAssetPath)
      .Select(CreateRecordFromPath)
      .Where(r => r != null)
      // .OrderBy(r => r.DisplayName) // optional: stable ordering
      .ToList();

        Undo.RecordObject(_registry, "Rebuild GameAssetRegistry");
        _registry.ClearAll();

        int nextId = 1;
        foreach (var rec in newRecords)
        {
            rec.SetNumericId(nextId++);   // <-- give each record a unique number
            _registry.AddOrReplace(rec);
        }

        _registry.BuildIndex();
        EditorUtility.SetDirty(_registry);


        Debug.Log($"GameAssetRegistry: Rebuilt with {newRecords.Count} records.");
    }

    private GameAssetRecord CreateRecordFromPath(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;
        if (!path.StartsWith(PrefabRoot)) return null;

        string relative = path.Substring(PrefabRoot.Length).TrimStart('/', '\\');
        relative = relative.Replace('\\', '/');

        string id = relative.EndsWith(".prefab")
            ? relative[..^".prefab".Length]
            : relative;
        id = id.ToLowerInvariant();

        string[] parts = id.Split('/');
        if (parts.Length == 0) return null;

        var category = InferCategory(parts[0]);
        var faction = InferFaction(parts);

        var rec = new GameAssetRecord();
        rec.SetId(id);
        rec.SetPrefab(prefab);
        rec.SetCategory(category);
        rec.SetFaction(faction);
        rec.SetDisplayName(ObjectNames.NicifyVariableName(prefab.name));

        AssignIconsForRecord(rec, prefab);

        return rec;
    }

    private static AssetCategory InferCategory(string root)
    {
        return root switch
        {
            "tiles" => AssetCategory.Tile,
            "units" => AssetCategory.Unit,
            "buildings" => AssetCategory.Building,
            "decoration" => AssetCategory.Decoration,
            _ => AssetCategory.Tile
        };
    }

    private static Faction InferFaction(string[] parts)
    {
        foreach (string p in parts)
        {
            switch (p)
            {
                case "blue": return Faction.Blue;
                case "green": return Faction.Green;
                case "red": return Faction.Red;
                case "yellow": return Faction.Yellow;
                case "neutral": return Faction.Neutral;
            }
        }
        return Faction.None;
    }

    // ---------- ICON LOGIC ----------

    private void AssignIconsForRecord(GameAssetRecord rec, GameObject prefab)
    {
        string id = rec.Id;
        string idUnder = id.Replace('/', '_');
        string prefabNameLower = prefab.name.ToLowerInvariant();

        // 1) Try existing season-specific sprites
        Sprite spring = FindSprite($"{idUnder}_spring") ?? FindSprite($"{prefabNameLower}_spring");
        Sprite summer = FindSprite($"{idUnder}_summer") ?? FindSprite($"{prefabNameLower}_summer");
        Sprite fall = FindSprite($"{idUnder}_fall") ?? FindSprite($"{prefabNameLower}_fall");
        Sprite winter = FindSprite($"{idUnder}_winter") ?? FindSprite($"{prefabNameLower}_winter");

        bool anySeasonal =
            spring != null || summer != null || fall != null || winter != null;

        if (anySeasonal)
        {
            if (spring != null) rec.SetIcon(Season.Spring, spring);
            if (summer != null) rec.SetIcon(Season.Summer, summer);
            if (fall != null) rec.SetIcon(Season.Fall, fall);
            if (winter != null) rec.SetIcon(Season.Winter, winter);

            Sprite fb = spring ?? summer ?? fall ?? winter;
            if (rec.GetIcon(Season.Spring) == null) rec.SetIcon(Season.Spring, fb);
            if (rec.GetIcon(Season.Summer) == null) rec.SetIcon(Season.Summer, fb);
            if (rec.GetIcon(Season.Fall) == null) rec.SetIcon(Season.Fall, fb);
            if (rec.GetIcon(Season.Winter) == null) rec.SetIcon(Season.Winter, fb);
            return;
        }

        // 2) Try generic sprite
        Sprite generic =
            FindSprite(idUnder) ??
            FindSprite(id.Replace('/', '_')) ??
            FindSprite(prefabNameLower);

        if (generic != null)
        {
            rec.SetIcon(Season.Spring, generic);
            rec.SetIcon(Season.Summer, generic);
            rec.SetIcon(Season.Fall, generic);
            rec.SetIcon(Season.Winter, generic);
            return;
        }

        // 3) Generate per-season using registry materials
        var springMat = _registry.springMaterial;
        var summerMat = _registry.summerMaterial;
        var fallMat = _registry.fallMaterial;
        var winterMat = _registry.winterMaterial;

        bool hasSeasonMats =
            springMat != null || summerMat != null || fallMat != null || winterMat != null;

        if (hasSeasonMats)
        {
            Sprite sSpring = springMat != null ? CapturePrefabWithMaterial(prefab, idUnder + "_spring", springMat) : null;
            Sprite sSummer = summerMat != null ? CapturePrefabWithMaterial(prefab, idUnder + "_summer", summerMat) : null;
            Sprite sFall = fallMat != null ? CapturePrefabWithMaterial(prefab, idUnder + "_fall", fallMat) : null;
            Sprite sWinter = winterMat != null ? CapturePrefabWithMaterial(prefab, idUnder + "_winter", winterMat) : null;

            if (sSpring != null) rec.SetIcon(Season.Spring, sSpring);
            if (sSummer != null) rec.SetIcon(Season.Summer, sSummer);
            if (sFall != null) rec.SetIcon(Season.Fall, sFall);
            if (sWinter != null) rec.SetIcon(Season.Winter, sWinter);

            Sprite fb = sSpring ?? sSummer ?? sFall ?? sWinter;
            if (fb != null)
            {
                if (rec.GetIcon(Season.Spring) == null) rec.SetIcon(Season.Spring, fb);
                if (rec.GetIcon(Season.Summer) == null) rec.SetIcon(Season.Summer, fb);
                if (rec.GetIcon(Season.Fall) == null) rec.SetIcon(Season.Fall, fb);
                if (rec.GetIcon(Season.Winter) == null) rec.SetIcon(Season.Winter, fb);
                return;
            }
        }

        // 4) Fallback: single auto icon from prefab (no override mat)
        Sprite fallbackGeneric = CapturePrefabWithMaterial(prefab, idUnder, null);
        if (fallbackGeneric != null)
        {
            rec.SetIcon(Season.Spring, fallbackGeneric);
            rec.SetIcon(Season.Summer, fallbackGeneric);
            rec.SetIcon(Season.Fall, fallbackGeneric);
            rec.SetIcon(Season.Winter, fallbackGeneric);
        }
    }

    private Sprite FindSprite(string baseName)
    {
        if (string.IsNullOrEmpty(baseName)) return null;

        string[] guids = AssetDatabase.FindAssets($"{baseName} t:Sprite", new[] { TextureRoot });
        if (guids == null || guids.Length == 0) return null;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null &&
                spr.name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                return spr;
        }

        // fallback: first found
        var firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Sprite>(firstPath);
    }

    private Sprite CapturePrefabWithMaterial(GameObject prefab, string fileBaseName, Material overrideMat)
    {
        string autoLocal = AutoIconRoot.Substring("Assets/".Length); // "Textures/AutoIcons"
        string dirFull = Path.Combine(Application.dataPath, autoLocal);
        if (!Directory.Exists(dirFull))
            Directory.CreateDirectory(dirFull);

        string fileFullPath = Path.Combine(dirFull, fileBaseName + ".png");
        string assetPath = Path.Combine(AutoIconRoot, fileBaseName + ".png").Replace("\\", "/");

        // --- Instantiate prefab ---
        var temp = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (temp == null) return null;
        temp.hideFlags = HideFlags.HideAndDontSave;

        var renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            UnityEngine.Object.DestroyImmediate(temp);
            return null;
        }

        // Apply override material if provided
        if (overrideMat != null)
        {
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = overrideMat;
                r.sharedMaterials = mats;
            }
        }

        // --- Bounds for framing ---
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (size <= 0f) size = 1f;

        // Pull back a bit so we're nicely zoomed out
        float dist = size * 3.0f;

        // --- Camera at 45-degree angle, zoomed out ---
        var camGO = new GameObject("IconCamera");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = size * 0.9f;   // bigger number = more zoomed out
        cam.cullingMask = ~0;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        // 45° iso-style angle: from above/front
        Vector3 dir = new Vector3(1f, 1f, -1f).normalized;
        cam.transform.position = b.center + dir * dist;
        cam.transform.LookAt(b.center);

        // --- Render to RT ---
        var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();

        var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        tex.Apply();

        RenderTexture.active = prevRT;
        cam.targetTexture = null;

        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(camGO);
        UnityEngine.Object.DestroyImmediate(temp);

        // --- Save PNG ---
        File.WriteAllBytes(fileFullPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        // --- Import as SPRITE (this was the missing bit) ---
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        // --- Load sprite asset ---
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            sprite = all.OfType<Sprite>().FirstOrDefault();
        }

        if (sprite == null)
        {
            Debug.LogWarning($"GameAssetRegistry: Created icon texture but could not load Sprite at '{assetPath}'.");
        }

        return sprite;
    }
}


#endif
