using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    public static class ATEditorGUIUtility
    {
        public static GUIStyle defaultTooltipStyle
        {
            get => new GUIStyle("HelpBox")
            {
                fontSize = 14,
                richText = true,
                wordWrap = true,
                margin = { top = 15 },
                padding = { bottom = 5, top = 5, left = 10, right = 10 }
            };
        }

        public static GUIStyle slimBox
        {
            get
            {
                var s = new GUIStyle("box");
                s.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                s.padding = new RectOffset(0, 0, 1, 0);
                return s;
            }
        }

        public class ShrinkWrapLabelScope : GUI.Scope
        {
            private readonly bool wrapField;

            /// <summary>
            /// Scope that forces the default labelWidth to be dynamically the size of the provided GUIContent.
            /// Resets value upon closing.
            /// </summary>
            public ShrinkWrapLabelScope(GUIContent label, SerializedProperty property = null)
            {
                var isPrefab = property != null && PrefabUtility.IsPartOfPrefabInstance(property.serializedObject.targetObject);
                EditorGUIUtility.labelWidth = ATEditorGUIUtility.GetLabelWidth(label, isPrefab);
            }

            public ShrinkWrapLabelScope(GUIContent label, bool wrapField, SerializedProperty property = null)
            {
                var isPrefab = property != null && PrefabUtility.IsPartOfPrefabInstance(property.serializedObject.targetObject);
                this.wrapField = wrapField;
                if (wrapField)
                {
                    EditorGUIUtility.labelWidth = 1;
                    EditorGUIUtility.fieldWidth = ATEditorGUIUtility.GetLabelWidth(label, isPrefab) - 1;
                }
                else EditorGUIUtility.labelWidth = ATEditorGUIUtility.GetLabelWidth(label, isPrefab);
            }

            protected override void CloseScope()
            {
                EditorGUIUtility.labelWidth = 0;
                if (wrapField) EditorGUIUtility.fieldWidth = 0;
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            [Obsolete("Use ATEditorGUIUtility.GetLabelWidth(GUIContent, bool) instead")]
            public static float GetLabelWidth(GUIContent label, bool isPrefab)
            {
                return GetGenericLabelStyle(isPrefab).CalcSize(label).x;
            }
        }

        public class SaveObjectScope : GUI.Scope
        {
            private readonly UnityEngine.Object target;

            /// <summary>
            /// Creates a disposable scope that calls the Undo op on the target and then closes out with the Prefabs modification logic.
            /// </summary>
            /// <param name="target">Component that needs checked against</param>
            /// <param name="undoMessage">The message that will be provided to the Undo operation</param>
            public SaveObjectScope(UnityEngine.Object target, string undoMessage = null)
            {
                this.target = target;
                if (undoMessage == null) undoMessage = $"Modify {target.GetType().Name} Content";
                Undo.RecordObject(target, undoMessage);
            }

            protected override void CloseScope()
            {
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        public class SectionScope : GUI.Scope
        {
            public SectionScope(string header)
            {
                ATEditorGUILayout.Header(header, false, 1);
                EditorGUILayout.BeginVertical("box");
            }

            protected override void CloseScope()
            {
                EditorGUILayout.EndVertical();
            }
        }

        public static GUIStyle GetGenericLabelStyle(bool isPrefab = true)
        {
            return new GUIStyle(EditorStyles.label) { font = isPrefab ? EditorStyles.boldFont : EditorStyles.standardFont };
        }

        public static float GetLabelWidth(GUIContent label, bool isPrefab)
        {
            return GetGenericLabelStyle(isPrefab).CalcSize(label).x;
        }

        /// <summary>
        /// This will give you the calculated width of the provided label
        /// Uses a SerializedProperty reference to determine certain characteristics about the font
        /// </summary>
        /// <param name="label">Label to calculate the width for</param>
        /// <param name="property">SerializedProperty involved</param>
        /// <returns>calculated width of the label</returns>
        public static float GetLabelWidth(GUIContent label, SerializedProperty property)
        {
            var isPrefab = property != null && PrefabUtility.IsPartOfPrefabInstance(property.serializedObject.targetObject);
            return GetLabelWidth(label, isPrefab);
        }

        public static float GetLabelWidth(SerializedProperty property)
        {
            return GetLabelWidth(new GUIContent(getInspectorName(property)), property);
        }

        /// <summary>
        /// Generates a GUIContent for a given property. Considers usage of InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="prop">the desired property to resolve the label for</param>
        /// <param name="showHint">flag for whether or not to have the tooltip displayed as separate text. Default is false which is normal tooltip hover behavior</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        public static GUIContent GetPropertyLabel(SerializedProperty prop, bool showHint, GUIStyle style = null)
        {
            var inspectorName = getInspectorName(prop);
            var tooltip = getTooltip(prop);
            if (!showHint) return new GUIContent(inspectorName, tooltip);
            // when hints are shown, the tooltip gets a label field
            if (tooltip != null) EditorGUILayout.LabelField(tooltip, style ?? defaultTooltipStyle);
            return new GUIContent(inspectorName);
        }

        /// <summary>
        /// Generates a GUIContent for a given property. Accepts usage of both InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="context">object to inspect for the fieldName</param>
        /// <param name="fieldName">field to check for related attributes</param>
        /// <param name="showHint">flag for whether or not to have the tooltip displayed as separate text. Default is false which is normal tooltip hover behavior</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        public static GUIContent GetPropertyLabel(UnityEngine.Object context, string fieldName, bool showHint = false, GUIStyle style = null)
        {
            var prop = new SerializedObject(context).FindProperty(fieldName);
            if (prop != null) return GetPropertyLabel(prop, showHint, style);
            var label = getInspectorName(context, fieldName);
            var tooltip = getTooltip(context, fieldName);
            if (!showHint) return new GUIContent(label, tooltip);
            // when hints are shown, the tooltip gets a label field
            if (tooltip != null) EditorGUILayout.LabelField(tooltip, style ?? defaultTooltipStyle);
            return new GUIContent(label);
        }

        private static string getInspectorName(SerializedProperty prop)
        {
            var inspectorName = resolveInspectorName(prop.GetAttributes<InspectorNameAttribute>());
            if (inspectorName.displayName == null)
            {
                if (inspectorName is I18nInspectorNameAttribute)
                    log(true, $"I18nInspectorNames are available for {prop.serializedObject.targetObject.GetType().FullName}::{prop.name} but none match the active language ({I18n.Language})");
                return prop.displayName; // no valid inspector name attribute
            }

            return inspectorName.displayName;
        }

        private static string getInspectorName(UnityEngine.Object context, string fieldName)
        {
            var prop = new SerializedObject(context).FindProperty(fieldName);
            if (prop != null) return getInspectorName(prop);
            var inspectorName = resolveInspectorName(context.GetFieldAttributes<InspectorNameAttribute>(fieldName));
            if (inspectorName.displayName == null)
            {
                if (inspectorName is I18nInspectorNameAttribute)
                    log(true, $"I18nInspectorNames are available for {context.GetType().FullName}::{fieldName} but none match the active language ({I18n.Language})");
                return fieldName; // no valid inspector name attribute
            }

            return inspectorName.displayName;
        }

        private static InspectorNameAttribute resolveInspectorName(IEnumerable<InspectorNameAttribute> inspectorNames)
        {
            InspectorNameAttribute inspectorName = new InspectorNameAttribute(null);
            InspectorNameAttribute fallback = new InspectorNameAttribute(null);
            I18nInspectorNameAttribute engFallback = new I18nInspectorNameAttribute(null);
            foreach (InspectorNameAttribute tip in inspectorNames)
            {
                if (!(tip is I18nInspectorNameAttribute i18n))
                {
                    // store the first generic inspector name found just in case
                    if (fallback.displayName == null) fallback = tip;
                    continue;
                }

                if (i18n.lang == SystemLanguage.English) engFallback = i18n;
                if (i18n.lang == I18n.Language)
                {
                    inspectorName = i18n;
                    break;
                }
            }

            if (inspectorName.displayName == null) inspectorName = engFallback;
            if (inspectorName.displayName == null) inspectorName = fallback;
            return inspectorName;
        }

        private static string getTooltip(SerializedProperty prop)
        {
            TooltipAttribute tooltip = resolveTooltip(prop.GetAttributes<TooltipAttribute>());
            if (tooltip.tooltip == null)
            {
                if (tooltip is I18nTooltipAttribute)
                    log(true, $"I18nTooltips are available for {prop.serializedObject.targetObject.GetType().FullName}::{prop.name} but none match the active language ({I18n.Language})");
                return null; // no valid tooltip attribute
            }

            return tooltip.tooltip;
        }

        private static string getTooltip(UnityEngine.Object context, string fieldName)
        {
            var prop = new SerializedObject(context).FindProperty(fieldName);
            if (prop != null) return getTooltip(prop);
            TooltipAttribute tooltip = resolveTooltip(context.GetFieldAttributes<TooltipAttribute>(fieldName));
            if (tooltip.tooltip == null)
            {
                if (tooltip is I18nTooltipAttribute)
                    log(true, $"I18nTooltips are available for {context.GetType().FullName}::{fieldName} but none match the active language ({I18n.Language})");
                return null; // no valid tooltip attribute
            }

            return tooltip.tooltip;
        }

        private static TooltipAttribute resolveTooltip(IEnumerable<TooltipAttribute> tooltips)
        {
            TooltipAttribute tooltip = new TooltipAttribute(null);
            TooltipAttribute fallback = new TooltipAttribute(null);
            I18nTooltipAttribute engFallback = new I18nTooltipAttribute(null);
            foreach (TooltipAttribute tip in tooltips)
            {
                if (!(tip is I18nTooltipAttribute i18n))
                {
                    // store the first generic tooltip found just in case
                    if (fallback.tooltip == null) fallback = tip;
                    continue;
                }

                if (i18n.lang == SystemLanguage.English) engFallback = i18n;
                if (i18n.lang == I18n.Language)
                {
                    tooltip = i18n;
                    break;
                }
            }

            if (tooltip.tooltip == null) tooltip = engFallback;
            if (tooltip.tooltip == null) tooltip = fallback;
            return tooltip;
        }

        private static void log(bool assert, string msg)
        {
            if (assert) UnityEngine.Debug.LogAssertion(msg);
            else UnityEngine.Debug.Log(msg);
        }
    }
}