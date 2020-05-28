using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Gatosyocora.UnityMenuSimpler
{
    public static class GatoGUILayout
    {

        public static bool ToggleLabelArea(string label, bool toggle, Color disactiveColor, Color activeColor)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1.5f);
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = (toggle) ? activeColor : disactiveColor;
            GUI.Label(rect, label, GUI.skin.box);
            GUI.backgroundColor = defaultColor;

            var e = Event.current;
            if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                toggle = !toggle;
                GUI.changed = true;
            }

            return toggle;
        }
    }
}