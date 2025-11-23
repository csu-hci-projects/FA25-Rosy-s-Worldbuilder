// Assets/Editor/SeasonalSpritePostProcessor.cs
// Converts rendered PNGs into Sprites and generates SeasonalSpriteSets.

using UnityEngine;
using UnityEditor;
using System.IO;

[System.Serializable]
public class SeasonalSpriteSet : ScriptableObject
{
    public Sprite spring;
    public Sprite summer;
    public Sprite fall;
    public Sprite winter;
}

public class SeasonalSpritePostProcessor
{
    [MenuItem("Tools/Convert Icons to Sprites")]
    public static void ConvertIconsToSprites()
    {
        string root = EditorUtility.OpenFolderPanel("Select Icon Root Folder", "Assets", "");
        if (string.IsNullOrEmpty(root)) return;

        if (!root.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a folder inside your Assets directory.", "OK");
            return;
        }

        string assetsRelative = "Assets" + root.Substring(Application.dataPath.Length);
        string[] pngs = Directory.GetFiles(assetsRelative, "*.png", SearchOption.AllDirectories);

        int done = 0;
        foreach (var png in pngs)
        {
            EditorUtility.DisplayProgressBar("Importing Sprites", png, (float)done / pngs.Length);

            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(png);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
            done++;
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"Converted {done} icons into Sprites ✅");

        CreateSpriteSets(assetsRelative);
    }

    static void CreateSpriteSets(string root)
    {
        string[] pngs = Directory.GetFiles(root, "*_spring.png", SearchOption.AllDirectories);
        int made = 0;

        foreach (var springPath in pngs)
        {
            string baseName = Path.GetFileNameWithoutExtension(springPath);
            string prefabBase = baseName.Replace("_spring", "");
            string folder = Path.GetDirectoryName(springPath).Replace("\\", "/");

            string assetPath = $"{folder}/{prefabBase}.asset";
            if (File.Exists(assetPath)) continue; // skip existing

            SeasonalSpriteSet set = ScriptableObject.CreateInstance<SeasonalSpriteSet>();

            set.spring = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefabBase}_spring.png");
            set.summer = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefabBase}_summer.png");
            set.fall = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefabBase}_fall.png");
            set.winter = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefabBase}_winter.png");

            AssetDatabase.CreateAsset(set, assetPath);
            AssetDatabase.SaveAssets();
            made++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Created {made} SeasonalSpriteSets 🌦️");
    }
}
