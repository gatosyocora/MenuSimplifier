using Gatosyocora.UnityMenuSimpler.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace Gatosyocora.UnityMenuSimpler.DataClass
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
    }
}