using System;
using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

// ReSharper disable RedundantCast

// ReSharper disable UseObjectOrCollectionInitializer

namespace ArchiTech.ProTV
{
    [
        UdonBehaviourSyncMode(BehaviourSyncMode.None),
        RequireComponent(typeof(BaseVRCVideoPlayer), typeof(MeshRenderer)),
        DefaultExecutionOrder(-9998), // init immediately after the TV
        HelpURL("https://protv.dev/guides/audio")
    ]
    public partial class VPManager : ATBehaviour
    {
        private readonly Vector4 DEFAULTST = new Vector4(1, 1, 0, 0);
        private readonly Vector4 DEFAULTTEXELSIZE = new Vector4(0.0625f, 0.0625f, 16f, 16f);

        [NonSerialized] public BaseVRCVideoPlayer videoPlayer;
        private TVManager tv;

        /// <summary>
        /// 
        /// </summary>
        [
            I18nInspectorName("Custom Label"), I18nTooltip("A custom name/label for the video manager that can be used by plugins. Typically shows up in any MediaControls dropdowns.")
        ]
        public string customLabel = "";

        // Speaker Management
        [HideInInspector, SerializeField,
         I18nInspectorName("Auto-Manage Volume"), I18nTooltip("Flag whether or not to have this video manager should automatically control the speakers' volume.")
        ]
        internal bool autoManageVolume = true;

        [HideInInspector, SerializeField,
         I18nInspectorName("Auto-Manage Mute"), I18nTooltip("Flag whether or not to have this video manager should automatically control the speakers' mute state.")
        ]
        internal bool autoManageMute = true;

        [SerializeField,
         I18nInspectorName("Managed Screens")
        ]
        internal GameObject[] screens;

        [SerializeField, FormerlySerializedAs("speakers"),
         I18nInspectorName("Spatial (3D)")
        ]
        internal AudioSource[] spatialSpeakers;

        [SerializeField, FormerlySerializedAs("managedSpeakerVolume"),
         I18nInspectorName("Volume")
        ]
        internal bool[] managedSpatialVolume = new bool[0];

        [SerializeField, FormerlySerializedAs("managedSpeakerMute"),
         I18nInspectorName("Mute")
        ]
        internal bool[] managedSpatialMute = new bool[0];

        [SerializeField,
         I18nInspectorName("Stereo (2D)")
        ]
        internal AudioSource[] stereoSpeakers;

        [SerializeField,
         I18nInspectorName("Volume")
        ]
        internal bool[] managedStereoVolume = new bool[0];

        [SerializeField,
         I18nInspectorName("Mute")
        ]
        internal bool[] managedStereoMute = new bool[0];

        [HideInInspector, SerializeField] private bool isAVPro;
        [HideInInspector, SerializeField] internal Renderer matRenderer;
        internal Animator mediaController;

        [NonSerialized] public bool isVisible;
        [NonSerialized] public bool mute = true;
        [NonSerialized] public float volume = 0.5f;
        [NonSerialized] public bool audio3d = true;

        private bool[] _spatialMuteCache;
        private bool[] _stereoMuteCache;
        private Material _videoMat;
        internal float playbackSpeed = 1f;
        private bool hasMediaController;
        private const string _playbackSpeedParameter = "PlaybackSpeed";
        private bool _hasVideoMat;

        // properties for the blit operation when using render texture
        private const string shaderName_MainTex = "_MainTex";
        private MaterialPropertyBlock matBlock;


        private int shaderID_MainTex;
        private int shaderID_MainTex_ST;

        // private bool extractionInProgress = false;
        internal Color32[] pixels = new Color32[0];
        internal Vector2Int pixelDims = Vector2Int.zero;

        // AVPro has a playback speed defect. Disable for AVPro until it's fixed.
        internal bool ValidMediaController => hasMediaController;

        public bool IsAVPro
        {
            get => isAVPro;
            internal set => isAVPro = value;
        }

        public bool IsManagedSpeaker(AudioSource source)
        {
            var idx = Array.IndexOf(spatialSpeakers, source);
            if (idx == -1) idx = Array.IndexOf(stereoSpeakers, source);
            return idx > -1;
        }

        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#00ccaa");
            base.Start();

            var stereoLen = stereoSpeakers.Length;
            var spatialLen = spatialSpeakers.Length;
            _stereoMuteCache = new bool[stereoLen];
            _spatialMuteCache = new bool[spatialLen];

            foreach (bool s in managedSpatialVolume)
            {
                autoManageVolume = s;
                if (s) break;
            }

            if (!autoManageVolume)
            {
                foreach (bool s in managedStereoVolume)
                {
                    autoManageVolume = s;
                    if (s) break;
                }
            }

