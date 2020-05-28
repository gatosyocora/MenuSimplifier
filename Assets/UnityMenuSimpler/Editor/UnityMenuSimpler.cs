using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Gatosyocora.UnityMenuSimpler.DataClass;

// UnityMenuSimpler v1.0
// Copyright (c) 2020 gatosyocora
// MIT License

namespace Gatosyocora.UnityMenuSimpler
{
    public class UnityMenuSimpler : EditorWindow
    {
        private List<EditorWindowInfo> editorWindowInfoList;
        private string folderName = string.Empty;
        private List<EditorWindowFolder> folderList;

        /// <summary>
        /// MenuItemのフォルダの除外対象
        /// </summary>
        private readonly static string[] exclusionFolderNames = new string[] { "GameObject", "CONTEXT", "Assets" };

        private Vector2 unallocatedListScrollPos = Vector2.zero;
        private Vector2 folderListScrollPos = Vector2.zero;

        [MenuItem(itemName: "GatoTool/UnityMenuSimpler")]
        public static void Open()
        {
            GetWindow<UnityMenuSimpler>("UnityMenuSimpler");
        }

        private void OnEnable()
        {
            editorWindowInfoList = LoadEditorWindowList();
            folderList = CreateExistFolderList(editorWindowInfoList);
        }

