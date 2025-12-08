using System;
using System.Collections.Generic;
using System.Linq;
using ArchiTech.SDK.Editor;
using UnityEditor;

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(TVAuthPlugin), true)]
    public class TVAuthPluginEditor : ATBehaviourEditor
    {
        private TVAuthPlugin script;
        private TVManager parentTV;

        private void OnEnable()
        {
            SetupCoreReferences();
        }

        protected override void RenderChangeCheck()
        {
            DrawCoreReferences();
        }

        private TVManager[] detectedTVs;
        private string[] detectedTVNames;

        protected void SetupCoreReferences()
        {
            script = (TVAuthPlugin)target;
            parentTV = script.GetComponentInParent<TVManager>();
            detectedTVs = ATEditorUtility.GetComponentsInSceneWithDistinctNames<TVManager>(out detectedTVNames);
        }

        /// <summary>
        /// Draws the TV property will auto-detection dropdown, optionally draws the Queue property if it also exists on the component.
        /// </summary>
        /// <param name="includeQueue">flag whether queue should also be draw if it exists, defaults to true</param>
        /// <returns>whether or not any of the variables have been modified</returns>
        protected bool DrawCoreReferences(bool includeQueue = true)
        {
            DrawCustomHeaderLarge("Core References");
            bool isChanged = false;
            using (VBox)
            {
                if (parentTV == null) isChanged |= DrawVariableWithDropdown(nameof(script.tv));
                else
                {
                    if (parentTV != script.tv) SetVariableByName(nameof(script.tv), parentTV);
                    using (DisabledScope()) DrawVariablesByName(nameof(script.tv));
                }
            }

            return isChanged;
        }
    }
}