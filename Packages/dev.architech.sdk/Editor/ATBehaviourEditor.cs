using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

#if VUDON_LOGGER
using Varneon.VUdon.Logger.Abstract;
#endif

#pragma warning disable CS0612

namespace ArchiTech.SDK.Editor
{
    /// <summary>
    /// Base class for custom editors used across the ArchiTech namespace.
    /// Simplifies the setup and boilerplate for more complex editors.
    /// </summary>
    public abstract class ATBehaviourEditor : ATBaseEditor
    {
        private ATBehaviour _atScript;
#if VUDON_LOGGER
        private UdonLogger[] _udonLoggers;
        private string[] _udonLoggerNames;
#endif


        protected override void DrawInspector()
        {
            _atScript = (ATBehaviour)target;
            serializedObject.Update();
            LoadData();
            if (!init)
            {
                init = true;
                InitData();
                HandleSave();
            }

            Header();
            if (autoRenderHeader) DrawProgramHeader(_atScript);
            showHints = EditorGUILayout.Toggle(GetPropertyLabel(this, nameof(showHints)), showHints);
            using (ChangeCheckScope)
            {
#if VUDON_LOGGER
                // DrawVariablesByNameAsType(typeof(UdonLogger), nameof(_atScript._logger));
                if (_udonLoggers == null) _udonLoggers = ATEditorUtility.GetComponentsInSceneWithDistinctNames<UdonLogger>(out _udonLoggerNames);
                DrawVariableWithDropdown(nameof(_atScript._logger));
#endif
                VariablesDrawn(nameof(_atScript._logger));
                DrawVariablesByName(nameof(_atScript._maxLogLevel));
                DrawLine();
                RenderChangeCheck();
                if (autoRenderVariables) DrawVariables();
                HandleSave();
            }

            Footer();
        }

        protected static void DisplayTemplateError() => EditorGUILayout.HelpBox(I18n.Tr("Template components MUST be a descendant of the containing template object."), MessageType.Error);

        private static FieldInfo _serializedAssetField;

        protected void DrawProgramHeader(UdonSharpBehaviour script)
        {
            using (HArea)
            using (DisabledScope())
            {
                if (_serializedAssetField == null)
                    _serializedAssetField = typeof(UdonBehaviour).GetField("serializedProgramAsset", BindingFlags.NonPublic | BindingFlags.Instance);
                var behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(script);
                EditorGUILayout.PrefixLabel("Udon Program");
                EditorGUILayout.ObjectField(((UdonSharpProgramAsset)behaviour.programSource)?.sourceCsScript, typeof(MonoScript), false, GUILayout.ExpandWidth(true));
                EditorGUILayout.ObjectField(behaviour.programSource, typeof(AbstractUdonProgramSource), false, GUILayout.ExpandWidth(true));
            }

            UdonSharpGUI.DrawSyncSettings(target);
            UdonSharpGUI.DrawInteractSettings(target);
        }
    }
}