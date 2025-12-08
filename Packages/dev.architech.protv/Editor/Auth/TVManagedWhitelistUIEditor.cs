using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(TVManagedWhitelistUI), true)]
    public class TVManagedWhitelistUIEditor : TVPluginUIEditor
    {
        internal const int latestTemplateVersion = 1;
        private TVManagedWhitelistUI script;
        private TVManagedWhitelist[] detectedWhitelists;
        private string[] detecteWhitelistNames;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (TVManagedWhitelistUI)target;
            detectedWhitelists = ATEditorUtility.GetComponentsInSceneWithDistinctNames<TVManagedWhitelist>(out detecteWhitelistNames);
        }

        protected override void RenderChangeCheck()
        {
            DrawVariableWithDropdown(nameof(script.whitelist));
            DrawCustomHeaderLarge("UI References");
            using (VBox)
            {
                DrawVariablesByName(nameof(script.listContainer));
                if (script.listContainer != null)
                {
                    bool templateChanged = DrawVariablesByName(nameof(script.template)) || script._EDITOR_templateUpgrade < latestTemplateVersion;
                    if (script.template != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        if (templateChanged) GUI.changed = true;
                        var template = script.template;
                        EditorGUI.indentLevel++;
                        var label = GetPropertyLabel(nameof(script.nameDisplay));
                        label.text = "└ " + label.text;
                        using (HArea)
                        {
                            EditorGUILayout.PrefixLabel(label);
                            EditorGUI.indentLevel--;
                            DrawVariablesByNameWithoutLabels(nameof(script.nameDisplay), nameof(script.nameDisplayTMP));
                            EditorGUI.indentLevel++;
                        }

                        if (!template.IsComponentsInChildren(script.nameDisplay, script.nameDisplayTMP)) DisplayTemplateError();

                        label = GetPropertyLabel(nameof(script.authAction));
                        label.text = "└ " + label.text;
                        DrawVariablesByNameWithLabel(label, nameof(script.authAction));
                        if (!template.IsComponentsInChildren(script.authAction)) DisplayTemplateError();

                        label = GetPropertyLabel(nameof(script.hereIndicator));
                        label.text = "└ " + label.text;
                        DrawVariablesByNameWithLabel(label, nameof(script.hereIndicator));
                        if (!template.IsComponentsInChildren(script.hereIndicator)) DisplayTemplateError();

                        Spacer(2f);
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
                }
            }
        }

        internal static void AutopopulateTemplateFields(TVManagedWhitelistUI script)
        {
            var template = script.template;
            if (template == null) return; // no template, no autofill
            // clear old template references that don't match the template
            if (!template.IsComponentsInChildren(script.nameDisplay)) script.nameDisplay = null;
            if (!template.IsComponentsInChildren(script.nameDisplayTMP)) script.nameDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.authAction)) script.authAction = null;
            if (!template.IsComponentsInChildren(script.hereIndicator)) script.hereIndicator = null;
            var texts = template.GetComponentsInChildren<Text>(true);
            var tmpTexts = template.GetComponentsInChildren<TextMeshProUGUI>(true);
            var toggles = template.GetComponentsInChildren<Toggle>(true);

            foreach (var text in texts)
            {
                var textName = text.name.ToLower();
                if (script.nameDisplay == null && textName.Contains("name")) script.nameDisplay = text;
            }

            foreach (var tmpText in tmpTexts)
            {
                var textName = tmpText.name.ToLower();
                if (script.nameDisplayTMP == null && textName.Contains("name")) script.nameDisplayTMP = tmpText;
            }

            foreach (var toggle in toggles)
            {
                var toggleName = toggle.name.ToLower();
                // catches persist, persistence, persisting, etc
                UnityEngine.Debug.Log($"toggle name {toggleName}");
                if (script.authAction == null && toggleName.Contains("auth")) script.authAction = toggle;
                if (script.hereIndicator == null && toggleName.Contains("here")) script.hereIndicator = toggle;
            }

            script._EDITOR_templateUpgrade = latestTemplateVersion;
        }

        internal static void UpdateTmplPaths(TVManagedWhitelistUI script)
        {
            script.authActionTmplPath = null;
            script.hereIndicatorTmplPath = null;
            script.nameDisplayTmplPath = null;
            script.nameDisplayTMPTmplPath = null;
            Transform t = script.template.transform;
            Transform st;
#pragma warning disable CS0618
            if (script.nameDisplay != null)
            {
                st = script.nameDisplay.transform;
                script.nameDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.nameDisplayTMP != null)
            {
                st = script.nameDisplayTMP.transform;
                script.nameDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.authAction != null)
            {
                st = script.authAction.transform;
                script.authActionTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.hereIndicator != null)
            {
                st = script.hereIndicator.transform;
                script.hereIndicatorTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

#pragma warning restore CS0618
        }
    }
}