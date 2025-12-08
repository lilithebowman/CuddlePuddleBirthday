using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.ProTV.Extras.Editor
{
    [CustomEditor(typeof(UIToAnimatorProxy))]
    public class UIToAnimatorProxyEditor : ATBaseEditor
    {
        private UIToAnimatorProxy script;
        private ATReorderableList animatorToggles;
        private SerializedProperty animatorProperty;
        private SerializedProperty parameterProperty;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (UIToAnimatorProxy)target;
            animatorProperty = serializedObject.FindProperty(nameof(script.animators));
            parameterProperty = serializedObject.FindProperty(nameof(script.parameters));
            animatorToggles = new ATReorderableList("Animator Parameters")
                .AddArrayProperty(animatorProperty, new GUIContent(I18n.Tr("Animator")))
                .AddArrayProperty(parameterProperty, new GUIContent(I18n.Tr("Parameter")));

        }
        
        protected override void RenderChangeCheck()
        {
            DrawVariablesByName(nameof(script._bool));
            DrawVariablesByName(nameof(script._int));
            DrawVariablesByName(nameof(script._float));
            Spacer(5f);
            animatorToggles.DrawLayout(showHints);
        }
    }
}