        private void OnGUI()
        {
            if (editorWindowInfoList != null)
            {
                EditorGUILayout.Space();

                using (var scroll = new  EditorGUILayout.ScrollViewScope(folderListScrollPos, 
                                            alwaysShowVertical: false, 
                                            alwaysShowHorizontal: true))
                using (new EditorGUILayout.HorizontalScope())
                {
                    folderListScrollPos = scroll.scrollPosition;

                    foreach (var folder in folderList.ToList())
                    {
                        if (folder.ParentFolder != null) continue;

                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            if (GatoGUILayout.FolderField(folder))
                            {

                                foreach (var selectedItem in editorWindowInfoList.Where(x => x.Selected))
                                {
                                    selectedItem.Selected = false;
                                    var filePath = selectedItem.SourceMenuItemPath.Split('/').Last();
                                    selectedItem.DestMenuItemPath = folder.Name + "/" + filePath;
                                    folder.EditorWindowList.Add(selectedItem);
                                }

                                foreach (var selectedFolder in folderList.Where(x => x.Selected))
                                {
                                    if (selectedFolder == folder) continue;

                                    selectedFolder.Selected = false;
                                    folder.EditorWindowFolderList.Add(selectedFolder);
                                    selectedFolder.ParentFolder = folder;
                                }
                            }

                            if (check.changed)
                            {
                                Repaint();
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Add Folder"))
                {
                    folderList.Add(new EditorWindowFolder());
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Unallocated", EditorStyles.boldLabel);

                using (var scroll = new EditorGUILayout.ScrollViewScope(unallocatedListScrollPos))
                {
                    unallocatedListScrollPos = scroll.scrollPosition;

                    foreach (var editorWindowInfo in editorWindowInfoList)
                    {
                        if (!string.IsNullOrEmpty(editorWindowInfo.DestMenuItemPath)) continue;

                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            editorWindowInfo.Selected = GatoGUILayout.ToggleLabelArea(
                                                            editorWindowInfo.Name, 
                                                            editorWindowInfo.Selected, 
                                                            Color.white, Color.grey);

                            if (check.changed) Repaint();
                        }
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(!editorWindowInfoList.Any(x => x.HasChanged)))
                {
                    if (GUILayout.Button("Apply"))
                    {
                        ReplaceMenuItem(editorWindowInfoList);
                    }
                }
            }
        }

        /// <summary>
        /// 特定の型クラスのファイルのパスをAssetsフォルダ以下から取得する
        /// </summary>
        /// <param name="type">クラスの型</param>
        /// <returns>ファイルパス</returns>
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

        /// <summary>
        /// 特定の型のクラスが特定のアトリビュートを持つメソッドを含んでいるか判定する
        /// </summary>
        /// <param name="type">クラスの型</param>
        /// <param name="attrType">アトリビュートの型</param>
        /// <returns>含まれる場合true</returns>
        private bool ContainAttribute(Type type, Type attrType)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .SelectMany(x => x.CustomAttributes)
                        .Any(x => x.AttributeType == attrType);
        }

        /// <summary>
        /// 特定の型のクラスの関数が持つアトリビュートMenuItemのパスを取得する
        /// </summary>
        /// <param name="type">MenuItemアトリビュートをつけた関数を持つクラスの型</param>
        /// <returns>MenuItemのパス</returns>
        private string GetMenuItemPath(Type type)
        {
            var attr = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .SelectMany(x => x.CustomAttributes)
                        .Where(x => x.AttributeType == typeof(MenuItem))
                        .Where(x => !ContainExclusionFolder(x, exclusionFolderNames))
                        .FirstOrDefault();

            if (attr == null)
            {
                return string.Empty;
            }

            return attr.ConstructorArguments.Select(x => x.Value as string).FirstOrDefault();
        }

        /// <summary>
        /// Assetsフォルダ以下からMenuItemアトリビュートをもつスクリプトの一覧を取得する
        /// </summary>
        /// <returns>MenuItemアトリビュートをもつスクリプトのリスト</returns>
        private List<EditorWindowInfo> LoadEditorWindowList()
        {
            return Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(x => ContainAttribute(x, typeof(MenuItem)))
                        .Select(x =>
                        {
                            var menuItemPath = GetMenuItemPath(x);

                            return new EditorWindowInfo()
                            {
                                Name = x.Name,
                                SourceMenuItemPath = menuItemPath,
                                DestMenuItemPath = menuItemPath,
                                FilePath = GetFilePath(x),
                                Selected = false
                            };
                        })
                        .Where(x => !string.IsNullOrEmpty(x.SourceMenuItemPath))
                        .OrderByDescending(x => x.SourceMenuItemPath)
                        .ToList();
        }

        private List<EditorWindowFolder> CreateExistFolderList(List<EditorWindowInfo> editorWindowInfoList)
        {
            var dict = new Dictionary<string, EditorWindowFolder>();

            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                var folderName = Regex.Replace(editorWindowInfo.SourceMenuItemPath, "/[^/]+$", string.Empty);

                if (!dict.TryGetValue(folderName, out EditorWindowFolder folder))
                {
                    folder = new EditorWindowFolder()
                    {
                        Name = folderName,
                        ParentFolder = null
                    };
                    dict.Add(folderName, folder);
                }

                folder.EditorWindowList.Add(editorWindowInfo);
            }

            return dict.Values.ToList();
        }

        /// <summary>
        /// パスが除外するフォルダに入ったMenuItemアトリビュートが含まれるか判断する
        /// </summary>
        /// <param name="attrData">アトリビュートのデータ</param>
        /// <param name="exclusionFolderNames">除外するフォルダの一覧</param>
        /// <returns>含まれる場合true</returns>
        private bool ContainExclusionFolder(CustomAttributeData attrData, IReadOnlyCollection<string> exclusionFolderNames)
        {
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

        /// <summary>
        /// すぐにコンパイルを実行する
        /// </summary>
        private static void ForceCompile()
        {
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        }

        private void ReplaceMenuItem(List<EditorWindowInfo> editorWindowInfoList)
        {
            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.HasChanged) continue;

                var code = File.ReadAllText(editorWindowInfo.FilePath);
                code = code.Replace(editorWindowInfo.SourceMenuItemPath, editorWindowInfo.DestMenuItemPath);
                File.WriteAllText(editorWindowInfo.FilePath, code);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            editorWindowInfoList = LoadEditorWindowList();
            folderList = CreateExistFolderList(editorWindowInfoList);
        }
    }
}