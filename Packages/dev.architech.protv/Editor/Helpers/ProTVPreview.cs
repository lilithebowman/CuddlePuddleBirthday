using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ArchiTech.ProTV.Editor
{
    // [CustomPreview(typeof(TVManager))]
    public class ProTVPreview : ObjectPreview
    {

        public override bool HasPreviewGUI()
        {
            // only preview for prefab assets. Once in scene it doesn't matter cause
            // the user will already see what it looks like.
            // var script = (TVManager)target;
            // var isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(script);
            
            // return isPrefabAsset;
            return false;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            GUI.Label(r, target.name + " is being previewed");
            //todo add details about what this TV has
        }
    }
}