using UnityEditor;

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(TVToggles))]
    public class TVTogglesEditor : TVPluginEditor
    {
        protected override void RenderChangeCheck()
        {
            base.RenderChangeCheck();

            using (SectionScope("Enabled only for Super Users"))
                DrawVariablesByName(nameof(TVToggles.superGameObjects), nameof(TVToggles.superColliders));

            using (SectionScope("Enabled for any Authorized Users"))
                DrawVariablesByName(nameof(TVToggles.authorizedGameObjects), nameof(TVToggles.authorizedColliders));

            using (SectionScope("Enabled only for Unauthorized Users"))
                DrawVariablesByName(nameof(TVToggles.unauthorizedGameObjects), nameof(TVToggles.unauthorizedColliders));
        }
    }
}