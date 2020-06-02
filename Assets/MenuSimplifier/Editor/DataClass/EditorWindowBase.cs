using Gatosyocora.MenuSimplifier.Interfaces;

// MenuSimplifier v1.0
// Copyright (c) 2020 gatosyocora
// MIT License | see LICENSE

namespace Gatosyocora.MenuSimplifier.DataClass
{
    public class EditorWindowBase : IEditorWindowItem
    {
        /// <summary>
        /// 表示用の名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 選択状態であるか
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// 親フォルダ
        /// </summary>
        public EditorWindowFolder ParentFolder { get; set; }

        /// <summary>
        /// MenuItemの絶対パス
        /// </summary>
        public string Path { get; set; }

        public override string ToString()
        {
            return Path;
        }

        /// <summary>
        /// 絶対パスを取得する
        /// </summary>
        /// <returns></returns>
        public string GetMenuItemPath()
        {
            IEditorWindowItem item = this;
            var path = item.Name;
            while (item.ParentFolder != null)
            {
                path = item.ParentFolder.Name + "/" + path;
                item = item.ParentFolder;
            }

            return path;
        }
    }
}