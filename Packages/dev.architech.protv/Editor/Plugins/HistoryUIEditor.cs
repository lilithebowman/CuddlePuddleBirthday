using System;
using ArchiTech.SDK.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HistoryUI))]
    public class HistoryUIEditor : TVPluginUIEditor
    {
        internal const int latestTemplateVersion = 1;
        private HistoryUI script;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (HistoryUI)target;
        }

        protected override void RenderChangeCheck()
        {
            DrawVariableWithDropdown(nameof(script.history));
            DrawCustomHeaderLarge("UI References");
            using (VBox) drawUIComponents();
        }


        private void drawUIComponents()
        {
            using (VBox)
            {
                EditorGUI.BeginDisabledGroup(targets.Length > 1);
                DrawVariablesByName(nameof(script.listContainer));


                GUIContent label = GetPropertyLabel(nameof(script.headerDisplay), showHints);
                using (HArea)
                {
                    EditorGUILayout.PrefixLabel(label);
                    DrawVariablesByNameWithoutLabels(nameof(script.headerDisplay), nameof(script.headerDisplayTMP));
                }

                bool templateChanged = DrawVariablesByName(nameof(script.template)) || script._EDITOR_templateUpgrade < latestTemplateVersion;


                if (script.template != null)
                {
                    EditorGUI.BeginChangeCheck();
                    if (templateChanged) GUI.changed = true;
                    var template = script.template;
                    EditorGUILayout.BeginHorizontal();
                    Spacer(15f);
                    using (VArea)
                    {
                        label = GetPropertyLabel(nameof(script.urlDisplay));
                        label.text = "└ " + label.text;
                        using (HArea)
                        {
                            EditorGUILayout.PrefixLabel(label);
                            DrawVariablesByNameWithoutLabels(nameof(script.urlDisplay), nameof(script.urlDisplayTMP));
                        }

                        if (!template.IsComponentsInChildren(script.urlDisplay, script.urlDisplayTMP)) DisplayTemplateError();

                        label = GetPropertyLabel(nameof(script.titleDisplay));
                        label.text = "└ " + label.text;
                        using (HArea)
                        {
                            EditorGUILayout.PrefixLabel(label);
                            DrawVariablesByNameWithoutLabels(nameof(script.titleDisplay), nameof(script.titleDisplayTMP));
                        }

                        if (!template.IsComponentsInChildren(script.titleDisplay, script.titleDisplayTMP)) DisplayTemplateError();

                        label = GetPropertyLabel(nameof(script.addedByDisplay));
                        label.text = "└ " + label.text;
                        using (HArea)
                        {
                            EditorGUILayout.PrefixLabel(label);
                            DrawVariablesByNameWithoutLabels(nameof(script.addedByDisplay), nameof(script.addedByDisplayTMP));
                        }

                        if (!template.IsComponentsInChildren(script.addedByDisplay, script.addedByDisplayTMP)) DisplayTemplateError();

                        label = GetPropertyLabel(nameof(script.restoreAction));
                        label.text = "└ " + label.text;
                        DrawVariablesByNameWithLabel(label, nameof(script.restoreAction));
                        if (!template.IsComponentsInChildren(script.restoreAction)) DisplayTemplateError();

                        label = GetPropertyLabel(nameof(script.copyAction));
                        label.text = "└ " + label.text;
                        DrawVariablesByNameWithLabel(label, nameof(script.copyAction));
                        if (!template.IsComponentsInChildren(script.copyAction)) DisplayTemplateError();

                        Spacer(2f);
                    }

                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        var msg = templateChanged ? "Auto-populating the template child references" : "Update template reference paths";
                        using (new SaveObjectScope(script, msg))
                        {
                            if (templateChanged) AutopopulateTemplateFields(script);
                            UpdateTmplPaths(script);
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        internal static void UpdateHeader(HistoryUI ui)
        {
            var script = ui.history;
            if (script != null && !string.IsNullOrEmpty(script.header))
            {
                if (ui.headerDisplay != null) ui.headerDisplay.text = script.header;
                if (ui.headerDisplayTMP != null) ui.headerDisplayTMP.text = script.header;
            }
        }

        internal static void AutopopulateTemplateFields(HistoryUI script)
        {
            var template = script.template;
            if (template == null) return; // no template, no autofill
            // clear old template references that don't match the template
            if (!template.IsComponentsInChildren(script.urlDisplay)) script.urlDisplay = null;
            if (!template.IsComponentsInChildren(script.urlDisplayTMP)) script.urlDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.titleDisplay)) script.titleDisplay = null;
            if (!template.IsComponentsInChildren(script.titleDisplayTMP)) script.titleDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.addedByDisplay)) script.addedByDisplay = null;
            if (!template.IsComponentsInChildren(script.addedByDisplayTMP)) script.addedByDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.restoreAction)) script.restoreAction = null;
            if (!template.IsComponentsInChildren(script.copyAction)) script.copyAction = null;
            var texts = template.GetComponentsInChildren<Text>(true);
            var tmpTexts = template.GetComponentsInChildren<TextMeshProUGUI>(true);
            var buttons = template.GetComponentsInChildren<Button>(true);
            var inputs = template.GetComponentsInChildren<InputField>(true);

            foreach (var text in texts)
            {
                var textName = text.name.ToLower();
                if (script.urlDisplay == null && textName.Contains("url")) script.urlDisplay = text;
                if (script.titleDisplay == null && textName.Contains("title")) script.titleDisplay = text;
                if (script.addedByDisplay == null && (textName.Contains("owner") || textName.Contains("add"))) script.addedByDisplay = text;
            }

            foreach (var tmpText in tmpTexts)
            {
                var textName = tmpText.name.ToLower();
                if (script.urlDisplayTMP == null && textName.Contains("url")) script.urlDisplayTMP = tmpText;
                if (script.titleDisplayTMP == null && textName.Contains("title")) script.titleDisplayTMP = tmpText;
                if (script.addedByDisplayTMP == null && (textName.Contains("owner") || textName.Contains("add"))) script.addedByDisplayTMP = tmpText;
            }

            foreach (var button in buttons)
            {
                var btnName = button.name.ToLower();
                // catches remove, removal, removing, etc
                if (script.restoreAction == null && (btnName.Equals("template") || btnName.Contains("restore"))) script.restoreAction = button;
            }

            foreach (var input in inputs)
            {
                var inputName = input.name.ToLower();
                if (script.copyAction == null && inputName.Contains("copy")) script.copyAction = input;
            }

            script._EDITOR_templateUpgrade = latestTemplateVersion;
        }

        internal static void UpdateTmplPaths(HistoryUI script)
        {
            script.urlDisplayTmplPath = null;
            script.titleDisplayTmplPath = null;
            script.addedByDisplayTmplPath = null;
            script.urlDisplayTMPTmplPath = null;
            script.titleDisplayTMPTmplPath = null;
            script.addedByDisplayTMPTmplPath = null;
            script.restoreActionTmplPath = null;
            script.copyActionTmplPath = null;
            Transform t = script.template.transform;
            Transform st;
#pragma warning disable CS0618
            if (script.urlDisplay != null)
            {
                st = script.urlDisplay.transform;
                script.urlDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.titleDisplay != null)
            {
                st = script.titleDisplay.transform;
                script.titleDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.addedByDisplay != null)
            {
                st = script.addedByDisplay.transform;
                script.addedByDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.urlDisplayTMP != null)
            {
                st = script.urlDisplayTMP.transform;
                script.urlDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.titleDisplayTMP != null)
            {
                st = script.titleDisplayTMP.transform;
                script.titleDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.addedByDisplayTMP != null)
            {
                st = script.addedByDisplayTMP.transform;
                script.addedByDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.restoreAction != null)
            {
                st = script.restoreAction.transform;
                script.restoreActionTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.copyAction != null)
            {
                st = script.copyAction.transform;
                script.copyActionTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

#pragma warning restore CS0618
        }
    }
}