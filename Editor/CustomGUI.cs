using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace PeanutTools_VRC3CVR {
    class CustomGUI {
        public static void SmallLineGap() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public static void LineGap() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public static void ItalicLabel(string text) {
            GUIStyle italicStyle = new GUIStyle(GUI.skin.label);
            italicStyle.fontStyle = FontStyle.Italic;
            GUILayout.Label(text, italicStyle);
        }

        public static void LargeLabel(string text) {
            GUIStyle italicStyle = new GUIStyle(GUI.skin.label);
            italicStyle.fontSize = 20;
            GUILayout.Label(text, italicStyle);
        }

        public static void BoldLabel(string text, params GUILayoutOption[] options) {
            GUILayout.Label(text, EditorStyles.boldLabel, options);
        }

        public static void MyLinks(string repoName) {
            GUILayout.Label("Links:");

            RenderLink("  Download new versions from GitHub", "https://github.com/imagitama/" + repoName);
            RenderLink("  Get support from my Discord", "https://discord.gg/R6Scz6ccdn");
            RenderLink("  Follow me on Twitter", "https://twitter.com/@HiPeanutBuddha");
        }

        public static void HorizontalRule() {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
        }

        public static void ForceRefresh() {
            GUI.FocusControl(null);
        }

        public static void RenderLink(string label, string url) {
            Rect rect = EditorGUILayout.GetControlRect();

            if (rect.Contains(Event.current.mousePosition)) {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                if (Event.current.type == EventType.MouseUp) {
                    Help.BrowseURL(url);
                }
            }

            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.5f, 0.5f, 1);

            GUI.Label(rect, label, style);
        }

        public static bool PrimaryButton(string label) {
            return GUILayout.Button(label, GUILayout.Width(250), GUILayout.Height(50));
        }

        public static bool StandardButton(string label) {
            return GUILayout.Button(label, GUILayout.Width(150), GUILayout.Height(25));
        }

        public static bool TinyButton(string label) {
            return GUILayout.Button(label, GUILayout.Width(50), GUILayout.Height(15));
        }

        public static bool ToggleButton(string label, bool isOpen) {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            if (isOpen) {
                style.normal.background = style.active.background;
                style.fontStyle = FontStyle.Bold;
            }

            return GUILayout.Button(label + "...", style, GUILayout.Width(150), GUILayout.Height(25));
        }

        public static string RenderAssetFolderSelector(ref string pathToUse) {
            GUILayout.Label("Path:");
            pathToUse = EditorGUILayout.TextField(pathToUse);
            
            if (CustomGUI.StandardButton("Select Folder")) {
                string absolutePath = EditorUtility.OpenFolderPanel("Select a folder", Application.dataPath, "");
                string pathInsideProject = absolutePath.Replace(Application.dataPath + "/", "").Replace(Application.dataPath, "");
                pathToUse = pathInsideProject;
                CustomGUI.ForceRefresh();
            }
            
            return "";
        }

        public static void RenderSuccessMessage(string message) {
             GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.normal.textColor = new Color(0.5f, 1.0f, 0.5f);
            GUILayout.Label(message, guiStyle);
        }

        public static void RenderErrorMessage(string message) {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.normal.textColor = new Color(1.0f, 0.5f, 0.5f);
            GUILayout.Label(message, guiStyle);
        }

        public static void RenderWarningMessage(string message) {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.normal.textColor = new Color(1.0f, 1.0f, 0.5f);
            GUILayout.Label(message, guiStyle);
        }
    }
}