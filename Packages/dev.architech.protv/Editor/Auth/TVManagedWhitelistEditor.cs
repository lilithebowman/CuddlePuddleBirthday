using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(TVManagedWhitelist), true)]
    public class TVManagedWhitelistEditor : TVAuthPluginEditor
    {
        private TVManagedWhitelist script;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (TVManagedWhitelist)target;
            SetupCoreReferences();
        }

        protected override void RenderChangeCheck()
        {
            DrawCoreReferences();
            DrawVariablesByName(
                nameof(script.superUsers),
                nameof(script.authorizedUsers),
                nameof(script.secureWhitelist)
            );
        }
    }
}