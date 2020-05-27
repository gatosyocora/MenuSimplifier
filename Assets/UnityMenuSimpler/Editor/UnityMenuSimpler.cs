using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;
using System.IO;
using System.Text.RegularExpressions;

// UnityMenuSimpler v1.0
// Copyright (c) 2020 gatosyocora
// MIT License

namespace Gatosyocora.UnityMenuSimpler
{
    public class UnityMenuSimpler : EditorWindow
    {
        /// <summary>
        /// EditorWindow�X�N���v�g�Ɋւ�����̃N���X
        /// </summary>
        public class EditorWindowInfo
        {
            public string Name { get; set; }
            public string SourceMenuItemPath { get; set; }
            public string DestMenuItemPath { get; set; }
            public string FilePath { get; set; }
            public bool Selected { get; set; }
            public bool HasChanged { get { return SourceMenuItemPath != DestMenuItemPath; } }
        }

        public class EditorWindowFolder
        {
            public string Name { get; set; }
            public List<EditorWindowInfo> EditorWindowList { get; } = new List<EditorWindowInfo>();
            public List<EditorWindowFolder> EditorWindowFolderList { get; } = new List<EditorWindowFolder>();
        }

        private List<EditorWindowInfo> editorWindowInfoList;
        private string folderName = string.Empty;
        private List<EditorWindowFolder> folderList;

        /// <summary>
        /// MenuItem�̃t�H���_�̏��O�Ώ�
        /// </summary>
        private readonly static string[] exclusionFolderNames = new string[] { "GameObject", "CONTEXT", "Assets" };

        private Vector2 scrollPos = Vector2.zero;

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

                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (var folder in folderList.ToList())
                    {
                        DrawFolder(folder);
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Add Folder"))
                {
                    folderList.Add(new EditorWindowFolder());
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = scroll.scrollPosition;

                    foreach (var editorWindowInfo in editorWindowInfoList)
                    {
                        if (!string.IsNullOrEmpty(editorWindowInfo.DestMenuItemPath)) continue;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            editorWindowInfo.Selected = EditorGUILayout.ToggleLeft(
                                                            string.Empty,
                                                            editorWindowInfo.Selected,
                                                            GUILayout.Width(30f));
                            EditorGUILayout.LabelField(editorWindowInfo.Name, editorWindowInfo.DestMenuItemPath);
                        }
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Move MenuItem to Child"))
                {
                    MoveMenuItemToChildren(folderName);
                }

                if (GUILayout.Button("Move MenuItem to Parent"))
                {
                    MoveMenuItemToParent(folderName);
                }
            }
        }

        private void DrawFolder(EditorWindowFolder folder)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    folder.Name = EditorGUILayout.TextField(folder.Name);

                    if (GUILayout.Button("x", GUILayout.Width(30f)))
                    {
                        //folderList.Remove(folder);
                    }
                }

                foreach (var editorWindowfolder in folder.EditorWindowFolderList)
                {
                }

                foreach (var editorWindowInfo in folder.EditorWindowList.ToList())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(editorWindowInfo.Name);

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
                    if (GUILayout.Button("Contain"))
                    {
                        foreach (var selectedItem in editorWindowInfoList.Where(x => x.Selected))
                        {
                            selectedItem.Selected = false;
                            var filePath = selectedItem.SourceMenuItemPath.Split('/').Last();
                            selectedItem.DestMenuItemPath = folder.Name + "/" + filePath;
                            folder.EditorWindowList.Add(selectedItem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ����̌^�N���X�̃t�@�C���̃p�X��Assets�t�H���_�ȉ�����擾����
        /// </summary>
        /// <param name="type">�N���X�̌^</param>
        /// <returns>�t�@�C���p�X</returns>
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
        /// ����̌^�̃N���X������̃A�g���r���[�g�������\�b�h���܂�ł��邩���肷��
        /// </summary>
        /// <param name="type">�N���X�̌^</param>
        /// <param name="attrType">�A�g���r���[�g�̌^</param>
        /// <returns>�܂܂��ꍇtrue</returns>
        private bool ContainAttribute(Type type, Type attrType)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .SelectMany(x => x.CustomAttributes)
                        .Any(x => x.AttributeType == attrType);
        }

        /// <summary>
        /// ����̌^�̃N���X�̊֐������A�g���r���[�gMenuItem�̃p�X���擾����
        /// </summary>
        /// <param name="type">MenuItem�A�g���r���[�g�������֐������N���X�̌^</param>
        /// <returns>MenuItem�̃p�X</returns>
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
        /// Assets�t�H���_�ȉ�����MenuItem�A�g���r���[�g�����X�N���v�g�̈ꗗ���擾����
        /// </summary>
        /// <returns>MenuItem�A�g���r���[�g�����X�N���v�g�̃��X�g</returns>
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
                        Name = folderName
                    };
                    dict.Add(folderName, folder);
                }

                folder.EditorWindowList.Add(editorWindowInfo);
            }

            return dict.Values.ToList();
        }

        /// <summary>
        /// �p�X�����O����t�H���_�ɓ�����MenuItem�A�g���r���[�g���܂܂�邩���f����
        /// </summary>
        /// <param name="attrData">�A�g���r���[�g�̃f�[�^</param>
        /// <param name="exclusionFolderNames">���O����t�H���_�̈ꗗ</param>
        /// <returns>�܂܂��ꍇtrue</returns>
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
        /// �����ɃR���p�C�������s����
        /// </summary>
        private static void ForceCompile()
        {
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        }

        private void MoveMenuItemToChildren(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return;

            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.Selected) continue;

                var code = File.ReadAllText(editorWindowInfo.FilePath);
                code = code.Replace(editorWindowInfo.SourceMenuItemPath, folderName + "/" + editorWindowInfo.SourceMenuItemPath);
                File.WriteAllText(editorWindowInfo.FilePath, code);
            }

            ForceCompile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            editorWindowInfoList = LoadEditorWindowList();
        }

        private void MoveMenuItemToParent(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return;

            foreach (var editorWindowInfo in editorWindowInfoList)
            {
                if (!editorWindowInfo.Selected) continue;

                if (!editorWindowInfo.SourceMenuItemPath.StartsWith(folderName + "/")) continue;

                var code = File.ReadAllText(editorWindowInfo.FilePath);
                var replacedMenuItemPath = editorWindowInfo.SourceMenuItemPath.Remove(0, folderName.Length + 1);
                code = code.Replace(editorWindowInfo.SourceMenuItemPath, replacedMenuItemPath);
                File.WriteAllText(editorWindowInfo.FilePath, code);
            }

            ForceCompile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            editorWindowInfoList = LoadEditorWindowList();
        }
    }
}