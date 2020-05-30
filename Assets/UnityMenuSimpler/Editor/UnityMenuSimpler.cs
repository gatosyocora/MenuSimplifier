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

                using (var scroll = new EditorGUILayout.ScrollViewScope(folderListScrollPos,
                                            alwaysShowVertical: false,
                                            alwaysShowHorizontal: true))
                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    folderListScrollPos = scroll.scrollPosition;
                    folderRect = scope.rect;

                    foreach (var folder in folderList.Where(x => x.ParentFolder == null).ToList())
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
                                () => MoveFolder(folder, folderList.Where(x => x != folder && x.ParentFolder == null)),
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

                            if (selectedFolder.ParentFolder == null) continue;

                            DropSubFolder(selectedFolder);
                        }
                    }

                    if (check.changed) Repaint();
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
                    if (GUILayout.Button("Show Changed"))
                    {
                        // TODO:厳密にする必要がある
                        // サブフォルダに変更後のものがあったかは開いているかで判断
                        // 要素数が多いほどサブフォルダである可能性があるので要素数が多いものから開いていく
                        foreach (var folder in folderList.OrderByDescending(x => x.EditorWindowList.Count))
                        {
                            if (folder.EditorWindowList.Any(x => x.HasChanged) || 
                                folder.EditorWindowFolderList.Any(x => x.Foldout))
                            {
                                folder.Foldout = true;
                            }
                        }
                    }

                    if (GUILayout.Button("Apply"))
                    {
                        ReplaceMenuItem(editorWindowInfoList);
                    }
                }

                if (GUILayout.Button("All Reset"))
                {
                    ReplaceMenuItem(editorWindowInfoList, true);
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
                                Name = menuItemPath.Split('/').Last(),
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

        /// <summary>
        /// 存在するフォルダのリストを作成する
        /// </summary>
        /// <param name="editorWindowInfoList">MenuItemの情報のリスト</param>
        /// <returns></returns>
        private List<EditorWindowFolder> CreateExistFolderList(IReadOnlyCollection<EditorWindowInfo> editorWindowInfoList)
        {
            var dict = new Dictionary<string, EditorWindowFolder>();

            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                // パスの最後のスラッシュ以降だけ削除
                var folderName = Regex.Replace(editorWindowInfo.SourceMenuItemPath, "/[^/]+$", string.Empty);

                if (!dict.TryGetValue(folderName, out EditorWindowFolder folder))
                {
                    folder = new EditorWindowFolder()
                    {
                        Name = folderName,
                        ParentFolder = null,
                        NameEdittable = false
                    };
                    dict.Add(folderName, folder);
                }

                folder.EditorWindowList.Add(editorWindowInfo);
            }

            // パスが深いところから処理していく
            var orderedKeys = dict.Keys.OrderByDescending(x => x);

            foreach (var keyName in orderedKeys)
            {
                // ルートフォルダなので親フォルダを探さなくてよい
                if (keyName.IndexOf('/') == -1) continue;

                // パスの最後のスラッシュ以降だけ削除
                var parentFolderName = Regex.Replace(keyName, "/[^/]+$", string.Empty);

                // 親フォルダが見つかったのでフォルダ情報を変更する
                if (dict.TryGetValue(parentFolderName, out EditorWindowFolder parentFolder))
                {
                    dict[keyName].Name = keyName.Split('/').Last();
                    dict[keyName].ParentFolder = parentFolder;
                    parentFolder.EditorWindowFolderList.Add(dict[keyName]);
                }
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
        /// MenuItemのパスを変更する
        /// </summary>
        /// <param name="editorWindowInfoList">フォルダのリスト</param>
        /// <param name="reset">パスを初期状態に戻すかどうか</param>
        private void ReplaceMenuItem(IReadOnlyCollection<EditorWindowInfo> editorWindowInfoList, bool reset = false)
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
                    folder.EditorWindowFolderList.Contains(selectedFolder)) continue;

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
            if (folder.ParentFolder == null) return;

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
    }
}