using System;
using ArchiTech.SDK;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArchiTech.Umbrella.Editor
{
    [CustomEditor(typeof(ATToggle), true)]
    public class ToggleEditor : UdonActionEditor
    {
        private ATToggle _atToggleScript;

        // private bool initialState;
        // private bool oneWay;

        private bool[] inverseOfState;

        private ATToggle[] siblings;

        protected override Type dynamicType { get => typeof(bool); }

        protected override float customElementHeight { get => EditorGUIUtility.singleLineHeight; }

        protected override void InitData()
        {
            base.InitData();
            _atToggleScript = (ATToggle)target;
            // initialState = _toggleScript.initialState;
            // oneWay = _toggleScript.oneWay;
            inverseOfState = NormalizeArray(_atToggleScript.inverseOfState, actionObjects.Count);

            siblings = NormalizeArray(_atToggleScript.siblings, 0);
        }

        protected override void RenderChangeCheck()
        {
            DrawVariablesByName(nameof(ATToggle.siblings));
            DrawVariablesByType(typeof(bool));
            VariablesDrawn(nameof(ATToggle.inverseOfState));
            DrawVariables();
            base.RenderChangeCheck();
        }

        protected override void DrawElementExtension(Rect rect, int listIndex, in Object @object, in ActionType action)
        {
            inverseOfState[listIndex] = EditorGUI.Toggle(new Rect(rect.x, rect.y, rect.width / 2, rect.height), I18n.Tr("Inverse Of State"), inverseOfState[listIndex]);
        }

        protected override void SaveData()
        {
            base.SaveData();
            init = false;
            _atToggleScript.inverseOfState = inverseOfState;
            _atToggleScript.siblings = siblings;
        }
    }
}