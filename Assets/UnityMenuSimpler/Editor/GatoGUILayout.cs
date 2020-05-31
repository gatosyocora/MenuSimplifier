using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using static Gatosyocora.UnityMenuSimpler.UnityMenuSimpler;
using Gatosyocora.UnityMenuSimpler.DataClass;
using System;

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

        public static void FolderField(EditorWindowFolder folder, Action<EditorWindowFolder> OnDrop, Action AllIn, Action DeleteSelf, Action<EditorWindowFolder> DropSubFolder)
        {
            var defaultColor = GUI.backgroundColor;
            if (folder.Selected) GUI.backgroundColor = Color.gray;

            var e = Event.current;

            Rect itemRect;

            using (var scope = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (folder.ParentFolder is null)
                    {
                        if (folder.NameEdittable)
                        {
                            folder.Name = EditorGUILayout.TextField(folder.Name);

                            if ((GUILayout.Button("OK", GUILayout.Width(50f)) ||
                                e.Equals(Event.KeyboardEvent("return")))
                                && !string.IsNullOrEmpty(folder.Name))
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
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            folder.Foldout = EditorGUILayout.Foldout(folder.Foldout, folder.Name);

                            if (GUILayout.Button("Drop"))
                            {
                                DropSubFolder(folder);
                            }
                        }
                    }
                }

                using (var itemsScope = new EditorGUILayout.VerticalScope())
                {
                    itemRect = itemsScope.rect;

                    if (folder.Foldout || folder.ParentFolder is null)
                    {
                        foreach (var editorWindowfolder in folder.EditorWindowFolderList.ToArray())
                        {
                            FolderField(editorWindowfolder, OnDrop, AllIn, DeleteSelf, DropSubFolder);
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

                                // ファイルだけの移動はできなくてもよいので一時削除
                                //if (GUILayout.Button("x"))
                                //{
                                //    folder.EditorWindowList.Remove(editorWindowInfo);
                                //    editorWindowInfo.DestMenuItemPath = string.Empty;
                                //}
                            }

                            if (editorWindowInfo.HasChanged)
                            {
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    var pathStyle = new GUIStyle(GUI.skin.label)
                                    {
                                        wordWrap = true
                                    };
                                    EditorGUILayout.LabelField(editorWindowInfo.Path, pathStyle);
                                    EditorGUILayout.LabelField("→ " + editorWindowInfo.DestMenuItemPath, pathStyle);
                                }
                            }
                        }
                    }
                }

                if (!folder.EditorWindowFolderList.Any() && !folder.EditorWindowList.Any())
                {
                    using (new EditorGUI.DisabledScope(folder.NameEdittable))
                    {
                        if (GUILayout.Button("All In"))
                        {
                            AllIn();
                        }
                    }

                    if (GUILayout.Button("Delete"))
                    {
                        DeleteSelf();
                    }
                }

                GUI.backgroundColor = defaultColor;

                if (folder.ParentFolder is null) GUILayout.FlexibleSpace();

                if (scope.rect.Contains(e.mousePosition) && !itemRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        GUI.changed = true;
                        Highlighter.Stop();
                        folder.Selected = !folder.Selected;

                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        GUI.changed = true;
                        Highlighter.Stop();
                        OnDrop(folder);
                    }
                    else if (e.type == EventType.MouseDrag)
                    {
                        GUI.changed = true;
                        // TODO: 本当はFolder全体が囲まれるようにしたい
                        // Highlighter.HighlightIdentifierはうまくいかない
                        Highlighter.Highlight("UnityMenuSimpler", folder.Name);
                    }
                }

            }
        }

        public static bool DropArea(string label, float height)
        {
            var rect = EditorGUILayout.GetControlRect(true, height);
            GUI.Label(rect, label, GUI.skin.box);
            var e = Event.current;
            if (rect.Contains(e.mousePosition)) 
            {
                if (e.type == EventType.MouseDrag)
                {
                    GUI.changed = true;
                    Highlighter.Highlight("UnityMenuSimpler", label);
                }
                else if (e.type == EventType.MouseUp)
                {
                    Highlighter.Stop();
                    return true;
                }
            }
            return false;
        }
    }
}