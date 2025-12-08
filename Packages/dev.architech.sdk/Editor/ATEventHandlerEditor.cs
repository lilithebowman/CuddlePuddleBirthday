using UnityEditor;
#if VUDON_LOGGER
using Varneon.VUdon.Logger.Abstract;
#endif

namespace ArchiTech.SDK.Editor
{
    public abstract class ATEventHandlerEditor : ATBehaviourEditor
    {
        private ATEventHandler _emScript;

        protected override void DrawInspector()
        {
            _emScript = (ATEventHandler)target;

            LoadData();
            if (!init)
            {
                init = true;
                InitData();
                HandleSave();
            }

            Header();
            if (autoRenderHeader) DrawProgramHeader(_emScript);
            showHints = EditorGUILayout.Toggle(GetPropertyLabel(this, nameof(showHints), showHints), showHints);
            using (ChangeCheckScope)
            {
#if VUDON_LOGGER
                DrawVariablesByNameAsType(typeof(UdonLogger), nameof(_emScript._logger));
#endif
                VariablesDrawn(nameof(_emScript._logger));
                DrawVariablesByName(nameof(_emScript._maxLogLevel));
                DrawVariablesByName(nameof(_emScript.LogLevelOverride));
                DrawLine();
                RenderChangeCheck();
                if (autoRenderVariables) DrawVariables();
                HandleSave();
            }

            Footer();
        }
    }
}