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

        public static void FolderField(EditorWindowFolder folder, LanguageTemplate lang, Action<EditorWindowFolder> OnDrop, Action AllIn, Action RemoveSelf, Action<EditorWindowFolder> DropSubFolder, Action<EditorWindowFolder> OnSelect)
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

                            if ((GUILayout.Button(lang.ok, GUILayout.Width(50f)) ||
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
                        folder.Foldout = EditorGUILayout.Foldout(folder.Foldout, folder.Name);

                        if (GUILayout.Button(lang.drop))
                        {
                            DropSubFolder(folder);
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
                            FolderField(editorWindowfolder, lang, OnDrop, AllIn, RemoveSelf, DropSubFolder, OnSelect);
                        }

                        foreach (var editorWindowInfo in folder.EditorWindowList.ToList())
                        {
                            FileField(editorWindowInfo, lang);
                        }
                    }
                }

                if (!folder.EditorWindowFolderList.Any() && !folder.EditorWindowList.Any())
                {
                    using (new EditorGUI.DisabledScope(folder.NameEdittable))
                    {
                        if (GUILayout.Button(lang.allIn))
                        {
                            AllIn();
                        }
                    }

                    if (GUILayout.Button(lang.remove))
                    {
                        RemoveSelf();
                    }
                }

                GUI.backgroundColor = defaultColor;

                if (folder.ParentFolder is null) GUILayout.FlexibleSpace();

                if (scope.rect.Contains(e.mousePosition) && !itemRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        GUI.changed = true;
                        OnSelect(folder);
                        Highlighter.Stop();
                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        GUI.changed = true;
                        OnDrop(folder);
                        Highlighter.Stop();
                        GUIUtility.ExitGUI();
                    }
                    else if (e.type == EventType.MouseDrag)
                    {
                        GUI.changed = true;
                        // TODO: 本当はFolder全体が囲まれるようにしたい
                        // Highlighter.HighlightIdentifierはうまくいかない
                        Highlighter.Highlight("UnityMenuSimpler", folder.Name);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        public static bool DropArea(string label, LanguageTemplate lang, float height)
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
                    GUIUtility.ExitGUI();
                }
                else if (e.type == EventType.MouseUp)
                {
                    Highlighter.Stop();
                    return true;
                }
            }
            return false;
        }

        public static void FolderRowField(EditorWindowFolder folder, LanguageTemplate lang, Action<EditorWindowFolder> DropSubFolder, Action AllIn, Action RemoveSelf)
        {
            var e = Event.current;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (folder.NameEdittable)
                    {
                        folder.Name = EditorGUILayout.TextField(folder.Name);

                        if ((GUILayout.Button(lang.ok, GUILayout.Width(50f)) ||
                            e.Equals(Event.KeyboardEvent("return")))
                            && !string.IsNullOrEmpty(folder.Name))
                        {
                            folder.NameEdittable = false;
                            GUI.changed = true;
                        }
                    }
                    else
                    {
                        folder.Foldout = EditorGUILayout.Foldout(folder.Foldout, folder.Name);
                    }

                    if (!folder.EditorWindowFolderList.Any() && !folder.EditorWindowList.Any())
                    {
                        using (new EditorGUI.DisabledScope(folder.NameEdittable))
                        {
                            if (GUILayout.Button(lang.allIn))
                            {
                                AllIn();
                            }
                        }

                        if (GUILayout.Button(lang.remove))
                        {
                            RemoveSelf();
                        }
                    }

                    if (!(folder.ParentFolder is null))
                    {
                        if (GUILayout.Button(lang.drop, GUILayout.Width(80f)))
                        {
                            DropSubFolder(folder);
                        }
                    }
                }

                if (folder.Foldout)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (var subFolder in folder.EditorWindowFolderList.ToArray())
                        {
                            FolderRowField(subFolder, lang, DropSubFolder, AllIn, RemoveSelf);
                        }

                        foreach (var info in folder.EditorWindowList.ToArray())
                        {
                            FileField(info, lang);
                        }
                    }
                }
            }

        }

        private static void FileField(EditorWindowInfo info, LanguageTemplate lang)
        {
            var style = new GUIStyle(EditorStyles.label);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (info.HasChanged)
                {
                    style.normal.textColor = Color.red;
                }
                else
                {
                    style.normal.textColor = Color.black;
                }
                EditorGUILayout.LabelField(info.Name, style);

                if (info.Applied)
                    EditorGUILayout.LabelField(lang.applied);

                // ファイルだけの移動はできなくてもよいので一時削除
                //if (GUILayout.Button("x"))
                //{
                //    folder.EditorWindowList.Remove(editorWindowInfo);
                //    editorWindowInfo.DestMenuItemPath = string.Empty;
                //}
            }

            if (info.HasChanged && !info.Applied)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var pathStyle = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true
                    };
                    EditorGUILayout.LabelField(info.Path, pathStyle);
                    EditorGUILayout.LabelField("→ " + info.DestMenuItemPath, pathStyle);
                }
            }
        }
    }
}