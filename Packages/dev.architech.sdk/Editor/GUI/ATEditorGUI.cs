using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArchiTech.SDK.Editor
{
    public static class ATEditorGUI
    {
        public static bool showDropdown;

        public static PropertyDropdownScope PropertyDropdown => new PropertyDropdownScope();

        public class PropertyDropdownScope : GUI.Scope
        {
            private bool m_WithDropdownCache;

            internal PropertyDropdownScope()
            {
                m_WithDropdownCache = showDropdown;
                showDropdown = true;
            }

            protected override void CloseScope()
            {
                showDropdown = m_WithDropdownCache;
                m_WithDropdownCache = false;
            }
        }
    }

    [CustomPropertyDrawer(typeof(ATBehaviour), true)]
    public class ATPropertyWithDropdownDrawer : PropertyDrawer
    {
        private UnityEngine.Object[] referencesCache = new UnityEngine.Object[0];
        private string[] labelsCache = new string[0];

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string[] labels = new string[0];
            UnityEngine.Object[] options = new UnityEngine.Object[0];
            EditorGUI.BeginProperty(position, label, property);
            position = new Rect(position);
            Rect rect = new Rect(position);
            bool validPrefab = PrefabUtility.IsPartOfPrefabInstance(property.serializedObject.targetObject) 
                               || !PrefabUtility.IsPartOfPrefabAsset(property.serializedObject.targetObject);
            if (ATEditorGUI.showDropdown && GUI.enabled && validPrefab)
            {
                getOptions(property, out options, out labels);
                if (options.Length > 0)
                {
                    position.width -= 20;
                    rect.x += position.width;
                    rect.width = 20;
                }
            }

            EditorGUI.PropertyField(position, property, label);
            if (options.Length > 0)
            {
                var value = (UnityEngine.Object)property.GetValue(); // this will throw if types don't match
                var index = Array.IndexOf(options, value);
                var newIndex = EditorGUI.Popup(rect, index, labels);
                if (index != newIndex) property.SetValue(options[newIndex]);
            }

            EditorGUI.EndProperty();
        }

        private void getOptions(SerializedProperty property, out UnityEngine.Object[] options, out string[] labels)
        {
            System.Type type = property.GetValueType();
            if (type.HasElementType) type = type.GetElementType();
            if (referencesCache.Length == 0)
                referencesCache = ATEditorUtility.GetComponentsInSceneWithDistinctNames(type, out labelsCache);
            options = referencesCache;
            labels = labelsCache;
            if (labels.Length != options.Length) throw new Exception($"Labels length does not match options length for requested property {property.propertyPath}");
        }
    }
}