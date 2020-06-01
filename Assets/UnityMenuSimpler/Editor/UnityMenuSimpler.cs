using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Gatosyocora.UnityMenuSimpler.DataClass;
using Gatosyocora.UnityMenuSimpler.Interfaces;

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

        private EditorWindowBase selectedItem;

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
            if (!(selectedItem is null))
            {
                selectedItem.Selected = true;
            }

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
                                (f) => MoveItem(f, selectedItem),
                                () => {
                                    foreach (var selectedItem in folderList.Where(x => x != folder && x.ParentFolder is null))
                                    {
                                        MoveItem(folder, selectedItem);
                                    }
                                },
                                () => folderList.Remove(folder),
                                (f) => DropSubFolder(f),
                                (f) => selectedItem = f
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
                        if (!(selectedItem.ParentFolder is null) && 
                            selectedItem is EditorWindowFolder selectedFolder)
                        {
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
                            file.ParentFolder.ShowChildren();
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
                    selectedItem.Selected = false;
                    selectedItem = null;
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
            var filePaths = AssetDatabase.FindAssets(type.Name + " t:Script")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(x => Path.GetFileNameWithoutExtension(x) == type.Name)
                .ToArray();

            if (filePaths.Count() == 0 || filePaths.Count() >= 2)
            {
                Debug.LogErrorFormat("{0}のファイルが正しく取得できませんでした", type);
                return string.Empty;
            }

            return filePaths.Single();
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
                                Path = path,
                                FilePath = GetFilePath(x)
                            })
                        )
                        .Where(x => !string.IsNullOrEmpty(x.Path))
                        .OrderByDescending(x => x.Path)
                        .ToList();
        }

        /// <summary>
        /// 存在するフォルダのリストを作成する
        /// </summary>
        /// <param name="editorWindowInfoList">MenuItemの情報のリスト</param>
        /// <returns></returns>
        private List<EditorWindowFolder> CreateExistFolderList(IReadOnlyCollection<EditorWindowInfo> editorWindowInfoList)
        {
            /*
             * editorWindowInfoListのに含まれる要素のPathの例
             * A/B/a
             * C/D/b
             * A/c
             * A/B/d
             * A/E/e
             * T/F/f
             */


            var folderList = editorWindowInfoList
                .SelectMany(x =>
                {
                    var deepFolderPath = Regex.Replace(x.Path, "/[^/]+$", string.Empty);
                    var folderCount = deepFolderPath.Count(c => c == '/') + 1;
                    var folders = deepFolderPath.Split('/');
                    return Enumerable.Range(1, folderCount).Select(n => string.Join("/", folders.Take(n)));
                })
                .Distinct()
                .Select(x => new EditorWindowFolder
                {
                    Name = x.Split('/').Last(),
                    ParentFolder = null,
                    NameEdittable = false,
                    Path = x
                })
                .ToList();

            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                var parentPath = Regex.Replace(editorWindowInfo.Path, "/[^/]+$", string.Empty);

                // 親を取得
                var parent = folderList.Single(x => x.Path == parentPath);

                // 親に子を設定
                parent.EditorWindowList.Add(editorWindowInfo);

                // 子に親を設定
                editorWindowInfo.ParentFolder = parent;
            }

            // 兄弟でGroupBy
            var brotherGroups = folderList
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
        private void ReplaceMenuItem(IReadOnlyCollection<EditorWindowInfo> editorWindowInfoList, bool reset = false)
        {
            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.HasChanged && !reset &&
                string.IsNullOrEmpty(editorWindowInfo.FilePath)) continue;

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

        private void MoveItem(EditorWindowFolder folder, EditorWindowBase movedItem)
        {
            if (movedItem == null || folder == movedItem) return;

            if (movedItem is EditorWindowFolder movedFolder)
            {
                if (movedFolder.IsParentOf(folder)) return;

                if (!(movedFolder.ParentFolder is null))
                {
                    movedFolder.ParentFolder.EditorWindowFolderList.Remove(movedFolder);
                }
                folder.EditorWindowFolderList.Add(movedFolder);
                movedFolder.ParentFolder = folder;
            }
            else if (movedItem is EditorWindowInfo movedFile)
            {
                if (!(movedFile.ParentFolder is null))
                {
                    movedFile.ParentFolder.EditorWindowList.Remove(movedFile);
                }
                folder.EditorWindowList.Add(movedFile);
                movedFile.ParentFolder = folder;
            }

            selectedItem.Selected = false;
            selectedItem = null;
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
        }

        private void RevertAllMenuItem()
        {
            editorWindowInfoList = LoadEditorWindowList();
            folderList = CreateExistFolderList(editorWindowInfoList);
        }
    }
}