using UnityEngine;

// MenuSimplifier v1.0
// Copyright (c) 2020 gatosyocora
// MIT License | see LICENSE

namespace Gatosyocora.MenuSimplifier.DataClass
{
    [CreateAssetMenu]
    public class LanguageTemplate : ScriptableObject
    {
        #region LayoutType
        public string simple;
        public string advanced;
        #endregion

        #region Action
        public string allResetToDefault;
        public string addFolder;
        public string dropSubFolder;
        public string showChanged;
        public string apply;
        public string allRevert;
        public string drop;
        public string ok;
        public string allIn;
        public string remove;
        #endregion

        #region status
        public string applied;
        #endregion
    }
}