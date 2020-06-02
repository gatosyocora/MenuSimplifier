using Gatosyocora.MenuSimplifier.DataClass;
using System.Collections;
using System.Collections.Generic;

namespace Gatosyocora.MenuSimplifier.Interfaces
{
    public interface IEditorWindowItem
    {

        /// <summary>
        /// 表示用の名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// MenuItemの絶対パス
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// 属しているフォルダ
        /// </summary>
        EditorWindowFolder ParentFolder { get; set; }

        /// <summary>
        /// 絶対パスを取得する
        /// </summary>
        /// <returns></returns>
        string GetMenuItemPath();
    }
}