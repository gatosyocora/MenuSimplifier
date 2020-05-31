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
        private List<EditorWindowFolder> folderList;

        /// <summary>
        /// MenuItemのフォルダの除外対象
        /// </summary>
        private readonly static string[] exclusionFolderNames
                        = new string[] { "GameObject", "CONTEXT", "Assets" };

        private Vector2 unallocatedListScrollPos = Vector2.zero;
        private Vector2 folderListScrollPos = Vector2.zero;
        private Rect folderRect;

        private readonly static string TOOL_KEYWORD = "UNITYMENUSIMPLER:";

        [MenuItem("GatoTool/UnityMenuSimpler")]
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

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("All Reset to Default"))
                    {
                        ReplaceMenuItem(editorWindowInfoList, true);
                    }
                }

                EditorGUILayout.Space();

                using (var scroll = new EditorGUILayout.ScrollViewScope(folderListScrollPos,
                                            alwaysShowVertical: false,
                                            alwaysShowHorizontal: true))
                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    folderListScrollPos = scroll.scrollPosition;
                    folderRect = scope.rect;

                    foreach (var folder in folderList.Where(x => x.ParentFolder is null).ToList())
                    {
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            GatoGUILayout.FolderField(folder,
                                (f) => {
                                    // ファイルを移動させたときの処理
                                    MoveFile(f, editorWindowInfoList.Where(x => x.Selected));

                                    // フォルダを移動させたときの処理
                                    MoveFolder(f, folderList.Where(x => x.Selected));
                                },
                                () => MoveFolder(folder, folderList.Where(x => x != folder && x.ParentFolder is null)),
                                () => folderList.Remove(folder),
                                (f) => DropSubFolder(f)
                            );

                            if (check.changed)
                            {
                                Repaint();
                            }
                        }
                    }

                    // 自動スクロール
                    var e = Event.current;
                    if (e.type == EventType.MouseDrag)
                    {
                        folderListScrollPos.x = Mathf.Clamp(
                                                    (e.mousePosition.x - position.x - (scope.rect.width - position.width / 2)) / 2,
                                                    0, position.width / 2);
                        Repaint();
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Add Folder"))
                {
                    folderListScrollPos.x = folderRect.width;

                    var newFolder = new EditorWindowFolder()
                    {
                        NameEdittable = true
                    };
                    folderList.Add(newFolder);
                }

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    if (GatoGUILayout.DropArea("Drop SubFolder", EditorGUIUtility.singleLineHeight * 4f))
                    {
                        foreach (var selectedFolder in folderList.Where(x => x.Selected).ToArray())
                        {
                            selectedFolder.Selected = false;

                            if (selectedFolder.ParentFolder is null) continue;

                            DropSubFolder(selectedFolder);
                        }
                    }

                    if (check.changed) Repaint();
                }

                // ファイルだけの移動はできなくてもよいので一時削除
                //EditorGUILayout.Space();

                //EditorGUILayout.LabelField("Unallocated", EditorStyles.boldLabel);

                //using (var scroll = new EditorGUILayout.ScrollViewScope(unallocatedListScrollPos))
                //{
                //    unallocatedListScrollPos = scroll.scrollPosition;

                //    foreach (var editorWindowInfo in editorWindowInfoList)
                //    {
                //        if (!string.IsNullOrEmpty(editorWindowInfo.DestMenuItemPath)) continue;

                //        using (var check = new EditorGUI.ChangeCheckScope())
                //        {
                //            editorWindowInfo.Selected = GatoGUILayout.ToggleLabelArea(
                //                                            editorWindowInfo.Name,
                //                                            editorWindowInfo.Selected,
                //                                            Color.white, Color.grey);

                //            if (check.changed) Repaint();
                //        }
                //    }
                //}

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(!editorWindowInfoList.Any(x => x.HasChanged)))
                {
                    if (GUILayout.Button("Show Changed"))
                    {
                        foreach (var file in editorWindowInfoList.Where(x => x.HasChanged))
                        {
                            OpenFolder(file.ParentFolder);
                        }
                    }

                    if (GUILayout.Button("Apply"))
                    {
                        ReplaceMenuItem(editorWindowInfoList);
                    }

                    EditorGUILayout.Space();

                    if (GUILayout.Button("All Revert"))
                    {
                        RevertAllMenuItem();
                    }
                }

                // マウスドラッグが終わったのですべて選択解除
                if (Event.current.type == EventType.MouseUp)
                {
                    foreach (var folder in folderList)
                    {
                        folder.Selected = false;
                    }
                    foreach (var file in editorWindowInfoList)
                    {
                        file.Selected = false;
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
        private bool ContainAttribute(Type type, Type attrType) =>
                type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(x => x.CustomAttributes)
                .Any(x => x.AttributeType == attrType);

        /// <summary>
        /// 特定の型のクラスの関数が持つアトリビュートMenuItemのパスを取得する
        /// </summary>
        /// <param name="type">MenuItemアトリビュートをつけた関数を持つクラスの型</param>
        /// <returns>MenuItemのパス</returns>
        private IEnumerable<string> GetMenuItemPaths(Type type) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .SelectMany(x => x.CustomAttributes)
            .Where(x => x.AttributeType == typeof(MenuItem) && !ContainExclusionFolder(x, exclusionFolderNames))
            .SelectMany(x => x.ConstructorArguments)
            .Where(x => x.Value is string)
            .Select(x => x.Value.ToString())
            .Distinct();

        /// <summary>
        /// Assetsフォルダ以下からMenuItemアトリビュートをもつスクリプトの一覧を取得する
        /// </summary>
        /// <returns>MenuItemアトリビュートをもつスクリプトのリスト</returns>
        private List<EditorWindowInfo> LoadEditorWindowList()
        {
            return Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(x => ContainAttribute(x, typeof(MenuItem)))
                        .SelectMany(x => GetMenuItemPaths(x)
                            .Select(path => new EditorWindowInfo()
                            {
                                Name = path.Split('/').Last(),
                                SourceMenuItemPath = path,
                                DestMenuItemPath = path,
                                FilePath = GetFilePath(x),
                                Selected = false
                            })
                        )
                        .Where(x => !string.IsNullOrEmpty(x.SourceMenuItemPath))
                        .OrderByDescending(x => x.SourceMenuItemPath)
                        .ToList();
        }

        /// <summary>
        /// 存在するフォルダのリストを作成する
        /// </summary>
        /// <param name="editorWindowInfoList">MenuItemの情報のリスト</param>
        /// <returns></returns>
        private List<EditorWindowFolder> CreateExistFolderList(IList<EditorWindowInfo> editorWindowInfoList)
        {
            var folderList = editorWindowInfoList
                .Select(x => new
                {
                    FolderName = Regex.Replace(x.SourceMenuItemPath, "/[^/]+$", string.Empty),
                    EditorWindowInfo = x
                })
                .GroupBy(x => x.FolderName)
                .Select(g =>
                {
                    var editorWindowFolder = new EditorWindowFolder()
                    {
                        Name = g.Key.Split('/').Last(),
                        ParentFolder = null,
                        NameEdittable = false,
                        Path = g.Key
                    };

                    // ファイルに関する親子関係を設定
                    editorWindowFolder.EditorWindowList.AddRange(g.Select(x => 
                    {
                        x.EditorWindowInfo.ParentFolder = editorWindowFolder;
                        return x.EditorWindowInfo;
                    }));

                    return editorWindowFolder;
                })
                .ToList();

            // 兄弟でGroupBy
            var brotherGroups = folderList
                .OrderBy(x => x.Path)
                .GroupBy(x =>
                {
                    var folders = x.Path.Split('/');
                    return string.Join("/", folders.Take(folders.Length - 1));
                })
                .ToList();

            // 兄弟グループでループ
            foreach (var brotherGroup in brotherGroups)
            {
                // 親を取得
                var parent = folderList.FirstOrDefault(x => x.Path == brotherGroup.Key);

                if (parent is null)
                {
                    continue;
                }

                // 親に子を設定
                parent.EditorWindowFolderList.AddRange(brotherGroup);

                // 子に親を設定
                foreach (var child in brotherGroup)
                {
                    child.ParentFolder = parent;
                }
            }

            return folderList;
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
        /// MenuItemのパスを変更する
        /// </summary>
        /// <param name="editorWindowInfoList">フォルダのリスト</param>
        /// <param name="reset">パスを初期状態に戻すかどうか</param>
        private void ReplaceMenuItem(IList<EditorWindowInfo> editorWindowInfoList, bool reset = false)
        {
            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.HasChanged && !reset) continue;

                var code = File.ReadAllText(editorWindowInfo.FilePath);

                var resetPartten = @"(?<keyword>// "+Regex.Escape(TOOL_KEYWORD)+@" )(?<original>\[MenuItem\(.*\)\](\r\n|\r|\n))(?<replaced>\s*\[MenuItem\(.*\)\].*(\r\n|\r|\n))";
                var resetMatch = Regex.Match(code, resetPartten);

                var overrideWritePattern = @"(?<prefix>// UNITYMENUSIMPLER: \[MenuItem\(.*\)\]\s+\[MenuItem\("")(?<replaced>.*)(?<suffix>"".*\)\])";
                var overrideWriteMatch = Regex.Match(code, overrideWritePattern);

                if (reset)
                {
                    if (!resetMatch.Success) continue;

                    // 複製によって追加したアトリビュートとコメントアウトのためのスラッシュとキーワードを削除
                    code = Regex.Replace(code, resetPartten, m => $"{m.Groups["original"]}");
                }
                else if (overrideWriteMatch.Success)
                {
                    // 一度でも変更済みなら追加したアトリビュートを変更する
                    code = Regex.Replace(code, overrideWritePattern, m => $"{m.Groups["prefix"]}{editorWindowInfo.DestMenuItemPath}{m.Groups["suffix"]}");
                }
                else
                {
                    // まだ変更していないならアトリビュートを複製によって追加し変更する
                    // 元のアトリビュートはコメントアウトしてキーワードをつけておく
                    var duplicatePattern = @"(?<indent>( |\t)*)(?<prefix>\[MenuItem\("")(?<replaced>.*)(?<suffix>"".*\)\])(?<newline>(\r\n|\r|\n))";
                    code = Regex.Replace(code, duplicatePattern, m => $"{m.Groups["indent"]}// {TOOL_KEYWORD} {m.Groups["prefix"]}{m.Groups["replaced"]}{m.Groups["suffix"]}{m.Groups["newline"]}{m.Groups["indent"]}{m.Groups["prefix"]}{editorWindowInfo.DestMenuItemPath}{m.Groups["suffix"]}{m.Groups["newline"]}");
                }

                File.WriteAllText(editorWindowInfo.FilePath, code);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            editorWindowInfoList = LoadEditorWindowList();
            folderList = CreateExistFolderList(editorWindowInfoList);
        }

        /// <summary>
        /// MenuItemのフォルダのパスを取得する
        /// </summary>
        /// <param name="folder">パスを取得するフォルダ</param>
        /// <returns></returns>
        private string GetMenuItemFolderPath(EditorWindowFolder folder)
        {
            var currentFolder = folder;
            var path = folder.Name;
            while (currentFolder.ParentFolder != null)
            {
                path = currentFolder.ParentFolder.Name + "/" + path;
                currentFolder = currentFolder.ParentFolder;
            }

            return path;
        }

        /// <summary>
        /// 複数のMenuItemの情報（ファイル）をフォルダに移動させる
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="items"></param>
        private void MoveFile(EditorWindowFolder folder, IEnumerable<EditorWindowInfo> items)
        {
            foreach (var selectedItem in items)
            {
                if (folder.EditorWindowList.Contains(selectedItem)) continue;

                selectedItem.Selected = false;
                var filePath = selectedItem.SourceMenuItemPath.Split('/').Last();
                selectedItem.DestMenuItemPath = folder.Name + "/" + filePath;
                folder.EditorWindowList.Add(selectedItem);
            }
        }

        /// <summary>
        /// 複数のフォルダを別のフォルダに移動させる
        /// </summary>
        /// <param name="folder">移動先のフォルダ</param>
        /// <param name="selectedFolderList">移動させられるフォルダのリスト</param>
        private void MoveFolder(EditorWindowFolder folder, IEnumerable<EditorWindowFolder> selectedFolderList, bool needRemoveFromParentList = true)
        {
            foreach (var selectedFolder in selectedFolderList)
            {
                if (selectedFolder == folder ||
                    folder.EditorWindowFolderList.Contains(selectedFolder)||
                    selectedFolder.EditorWindowFolderList.Contains(folder)) continue;

                selectedFolder.Selected = false;

                //サブフォルダを移動させたとき
                if (selectedFolder.ParentFolder != null && needRemoveFromParentList)
                {
                    selectedFolder.ParentFolder.EditorWindowFolderList.Remove(selectedFolder);
                }

                if (needRemoveFromParentList)
                {
                    folder.EditorWindowFolderList.Add(selectedFolder);
                    selectedFolder.ParentFolder = folder;
                }

                // フォルダに属するファイルへの処理
                foreach (var containItem in selectedFolder.EditorWindowList)
                {
                    containItem.DestMenuItemPath =  GetMenuItemFolderPath(selectedFolder) + "/" + containItem.Name;
                }

                // フォルダに属するフォルダへの処理
                MoveFolder(folder, selectedFolder.EditorWindowFolderList, false);
            }
        }

        /// <summary>
        /// フォルダを親フォルダから抜けさせる
        /// </summary>
        /// <param name="folder">抜けるフォルダ</param>
        private void DropSubFolder(EditorWindowFolder folder, bool needRemoveFromParent = true)
        {
            if (folder.ParentFolder is null) return;

            if (needRemoveFromParent)
            {
                var parentFolder = folder.ParentFolder;
                parentFolder.EditorWindowFolderList.Remove(folder);
                folder.ParentFolder = null;
            }

            // 含まれるファイルへの処理
            var folderPath = GetMenuItemFolderPath(folder);
            foreach (var containItem in folder.EditorWindowList)
            {
                containItem.DestMenuItemPath = folderPath + "/" + containItem.Name;
            }

            // 含まれているサブフォルダへの処理
            foreach (var containFolder in folder.EditorWindowFolderList)
            {
                DropSubFolder(containFolder, false);
            }
        }

        private void RevertAllMenuItem()
        {
            editorWindowInfoList = LoadEditorWindowList();
            folderList = CreateExistFolderList(editorWindowInfoList);
        }

        /// <summary>
        /// フォルダを開いた状態にする
        /// </summary>
        /// <param name="folder"></param>
        private void OpenFolder(EditorWindowFolder folder)
        {
            folder.Foldout = true;
            if (folder.ParentFolder != null)
            {
                OpenFolder(folder.ParentFolder);
            }
        }
    }
}