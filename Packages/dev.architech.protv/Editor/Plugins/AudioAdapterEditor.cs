using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;

#if AUDIOLINK_0 && !AUDIOLINK_1
using AudioLink = VRCAudioLink;
#endif

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(AudioAdapter), true)]
    public class AudioAdapterEditor : TVPluginEditor
    {
        private AudioAdapter script;
        private TVManager tv;
        private AudioSource[] targetSpeakers;

        protected override bool autoRenderVariables => false;

#if AUDIOLINK_0 || AUDIOLINK_1
        protected override bool autoRenderHeader => false;
#endif

        protected override void LoadData()
        {
            script = (AudioAdapter)target;
            tv = script.tv;
            targetSpeakers = script.targetSpeakers;
        }

        protected override void InitData()
        {
            SetupTVReferences();
            if (tv == null) tv = ProTVEditorUtility.FindParentTVManager(script);
#if AUDIOLINK_0 || AUDIOLINK_1
            initAudioLinkControls();
#endif
        }

        protected override void RenderChangeCheck()
        {
            // if the TV changed, rerun InitData
            if (DrawTVReferences()) init = false;
#if AUDIOLINK_0 || AUDIOLINK_1
            drawAudioLinkControls();
#else
            EditorGUILayout.HelpBox(I18n.Tr("AudioLink is not detected in the project. If you wish to use AudioLink, please import AudioLink version 0.3.0 or later. Recommend importing into the project via VCC."), MessageType.Info);
#endif
            drawWorldAudioControls();
        }


#if AUDIOLINK_0 || AUDIOLINK_1
        private class SpeakerSelection
        {
            public string name;
            public AudioSource[] speakers;
            public string[] speakerNames;
            public int[] speakerIndexes;
            public int current;
        }

        private readonly List<SpeakerSelection> _selectableSpeakers = new List<SpeakerSelection>();
        private AudioLink.AudioLink audioLinkInstance;

        private void initAudioLinkControls()
        {
            _selectableSpeakers.Clear();
            audioLinkInstance = ATEditorUtility.GetComponentInScene<AudioLink.AudioLink>();
            if (tv == null) return;

            var managers = tv.GetComponentsInChildren<VPManager>(true);

            if (targetSpeakers.Length == 0 && managers.Length != targetSpeakers.Length) ForceSave();
            targetSpeakers = NormalizeArray(targetSpeakers, managers.Length);

            for (int i = 0; i < managers.Length; i++)
            {
                VPManager manager = managers[i];
                AudioSource[] sources = new AudioSource[0];
                var videoPlayer = manager.GetComponent<BaseVRCVideoPlayer>();
                if (videoPlayer is VRCAVProVideoPlayer avpro)
                {
                    sources = ATEditorUtility.GetComponentsInScene<VRCAVProVideoSpeaker>()
                        .Where(s => s.VideoPlayer == avpro)
                        .Select(s => s.GetComponent<AudioSource>())
                        .ToArray();
                }
                else if (videoPlayer is VRCUnityVideoPlayer unityVideo)
                {
                    // use reflection to extract the reference to the audio source array because it's normally private
                    sources = (AudioSource[])typeof(VRCUnityVideoPlayer).GetField("targetAudioSources", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(unityVideo);
                    sources = sources?.Where(s => s != null).ToArray() ?? new AudioSource[0];
                }
                else UnityEngine.Debug.LogError($"VideoPlayer type unknown: {videoPlayer.GetType().FullName}");

                _selectableSpeakers.Add(new SpeakerSelection
                {
                    name = string.IsNullOrWhiteSpace(manager.customLabel) ? manager.gameObject.name : manager.customLabel,
                    speakers = sources,
                    speakerNames = sources.Select(s => s.gameObject.name).ToArray(),
                    speakerIndexes = sources.Select((s, j) => j).ToArray(),
                    current = System.Array.IndexOf(sources, targetSpeakers[i] ?? sources.FirstOrDefault(s => s.gameObject.activeSelf) ?? sources.FirstOrDefault())
                });
            }
        }

        private void drawAudioLinkControls()
        {
            DrawCustomHeaderLarge(I18n.Tr("AudioLink Options"));
            using (VBox)
            {
                using (HArea)
                {
                    DrawVariablesByName(nameof(AudioAdapter.enableAudioLink));
                    if (script.enableAudioLink)
                    {
                        using (DisabledScope())
                        {
                            EditorGUILayout.ObjectField(GUIContent.none, audioLinkInstance, typeof(AudioLink.AudioLink), true);
                        }
                    }
                }

                if (script.enableAudioLink)
                {
                    DrawVariablesByName(nameof(script.muteAudioLinkDuringSilence));

                    if (script.tv == null) EditorGUILayout.LabelField(I18n.Tr("No TV detected"));
                    else
                    {
                        DrawCustomHeader("Speaker Selection");
                        foreach (var s in _selectableSpeakers)
                        {
                            using (HArea)
                            {
                                EditorGUILayout.PrefixLabel(s.name);
                                s.current = EditorGUILayout.IntPopup(
                                    s.current,
                                    s.speakerNames,
                                    s.speakerIndexes
                                );
                            }
                        }
                    }
                }
            }
        }
#endif

        private void drawWorldAudioControls()
        {
            DrawCustomHeaderLarge("World Audio Settings");
            using (VBox)
            {
                if (script.worldAudio && script.worldAudio.GetComponent<VRCAVProVideoSpeaker>())
                    EditorGUILayout.HelpBox("Do not use a TV speaker as the world audio. This will cause unexpected behaviour and is not its intended usage.", MessageType.Error);

                DrawVariablesByName(nameof(AudioAdapter.worldAudio));
                if (script.worldAudio != null)
                {
                    DrawVariablesByName(
                        nameof(AudioAdapter.worldAudioResumeDelay),
                        nameof(AudioAdapter.worldAudioFadeInTime),
                        nameof(AudioAdapter.worldAudioResumeDuringSilence));
                }
            }
        }

        protected override void SaveData()
        {
#if AUDIOLINK_0 || AUDIOLINK_1
            script.targetSpeakers = _selectableSpeakers.Select(s1 => s1.current == -1 ? null : s1.speakers[s1.current]).ToArray();
#endif
        }
    }
}