using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SeasonalSpriteMerger
{
    [MenuItem("Tools/Merge Seasonal Icons Into Single Sprite")]
    public static void MergeSeasonalIcons()
    {
        string root = EditorUtility.OpenFolderPanel("Select Folder With Seasonal Icons", "Assets", "");
        if (string.IsNullOrEmpty(root)) return;

        if (!root.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a folder inside your Assets directory.", "OK");
            return;
        }

        string relRoot = "Assets" + root.Substring(Application.dataPath.Length);
        string[] springIcons = Directory.GetFiles(relRoot, "*_spring.png", SearchOption.AllDirectories);
        int made = 0;

        foreach (var springPath in springIcons)
        {
            string folder = Path.GetDirectoryName(springPath).Replace("\\", "/");
            string baseName = Path.GetFileNameWithoutExtension(springPath).Replace("_spring", "");
            string mergedPath = $"{folder}/{baseName}.png";

            string summerPath = $"{folder}/{baseName}_summer.png";
            string fallPath = $"{folder}/{baseName}_fall.png";
            string winterPath = $"{folder}/{baseName}_winter.png";

            if (!File.Exists(summerPath) || !File.Exists(fallPath) || !File.Exists(winterPath))
                continue;

            // Load all four textures
            Texture2D spring = AssetDatabase.LoadAssetAtPath<Texture2D>(springPath);
            Texture2D summer = AssetDatabase.LoadAssetAtPath<Texture2D>(summerPath);
            Texture2D fall = AssetDatabase.LoadAssetAtPath<Texture2D>(fallPath);
            Texture2D winter = AssetDatabase.LoadAssetAtPath<Texture2D>(winterPath);
            if (!spring || !summer || !fall || !winter) continue;

            int width = spring.width;
            int height = spring.height;
            int totalWidth = width * 4;

            // Combine them horizontally
            Texture2D combined = new Texture2D(totalWidth, height, TextureFormat.RGBA32, false);
            combined.SetPixels(0, 0, width, height, spring.GetPixels());
            combined.SetPixels(width, 0, width, height, summer.GetPixels());
            combined.SetPixels(width * 2, 0, width, height, fall.GetPixels());
            combined.SetPixels(width * 3, 0, width, height, winter.GetPixels());
            combined.Apply();

            byte[] pngData = combined.EncodeToPNG();
            File.WriteAllBytes(mergedPath, pngData);
            AssetDatabase.ImportAsset(mergedPath, ImportAssetOptions.ForceUpdate);

            // Set up importer as multi-sprite
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(mergedPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;

            List<SpriteMetaData> metas = new();
            string[] names = { "spring", "summer", "fall", "winter" };

            for (int i = 0; i < 4; i++)
            {
                SpriteMetaData smd = new SpriteMetaData();
                smd.rect = new Rect(i * width, 0, width, height);
                smd.name = $"{baseName}_{names[i]}";
                smd.pivot = new Vector2(0.5f, 0.5f);
                smd.alignment = (int)SpriteAlignment.Center;
                metas.Add(smd);
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();

            Object.DestroyImmediate(combined);
            made++;
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Done", $"Merged {made} icons into single seasonal sprite sheets.", "OK");
    }
}
