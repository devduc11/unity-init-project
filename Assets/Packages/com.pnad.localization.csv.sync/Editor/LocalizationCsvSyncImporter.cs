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

        // 1. Lấy danh sách Key hiện có trong Unity để so sánh
        HashSet<string> existingKeys = new HashSet<string>(
            collection.SharedData.Entries.Select(e => e.Key.Trim().ToUpperInvariant())
        );

        // 2. Lọc CSV: CHỈ giữ lại những dòng có Key chưa tồn tại
        // Đảm bảo giữ nguyên toàn bộ nội dung dòng để tránh lỗi MissingField
        string filteredCsvText = FilterNewKeysOnly(csvText, existingKeys);

        // 3. Lấy toàn bộ Key từ CSV gốc để dùng cho việc xóa (Delete)
        HashSet<string> csvKeys = ExtractKeysFromCsv(csvText);

        // 4. Import dữ liệu đã lọc (Chỉ thêm mới, không đè)
        using (StringReader reader = new StringReader(filteredCsvText))
        {
            // Tham số true xác nhận có Header dòng đầu
            Csv.ImportInto(reader, collection, true, null, false);
        }

        // 5. Xử lý xóa dòng thừa (Nếu Key có trong Unity nhưng không có trên Sheets)
        RemoveMissingRows(collection, csvKeys);

        // 6. Lưu Asset
        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) 
        {
            EditorUtility.SetDirty(table);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ SYNC HOÀN TẤT: {collection.name}. Đã thêm Key mới và giữ nguyên các thay đổi cũ!");
    }

    // ===================== LOGIC LỌC TRÙNG (FIXED) =====================

    private static string FilterNewKeysOnly(string csvText, HashSet<string> existingKeys)
    {
        StringBuilder sb = new StringBuilder();
        using (StringReader reader = new StringReader(csvText))
        {
            string header = reader.ReadLine();
            if (header != null) sb.AppendLine(header); // Luôn giữ Header

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Lấy Key từ cột đầu tiên (trước dấu phẩy đầu tiên)
                int firstCommaIndex = line.IndexOf(',');
                string keyInCsv = "";

                if (firstCommaIndex == -1) 
                    keyInCsv = line.Trim();
                else 
                    keyInCsv = line.Substring(0, firstCommaIndex).Trim();

                keyInCsv = keyInCsv.Trim('\"').ToUpperInvariant();

                // CHỈ thêm dòng này vào danh sách Import nếu Key chưa có trong Unity
                if (!existingKeys.Contains(keyInCsv))
                {
                    sb.AppendLine(line); // Bê nguyên xi cả dòng để không mất cột (index)
                }
            }
        }
        return sb.ToString();
    }

    private static HashSet<string> ExtractKeysFromCsv(string csvText)
    {
        HashSet<string> keys = new HashSet<string>();
        using (StringReader reader = new StringReader(csvText))
        {
            reader.ReadLine(); // Bỏ qua Header
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                int firstCommaIndex = line.IndexOf(',');
                string key = (firstCommaIndex == -1) ? line : line.Substring(0, firstCommaIndex);
                key = key.Trim('\"', ' ').ToUpperInvariant();
                
                if (!string.IsNullOrEmpty(key)) keys.Add(key);
            }
        }
        return keys;
    }

    private static void RemoveMissingRows(StringTableCollection collection, HashSet<string> csvKeys)
    {
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
        if (count > 0) Debug.Log($"🗑 Đã xóa {count} Key không còn tồn tại trong CSV.");
    }
}
#endif