using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using static Gatosyocora.UnityMenuSimpler.UnityMenuSimpler;
using Gatosyocora.UnityMenuSimpler.DataClass;

namespace Gatosyocora.UnityMenuSimpler
{
    public static class GatoGUILayout
    {
        public static bool ToggleLabelArea(string label, bool toggle, Color disactiveColor, Color activeColor)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1.5f);
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = (toggle) ? activeColor : disactiveColor;
            GUI.Label(rect, label, GUI.skin.box);
            GUI.backgroundColor = defaultColor;

            var e = Event.current;
            if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                toggle = !toggle;
                GUI.changed = true;
            }

            return toggle;
        }

        public static void FolderField(EditorWindowFolder folder)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    folder.Name = EditorGUILayout.TextField(folder.Name);

                    if (GUILayout.Button("x", GUILayout.Width(30f)))
                    {
                        // TODO: 削除時にすべてのアイテムを外に出してフォルダは削除する
                        // フォルダごとどこかのフォルダの下に入れるイメージ
                    }
                }

                foreach (var editorWindowfolder in folder.EditorWindowFolderList)
                {
                }

                foreach (var editorWindowInfo in folder.EditorWindowList.ToList())
                {
                    var style = new GUIStyle(EditorStyles.label);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (editorWindowInfo.HasChanged)
                        {
                            style.normal.textColor = Color.red;
                        }
                        else
                        {
                            style.normal.textColor = Color.black;
                        }
                        EditorGUILayout.LabelField(editorWindowInfo.Name, style);

                        if (GUILayout.Button("x"))
                        {
                            folder.EditorWindowList.Remove(editorWindowInfo);
                            editorWindowInfo.DestMenuItemPath = string.Empty;
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(folder.Name)))
                {
                    var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 2);
                    var e = Event.current;
                    var defaultColor = GUI.backgroundColor;
                    if (rect.Contains(e.mousePosition))
                    {
                        if (e.type == EventType.MouseMove)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            GUI.backgroundColor = Color.gray;
                        }
                        else if (e.type == EventType.MouseUp)
                        {
                            GUI.changed = true;
                        }
                    }

                    GUI.Label(rect, "Drag & Drop", GUI.skin.box);

                    GUI.backgroundColor = defaultColor;
                }
            }
        }
    }
}