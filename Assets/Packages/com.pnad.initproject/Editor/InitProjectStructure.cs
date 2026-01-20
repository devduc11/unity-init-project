using UnityEditor;
using UnityEngine;
using System.IO;

public class InitProjectStructure
{
    [MenuItem("Tools/Init Project Structure")]
    public static void Init()
    {
        string root = "Assets/_Project";

        // Root
        CreateFolder("Assets", "_Project");

        // Basic folders
        CreateFolder(root, "Material");
        CreateFolder(root, "Prefab");
        CreateFolder(root, "Scripts");
        CreateFolder(root, "ScriptableObject");
        CreateFolder(root, "Sprites");

        // Prefab/UI
        CreateFolder($"{root}/Prefab", "UI");

        // Scripts/UI/UIManager
        CreateFolder($"{root}/Scripts", "UI");
        CreateFolder($"{root}/Scripts", "SaveGame");
        CreateFolder($"{root}/Scripts", "ScriptableObject");
        CreateFolder($"{root}/Scripts/UI", "UIManager");

        // ===== SCENES LOGIC =====
        HandleScenesFolder(root);

        AssetDatabase.Refresh();
        Debug.Log("✅ Init Project Structure DONE");
    }

    // ==============================
    static void HandleScenesFolder(string root)
    {
        string oldScenesPath = "Assets/Scenes";
        string newScenesPath = $"{root}/Scenes";

        // Không có Assets/Scenes → chỉ cần tạo mới
        if (!AssetDatabase.IsValidFolder(oldScenesPath))
        {
            if (!AssetDatabase.IsValidFolder(newScenesPath))
            {
                AssetDatabase.CreateFolder(root, "Scenes");
            }
            Debug.Log("ℹ No Assets/Scenes found → created _Project/Scenes");
            return;
        }

        // Có Assets/Scenes
        if (!AssetDatabase.IsValidFolder(newScenesPath))
        {
            // Chưa có _Project/Scenes → move nguyên folder
            string result = AssetDatabase.MoveAsset(oldScenesPath, newScenesPath);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError("❌ Move Scenes failed: " + result);
            }
            else
            {
                Debug.Log("➡ Moved Assets/Scenes → _Project/Scenes");
            }
        }
        else
        {
            // ĐÃ có _Project/Scenes → move từng scene
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { oldScenesPath });

            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(scenePath);
                string targetPath = $"{newScenesPath}/{fileName}";

                AssetDatabase.MoveAsset(scenePath, targetPath);
            }

            // Nếu Assets/Scenes rỗng → xóa
            if (Directory.GetFiles(oldScenesPath).Length <= 1) // chỉ còn .meta
            {
                AssetDatabase.DeleteAsset(oldScenesPath);
            }

            Debug.Log("➡ Moved scene files into _Project/Scenes");
        }
    }


    static void CreateFolder(string parent, string folderName)
    {
        string path = Path.Combine(parent, folderName);
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
