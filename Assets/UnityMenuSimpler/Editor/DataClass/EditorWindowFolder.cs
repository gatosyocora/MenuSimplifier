using Gatosyocora.UnityMenuSimpler.Interfaces;
using System.Collections.Generic;

namespace Gatosyocora.UnityMenuSimpler.DataClass
{
    /// <summary>
    /// EditorWindowスクリプトに関する情報を複数まとめるクラス
    /// </summary>
    public class EditorWindowFolder : EditorWindowBase
    {
        /// <summary>
        /// 属している情報のリスト
        /// </summary>
        public List<EditorWindowInfo> EditorWindowList { get; } = new List<EditorWindowInfo>();

        /// <summary>
        /// 属している情報をまとめるもののリスト
        /// </summary>
        public List<EditorWindowFolder> EditorWindowFolderList { get; } = new List<EditorWindowFolder>();

        /// <summary>
        ///　開かれた状態であるか
        /// </summary>
        public bool Foldout { get; set; }

        /// <summary>
        /// 名前を変更可能か
        /// </summary>
        public bool NameEdittable { get; set; }

        /// <summary>
        /// フォルダを開いた状態にする
        /// </summary>
        public void ShowChildren()
        {
            var folder = this;
            while (folder != null)
            {
                folder.Foldout = true;
                folder = folder.ParentFolder;
            }
        }


        /// <summary>
        /// 親であるか調べる
        /// </summary>
        /// <param name="item">子であるか調べる要素</param>
        /// <returns></returns>
        public bool IsParentOf(EditorWindowBase item)
        {
            var current = item;
            while(current.ParentFolder != null)
            {
                if (current.ParentFolder == this)
                {
                    return true;
                }
                current = current.ParentFolder;
            }

            return false;
        }
    }
}