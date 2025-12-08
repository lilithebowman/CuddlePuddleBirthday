using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.ProTV.Editor
{
    public abstract class TVPluginUIEditor : ATBehaviourEditor
    {
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
                                indicator.color = (UnityEngine.Color)GetVariableByName(firstIconColorName);
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
    }
}