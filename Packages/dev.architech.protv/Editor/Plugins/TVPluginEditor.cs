using System;
using System.Collections.Generic;
using System.Linq;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(TVPlugin), true)]
    public class TVPluginEditor : ATEventHandlerEditor
    {
        private TVPlugin script;
        private Texture linkIcon;
        private Texture checkmarkIcon;

        protected void SetupTVReferences()
        {
            script = (TVPlugin)target;
            checkmarkIcon = AssetDatabase.LoadAssetAtPath<Texture>(ProTVEditorUtility.checkmarkIconPath);
            linkIcon = AssetDatabase.LoadAssetAtPath<Texture>(ProTVEditorUtility.linkIconPath);
        }

        /// <summary>
        /// Draws the TV property will auto-detection dropdown, optionally draws the Queue property if it also exists on the component.
        /// </summary>
        /// <param name="includePlaylist">flag whether playlist should also be drawn if it exists, defaults to true</param>
        /// <param name="includeQueue">flag whether queue should also be drawn if it exists, defaults to true</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawTVReferences(bool includePlaylist = true, bool includeQueue = true)
        {
            if (script == null) SetupTVReferences();
            List<string> fields = new List<string> { "tv" };
            if (includePlaylist) fields.Add("playlist");
            if (includeQueue) fields.Add("queue");
            return DrawSectionWithDropdowns("TV References", fields.ToArray());
        }

        protected bool DrawTVDropdown() => DrawVariableWithDropdown("tv");
        protected bool DrawPlaylistDropdown() => DrawVariableWithDropdown("playlist");
        protected bool DrawQueueDropdown() => DrawVariableWithDropdown("queue");

        protected void DrawToggleIconsControls(string title, string actionName, string indicatorName, string firstIconName, string secondIconName, string firstIconColorName, string secondIconColorName)
        {
            DrawCustomHeaderSmall(I18n.Tr(title, 1));
            DrawVariablesByName(actionName);
            var indicator = (UnityEngine.UI.Image)GetVariableByName(indicatorName);
            bool wasNull = indicator == null;
            if (DrawVariablesByName(indicatorName))
            {
                if (wasNull && indicator != null)
                {
                    SetVariableByName(firstIconColorName, indicator.color);
                    SetVariableByName(secondIconColorName, indicator.color);
                }
            }

            if (indicator != null)
            {
                using (HArea)
                {
                    EditorGUILayout.PrefixLabel(I18n.Tr("Icons"));
                    using (VArea)
                    {
                        DrawVariablesByNameAsSprites(firstIconName);
                        if (DrawVariablesByNameWithoutLabels(new[] { firstIconColorName }, GUILayout.Width(75f)))
                        {
                            using (new SaveObjectScope(indicator))
                                indicator.color = (Color)GetVariableByName(firstIconColorName);
                        }
                    }

                    using (VArea)
                    {
                        DrawVariablesByNameAsSprites(secondIconName);
                        DrawVariablesByNameWithoutLabels(new[] { secondIconColorName }, GUILayout.Width(75f));
                    }
                }
            }
        }


        #region Detected Plugins

        private readonly Dictionary<(System.Type, Component), Component[]> detectedComponents
            = new Dictionary<(Type, Component), Component[]>();

        private void cacheDetectedComponents<T>(System.Type searchIn, T lookFor) where T : Component
        {
            List<Component> found = new List<Component>();
            var components = ATEditorUtility.GetComponentsInScene(searchIn);
            foreach (Component component in components)
            {
                var serialized = new SerializedObject(component);
                var prop = serialized.GetIterator();
                prop.Next(true);
                do
                {
                    if (string.IsNullOrEmpty(prop.propertyPath) || prop.GetValueType() != lookFor.GetType()) continue;
                    found.Add(component);
                } while (prop.Next(false));
            }

            detectedComponents[(searchIn, lookFor)] = found.ToArray();
        }

        private Component[] findMatchingComponents<T>(Component[] components, string searchField, bool negativeSearch, params T[] lookFor) where T : Component
        {
            List<Component> found = new List<Component>();
            if (lookFor.Length == 0) return found.ToArray(); // nothing can be checked for.
            foreach (var component in components)
            {
                var obj = new SerializedObject(component);
                var prop = obj.FindProperty(searchField);
                if (!prop.GetValueType().IsInstanceOfType(lookFor[0])) continue;
                var index = System.Array.IndexOf(lookFor, prop.GetValue());
                if (negativeSearch && index == -1 || !negativeSearch && index > -1) found.Add(component);
            }

            return found.ToArray();
        }

        private Component[] nonOtherComponents = new Component[0];
        private SerializedObject[] nonOtherComponentsSerialized = new SerializedObject[0];
        private Component[] otherComponents = new Component[0];
        private SerializedObject[] otherComponentsSerialized = new SerializedObject[0];
        private bool recacheComponents = true;

        private void OnUndoRedo() => recacheComponents = true;

        protected void DrawRelatedComponents<T>(string headerText, System.Type searchIn, string searchField, T lookFor) where T : Component
        {
            if (EditorApplication.isPlaying) return;
            bool recache = recacheComponents;
            recacheComponents = false;
            if (recache)
            {
                Undo.undoRedoPerformed -= OnUndoRedo;
                Undo.undoRedoPerformed += OnUndoRedo;
            }

            EditorGUILayout.Space(5f);
            DrawCustomHeader(headerText);

            if (!detectedComponents.ContainsKey((searchIn, lookFor))) cacheDetectedComponents(searchIn, lookFor);
            var components = detectedComponents[(searchIn, lookFor)];
            var checkmark = new GUIContent(checkmarkIcon);
            var link = new GUIContent(linkIcon);

            using (VBox)
            {
                Checkpoint("Connected Self");
                if (recache)
                {
                    nonOtherComponents = findMatchingComponents(components, searchField, false, lookFor, null);
                    nonOtherComponentsSerialized = nonOtherComponents.Select(c => new SerializedObject(c)).ToArray();
                }

                EditorGUI.indentLevel++;
                for (var index = 0; index < nonOtherComponents.Length; index++)
                {
                    var component = nonOtherComponents[index];
                    var serialized = nonOtherComponentsSerialized[index];
                    var field = serialized.FindProperty(searchField);
                    var connected = UnityEngine.Object.Equals(field.GetValue(), script);
                    using (HArea)
                    {
                        using (DisabledScope()) EditorGUILayout.ObjectField(component, typeof(TVPlugin), true);
                        using (DisabledScope(connected))
                            if (GUILayout.Button(connected ? checkmark : link, GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                            {
                                field.SetValue(script);
                                recacheComponents = true;
                            }
                    }
                }

                Checkpoint("Connected Other");
                if (recache)
                {
                    otherComponents = findMatchingComponents(components, searchField, true, script, null);
                    otherComponentsSerialized = otherComponents.Select(c => new SerializedObject(c)).ToArray();
                }
                if (otherComponents.Length > 0 && DrawCustomFoldout(nameof(script.tv), new GUIContent(I18n.Tr("Connected to Others"))))
                {
                    for (var index = 0; index < otherComponentsSerialized.Length; index++)
                    {
                        var component = otherComponents[index];
                        var serialized = otherComponentsSerialized[index];
                        var prop = serialized.FindProperty(searchField);
                        using (HArea)
                        {
                            using (DisabledScope())
                            {
                                EditorGUILayout.ObjectField(component, searchIn, true);
                                EditorGUILayout.ObjectField((T)prop.GetValue(), lookFor.GetType(), true, GUILayout.MaxWidth(150));
                            }

                            if (GUILayout.Button(link, GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                            {
                                prop.SetValue(script);
                                recacheComponents = true;
                            }
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        #endregion

        private void OnEnable()
        {
            SetupTVReferences();
        }

        protected override void RenderChangeCheck()
        {
            DrawTVReferences();
        }
    }
}