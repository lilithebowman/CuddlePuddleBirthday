using System.Linq;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(PlaylistData))]
    public class PlaylistDataEditor : UnityEditor.Editor
    {
        private PlaylistData script;

        public void OnEnable()
        {
            script = (PlaylistData)target;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new GUIContent("PlaylistData not allowed while editing prefabs."));
                DestroyImmediate(script);
                return;
            }

            if (script.mainUrls == null) script.mainUrls = new VRCUrl[0];
            if (script.alternateUrls == null) script.alternateUrls = new VRCUrl[0];
            if (script.titles == null) script.titles = new string[0];
            if (script.tags == null) script.tags = new string[0];
            if (script.descriptions == null) script.descriptions = new string[0];
            if (script.images == null) script.images = new Sprite[0];
            if (script.entriesCount == 0) script.entriesCount = script.mainUrls.Length;
            if (script.imagesCount == 0) script.imagesCount = script.images.Count(i => i != null);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Currently storing {script.entriesCount} entries.");
        }
    }
}