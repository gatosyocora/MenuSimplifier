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

            Rect itemRect;

            using (var scope = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (folder.ParentFolder == null)
                    {
                        if (folder.NameEdittable)
                        {
                            folder.Name = EditorGUILayout.TextField(folder.Name);

                            if (e.Equals(Event.KeyboardEvent("return")) && !string.IsNullOrEmpty(folder.Name))
                            {
                                folder.NameEdittable = false;
                                GUI.changed = true;
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(folder.Name, EditorStyles.boldLabel);
                        }
                    }
                    else
                    {
                        folder.Foldout = EditorGUILayout.Foldout(folder.Foldout, folder.Name);
                    }

                    if (GUILayout.Button("x", GUILayout.Width(30f)))
                    {
                        // TODO: 削除時にすべてのアイテムを外に出してフォルダは削除する
                        // フォルダごとどこかのフォルダの下に入れるイメージ
                    }
                }

                using (var itemsScope = new EditorGUILayout.VerticalScope())
                {
                    itemRect = itemsScope.rect;

                    if (folder.Foldout || folder.ParentFolder == null)
                    {
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
                    }
                }

                GUI.backgroundColor = defaultColor;

                if (folder.ParentFolder == null) GUILayout.FlexibleSpace();

                if (scope.rect.Contains(e.mousePosition) && !itemRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        GUI.changed = true;
                        folder.Selected = !folder.Selected;

                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        GUI.changed = true;
                        return true;
                    }
                }

            }

            return false;
        }

        public static bool DropArea(string label, float height)
        {
            var rect = EditorGUILayout.GetControlRect(true, height);
            GUI.Label(rect, label, GUI.skin.box);
            var e = Event.current;
            if (rect.Contains(e.mousePosition) && e.type == EventType.MouseUp) 
            {
                return true;
            }
            return false;
        }
    }
}