﻿using System.Collections;
using System.Collections.Generic;

namespace Gatosyocora.UnityMenuSimpler.DataClass
{
    /// <summary>
    /// EditorWindowスクリプトに関する情報のクラス
    /// </summary>
    public class EditorWindowInfo
    {
        /// <summary>
        /// 表示用の名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 変更前のMenuItemのパス
        /// </summary>
        public string SourceMenuItemPath { get; set; }

        /// <summary>
        /// 変更後のMenuItemのパス
        /// </summary>
        public string DestMenuItemPath { get; set; }

        /// <summary>
        /// EditorWindowスクリプトファイルのパス
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 選択状態であるか
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// MenuItemのパスが変更されたかどうか
        /// </summary>
        public bool HasChanged
        {
            get
            {
                return !string.IsNullOrEmpty(DestMenuItemPath) &&
                         SourceMenuItemPath != DestMenuItemPath;
            }
        }
    }
}