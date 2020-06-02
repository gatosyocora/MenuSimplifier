// MenuSimplifier v1.0
// Copyright (c) 2020 gatosyocora
// MIT License | see LICENSE

namespace Gatosyocora.MenuSimplifier.DataClass
{
    /// <summary>
    /// EditorWindowスクリプトに関する情報のクラス
    /// </summary>
    public class EditorWindowInfo : EditorWindowBase
    {
        /// <summary>
        /// 変更後のMenuItemのパス
        /// </summary>
        public string DestMenuItemPath => GetMenuItemPath();

        /// <summary>
        /// EditorWindowスクリプトファイルのパス
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// MenuItemのパスが変更されたかどうか
        /// </summary>
        public bool HasChanged
        {
            get
            {
                return !string.IsNullOrEmpty(DestMenuItemPath) &&
                         Path != DestMenuItemPath;
            }
        }

        /// <summary>
        /// 変更がファイルに適用済みかどうか
        /// </summary>
        public bool Applied { get; set; } = false;
    }
}