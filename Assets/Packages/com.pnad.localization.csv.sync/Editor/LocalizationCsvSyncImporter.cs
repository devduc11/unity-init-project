#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine.Localization.Tables;
using System.IO;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class LocalizationCsvSyncImporter : EditorWindow
{
    private string csvUrl = "";
    private StringTableCollection targetCollection;

    [MenuItem("Localization Tools/Sync CSV (Win-Mac Fixed)")]
    public static void Open()
    {
        GetWindow<LocalizationCsvSyncImporter>("Localization CSV Sync");
    }

    private void OnGUI()
    {
        GUILayout.Label("Localization CSV Sync (Fixed Vietnamese & Header)", EditorStyles.boldLabel);

        targetCollection = (StringTableCollection)EditorGUILayout.ObjectField(
            "String Table Collection",
            targetCollection,
            typeof(StringTableCollection),
            false
        );

        csvUrl = EditorGUILayout.TextField("CSV URL", csvUrl);

        EditorGUILayout.Space();

        if (GUILayout.Button("SYNC CSV (Add / Update / Delete)"))
        {
            if (targetCollection == null || string.IsNullOrEmpty(csvUrl))
            {
                Debug.LogError("❌ Vui lòng gán StringTableCollection và nhập URL!");
                return;
            }

            SyncCsv(csvUrl, targetCollection);
        }
    }

    // ===================== CORE =====================

    private static void SyncCsv(string url, StringTableCollection collection)
    {
        var www = UnityWebRequest.Get(url);
        var operation = www.SendWebRequest();

        while (!operation.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ Lỗi tải CSV: {www.error}");
            return;
        }

        byte[] rawData = www.downloadHandler.data;
        string csvText = Encoding.UTF8.GetString(rawData);
        csvText = csvText.Trim('\uFEFF', '\u200B');

        // 1. Lọc các Key duy nhất và loại bỏ trùng lặp từ nội dung CSV gốc
        string uniqueCsvText = FilterDuplicateKeys(csvText);
        
        // 2. Parse keys để dùng cho việc xóa dòng thừa
        HashSet<string> csvKeys = ExtractKeysFromCsv(uniqueCsvText);

        // 3. Import dữ liệu đã lọc sạch vào Collection
        using (StringReader reader = new StringReader(uniqueCsvText))
        {
            Csv.ImportInto(reader, collection, true, null, false);
        }

        RemoveMissingRows(collection, csvKeys);

        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) 
        {
            EditorUtility.SetDirty(table);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ SYNC HOÀN TẤT: {collection.name}. Đã loại bỏ trùng lặp và giữ đúng Tiếng Việt!");
    }

    // ===================== XỬ LÝ TRÙNG LẶP =====================

    private static string FilterDuplicateKeys(string csvText)
    {
        StringBuilder sb = new StringBuilder();
        HashSet<string> processedKeys = new HashSet<string>();

        using (StringReader reader = new StringReader(csvText))
        {
            // Giữ lại dòng Header
            string header = reader.ReadLine();
            if (header != null) sb.AppendLine(header);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string cleanedLine = line.Trim('\uFEFF', '\u200B');
                string[] cols = cleanedLine.Split(',');

                if (cols.Length > 0)
                {
                    string key = cols[0].Trim().ToUpperInvariant();
                    
                    // Nếu Key chưa xuất hiện, thì mới thêm vào nội dung cuối cùng
                    if (!processedKeys.Contains(key))
                    {
                        processedKeys.Add(key);
                        sb.AppendLine(line);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Phát hiện trùng Key: [{cols[0]}]. Đã bỏ qua dòng này.");
                    }
                }
            }
        }
        return sb.ToString();
    }

    // ===================== XỬ LÝ CSV =====================

    private static HashSet<string> ExtractKeysFromCsv(string csvText)
    {
        HashSet<string> keys = new HashSet<string>();

        using (StringReader reader = new StringReader(csvText))
        {
            string line = reader.ReadLine(); // Đọc dòng đầu tiên (Header)
            
            // Làm sạch Header để tránh lỗi khi so sánh
            line = line?.Trim('\uFEFF', '\u200B');

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Làm sạch dòng và lấy cột đầu tiên (Key)
                string cleanedLine = line.Trim('\uFEFF', '\u200B');
                string[] cols = cleanedLine.Split(',');

                if (cols.Length > 0)
                {
                    string key = cols[0].Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(key)) keys.Add(key);
                }
            }
        }

        return keys;
    }

    // ===================== XÓA DÒNG THỪA =====================

    private static void RemoveMissingRows(StringTableCollection collection, HashSet<string> csvKeys)
    {
        // Chuyển sang ToList để tránh lỗi "Collection modified" khi đang lặp
        var entries = collection.SharedData.Entries.ToList();
        int count = 0;

        foreach (var entry in entries)
        {
            string unityKey = entry.Key.Trim().ToUpperInvariant();

            if (!csvKeys.Contains(unityKey))
            {
                collection.SharedData.RemoveKey(entry.Id);
                count++;
            }
        }

        if (count > 0)
        {
            Debug.Log($"🗑 Đã xóa {count} Key không còn tồn tại trong CSV.");
        }
    }
}
#endif