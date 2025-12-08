using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(VPManager), true)]
    public class VPManagerEditor : ATBehaviourEditor
    {
        private static FieldInfo targetAudioSourcesInfo = typeof(VRCUnityVideoPlayer).GetField("targetAudioSources", BindingFlags.Instance | BindingFlags.NonPublic);
        private VPManager script;
        private BaseVRCVideoPlayer _videoPlayer;
        [InspectorName("Detected Speakers")] private AudioSource[] _availableSpeakers = new AudioSource[0];
        [InspectorName("Detected Screens")] private GameObject[] _availableScreens = new GameObject[0];
        private string[] _availableSpeakerNames = new string[0];
        private bool[] _availableSpatialStates = new bool[0];
        private bool[] _availableStereoStates = new bool[0];
        private bool[] _availableScreenStates = new bool[0];
        private bool _updateScreens = false;

        private ATReorderableList _spatialSpeakerSettings;
        private SerializedProperty _spatialSpeakers;
        private SerializedProperty _managedSpatialVolume;
        private SerializedProperty _managedSpatialMute;

        private ATReorderableList _stereoSpeakerSettings;
        private SerializedProperty _stereoSpeakers;
        private SerializedProperty _managedStereoVolume;
        private SerializedProperty _managedStereoMute;

        protected override bool autoRenderVariables => false;

        public void OnEnable()
        {
            script = (VPManager)target;

            _spatialSpeakers = serializedObject.FindProperty(nameof(script.spatialSpeakers));
            _managedSpatialVolume = serializedObject.FindProperty(nameof(script.managedSpatialVolume));
            _managedSpatialMute = serializedObject.FindProperty(nameof(script.managedSpatialMute));
            _spatialSpeakerSettings = new ATReorderableList("Managed Spatial (3D) Speakers") { onDropObject = handleSpeakerDrop }
                .AddArrayProperty(_spatialSpeakers, GUIContent.none)
                .AddArrayProperty(_managedSpatialVolume, true)
                .AddArrayProperty(_managedSpatialMute, true);
            _spatialSpeakerSettings.Resize();
            _spatialSpeakerSettings.HideProperties(1, 2);

            _stereoSpeakers = serializedObject.FindProperty(nameof(script.stereoSpeakers));
            _managedStereoVolume = serializedObject.FindProperty(nameof(script.managedStereoVolume));
            _managedStereoMute = serializedObject.FindProperty(nameof(script.managedStereoMute));
            _stereoSpeakerSettings = new ATReorderableList("Managed Stereo (2D) Speakers") { onDropObject = handleSpeakerDrop }
                .AddArrayProperty(_stereoSpeakers, GUIContent.none)
                .AddArrayProperty(_managedStereoVolume, true)
                .AddArrayProperty(_managedStereoMute, true);
            _stereoSpeakerSettings.Resize();
            _stereoSpeakerSettings.HideProperties(1, 2);

            _videoPlayer = script.GetComponent<BaseVRCVideoPlayer>();
            if (_videoPlayer is VRCAVProVideoPlayer)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                GameObject[] roots = stage == null ? SceneManager.GetActiveScene().GetRootGameObjects() : new[] { stage.prefabContentsRoot };
                List<VRCAVProVideoSpeaker> avProSpeakers = new List<VRCAVProVideoSpeaker>();
                List<VRCAVProVideoScreen> avProScreens = new List<VRCAVProVideoScreen>();
                List<Renderer> renderers = new List<Renderer>();
                foreach (GameObject root in roots)
                {
                    avProSpeakers.AddRange(root.GetComponentsInChildren<VRCAVProVideoSpeaker>(true));
                    var rend = root.GetComponentsInChildren<Renderer>(true);
                    renderers.AddRange(rend);
                    avProScreens.AddRange(rend.Select(r => r.GetComponent<VRCAVProVideoScreen>()).Where(s => s != null && s.VideoPlayer == _videoPlayer));
                }

                _availableSpeakers = avProSpeakers.Where(s => s.VideoPlayer == _videoPlayer).Select(s => s.GetComponent<AudioSource>()).ToArray();

                var finalScreens = new List<GameObject>();
                foreach (var screen in avProScreens)
                {
                    // Find all renderers with the same material as the screen's matIndex target but that do not have another video screen to the same game object.
                    var mat = screen.GetComponent<Renderer>().sharedMaterials[screen.MaterialIndex];
                    finalScreens.AddRange(renderers
                        .Where(r => r.GetComponent<VRCAVProVideoScreen>() == null && r.GetComponent<BaseVRCVideoPlayer>() == null && r.sharedMaterials.Contains(mat))
                        .Select(r => r.gameObject)
                    );
                }

                // remove accidental duplicates
                _availableScreens = finalScreens.Distinct().ToArray();
            }
            else if (_videoPlayer is VRCUnityVideoPlayer)
            {
                // use reflection to extract the reference to the audio source array because it's normally private
                _availableSpeakers = (AudioSource[])targetAudioSourcesInfo?.GetValue(_videoPlayer) ?? new AudioSource[0];
                var sources = _availableSpeakers.Where(s => s != null).ToArray();
                if (script.spatialSpeakers != null && sources.Length == 0)
                {
                    sources = script.spatialSpeakers.Length > 0 && script.spatialSpeakers[0] != null ? new[] { script.spatialSpeakers[0] } : new AudioSource[0];
                    using (new SaveObjectScope(_videoPlayer))
                        targetAudioSourcesInfo?.SetValue(_videoPlayer, sources);
                }

                _availableSpeakers = sources;

                var m = typeof(VRCUnityVideoPlayer).GetField("renderMode", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_videoPlayer);
                if (m != null && (int)m == 1)
                {
                    // material override aka Renderer
                    var screen = (Renderer)typeof(VRCUnityVideoPlayer).GetField("targetMaterialRenderer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_videoPlayer);
                    if (screen != null && screen.gameObject != _videoPlayer.gameObject) _availableScreens = new[] { screen.gameObject };
                }
            }
            else
            {
                _availableSpeakers = new AudioSource[0];
                Debug.LogError($"VideoPlayer type unknown: {_videoPlayer.GetType().FullName}");
            }

            if (script.spatialSpeakers == null)
            {
                using (new SaveObjectScope(script))
                    script.spatialSpeakers = _availableSpeakers;
                serializedObject.UpdateIfRequiredOrScript();
            }

            if (script.stereoSpeakers == null)
            {
                using (new SaveObjectScope(script))
                    script.stereoSpeakers = new AudioSource[0];
                serializedObject.UpdateIfRequiredOrScript();
            }

            if (script.screens == null)
            {
                using (new SaveObjectScope(script))
                    script.screens = _availableScreens;
                serializedObject.UpdateIfRequiredOrScript();
            }

            if (script.screens.Any(s => s == null))
            {
                using (new SaveObjectScope(script))
                    script.screens = script.screens.Where(go => go != null).ToArray();
                serializedObject.UpdateIfRequiredOrScript();
            }

            _availableSpeakerNames = _availableSpeakers.Select(s => s.gameObject.name).ToArray();
            _availableSpatialStates = _availableSpeakers.Select(s => Array.IndexOf(script.spatialSpeakers, s) > -1).ToArray();
            _availableStereoStates = _availableSpeakers.Select(s => Array.IndexOf(script.stereoSpeakers, s) > -1).ToArray();
            _availableScreenStates = _availableScreens.Select(s => Array.IndexOf(script.screens, s) > -1).ToArray();
        }

        private bool handleSpeakerDrop(ATReorderableList list, UnityEngine.Object dropped, ATPropertyListData propListData)
        {
            int index = list.Size;
            list.AppendNewEntry();
            list.MainProperty.GetArrayElementAtIndex(index).SetValue(dropped);
            return true;
        }

        protected override void RenderChangeCheck()
        {
            DrawVariablesByName(nameof(VPManager.customLabel));
            ATEditorGUILayout.Spacer();

            DrawCustomHeader("Speaker Management");
            using (VBox)
            {
                using (HArea)
                {
                    // names
                    using (VArea)
                    {
                        EditorGUILayout.PrefixLabel(GetPropertyLabel(this, nameof(_availableSpeakers)));
                        for (int i = 0; i < _availableSpeakers.Length; i++)
                            EditorGUILayout.LabelField(_availableSpeakerNames[i]);
                    }

                    // spatial
                    using (VArea)
                    {
                        var label = GetPropertyLabel(_spatialSpeakers, false);
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label, true, _spatialSpeakers))
                            EditorGUILayout.LabelField(label);

                        for (int i = 0; i < _availableSpeakers.Length; i++)
                        {
                            var speaker = _availableSpeakers[i];
                            _availableSpatialStates[i] = _spatialSpeakers.Contains(speaker);
                            var newState = EditorGUILayout.ToggleLeft(GUIContent.none, _availableSpatialStates[i], GUILayout.Width(25));
                            if (newState != _availableSpatialStates[i])
                            {
                                _availableSpatialStates[i] = newState;
                                if (newState) _spatialSpeakerSettings.AppendNewEntry(speaker);
                                else _spatialSpeakerSettings.RemoveEntryByValue(speaker);
                            }
                        }
                    }

                    // stereo
                    using (VArea)
                    {
                        var label = GetPropertyLabel(_stereoSpeakers, false);
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label, true, _stereoSpeakers))
                            EditorGUILayout.LabelField(label);


                        for (int i = 0; i < _availableSpeakers.Length; i++)
                        {
                            var speaker = _availableSpeakers[i];
                            _availableStereoStates[i] = _stereoSpeakers.Contains(speaker);
                            var newState = EditorGUILayout.ToggleLeft(GUIContent.none, _availableStereoStates[i], GUILayout.Width(25));
                            if (newState != _availableStereoStates[i])
                            {
                                _availableStereoStates[i] = newState;
                                if (newState) _stereoSpeakerSettings.AppendNewEntry(speaker);
                                else _stereoSpeakerSettings.RemoveEntryByValue(speaker);
                            }
                        }
                    }

                    // object ping
                    using (VArea)
                    {
                        var label = new GUIContent("Find");
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label, true))
                            EditorGUILayout.LabelField(label);

                        var pingContent = new GUIContent("?", I18n.Tr("Highlight Object in Scene"));
                        foreach (var t in _availableSpeakers)
                            if (GUILayout.Button(pingContent, GUILayout.Width(25)))
                                EditorGUIUtility.PingObject(t);
                    }
                }
            }

            var tmp = EditorGUILayout.ToggleLeft(I18n.Tr("Customize Auto-Management"), _managedSpatialMute.isExpanded);
            if (tmp != _managedSpatialMute.isExpanded)
            {
                _managedSpatialMute.isExpanded = tmp;

                if (tmp)
                {
                    _stereoSpeakerSettings.UnhideProperties(1, 2);
                    _spatialSpeakerSettings.UnhideProperties(1, 2);
                }
                else
                {
                    _stereoSpeakerSettings.HideProperties(1, 2);
                    _spatialSpeakerSettings.HideProperties(1, 2);
                }
            }

            _spatialSpeakerSettings.DrawLayout(showHints);
            _stereoSpeakerSettings.DrawLayout(showHints);
        }

        protected override void SaveData()
        {
            if (_updateScreens)
            {
                _updateScreens = false;
                var finalScreens = new List<GameObject>(script.screens);
                // combine existing and possibles
                finalScreens.AddRange(_availableScreens);
                script.screens = finalScreens
                    // clear duplicates
                    .Distinct()
                    // purge possibles that have been unselected
                    .Where(s =>
                    {
                        var i = Array.IndexOf(_availableScreens, s);
                        if (i == -1) return s != null;
                        return _availableScreenStates[i];
                    }).ToArray();
            }
        }
    }
}