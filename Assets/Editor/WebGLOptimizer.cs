using UnityEngine;
using UnityEditor;

public class WebGLOptimizer : EditorWindow
{
    int maxTextureSize = 1024;
    float audioQuality = 0.5f;

    [MenuItem("Tools/WebGL Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<WebGLOptimizer>("WebGL Optimizer");
    }

    void OnGUI()
    {
        GUILayout.Label("Texture Settings", EditorStyles.boldLabel);
        maxTextureSize = EditorGUILayout.IntPopup("Max Texture Size", maxTextureSize,
            new string[] { "256", "512", "1024", "2048" },
            new int[] { 256, 512, 1024, 2048 });

        GUILayout.Space(10);
        GUILayout.Label("Audio Settings", EditorStyles.boldLabel);
        audioQuality = EditorGUILayout.Slider("Vorbis Quality", audioQuality, 0.1f, 1f);

        GUILayout.Space(20);
        if (GUILayout.Button("Optimize for WebGL", GUILayout.Height(40)))
        {
            OptimizeTextures();
            OptimizeAudio();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", "WebGL optimization complete! Rebuild your project.", "OK");
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Revert WebGL Overrides", GUILayout.Height(40)))
        {
            RevertTextures();
            RevertAudio();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", "Reverted all WebGL overrides.", "OK");
        }
    }

    void OptimizeTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        int count = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Optimizing Textures", path, (float)i / guids.Length);

            if (path.Contains("Lightmap") || path.Contains("lightmap") || path.Contains("LightingData"))
            {
                Debug.Log($"Skipping lightmap: {path}");
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType == TextureImporterType.Lightmap) continue;

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("WebGL");
            settings.overridden = true;
            settings.maxTextureSize = maxTextureSize;
            settings.format = TextureImporterFormat.ASTC_4x4;
            settings.compressionQuality = 50;

            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            count++;
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"WebGL Optimizer: processed {count} textures");
    }

    void OptimizeAudio()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        int count = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Optimizing Audio", path, (float)i / guids.Length);

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            AudioImporterSampleSettings settings = importer.GetOverrideSampleSettings("WebGL");
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = audioQuality;

            importer.SetOverrideSampleSettings("WebGL", settings);
            importer.SaveAndReimport();
            count++;
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"WebGL Optimizer: processed {count} audio clips");
    }

    void RevertTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Reverting Textures", path, (float)i / guids.Length);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("WebGL");
            settings.overridden = false;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("WebGL Optimizer: reverted all texture overrides");
    }

    void RevertAudio()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Reverting Audio", path, (float)i / guids.Length);

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            importer.ClearSampleSettingOverride("WebGL");
            importer.SaveAndReimport();
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("WebGL Optimizer: reverted all audio overrides");
    }
}