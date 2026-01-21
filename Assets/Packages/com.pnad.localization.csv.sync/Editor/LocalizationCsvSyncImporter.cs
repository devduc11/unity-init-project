#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine.Localization.Tables;
using System.IO;
using System.Net;
using System.Collections.Generic;

public class LocalizationCsvSyncImporter : EditorWindow
{
    private string csvUrl = "";
    // private string csvUrl =
    //     "https://docs.google.com/spreadsheets/d/1-b4mlz24AneNcTcLDRqC6kkMQtZ6d6hb5J0i119F5kk/export?format=csv";

    private StringTableCollection targetCollection;

    [MenuItem("Localization Tools/Sync CSV From Google Sheet")]
    public static void Open()
    {
        GetWindow<LocalizationCsvSyncImporter>("Localization CSV Sync");
    }

    private void OnGUI()
    {
        GUILayout.Label("Localization CSV Sync (UI-Accurate)", EditorStyles.boldLabel);

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
            if (targetCollection == null)
            {
                Debug.LogError("❌ Please assign StringTableCollection");
                return;
            }

            SyncCsv(csvUrl, targetCollection);
        }
    }

    // ===================== CORE =====================

    private static void SyncCsv(string url, StringTableCollection collection)
    {
        string csvText;

        using (WebClient wc = new WebClient())
        {
            csvText = wc.DownloadString(url);
        }

        // 1. Parse keys từ CSV
        HashSet<string> csvKeys = ExtractKeysFromCsv(csvText);

        // 2. Import ADD + UPDATE
        using (StringReader reader = new StringReader(csvText))
        {
            Csv.ImportInto(reader, collection, true, null, false);
        }

        // 3. DELETE ROW (GIỐNG BẤM NÚT "-")
        RemoveMissingRows(collection, csvKeys);

        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Localization SYNC DONE (UI Accurate): {collection.name}");
    }

    // ===================== CSV =====================

    private static HashSet<string> ExtractKeysFromCsv(string csvText)
    {
        HashSet<string> keys = new HashSet<string>();

        using (StringReader reader = new StringReader(csvText))
        {
            bool isHeader = true;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string cleanedLine = line
                    .Replace("\uFEFF", "")
                    .Trim();

                string[] cols = cleanedLine.Split(',');
                if (cols.Length == 0)
                    continue;

                string key = cols[0]
                    .Trim()
                    .ToUpperInvariant();

                if (!string.IsNullOrEmpty(key))
                    keys.Add(key);
            }
        }

        return keys;
    }

    // ===================== DELETE ROW =====================

    private static void RemoveMissingRows(
        StringTableCollection collection,
        HashSet<string> csvKeys)
    {
        List<string> keysToRemove = new List<string>();

        foreach (var keyData in collection.SharedData.Entries)
        {
            string unityKey = keyData.Key
                .Trim()
                .ToUpperInvariant();

            if (!csvKeys.Contains(unityKey))
            {
                keysToRemove.Add(keyData.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            collection.SharedData.RemoveKey(key);
            Debug.Log($"🗑 Removed ROW (UI '-'): {key}");
        }
    }
}
#endif
