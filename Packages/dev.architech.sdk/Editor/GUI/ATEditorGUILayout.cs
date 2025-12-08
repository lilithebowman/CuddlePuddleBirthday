using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    public static class ATEditorGUILayout
    {
        
        /// <summary>
        /// An alternative to EditorGUILayout.Space which will pick the rect dimensions
        /// based on the most recent layout group's direction (either vertical or horizontal)
        /// </summary>
        /// <param name="size">the size of the spacer that you want to render</param>
        /// <param name="expand">whether or not to allow the spacer to expand beyond the size value</param>
        public static void Spacer(float size = 6f, bool expand = false)
        {
            float width = 0;
            float height = 0;
            if (IsAutoLayoutVertical()) height = size;
            else width = size;
            GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(expand));
        }

        private static FieldInfo currentInfo;
        private static FieldInfo topLevelInfo;
        private static FieldInfo isVerticalInfo;

        /// <summary>
        /// Get the current axis mode that the Unity auto-layout is in.
        /// </summary>
        /// <returns>whether the auto-layout mode is currently vertical</returns>
        public static bool IsAutoLayoutVertical()
        {
            currentInfo = currentInfo ?? typeof(GUILayoutUtility).GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
            if (currentInfo == null) return false;
            var current = currentInfo.GetValue(null);
            topLevelInfo = topLevelInfo ?? current.GetType().GetField("topLevel", BindingFlags.Instance | BindingFlags.NonPublic);
            if (topLevelInfo == null) return false;
            var topLevel = topLevelInfo.GetValue(current);
            isVerticalInfo = isVerticalInfo ?? topLevel.GetType().GetField("isVertical", BindingFlags.Public | BindingFlags.Instance);
            if (isVerticalInfo == null) return false;
            return (bool)isVerticalInfo.GetValue(topLevel);
        }

        public static void HeaderLarge(string header, bool inline = false) => Header(header, inline, 1);

        public static void HeaderSmall(string header, bool inline = false) => Header(header, inline, -1);

        /// <summary>
        /// Draws some text in the same style as the [Header] attribute.
        /// </summary>
        /// <param name="header">the desired text to draw</param>
        /// <param name="inline">Whether or not the header should be handled as a PrefixLabel instead LabelField</param>
        /// <param name="fontSizeDelta"></param>
        public static void Header(string header, bool inline = false, int fontSizeDelta = 0)
        {
            if (!inline) Spacer(5f);
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = EditorStyles.largeLabel.fontSize + fontSizeDelta };
            if (inline) EditorGUILayout.LabelField(I18n.Tr(header, 1), style, GUILayout.Width(EditorGUIUtility.labelWidth));
            else EditorGUILayout.LabelField(I18n.Tr(header, 1), style);
        }

        /// <summary>
        /// Draws some text in the same style as the [Header] attribute.
        /// </summary>
        /// <param name="header">the desired content to draw</param>
        /// <param name="inline">Whether or not the header should be handled as a PrefixLabel instead LabelField</param>
        public static void Header(GUIContent header, bool inline = false)
        {
            if (!inline) Spacer(5f);
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = EditorStyles.largeLabel.fontSize };
            if (inline) EditorGUILayout.LabelField(header, style, GUILayout.Width(EditorGUIUtility.labelWidth));
            else EditorGUILayout.LabelField(header, style);
        }
    }
}