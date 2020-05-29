using System.Collections.Generic;

namespace Gatosyocora.UnityMenuSimpler.DataClass
{
    /// <summary>
    /// EditorWindowスクリプトに関する情報を複数まとめるクラス
    /// </summary>
    public class EditorWindowFolder
    {
        /// <summary>
        /// 表示用の名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 属している情報のリスト
        /// </summary>
        public List<EditorWindowInfo> EditorWindowList { get; } = new List<EditorWindowInfo>();

        /// <summary>
        /// 属している情報をまとめるもののリスト
        /// </summary>
        public List<EditorWindowFolder> EditorWindowFolderList { get; } = new List<EditorWindowFolder>();

        /// <summary>
        /// 選択状態であるか
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        ///　閉じられた状態であるか
        /// </summary>
        public bool Foldout { get; set; }

        /// <summary>
        /// 親フォルダ
        /// </summary>
        public EditorWindowFolder ParentFolder { get; set; }
    }
}