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

        public static bool FolderField(EditorWindowFolder folder)
        {
            var defaultColor = GUI.backgroundColor;
            if (folder.Selected) GUI.backgroundColor = Color.gray;

            var e = Event.current;

            using (var scope = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var changed = GUI.changed;
                    folder.Name = EditorGUILayout.TextField(folder.Name);
                    GUI.changed = changed;

                    if (GUILayout.Button("x", GUILayout.Width(30f)))
                    {
                        // TODO: 削除時にすべてのアイテムを外に出してフォルダは削除する
                        // フォルダごとどこかのフォルダの下に入れるイメージ
                    }
                }

                foreach (var editorWindowfolder in folder.EditorWindowFolderList)
                {
                    FolderField(editorWindowfolder);
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

                    if (editorWindowInfo.HasChanged)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            var pathStyle = new GUIStyle(GUI.skin.label);
                            pathStyle.wordWrap = true;
                            EditorGUILayout.LabelField(editorWindowInfo.SourceMenuItemPath, pathStyle);
                            EditorGUILayout.LabelField("→ " + editorWindowInfo.DestMenuItemPath, pathStyle);
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                if (scope.rect.Contains(Event.current.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        folder.Selected = !folder.Selected;
                        GUI.changed = true;
                    }
                    else if (e.type == EventType.MouseMove)
                    {
                        GUI.changed = true;
                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        GUI.changed = true;
                        return true;
                    }
                }

                GUI.backgroundColor = defaultColor;
            }

            return false;
        }
    }
}