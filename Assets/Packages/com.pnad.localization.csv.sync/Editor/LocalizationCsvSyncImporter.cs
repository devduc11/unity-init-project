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
        // Sử dụng UnityWebRequest để tránh lỗi font trên Mac
        var www = UnityWebRequest.Get(url);
        var operation = www.SendWebRequest();

        // Đợi download hoàn tất (Blocking trong Editor)
        while (!operation.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ Lỗi tải CSV: {www.error}");
            return;
        }

        // 1. Giải mã bằng UTF-8 để giữ đúng dấu Tiếng Việt
        byte[] rawData = www.downloadHandler.data;
        string csvText = Encoding.UTF8.GetString(rawData);

        // 2. XÓA KÝ TỰ BOM (\uFEFF) - Đây là nguyên nhân gây lỗi Header "ey" thay vì "Key"
        csvText = csvText.Trim('\uFEFF', '\u200B');

        // --- MỚI: LỌC TRÙNG KEY ---
        // Lấy danh sách các Key hiện đang có trong Unity để so sánh
        HashSet<string> existingKeys = new HashSet<string>(
            collection.SharedData.Entries.Select(e => e.Key.Trim().ToUpperInvariant())
        );

        // Lọc nội dung CSV: Chỉ giữ lại những dòng có Key CHƯA tồn tại trong Unity
        string filteredCsvText = FilterNewKeysOnly(csvText, existingKeys);
        // --------------------------

        // 3. Parse keys từ CSV để dùng cho việc xóa dòng thừa
        HashSet<string> csvKeys = ExtractKeysFromCsv(csvText);

        // 4. Import dữ liệu đã lọc vào Collection (Chỉ thêm mới, không ghi đè)
        using (StringReader reader = new StringReader(filteredCsvText))
        {
            Csv.ImportInto(reader, collection, true, null, false);
        }

        // 5. Xử lý DELETE (Xóa những Key không còn tồn tại trên Google Sheets)
        RemoveMissingRows(collection, csvKeys);

        // 6. Đánh dấu thay đổi để Unity lưu lại Asset
        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) 
        {
            EditorUtility.SetDirty(table);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ SYNC HOÀN TẤT: {collection.name}. Đã thêm các Key mới và giữ nguyên các Key trùng đã có!");
    }

    // ===================== HÀM HỖ TRỢ LỌC TRÙNG =====================

    private static string FilterNewKeysOnly(string csvText, HashSet<string> existingKeys)
    {
        StringBuilder sb = new StringBuilder();
        using (StringReader reader = new StringReader(csvText))
        {
            string header = reader.ReadLine();
            if (header != null) sb.AppendLine(header); // Luôn giữ lại dòng tiêu đề (Header)

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Tách cột để lấy Key (Cột đầu tiên)
                string cleanedLine = line.Trim('\uFEFF', '\u200B');
                string[] cols = cleanedLine.Split(',');

                if (cols.Length > 0)
                {
                    string keyInCsv = cols[0].Trim().ToUpperInvariant();

                    // Nếu Key này CHƯA có trong Unity thì mới thêm vào danh sách nạp
                    if (!existingKeys.Contains(keyInCsv))
                    {
                        sb.AppendLine(line);
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