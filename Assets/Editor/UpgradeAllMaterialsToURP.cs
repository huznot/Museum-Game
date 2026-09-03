using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering; // <- needed for GraphicsSettings


public class UpgradeAllMaterialsToURP : EditorWindow
{
    [MenuItem("Tools/URP/Upgrade All Materials")]
    public static void UpgradeAllMaterials()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");

        int count = 0;
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                // Upgrade Standard shader to URP/Lit
                if (mat.shader.name == "Standard")
                {
                    UpgradeMaterialToURP(mat);
                    count++;
                }
            }
        }

        Debug.Log($"[URP Upgrade] Upgraded {count} materials to URP/Lit.");
    }

    private static void UpgradeMaterialToURP(Material mat)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            mat.shader = urpLit;
        }
    }
}
