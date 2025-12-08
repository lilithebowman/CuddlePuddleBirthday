using ArchiTech.SDK.Editor;
using UnityEditor;

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Queue))]
    public class QueueEditor : TVPluginEditor
    {
        protected override bool autoRenderVariables => false;

        protected override void RenderChangeCheck()
        {
            var script = (Queue)target;

            DrawTVReferences();

            using (SectionScope("General Settings"))
            {
                if (DrawVariablesByName(nameof(script.header)))
                    UpdateHeader(script);

                DrawVariablesByName(
                    nameof(script.maxQueueLength),
                    nameof(script.preventDuplicateVideos),
                    nameof(script.enableAddWhileLocked),
                    nameof(script.openEntrySelection),
                    nameof(script.showUrlsInQueue),
                    nameof(script.loop));
            }

            using (SectionScope("Per-User Settings"))
            {
                DrawVariablesByName(
                    nameof(script.maxEntriesPerPlayer),
                    nameof(script.maxBurstEntriesPerPlayer));

                if (script.maxBurstEntriesPerPlayer > 0)
                    DrawVariablesByName(nameof(script.burstThrottleTime));
            }
        }

        public static void UpdateHeader(Queue script)
        {
            if (script == null) return;
            // Find all playlistUIs that are targeting this playlist and update the header for them.
            var uis = ATEditorUtility.GetComponentsInScene<QueueUI>();
            Undo.RecordObjects(uis, "Updating header text");
            foreach (var ui in uis)
                if (ui.queue == script)
                    QueueUIEditor.UpdateHeader(ui);
        }
    }
}