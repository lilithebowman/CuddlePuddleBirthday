using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.Umbrella.Editor
{
    [CustomEditor(typeof(ColliderActionProxy))]
    public class ColliderActionProxyEditor : ATBehaviourEditor
    {
        private ColliderActionProxy script;

        private void OnEnable()
        {
            script = (ColliderActionProxy)target;
        }

        protected override void RenderChangeCheck()
        {
            DrawVariablesByName(nameof(script.eventTarget));

            using (SectionScope(I18n.Tr("Collider Options")))
            {
                using (HArea)
                {
                    using (new MinimumWidthScope(0, 20))
                        DrawVariablesByName(nameof(script.eventInteract));
                    if (script.eventInteract)
                    {
                        var label = GetPropertyLabel(nameof(script.eventInteractName), false);
                        using (new MinimumWidthScope(1, ATEditorGUIUtility.GetLabelWidth(label, false))) EditorGUILayout.LabelField(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.eventInteractName));
                    }
                    else VariablesDrawn(nameof(script.eventInteractName));
                }

                using (HArea)
                {
                    using (new MinimumWidthScope(0, 20))
                        DrawVariablesByName(nameof(script.eventOnCollisionEnter));
                    if (script.eventOnCollisionEnter)
                    {
                        var label = GetPropertyLabel(nameof(script.eventOnCollisionEnterName), false);
                        using (new MinimumWidthScope(1, ATEditorGUIUtility.GetLabelWidth(label, false))) EditorGUILayout.LabelField(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.eventOnCollisionEnterName));
                    }
                    else VariablesDrawn(nameof(script.eventOnCollisionEnterName));
                }

                using (HArea)
                {
                    using (new MinimumWidthScope(0, 20))
                        DrawVariablesByName(nameof(script.eventOnCollisionExit));
                    if (script.eventOnCollisionExit)
                    {
                        var label = GetPropertyLabel(nameof(script.eventOnCollisionExitName), false);
                        using (new MinimumWidthScope(1, ATEditorGUIUtility.GetLabelWidth(label, false))) EditorGUILayout.LabelField(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.eventOnCollisionExitName));
                    }
                    else VariablesDrawn(nameof(script.eventOnCollisionExitName));
                }

                using (HArea)
                {
                    using (new MinimumWidthScope(0, 20))
                        DrawVariablesByName(nameof(script.eventOnTriggerEnter));
                    if (script.eventOnTriggerEnter)
                    {
                        var label = GetPropertyLabel(nameof(script.eventOnTriggerEnterName), false);
                        using (new MinimumWidthScope(1, ATEditorGUIUtility.GetLabelWidth(label, false))) EditorGUILayout.LabelField(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.eventOnTriggerEnterName));
                    }
                    else VariablesDrawn(nameof(script.eventOnTriggerEnterName));
                }

                using (HArea)
                {
                    using (new MinimumWidthScope(0, 20))
                        DrawVariablesByName(nameof(script.eventOnTriggerExit));
                    if (script.eventOnTriggerExit)
                    {
                        var label = GetPropertyLabel(nameof(script.eventOnTriggerExitName), false);
                        using (new MinimumWidthScope(1, ATEditorGUIUtility.GetLabelWidth(label, false))) EditorGUILayout.LabelField(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.eventOnTriggerExitName));
                    }
                    else VariablesDrawn(nameof(script.eventOnTriggerExitName));
                }

            }

            DrawVariables();
        }
    }
}