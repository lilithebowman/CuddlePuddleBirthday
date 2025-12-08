// Manually handle equivalence to version defines for udonsharp compiler phase since UdonSharp currently does not support version defines from the assembly definition file

#if COMPILER_UDONSHARP && AUDIOLINK && !AUDIOLINK_V1
    #define AUDIOLINK_0
#endif
#if COMPILER_UDONSHARP && AUDIOLINK_V1
    #define AUDIOLINK_1
#endif

#if AUDIOLINK_0 && !AUDIOLINK_1
using AudioLink = VRCAudioLink;
#endif

using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public partial class AudioAdapter : TVPlugin
    {
        [FormerlySerializedAs("allowAudioLinkControl"),
         I18nInspectorName("Enable AudioLink Integration")
        ]
        public bool enableAudioLink = true;

        [SerializeField,
         I18nInspectorName("Audio Link Instance"), I18nTooltip("The singleton reference to the AudioLink copy in the scene. This is automatically detected and assigned.")
        ]
#if AUDIOLINK_0 || AUDIOLINK_1
        protected internal AudioLink.AudioLink audioLinkInstance;
#else
        [HideInInspector] protected internal UdonSharpBehaviour audioLinkInstance;
#endif

        [SerializeField,
         I18nInspectorName("AudioLink Options"), Tooltip("Assign which speaker for each given VPManager will be used by AudioLink.")
        ]
        protected internal AudioSource[] targetSpeakers = new AudioSource[0];

        [SerializeField,
         I18nInspectorName("Mute AudioLink During Silence"), Tooltip("Whether AudioLink should react to the speaker when it's muted or at volume 0.")
        ]
        protected internal bool muteAudioLinkDuringSilence = true;

        [SerializeField, FormerlySerializedAs("worldMusic"), FormerlySerializedAs("audioSource"),
         I18nInspectorName("World Audio"), I18nTooltip("Optionally specify custom world audio to pause while the TV is playing. Will resume world audio a given number of seconds after TV stops playing.")
        ]
        protected internal AudioSource worldAudio = null;

        [SerializeField, FormerlySerializedAs("worldMusicResumeDelay"),
         Min(0),
         I18nInspectorName("World Audio Resume Delay"), I18nTooltip("How long to wait after the TV has finished before resuming the world music.")
        ]
        protected internal float worldAudioResumeDelay = 20f;

        [SerializeField, FormerlySerializedAs("worldMusicFadeInTime"),
         Min(0),
         I18nInspectorName("World Audio Fade-in Time"), I18nTooltip("How long does the world music take to fade in after the delay has completed.")
        ]
        protected internal float worldAudioFadeInTime = 4f;

        [SerializeField, FormerlySerializedAs("worldMusicResumeDuringSilence"),
         I18nInspectorName("Resume World Audio During Silence"), I18nTooltip("While the TV is muted or paused, allow the world music to continue playing?")
        ]
        protected internal bool worldAudioResumeDuringSilence = false;

        private float worldAudioVolume;
        private float worldAudioFadeAmount;
        private AudioSource activeSpeaker;
        private AudioSource nextSpeaker;
        private bool worldAudioActive = true;
        private bool hasWorldAudio = false;
        private bool hasAudioLink = false;

        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#55ccaa");
            base.Start();
            if (!hasTV)
            {
                if (IsWarnEnabled) Warn("No TV connected");
                return;
            }

            hasWorldAudio = worldAudio != null;
            hasAudioLink = audioLinkInstance != null && enableAudioLink;

            if (hasWorldAudio) worldAudioVolume = worldAudio.volume;

#if AUDIOLINK_0 || AUDIOLINK_1
            if (hasAudioLink)
            {
                if (hasWorldAudio)
                {
                    if (audioLinkInstance.audioSource == null)
                        audioLinkInstance.audioSource = worldAudio;
                }
#if !AUDIOLINK_0
                audioLinkInstance.autoSetMediaState = false;
#endif
            }
#endif

            if (IsTraceEnabled)
            {
                string _log = "";
                foreach (AudioSource speaker in targetSpeakers)
                    _log += $"{(speaker == null ? "<missing speaker>" : speaker.gameObject.name)} ";

                Trace($"In-game init speakers: {_log}");
            }
        }

        public void UpdateWorldAudioVolume()
        {
            if (hasWorldAudio)
            {
                if (worldAudio.isPlaying && worldAudio.volume < worldAudioVolume)
                {
                    // trigger the update loop
                    SendCustomEventDelayedFrames(nameof(UpdateWorldAudioVolume), 1);
                    if (worldAudioFadeInTime == 0f) worldAudio.volume = worldAudioVolume;
                    else
                    {
                        worldAudioFadeAmount += Time.deltaTime;
                        worldAudio.volume = Mathf.SmoothStep(0f, worldAudioVolume, worldAudioFadeAmount / worldAudioFadeInTime);
                    }
                }
            }
        }

        public void UpdateMediaTime()
        {
#if AUDIOLINK_1
            if (hasAudioLink && tv.state == TVPlayState.PLAYING)
            {
                SendCustomEventDelayedSeconds(nameof(UpdateMediaTime), 1f);
                audioLinkInstance.SetMediaTime(tv.SeekPercent);
            }
#endif
        }

        #region TV Events

        public override void _TvReady()
        {
            nextSpeaker = targetSpeakers[tv.videoPlayer];
            updateMediaVolume();
        }

        public override void _TvVideoPlayerChange()
        {
            nextSpeaker = targetSpeakers[OUT_VIDEOPLAYER];
        }

        public override void _TvMediaReady()
        {
            updateMediaLoop();
#if AUDIOLINK_0 || AUDIOLINK_1
            if (hasAudioLink && nextSpeaker != activeSpeaker)
            {
                audioLinkInstance.audioSource = nextSpeaker;
                updateVolume(tv.volume, tv.mute);
                if (IsInfoEnabled) Info($"Switching to {(nextSpeaker == null ? "<missing speaker>" : nextSpeaker.gameObject.name)}");
            }
#endif
            activeSpeaker = nextSpeaker;
        }

        public override void _TvMediaEnd()
        {
            if (hasWorldAudio) resumeWorldMusic();
#if AUDIOLINK_1
            else if (hasAudioLink) audioLinkInstance.SetMediaPlaying(AudioLink.MediaPlaying.Paused);
#endif
        }

        public override void _TvStop()
        {
            if (hasWorldAudio) resumeWorldMusic();
#if AUDIOLINK_1
            else if (hasAudioLink) audioLinkInstance.SetMediaPlaying(AudioLink.MediaPlaying.Stopped);
#endif
        }

        public override void _TvPause()
        {
            if (hasWorldAudio && worldAudioResumeDuringSilence) resumeWorldMusic();
#if AUDIOLINK_1
            else if (hasAudioLink) audioLinkInstance.SetMediaPlaying(AudioLink.MediaPlaying.Paused);
#endif
        }

        public override void _TvPlay()
        {
            updateVolume(tv.volume, tv.mute);
            resumeTVAudio();
#if AUDIOLINK_1
            if (hasAudioLink)
            {
                audioLinkInstance.SetMediaPlaying(tv.isLive ? AudioLink.MediaPlaying.Streaming : AudioLink.MediaPlaying.Playing);
                SendCustomEventDelayedFrames(nameof(UpdateMediaTime), 1);
            }
#endif
        }

        public override void _TvVideoPlayerError()
        {
#if AUDIOLINK_1
            if (hasAudioLink) audioLinkInstance.SetMediaPlaying(AudioLink.MediaPlaying.Error);
#endif
        }

        public override void _TvMute()
        {
            if (hasWorldAudio && worldAudioResumeDuringSilence) resumeWorldMusic();
        }

        public override void _TvUnMute()
        {
            updateVolume(tv.volume, false);
            if (tv.IsPlaying || !worldAudioResumeDuringSilence && tv.IsPaused)
                resumeTVAudio();
        }

        public override void _TvVolumeChange()
        {
            updateVolume(OUT_VOLUME, tv.mute);
        }

        public override void _TvEnableLoop() => updateMediaLoop();

        public override void _TvDisableLoop() => updateMediaLoop();

        public override void _TvLoading() => updateMediaPlayState();

        public override void _TvLoadingAbort() => updateMediaPlayState();

        #endregion

        private void updateVolume(float volume, bool mute)
        {
            if (muteAudioLinkDuringSilence && activeSpeaker != null && !tv._IsManagedSpeaker(activeSpeaker))
                activeSpeaker.mute = volume == 0f || mute;
            updateMediaVolume();
        }

        public void ActivateWorldMusic()
        {
            if (hasWorldAudio && worldAudioActive)
            {
                if (IsInfoEnabled) Info("Resuming world music...");
#if AUDIOLINK_0 || AUDIOLINK_1
                if (hasAudioLink) audioLinkInstance.audioSource = worldAudio;
#endif
                worldAudio.UnPause();
                SendCustomEventDelayedFrames(nameof(UpdateWorldAudioVolume), 2);
            }
        }

        private void resumeWorldMusic()
        {
            if (hasWorldAudio)
            {
                if (!worldAudioActive)
                {
                    worldAudioActive = true;
                    SendCustomEventDelayedSeconds(nameof(ActivateWorldMusic), worldAudioResumeDelay);
                }
            }
        }

        private void resumeTVAudio()
        {
            if (!worldAudioResumeDuringSilence || (tv.IsPlaying && !(tv.mute || tv.volume == 0)))
                disableWorldMusic();
#if AUDIOLINK_0 || AUDIOLINK_1
            if (hasAudioLink && activeSpeaker != null) audioLinkInstance.audioSource = activeSpeaker;
#endif
        }

        private void disableWorldMusic()
        {
            if (hasWorldAudio && worldAudioActive)
            {
                Debug("Halting world music...");
                worldAudio.Pause();
                worldAudio.volume = 0.001f;
                worldAudioFadeAmount = 0f;
                worldAudioActive = false;
            }
        }

        private void updateMediaVolume()
        {
#if AUDIOLINK_1
            if (hasAudioLink) audioLinkInstance.SetMediaVolume(tv.volume);
#endif
        }

        private void updateMediaLoop()
        {
#if AUDIOLINK_1
            if (hasAudioLink)
            {
                AudioLink.MediaLoop loopState = AudioLink.MediaLoop.None;
                var loop = tv.loop;
                if (loop == 1) loopState = AudioLink.MediaLoop.LoopOne;
                else if (loop != 0) loopState = AudioLink.MediaLoop.Loop;
                audioLinkInstance.SetMediaLoop(loopState);
            }
#endif
        }

        private void updateMediaPlayState()
        {
#if AUDIOLINK_1
            if (hasAudioLink)
            {
                var mp = AudioLink.MediaPlaying.None;
                if (tv.loading) mp = AudioLink.MediaPlaying.Loading;
                else
                    switch (tv.state)
                    {
                        case TVPlayState.WAITING:
                            mp = AudioLink.MediaPlaying.None;
                            break;
                        case TVPlayState.STOPPED:
                            mp = AudioLink.MediaPlaying.Stopped;
                            break;
                        case TVPlayState.PLAYING:
                            mp = tv.isLive ? AudioLink.MediaPlaying.Streaming : AudioLink.MediaPlaying.Playing;
                            break;
                        case TVPlayState.PAUSED:
                            mp = AudioLink.MediaPlaying.Paused;
                            break;
                    }

                audioLinkInstance.SetMediaPlaying(mp);
            }
#endif
        }

        public void EnableAudioLinkState() => ChangeAudioLinkState(true);
        public void DisableAudioLinkState() => ChangeAudioLinkState(false);
        public void ToggleAudioLinkState() => ChangeAudioLinkState(!enabled);

        public void ChangeAudioLinkState(bool state)
        {
            if (enabled == state) return;
            Start();
            enabled = state;
            enableAudioLink = state;
            hasWorldAudio = state && worldAudio != null;
            if (!state && worldAudioActive) disableWorldMusic();
#if AUDIOLINK_0 || AUDIOLINK_1
            hasAudioLink = state && audioLinkInstance != null;
            if (audioLinkInstance != null)
            {
                if (state)
                {
                    audioLinkInstance.audioSource = activeSpeaker;
                    audioLinkInstance.EnableAudioLink();
                }
                else audioLinkInstance.DisableAudioLink();
            }
#endif
        }
    }
}