using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gatosyocora.UnityMenuSimpler.DataClass
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