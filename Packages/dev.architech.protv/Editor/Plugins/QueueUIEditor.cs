using ArchiTech.SDK.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(QueueUI))]
    public class QueueUIEditor : TVPluginUIEditor
    {
        internal const int latestTemplateVersion = 1;
        private QueueUI script;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (QueueUI)target;
        }

        protected override void RenderChangeCheck()
        {
            // DrawTVReferences();
            DrawVariableWithDropdown(nameof(script.queue));
            DrawVariablesByName(nameof(script.showCountInHeader));
            DrawCustomHeaderLarge("UI References");
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
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical();
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

                    label = GetPropertyLabel(nameof(script.ownerDisplay));
                    label.text = "└ " + label.text;
                    using (HArea)
                    {
                        EditorGUILayout.PrefixLabel(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.ownerDisplay), nameof(script.ownerDisplayTMP));
                    }

                    if (!template.IsComponentsInChildren(script.ownerDisplay, script.ownerDisplayTMP)) DisplayTemplateError();

                    label = GetPropertyLabel(nameof(script.selectAction));
                    label.text = "└ " + label.text;
                    DrawVariablesByNameWithLabel(label, nameof(script.selectAction));
                    if (!template.IsComponentsInChildren(script.selectAction)) DisplayTemplateError();

                    label = GetPropertyLabel(nameof(script.removeAction));
                    label.text = "└ " + label.text;
                    DrawVariablesByNameWithLabel(label, nameof(script.removeAction));
                    if (!template.IsComponentsInChildren(script.removeAction)) DisplayTemplateError();

                    label = GetPropertyLabel(nameof(script.persistenceAction));
                    label.text = "└ " + label.text;
                    DrawVariablesByNameWithLabel(label, nameof(script.persistenceAction));
                    if (!template.IsComponentsInChildren(script.persistenceAction)) DisplayTemplateError();

                    label = GetPropertyLabel(nameof(script.loadingBar));
                    label.text = "└ " + label.text;
                    DrawVariablesByNameWithLabel(label, nameof(script.loadingBar));
                    if (!template.IsComponentsInChildren(script.loadingBar)) DisplayTemplateError();

                    Spacer(2f);
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
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

                var toasterMsgLabel = GetPropertyLabel(nameof(script.toasterMsg), showHints);
                using (HArea)
                {
                    EditorGUILayout.PrefixLabel(toasterMsgLabel);
                    DrawVariablesByNameWithoutLabels(nameof(script.toasterMsg), nameof(script.toasterMsgTMP));
                }
            }
        }

        internal static void UpdateHeader(QueueUI ui)
        {
            var script = ui.queue;
            if (script != null && !string.IsNullOrEmpty(script.header))
            {
                if (ui.headerDisplay != null) ui.headerDisplay.text = script.header;
                if (ui.headerDisplayTMP != null) ui.headerDisplayTMP.text = script.header;
            }
        }

        internal static void AutopopulateTemplateFields(QueueUI script)
        {
            var template = script.template;
            if (template == null) return; // no template, no autofill
            // clear old template references that don't match the template
            if (!template.IsComponentsInChildren(script.urlDisplay)) script.urlDisplay = null;
            if (!template.IsComponentsInChildren(script.urlDisplayTMP)) script.urlDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.titleDisplay)) script.titleDisplay = null;
            if (!template.IsComponentsInChildren(script.titleDisplayTMP)) script.titleDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.ownerDisplay)) script.ownerDisplay = null;
            if (!template.IsComponentsInChildren(script.ownerDisplayTMP)) script.ownerDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.selectAction)) script.selectAction = null;
            if (!template.IsComponentsInChildren(script.removeAction)) script.removeAction = null;
            if (!template.IsComponentsInChildren(script.persistenceAction)) script.persistenceAction = null;
            if (!template.IsComponentsInChildren(script.loadingBar)) script.loadingBar = null;
            var texts = template.GetComponentsInChildren<Text>(true);
            var tmpTexts = template.GetComponentsInChildren<TextMeshProUGUI>(true);
            var buttons = template.GetComponentsInChildren<Button>(true);
            var toggles = template.GetComponentsInChildren<Toggle>(true);
            var sliders = template.GetComponentsInChildren<Slider>(true);

            foreach (var text in texts)
            {
                var textName = text.name.ToLower();
                if (script.urlDisplay == null && textName.Contains("url")) script.urlDisplay = text;
                if (script.titleDisplay == null && textName.Contains("title")) script.titleDisplay = text;
                if (script.ownerDisplay == null && textName.Contains("owner")) script.ownerDisplay = text;
            }

            foreach (var tmpText in tmpTexts)
            {
                var textName = tmpText.name.ToLower();
                if (script.urlDisplayTMP == null && textName.Contains("url")) script.urlDisplayTMP = tmpText;
                if (script.titleDisplayTMP == null && textName.Contains("title")) script.titleDisplayTMP = tmpText;
                if (script.ownerDisplayTMP == null && textName.Contains("owner")) script.ownerDisplayTMP = tmpText;
            }

            foreach (var button in buttons)
            {
                var btnName = button.name.ToLower();
                // catches remove, removal, removing, etc
                if (script.removeAction == null && btnName.Contains("remov")) script.removeAction = button;
                if (script.selectAction == null && (btnName.Equals("template") || btnName.Contains("select"))) script.selectAction = button;
            }

            foreach (var toggle in toggles)
            {
                var toggleName = toggle.name.ToLower();
                // catches persist, persistence, persisting, etc
                if (script.persistenceAction == null && toggleName.Contains("persist")) script.persistenceAction = toggle;
            }

            foreach (var slider in sliders)
            {
                var sliderName = slider.name.ToLower();
                // catches loading, loader, etc
                if (script.loadingBar == null && sliderName.Contains("load")) script.loadingBar = slider;
            }

            script._EDITOR_templateUpgrade = latestTemplateVersion;
        }

        internal static void UpdateTmplPaths(QueueUI script)
        {
            script.urlDisplayTmplPath = null;
            script.titleDisplayTmplPath = null;
            script.ownerDisplayTmplPath = null;
            script.urlDisplayTMPTmplPath = null;
            script.titleDisplayTMPTmplPath = null;
            script.ownerDisplayTMPTmplPath = null;
            script.selectActionTmplPath = null;
            script.loadingBarTmplPath = null;
            script.persistenceToggleTmplPath = null;
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

            if (script.ownerDisplay != null)
            {
                st = script.ownerDisplay.transform;
                script.ownerDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
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

            if (script.ownerDisplayTMP != null)
            {
                st = script.ownerDisplayTMP.transform;
                script.ownerDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.removeAction != null)
            {
                st = script.removeAction.transform;
                script.removeActionTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.selectAction != null)
            {
                st = script.selectAction.transform;
                script.selectActionTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.persistenceAction != null)
            {
                st = script.persistenceAction.transform;
                script.persistenceToggleTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.loadingBar != null)
            {
                st = script.loadingBar.transform;
                script.loadingBarTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

#pragma warning restore CS0618
        }
    }
}