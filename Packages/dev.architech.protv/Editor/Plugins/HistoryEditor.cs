using System.Linq;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(History))]
    public class HistoryEditor : TVPluginEditor
    {
        private History script;
        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (History)target;
            SetupTVReferences();
        }

        protected override void RenderChangeCheck()
        {
            DrawTVReferences();
            DrawCustomHeaderLarge("General Settings");
            using (VBox)
            {
                if (DrawVariablesByName(nameof(script.header)))
                    UpdateHeader(script);

                DrawVariablesByName(
                    nameof(script.numberOfEntries),
                    nameof(script.enableUrlCopy)
                );

                if (script.enableUrlCopy) DrawVariablesByName(nameof(script.protectUrlCopy));

                DrawVariablesByName(nameof(script.emptyTitlePlaceholder));
            }

            DrawRelatedComponents(I18n.Tr("Detected UIs"), typeof(HistoryUI), "history", script);
        }

        public static void UpdateHeader(History script)
        {
            if (script == null) return;
            // Find all playlistUIs that are targeting this playlist and update the header for them.
            var uis = ATEditorUtility.GetComponentsInScene<HistoryUI>();
            Undo.RecordObjects(uis, "Updating header text");
            foreach (var ui in uis)
                if (ui.history == script)
                    HistoryUIEditor.UpdateHeader(ui);
        }
    }
}