using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TVManagerData), true)]
    public class TVManagerDataEditor : ATBehaviourEditor
    {
        private void OnEnable()
        {
            var script = (TVManagerData)target;
            var tv = ProTVEditorUtility.FindParentTVManager(script, false);
            if (tv == null)
            {
                EditorUtility.DisplayDialog(
                    "Ancestor Component Required.",
                    "Unable to find the required TVManager in the parent objects. This component will not be added.",
                    "Ok");
                UdonSharpEditor.UdonSharpEditorUtility.DestroyImmediate(script);
                return;
            }

            var data = tv.GetComponentInChildren<TVManagerData>(true);
            if (data != null && data != script)
            {
                EditorUtility.DisplayDialog(
                    "Duplicate Component Found.",
                    "Duplicate instances of this component are disallowed within the same TVManager. This duplicate instance will not be added.",
                    "Ok");
                UdonSharpEditor.UdonSharpEditorUtility.DestroyImmediate(script);
            }
        }

        protected override void RenderChangeCheck() { }
    }
}