            foreach (bool s in managedSpatialMute)
            {
                autoManageMute = s;
                if (s) break;
            }

            if (!autoManageMute)
            {
                foreach (bool s in managedStereoMute)
                {
                    autoManageMute = s;
                    if (s) break;
                }
            }

            videoPlayer = (BaseVRCVideoPlayer)GetComponent(typeof(BaseVRCVideoPlayer));
            videoPlayer.EnableAutomaticResync = false;

            shaderID_MainTex = VRCShader.PropertyToID(shaderName_MainTex);
            shaderID_MainTex_ST = VRCShader.PropertyToID(shaderName_MainTex + "_ST");
            matBlock = new MaterialPropertyBlock();
            mediaController = GetComponent<Animator>();
            hasMediaController = mediaController != null;

            if (matRenderer == null) matRenderer = GetComponent<MeshRenderer>();

#if UNITY_2022_3_OR_NEWER
            SetTV(GetComponentInParent<TVManager>(true));
#else
            SetTV(GetComponentInParent<TVManager>());
#endif
        }

        private void OnEnable()
        {
            Start();
            if (hasMediaController)
            {
                mediaController.enabled = true;
                mediaController.Rebind();
                ChangePlaybackSpeed(tv.playbackSpeed);
            }
        }

        #region Video Engine Proxy Methods

        public override void OnVideoEnd() => tv._OnVideoPlayerEnd();
        public override void OnVideoError(VideoError error) => tv._OnVideoPlayerError(error);

        public override void OnVideoReady()
        {
            if (tv.IsBuffering) return; // skip possible duplicate calls
            tv._OnVideoPlayerReady();
        }

        public override void OnVideoPlay() => tv._OnVideoPlayerPlay();

        // 2024/09/04 https://feedback.vrchat.com/sdk-bug-reports/p/u-compiler-is-not-properly-name-converting-onvideoplay-to-onvideoplay
        public void _onVideoPlay() => tv._OnVideoPlayerPlay();

        #endregion

        // === Public events to control the video player parts ===

        public void Show()
        {
            Start();
            foreach (var screen in screens)
            {
                if (screen == null) continue;
                screen.SetActive(true);
            }

            if (autoManageMute) ChangeMute(false);
            isVisible = true;
            Debug("Activated");
        }

        [Obsolete("Use Hide() instead")]
        public void _Hide() => Hide();

        public void Hide()
        {
            Start();
            if (autoManageMute) ChangeMute(true);
            if (tv.videoManagers.Length > 1)
            {
                foreach (var screen in screens)
                {
                    if (screen == null) continue;
                    screen.SetActive(false);
                }
            }

            isVisible = false;
            Debug("Deactivated");
        }

        public void Stop()
        {
            Hide();
            videoPlayer.Stop();
        }

        public void UpdateState()
        {
            // audiomode is first to make sure the correct speakers are targeted.
            ChangeAudioMode(tv.audio3d);
            ChangeMute(tv.mute);
            ChangeVolume(tv.volume);
            ChangePlaybackSpeed(tv.playbackSpeed);
        }

        #region Speaker Control

        public void ChangeMute(bool muted)
        {
            Start();
            if (!autoManageMute) return;
            mute = muted;
            var targetSpeakers = audio3d ? spatialSpeakers : stereoSpeakers;
            var targetMute = audio3d ? managedSpatialMute : managedStereoMute;
            if (IsTraceEnabled) Trace($"Speakers count {targetSpeakers.Length} | setting mute {mute}");
            for (var index = 0; index < targetSpeakers.Length; index++)
            {
                if (targetMute[index])
                {
                    var speaker = targetSpeakers[index];
                    if (speaker == null) continue;
                    speaker.mute = mute;
                }
            }
        }

        public void ChangeVolume(float useVolume, bool suppressLog = false)
        {
            Start();
            if (!autoManageVolume) return;
            volume = useVolume;
            var targetSpeakers = audio3d ? spatialSpeakers : stereoSpeakers;
            var targetVolume = audio3d ? managedSpatialVolume : managedStereoVolume;
            if (!suppressLog && IsTraceEnabled) Trace($"Speakers count {targetSpeakers.Length} | setting volume {volume}");
            for (var index = 0; index < targetSpeakers.Length; index++)
            {
                if (targetVolume[index])
                {
                    var speaker = targetSpeakers[index];
                    if (speaker == null) continue;
                    speaker.volume = volume;
                }
            }
        }

