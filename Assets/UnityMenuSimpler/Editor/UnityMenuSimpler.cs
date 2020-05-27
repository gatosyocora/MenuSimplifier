using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;
using System.IO;

namespace Gatosyocora.UnityMenuSimpler
{
    public class UnityMenuSimpler : EditorWindow
    {
        public class EditorWindowInfo 
        {
            public string Name { get; set; }
            public string MenuItemPath { get; set; }
            public string FilePath { get; set; }
            public bool Selected { get; set; }
        }

        private List<EditorWindowInfo> editorWindowInfoList;
        private string folderName = string.Empty;

        private Vector2 scrollPos = Vector2.zero;

        [MenuItem(itemName:"GatoTool/UnityMenuSimpler")]
		public static void Open()
		{
			GetWindow<UnityMenuSimpler>("UnityMenuSimpler");
		}

        private void OnEnable()
        {
            LoadEditorWindowList();
        }

        private void OnGUI()
		{
            if (editorWindowInfoList != null)
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = scroll.scrollPosition;

                    foreach (var editorWindowInfo in editorWindowInfoList)
                    {
                        if (string.IsNullOrEmpty(editorWindowInfo.MenuItemPath)) continue;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            editorWindowInfo.Selected = EditorGUILayout.ToggleLeft(
                                                            string.Empty, 
                                                            editorWindowInfo.Selected, 
                                                            GUILayout.Width(30f));
                            EditorGUILayout.LabelField(editorWindowInfo.Name, editorWindowInfo.MenuItemPath);
                        }
                    }
                }

                folderName = EditorGUILayout.TextField("Folder Name", folderName);

                if (GUILayout.Button("Move MenuItem to Child"))
                {
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        MoveMenuItemToChildren();
                    }

                }

                if (GUILayout.Button("Move MenuItem to Parent"))
                {
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        MoveMenuItemToParent();
                    }
                }
            }
		}

        private string GetFilePath(Type type)
        {
            var assetGuid = AssetDatabase.FindAssets(type.Name + " t:Script").FirstOrDefault();
            if (string.IsNullOrEmpty(assetGuid))
            {
                Debug.LogError(type.Name + " : Not Found Asset");
                return string.Empty;
            }

            var path = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (!File.Exists(path))
            {
                Debug.LogError(type.Name + " : Not Found File");
                Debug.LogError(path);
                return string.Empty;
            }

            return path;
        }

        private bool ContainAttribute(Type type, Type attrType)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .SelectMany(x => x.CustomAttributes)
                        .Any(x => x.AttributeType == attrType);
        }
        
        private string GetMenuItemPath(Type type)
        {
            var attr = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .SelectMany(x => x.CustomAttributes)
                        .Where(x => x.AttributeType == typeof(MenuItem))
                        .Where(x => !ContainExclusionFolder(x))
                        .FirstOrDefault();

            if (attr == null)
            {
                return string.Empty;
            }

            return attr.ConstructorArguments.Select(x => x.Value as string).FirstOrDefault();
        }

        private void LoadEditorWindowList()
        {
            editorWindowInfoList = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(x => ContainAttribute(x, typeof(MenuItem)))
                        .Select(x =>
                            new EditorWindowInfo()
                            {
                                Name = x.Name,
                                MenuItemPath = GetMenuItemPath(x),
                                FilePath = GetFilePath(x),
                                Selected = false
                            })
                        .OrderByDescending(x => x.MenuItemPath)
                        .ToList();
        }

        private bool ContainExclusionFolder(CustomAttributeData attrData)
        {
            var exclusionFolderNames = new string[]{"GameObject", "CONTEXT"};

            foreach (var arg in attrData.ConstructorArguments)
            {
                var path = arg.Value as string;
                if (string.IsNullOrEmpty(path)) continue;

                if (exclusionFolderNames.Any(x => path.StartsWith(x))) 
                {
                    return true;
                }
            }

            return false;
        }

        private static void ForceCompile()
        {
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        }

        private void MoveMenuItemToChildren()
        {
            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.Selected) continue;

                var code = File.ReadAllText(editorWindowInfo.FilePath);
                code = code.Replace(editorWindowInfo.MenuItemPath, folderName + "/" + editorWindowInfo.MenuItemPath);
                File.WriteAllText(editorWindowInfo.FilePath, code);
            }

            ForceCompile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadEditorWindowList();
        }

        private void MoveMenuItemToParent()
        {
            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.Selected) continue;

                var code = File.ReadAllText(editorWindowInfo.FilePath);
                var replacedMenuItemPath = editorWindowInfo.MenuItemPath.Remove(0, folderName.Length + 1);
                code = code.Replace(editorWindowInfo.MenuItemPath, replacedMenuItemPath);
                File.WriteAllText(editorWindowInfo.FilePath, code);
            }

            ForceCompile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadEditorWindowList();
        }
    }
}