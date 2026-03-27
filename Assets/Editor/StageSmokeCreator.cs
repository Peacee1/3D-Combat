#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool để tạo StageSmoke prefab đúng cách từ trong Unity Editor.
/// Menu: Tools → VFX → Create StageSmoke Prefab
/// </summary>
public static class StageSmokeCreator
{
    [MenuItem("Tools/VFX/Create StageSmoke Prefab")]
    public static void CreateStageSmokePreab()
    {
        // Đảm bảo thư mục tồn tại
        string prefabDir = "Assets/Prefabs/VFX";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");

        // Tạo GameObject gốc
        GameObject root = new GameObject("StageSmoke");
        var controller = root.AddComponent<StageSmokeController>();

        // Gán Smoke20ss material nếu tìm được
        string[] matGuids = AssetDatabase.FindAssets("Smoke20ss t:Material");
        if (matGuids.Length > 0)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(matGuids[0]);
            controller.smokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Debug.Log($"[StageSmokeCreator] Found material: {matPath}");
        }
        else
        {
            Debug.LogWarning("[StageSmokeCreator] Smoke20ss material not found. Please assign manually.");
        }

        // Default values tối ưu cho khói sân khấu
        controller.emitterCount      = 6;
        controller.emitAreaSize      = new Vector2(10f, 6f);
        controller.emissionRate      = 5f;
        controller.particleLifetime  = 8f;
        controller.startSize         = 2f;
        controller.endSize           = 6f;
        controller.riseSpeed         = 0.1f;
        controller.driftSpeed        = 0.05f;
        controller.heightLimit       = 1.5f;
        controller.smokeColor        = new Color(0.9f, 0.9f, 0.95f, 0.6f);
        controller.smokeColorFade    = new Color(0.9f, 0.9f, 0.95f, 0f);
        controller.playOnStart       = true;
        controller.isLooping         = true;

        // Lưu prefab
        string prefabPath = $"{prefabDir}/StageSmoke.prefab";
        bool success;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);

        // Dọn dẹp GameObject tạm
        Object.DestroyImmediate(root);

        if (success)
        {
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[StageSmokeCreator] ✅ StageSmoke prefab created at: {prefabPath}");
            EditorUtility.DisplayDialog("Success",
                $"StageSmoke prefab created!\nPath: {prefabPath}\n\nDrag it into the Scene to use.",
                "OK");
        }
        else
        {
            Debug.LogError("[StageSmokeCreator] ❌ Failed to create prefab.");
        }
    }

    /// <summary>
    /// Thêm nhanh StageSmoke vào Scene hiện tại (không tạo prefab)
    /// </summary>
    [MenuItem("Tools/VFX/Add StageSmoke to Scene")]
    public static void AddStageSmokeToScene()
    {
        // Thử dùng prefab nếu đã có
        string prefabPath = "Assets/Prefabs/VFX/StageSmoke.prefab";
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        GameObject instance;
        if (existingPrefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(existingPrefab);
        }
        else
        {
            // Tạo trực tiếp trong scene
            instance = new GameObject("StageSmoke");
            var controller = instance.AddComponent<StageSmokeController>();

            string[] matGuids = AssetDatabase.FindAssets("Smoke20ss t:Material");
            if (matGuids.Length > 0)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                controller.smokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            controller.emitterCount     = 6;
            controller.emitAreaSize     = new Vector2(10f, 6f);
            controller.emissionRate     = 5f;
            controller.particleLifetime = 8f;
            controller.startSize        = 2f;
            controller.endSize          = 6f;
            controller.riseSpeed        = 0.1f;
            controller.driftSpeed       = 0.05f;
            controller.heightLimit      = 1.5f;
            controller.smokeColor       = new Color(0.9f, 0.9f, 0.95f, 0.6f);
            controller.smokeColorFade   = new Color(0.9f, 0.9f, 0.95f, 0f);
            controller.playOnStart      = true;
            controller.isLooping        = true;
        }

        // Đặt vào trung tâm Scene view
        if (SceneView.lastActiveSceneView != null)
        {
            instance.transform.position = SceneView.lastActiveSceneView.pivot;
            instance.transform.position = new Vector3(
                instance.transform.position.x, 0f, instance.transform.position.z);
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Add StageSmoke");
        Debug.Log("[StageSmokeCreator] ✅ StageSmoke added to scene.");
    }
}
#endif