        public void ChangeAudioMode(bool use3dAudio)
        {
            Start();
            audio3d = use3dAudio;
            var fromSpeakers = audio3d ? stereoSpeakers : spatialSpeakers;
            var fromMuteCache = audio3d ? _stereoMuteCache : _spatialMuteCache;
            var toSpeakers = audio3d ? spatialSpeakers : stereoSpeakers;
            var toMuteCache = audio3d ? _spatialMuteCache : _stereoMuteCache;
            var toVolume = audio3d ? managedSpatialVolume : managedStereoVolume;
            if (IsTraceEnabled) Trace($"Setting audio mode to {(audio3d ? "3D" : "2D")}");
            for (var index = 0; index < fromSpeakers.Length; index++)
            {
                var speaker = fromSpeakers[index];
                if (speaker == null) continue;
                fromMuteCache[index] = speaker.mute;
                speaker.mute = true;
            }

            for (var index = 0; index < toSpeakers.Length; index++)
            {
                var speaker = toSpeakers[index];
                if (speaker == null) continue;
                speaker.mute = toMuteCache[index];
                // ensure volume setting is caught up as well
                if (toVolume[index]) speaker.volume = volume;
            }
        }

        /// <summary>
        /// Modify the playback speed of the current video player if the animator for managing it is available.
        /// Only really works on the UnityVideo, AVPro doesn't correctly respect the playback speed.
        /// Hardcoded limits are slow 0.5f and fast 2f. Values beyond those extremes tend to be intolerably bad quality.
        /// </summary>
        /// <param name="speed">The relative speed adjustment desired (allows 0.5f to 2f)</param>
        public void ChangePlaybackSpeed(float speed)
        {
            Start();
            if (!ValidMediaController) return;
            speed = Mathf.Clamp(speed, 0.5f, 2f);
            playbackSpeed = speed; // cache
            if (IsTraceEnabled) Trace($"Updating PlaybackSpeed to {playbackSpeed}");
            mediaController.SetFloat(_playbackSpeedParameter, speed);
        }

        #endregion

        // ================= Helper Methods =================

        public void SetTV(TVManager manager)
        {
            if (manager == null)
            {
                Warn("No TV Reference provided");
                return;
            }

            tv = manager;
            audio3d = tv.isReady ? tv.audio3d : !tv.startWith2DAudio;
            mute = tv.mute;
            volume = tv.isReady ? tv.volume : tv.defaultVolume;

            if (Logger == null) Logger = tv.Logger;
            if (tv.LogLevelOverride) LoggingLevel = tv.LoggingLevel;
            SetLogPrefixLabel($"{tv.gameObject.name}/{name}");
        }

        #region Texture Handling

        // public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        // {
        //     if (!request.done) return;
        //     if (request.hasError)
        //     {
        //         tv.enablePixelExtraction = false;
        //         Error("Pixel Readback had an error occur. Check logs.");
        //         return;
        //     }
        //     var pix = pixels;
        //     var pixelCount = textureWidth * textureHeight;
        //     if (pixelCount != pixels.Length)
        //     {
        //         pixelDims.x = textureWidth;
        //         pixelDims.y = textureHeight;
        //         Trace($"layer count {request.layerCount} layer size {request.layerDataSize}");
        //         pix = new Color32[request.layerDataSize];
        //     }
        //
        //     if (request.TryGetData(pix, 0)) pixels = pix;
        //     extractionInProgress = false;
        // }

        /// <summary>
        /// Extract the Texture from the respective video player target. This reference is not in the CPU so ReadPixels would be costly, just FYI.
        /// </summary>
        /// <returns>The Texture reference from the backing video player for the current frame.</returns>
        public Texture GetVideoTexture(out Vector4 textureST)
        {
            Texture texture = null;
            textureST = DEFAULTST;
            if ((int)tv.state <= (int)TVPlayState.STOPPED) return null;

            if (!_hasVideoMat)
            {
                if (!init) Start(); // make sure the shader variables have been properly init'd
                var mat = matRenderer.sharedMaterial;
                _videoMat = mat != null ? mat : matRenderer.material;
                _hasVideoMat = _videoMat != null && _videoMat.HasTexture(shaderID_MainTex);
            }


            if (_hasVideoMat)
            {
                Vector4 st = DEFAULTST;
                if (isAVPro)
                {
                    texture = _videoMat.GetTexture(shaderID_MainTex);
                    st = _videoMat.GetVector(shaderID_MainTex_ST);
                }
                else
                {
                    matRenderer.GetPropertyBlock(matBlock);
                    texture = matBlock.GetTexture(shaderID_MainTex);
                    st = matBlock.GetVector(shaderID_MainTex_ST);
                }

                if (st != Vector4.zero) textureST = st; // prevent empty vector 4 from being applied
            }

            return texture;
        }

        #endregion
    }
}