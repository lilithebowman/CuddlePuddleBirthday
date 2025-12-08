using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

#if AVPRO_IMPORTED
using System;
using VRC.SDK3.Video.Components.AVPro;
using RenderHeads.Media.AVProVideo;
using RenderHeads.Media.AVProVideo.Editor;
#endif

namespace ArchiTech.VideoPlayerShim
{
#if AVPRO_IMPORTED
    /// <summary>
    /// Editor for the AudioOutput component
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioOutputShim))]
    public class AudioOutputShimEditor : AudioOutputEditor
    {
        public override void OnInspectorGUI()
        {
            try
            {
                base.OnInspectorGUI();
            }
            catch (NullReferenceException) { }
        }

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        private static void SetupSwap()
        {
            EditorApplication.playModeStateChanged -= HandleSwap;
            EditorApplication.playModeStateChanged += HandleSwap;
        }

        private static void HandleSwap(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                var outputs = GetComponentsInScene<VRCAVProVideoSpeaker>()
                    .Select(s => s.GetComponent<AudioOutput>())
                    .Where(s => s != null && s.GetType() != typeof(AudioOutputShim))
                    .ToArray();
                // all AudioOutputs need to be converted into the shim in this package so we have corrective control of the speaker's volume
                // this is needed in order to have independent volume control on a per-AudioSource basis.
                foreach (var output in outputs) SwapComponent<AudioOutputShim>(output);
            }
        }

        private static T[] GetComponentsInScene<T>(bool includeInactive = true) where T : Component
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject[] roots = stage == null ? SceneManager.GetActiveScene().GetRootGameObjects() : new[] { stage.prefabContentsRoot };
            List<T> objects = new List<T>();
            foreach (GameObject root in roots)
                objects.AddRange(root.GetComponentsInChildren<T>(includeInactive));
            return objects.ToArray();
        }

        private static T SwapComponent<T>(MonoBehaviour fromBehaviour) where T : MonoBehaviour
        {
            T @out = null;
            var from = new SerializedObject(fromBehaviour);
            foreach (var newScript in Resources.FindObjectsOfTypeAll<MonoScript>())
            {
                if (newScript.GetClass() == typeof(T))
                {
                    from.FindProperty("m_Script").objectReferenceValue = newScript;
                    from.ApplyModifiedProperties();
                    from.Update();
                    @out = (T)from.targetObject;
                    break;
                }
            }

            return @out;
        }
#endif
    }
#endif
}