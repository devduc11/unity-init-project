using UnityEditor;
using UnityEngine;

namespace PNAD.DevTools.Editor
{
    public class PNADDevToolsWindow : EditorWindow
    {
        // Danh sách các tab bên cột trái
        private readonly string[] tabNames = new string[]
        {
            "Init Project",
            "UI Tools",
            "Clear Data",
            "Batch Tools",
            "Build Helper"
        };

        private int selectedTabIndex = 0;

        // Mở cửa sổ từ Menu Bar
        [MenuItem("Tools/PNAD DevTools")]
        public static void ShowWindow()
        {
            PNADDevToolsWindow window = GetWindow<PNADDevToolsWindow>("PNAD DevTools");
            window.minSize = new Vector2(750, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // ==========================================
            // CỘT BÊN TRÁI: SIDEBAR (NAVIGATION)
            // ==========================================
            EditorGUILayout.BeginVertical(GUILayout.Width(200), GUILayout.ExpandHeight(true));
            GUILayout.Space(15);

            // Tiêu đề Sidebar
            GUILayout.Label("  PNAD DevTools", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Danh sách các tab bấm chọn (Đã fix style)
            selectedTabIndex = GUILayout.SelectionGrid(
                selectedTabIndex,
                tabNames,
                1,
                "LargeButton",
                GUILayout.Height(tabNames.Length * 38)
            );

            EditorGUILayout.EndVertical();

            // Đường vạch kẻ đứng phân cách 2 cột
            Rect rect = GUILayoutUtility.GetLastRect();
            Handles.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            Handles.DrawLine(new Vector3(rect.xMax, 0, 0), new Vector3(rect.xMax, position.height, 0));

            // ==========================================
            // CỘT BÊN PHẢI: NỘI DUNG TƯƠNG ỨNG
            // ==========================================
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(15);

            // Tiêu đề tab hiện tại
            GUILayout.Label($"  {tabNames[selectedTabIndex]}", EditorStyles.largeLabel, GUILayout.Height(30));
            GUILayout.Space(10);

            // Nội dung chi tiết từng Tab
            switch (selectedTabIndex)
            {
                case 0:
                    DrawInitProjectTab();
                    break;
                case 1:
                    DrawUIToolsTab();
                    break;
                case 2:
                    DrawClearDataTab();
                    break;
                case 3:
                    DrawBatchToolsTab();
                    break;
                case 4:
                    DrawBuildHelperTab();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------
        // TAB 1: INIT PROJECT
        // ------------------------------------------------------------------
        private void DrawInitProjectTab()
        {
            EditorGUILayout.HelpBox(
                "Tự động khởi tạo cấu trúc thư mục chuẩn Assets/_Project và sinh các Script mẫu cốt lõi (UIManager, LoadingUI, GameManager...).",
                MessageType.Info
            );

            GUILayout.Space(20);

            if (GUILayout.Button("🚀 Execute Init Project Structure", GUILayout.Height(45)))
            {
                // Gọi hàm Init từ class InitProjectStructure của bạn
                InitProjectStructure.Init();
            }
        }

        // ------------------------------------------------------------------
        // TAB 2: UI TOOLS
        // ------------------------------------------------------------------
        private void DrawUIToolsTab()
        {
            EditorGUILayout.HelpBox("Các tiện ích hỗ trợ làm UI nhanh chóng.", MessageType.Info);
            GUILayout.Space(10);

            if (GUILayout.Button("Auto Bind UI Components", GUILayout.Height(35)))
            {
                Debug.Log("Auto Binding UI components...");
            }
        }

        // ------------------------------------------------------------------
        // TAB 3: CLEAR DATA
        // ------------------------------------------------------------------
        private void DrawClearDataTab()
        {
            EditorGUILayout.HelpBox("Xóa dữ liệu test khi đang làm game.", MessageType.Warning);
            GUILayout.Space(10);

            if (GUILayout.Button("🗑 Clear PlayerPrefs", GUILayout.Height(35)))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("✅ PlayerPrefs Cleared!");
            }

            if (GUILayout.Button("🗑 Clear Persistent Data Path", GUILayout.Height(35)))
            {
                // Logic xóa file save JSON / Binary
                Debug.Log("✅ Saved Files Cleared!");
            }
        }

        // ------------------------------------------------------------------
        // TAB 4: BATCH TOOLS
        // ------------------------------------------------------------------
        private void DrawBatchToolsTab()
        {
            EditorGUILayout.HelpBox("Thao tác hàng loạt trên Hierarchy / Project Window.", MessageType.Info);
        }

        // ------------------------------------------------------------------
        // TAB 5: BUILD HELPER
        // ------------------------------------------------------------------
        private void DrawBuildHelperTab()
        {
            EditorGUILayout.HelpBox("Hỗ trợ Fast Build Android / iOS.", MessageType.Info);
        }
    }
}