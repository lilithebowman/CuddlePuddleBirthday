using System.Reflection;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    public abstract class ATEditorWindow : UnityEditor.EditorWindow
    {
        

        protected class MinimumWidthScope : GUI.Scope
        {
            private readonly float oldLabelWidth;
            private readonly float oldFieldWidth;

            /// <summary>
            /// Create a disposable scope that manages the label and field minimum widths via EditorGUIUtility.
            /// </summary>
            /// <param name="labelWidth">minimum width of the property's label. Set to 0 to use unity's default value.</param>
            /// <param name="fieldWidth">minimum width of the property's field. Set to 0 to use Unity's default value.</param>
            public MinimumWidthScope(float labelWidth, float fieldWidth)
            {
                oldLabelWidth = EditorGUIUtility.labelWidth;
                oldFieldWidth = EditorGUIUtility.fieldWidth;
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUIUtility.fieldWidth = fieldWidth;
            }

            protected override void CloseScope()
            {
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUIUtility.fieldWidth = oldFieldWidth;
            }
        }
        

        protected class SaveObjectScope : GUI.Scope
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
                if (undoMessage == null && target is UdonSharpBehaviour bhvr)
                    undoMessage = $"Modify {bhvr.GetUdonTypeName()} Content";
                Undo.RecordObject(target, undoMessage);
            }

            protected override void CloseScope()
            {
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }
        
        /// <summary>
        /// Shorthand property for a new VerticalScope. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.VerticalScope VArea => new EditorGUILayout.VerticalScope(GUIStyle.none);

        /// <summary>
        /// Shorthand property for a new VerticalScope, but with a generic "box" style applied. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.VerticalScope VBox => new EditorGUILayout.VerticalScope("box");

        /// <summary>
        /// Shorthand property for a new HorizontalScope. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.HorizontalScope HArea => new EditorGUILayout.HorizontalScope(GUIStyle.none);

        /// <summary>
        /// Shorthand property for a new HorizontalScope, but with a generic "box" style applied. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.HorizontalScope HBox => new EditorGUILayout.HorizontalScope("box");

        protected static EditorGUI.DisabledGroupScope DisabledScope(bool isDisabled = true)
            => new EditorGUI.DisabledGroupScope(isDisabled);

        /// <summary>
        /// An alternative to EditorGUILayout.Space which will pick the rect dimensions
        /// based on the most recent layout group's direction (either vertical or horizontal)
        /// </summary>
        /// <param name="size">the size of the spacer that you want to render</param>
        /// <param name="expand">whether or not to allow the spacer to expand beyond the size value</param>
        protected static void Spacer(float size = 6f, bool expand = false)
        {
            float width = 0;
            float height = 0;

            var currentInfo = typeof(GUILayoutUtility).GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
            if (currentInfo == null) return;
            var current = currentInfo.GetValue(null);
            var topLevelInfo = current.GetType().GetField("topLevel", BindingFlags.Instance | BindingFlags.NonPublic);
            if (topLevelInfo == null) return;
            var topLevel = topLevelInfo.GetValue(current);
            var isVerticalInfo = topLevel.GetType().GetField("isVertical", BindingFlags.Public | BindingFlags.Instance);
            if (isVerticalInfo == null) return;
            bool isVertical = (bool)isVerticalInfo.GetValue(topLevel);

            if (isVertical) height = size;
            else width = size;
            GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(expand));
        }
    }
}