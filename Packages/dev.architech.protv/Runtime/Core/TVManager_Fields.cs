using System;
using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    public partial class TVManager
    {
        // === Event input variables (update these from external udon graphs. U# should use the corresponding parameterized methods instead) ===

        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangeMedia()"/> event.
        /// Can only access via SetProgramVariable. 
        /// </summary>
        [NonSerialized] internal VRCUrl IN_MAINURL = VRCUrl.Empty;

        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangeMedia()"/> event.
        /// Can only access via SetProgramVariable. 
        /// </summary>
        [NonSerialized] internal VRCUrl IN_ALTURL = VRCUrl.Empty;

        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangeMedia()"/> event.
        /// Can only access via SetProgramVariable. 
        /// </summary>
        [NonSerialized] internal string IN_TITLE = EMPTYSTR;

        /// <summary>
        /// Udon compatible variable that is utilized by the <see cref="_ChangeMedia()"/> event.
        /// Can only access via SetProgramVariable.
        /// </summary>
        [NonSerialized] internal string IN_NAME = EMPTYSTR;

        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangeVolume()"/> event.<br/>
        /// Expects it to be a normalized float between 0f and 1f.
        /// Can only access via SetProgramVariable. 
        /// </summary>
        [NonSerialized] internal float IN_VOLUME = 0f;

        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangeSeekTime()"/> and <see cref="_ChangeSeekPercent()"/> events.<br/>
        /// For the _ChangeSeekPercent, it expects to be a normalized value between 0f and 1f.
        /// Can only access via SetProgramVariable. 
        /// </summary>
        [NonSerialized] internal float IN_SEEK = 0f;

        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangePlaybackSpeed()"/> event.<br/>
        /// It expects to be a normalized value between 0.5f and 2f.
        /// Can only access via SetProgramVariable.
        /// </summary>
        [NonSerialized] internal float IN_SPEED = 0f;

        // paramter for _ChangeVideoPlayer event
        /// <summary>
        /// Udon compatible variable that is utilised by the <see cref="_ChangeVideoPlayer()"/> event.
        /// Can only access via SetProgramVariable. 
        /// </summary>
        [NonSerialized] internal int IN_VIDEOPLAYER = -1;

        [SerializeField] protected internal VPManager[] videoManagers;
        [SerializeField] internal TVManagerData syncData;

        #region Autoplay Settings

        /// <summary>
        /// This is the URL to set as automatically playing when the first user joins a new instance. This has no bearing on an existing instance as the TV has already been syncing data after the initial point.
        /// </summary>
        [SerializeField, FormerlySerializedAs("autoplayMainURL"), FormerlySerializedAs("autoplayURL"),
         I18nInspectorName("Autoplay Main URL"), I18nTooltip("This is the URL to set as automatically playing when the first user joins a new instance. This has no bearing on an existing instance as the TV has already been syncing data after the initial point.")
        ]
        internal VRCUrl autoplayMainUrl = new VRCUrl("");

        /// <summary>
        /// This is an optional alternate url that can be provided for situations when the main url is insufficient (such as an alternate stream endpoint for Quest to use)
        /// </summary>
        [SerializeField, FormerlySerializedAs("autoplayURLAlt"),
         I18nInspectorName("Autoplay Alternate URL"), I18nTooltip("This is an optional alternate url that can be provided for situations when the main url is insufficient (such as an alternate stream endpoint for Android/Quest to use)")
        ]
        internal VRCUrl autoplayAlternateUrl = new VRCUrl("");

        /// <summary>
        /// Optional string to use as the label for the autoplay urls. Generally replaces the domain name in the UIs.
        /// </summary>
        [SerializeField, FormerlySerializedAs("autoplayLabel"),
         I18nInspectorName("Autoplay Title"), I18nTooltip("Optional string to use as the label for the autoplay urls. Generally replaces the domain name in the UIs.")
        ]
        internal string autoplayTitle = EMPTYSTR;

        /// <summary>
        /// Optional string to use as the label for the autoplay urls. Generally replaces the domain name in the UIs.
        /// </summary>
        [SerializeField, FormerlySerializedAs("autoplayLabel"),
         I18nInspectorName("Autoplay Loop"), I18nTooltip("Should loop be enabled for the autoplay urls? Enabling this is the exact same as if you added the loop url parameter to the urls.")
        ]
        internal bool autoplayLoop = false;

        // This is auto-populated during the build phase
        [SerializeField, HideInInspector] internal float autoplayStartOffset = 0f;

        #endregion

        #region Default TV Settings

        /// <summary>
        /// The video manager for the TV to start off on.
        /// </summary>
        [SerializeField, FormerlySerializedAs("initialVideoManager"), FormerlySerializedAs("initialPlayer"),
         I18nInspectorName("Default Manager"), I18nTooltip("The player (based on the internal VideoManagers list) for the TV to use first.")
        ]
        internal int defaultVideoManager = 0;

        /// <summary>
        /// The volume that the TV starts off at.
        /// </summary>
        [SerializeField, FormerlySerializedAs("initialVolume"), Range(0f, 1f),
         I18nInspectorName("Default Volume"), I18nTooltip("The volume that the TV starts off at.")
        ]
        internal float defaultVolume = 0.3f;

        /// <summary>
        /// Flag to initialize the TV with 2D audio instead of 3D audio.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Start with 2D Audio"), I18nTooltip("Flag to initialize the TV with 2D audio instead of 3D audio.")
        ]
        internal bool startWith2DAudio = false;

        /// <summary>
        /// Flag to initialize the TV with 2D audio instead of 3D audio.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Start with Video Disabled"), I18nTooltip("Flag to initialize the TV with disabled video. Will need to call the _EnableVideoTexture event to enable.")
        ]
        internal bool startWithVideoDisabled = false;

        /// <summary>
        /// Flag to initialize the TV with 2D audio instead of 3D audio.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Start with Audio Muted"), I18nTooltip("Flag to initialize the TV with muted audio. Will need to call the _UnMute event to enable.")
        ]
        internal bool startWithAudioMuted = false;


        #endregion

        #region Sync Options

        // This flag is to track whether or not the local player is able to operate independently of the owner
        // Setting to false gives the local player full control of their local player. 
        // Once they value is set to true, it will automatically resync with the owner, even if the video URL has changed since desyncing.
        /// <summary>
        /// Flag that determines whether the video player should sync with the owner. If false, the local user has full control over the player and only affects the local user.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Enable Syncing"), I18nTooltip("Flag that determines whether the video player should sync with the owner. If false, the local user has full control over the player and only affects the local user.")
        ]
        public bool syncToOwner = true;

        /// <summary>
        /// The interval for the TV to trigger an automatic resync to correct any AV and Time de-sync issues.
        /// Defaults to 10 minutes.
        /// Set to Infinity to disable.
        /// </summary>
        [SerializeField, Min(5f),
         I18nInspectorName("Automatic Resync Interval"), I18nTooltip("The interval for the TV to trigger an automatic resync to correct any AV and Time de-sync issues. Defaults to 10 minutes. Set to Infinity to disable.")
        ]
        public float automaticResyncInterval = 600f;

        /// <summary>
        /// The number of seconds that a non-owner is allowed to deviated from the owner's timestamp.
        /// Set to Infinity to disable.
        /// </summary>
        [SerializeField, Min(2f),
         I18nInspectorName("Play Drift Threshold"), I18nTooltip("The number of seconds that a non-owner is allowed to deviated from the owner's timestamp during playback before a resync is forced. Set to Infinity to disable.")
        ]
        public float playDriftThreshold = float.PositiveInfinity;

        /// <summary>
        /// Time difference allowed between owner's synced seek time and the local seek time while the video is paused locally.
        /// Can be thought of as a 'frame preview' of what's currently playing.
        /// It's good to have this at a higher value, NOT recommended to have this value less than 1.0.
        /// Set to Infinity to disable.
        /// </summary>
        [SerializeField, FormerlySerializedAs("pausedResyncThreshold"),
         I18nInspectorName("Pause Drift Threshold"), I18nTooltip("Time difference allowed between owner's synced seek time and the local seek time while the video is paused locally. Can be thought of as a 'frame preview' of what's currently playing. It's good to have this at a higher value, NOT recommended to have this value less than 1.0. Set to Infinity to disable.")
        ]
        public float pauseDriftThreshold = float.PositiveInfinity;

        /// <summary>
        /// Flag that determines whether the current video player selection will be synced across users.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Allow Local Tweaks"), I18nTooltip("This specifies whether non-owners are not allowed to locally control tweak values when they are marked as synced. For example, if both this and Sync Volume Control is enabled, the non-owners would be able to modify their TV volume locally, whereas if this is unchecked, the owner's volume would be enforced.")
        ]
        public bool allowLocalTweaks = false;

        /// <summary>
        /// Flag that determines whether the current video player selection will be synced across users.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Sync Manager Selection"), I18nTooltip("Flag that determines whether the current video player selection will be synced across users.")
        ]
        public bool syncVideoManagerSelection = false;

        /// <summary>
        /// Flag for whether to match the local volume control to the owner's.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Sync Volume Control"), I18nTooltip("Flag for whether to match the local volume control to the owner's.")
        ]
        public bool syncVolumeControl = false;

        /// <summary>
        /// Flag for whether to match the local volume control to the owner's.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Sync Audio Mode"), I18nTooltip("Flag for whether to match the local audio mode to the owner's.")
        ]
        public bool syncAudioMode = false;

        /// <summary>
        /// Flag for whether to match the local volume control to the owner's.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Sync Video 3D Mode"), I18nTooltip("Flag for whether to match the local video 3d mode and width to the owner's.")
        ]
        public bool syncVideoMode = false;

        #endregion

        #region Media Load Options

        /// <summary>
        /// Flag to specify if the media should play immediately after it's been loaded. Unchecked means the media must be manually played to start.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Play Video After Load"), I18nTooltip("Flag to specify if the media should play immediately after it's been loaded. Unchecked means the media must be manually played to start.")
        ]
        internal bool playVideoAfterLoad = true;

        /// <summary>
        /// Amount of time (in seconds) to wait before playing the media after it's successfully been loaded.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Buffer Delay After Load"), I18nTooltip("Amount of time (in seconds) to wait before playing the media after it's successfully been loaded.")
        ]
        internal float bufferDelayAfterLoad = 0f;

        /// <summary>
        /// Media will implicitly loop once if the duration is shorter than the specified time (in seconds). Set to 0 to disable.
        /// </summary>
        [SerializeField, Range(0, 60),
         I18nInspectorName("Implicit Replay Duration"), I18nInspectorName("Media will implicitly loop once if the duration is shorter than the specified time (in seconds). Set to 0 to disable.")
        ]
        internal int implicitReplayThreshold = 15;

        /// <summary>
        /// The amount of time allowed for any given media to attempt loading. If the timeout is exceeded, it will fail with a VideoError.PlayerError
        /// </summary>
        [SerializeField, Range(0f, 60f),
         I18nInspectorName("Max Allowed Loading Time"), I18nTooltip("The amount of time allowed for any given media to attempt loading. If the timeout is exceeded, it will fail with a VideoError.PlayerError")
        ]
        internal float maxAllowedLoadingTime = 20f;

        /// <summary>
        /// The amount of time between automatic reloading of the URL if the media is a livestream. Setting is ignored when set to 0.
        /// </summary>
        [SerializeField, Range(0, 30),
         I18nInspectorName("Live Media Reload Interval"), I18nTooltip("The amount of minutes between automatic reloading of the URL if the media is a livestream. Setting is ignored when set to 0.")
        ]
        internal int liveMediaAutoReloadInterval = 0;

        /// <summary>
        /// Flag for quest to prioritize using the alternate URL over the main URL. Main URL will be used if Alternate URL is not available/provided.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Prefer Alternate URL For Quest"), I18nTooltip("Flag for quest to prioritize using the alternate URL over the main URL. Main URL will be used if Alternate URL is not available/provided.")
        ]
        internal bool preferAlternateUrlForQuest = true;

        /// <summary>
        /// Flag for whether to allow reloading to be done via keypress
        /// </summary>
        [SerializeField,
         I18nInspectorName("Enable Reload Keybind"), I18nTooltip("Flag for whether the given keybind will trigger a reload of the current video.")
        ]
        internal bool enableReloadKeybind = true;

        /// <summary>
        /// Keypress key for reloading.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Key")
        ]
        internal KeyCode reloadKey = KeyCode.F6;

        #endregion

        #region Error/Retry Options

        /// <summary>
        /// When attempting to retry a url, it will swap to the alternate url and try it instead. If that also fails, it will simply resume any remaining retries with the main url.
        /// If you are using the alternate URL for non-media content, disable this option.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Graceful URL Fallback"), I18nTooltip("When attempting to retry a url, it will swap to the alternate url and try it instead. If that also fails, it will simply resume any remaining retries with the main url. If you are using this URL for non-media content, disable this option.")
        ]
        public bool retryUsingAlternateUrl = true;

        /// <summary>
        /// The number of times a url should retry if no explicit retry amount is specified for a given url.
        /// </summary>
        [SerializeField, Min(0),
         I18nInspectorName("Default Retry Count"), I18nTooltip("The number of times a url should retry if no explicit retry amount is specified for a given url.")
        ]
        internal int defaultRetryCount = 0;

        /// <summary>
        /// Amount of time (in seconds) to wait before reloading the media after an error occurs if the url specifies infinite retries.
        /// </summary>
        [SerializeField, Min(5f),
         I18nInspectorName("Repeating Retry Delay"), I18nTooltip("Amount of time (in seconds) to wait before reloading the media after an error occurs if the url specifies infinite retries.")
        ]
        internal float repeatingRetryDelay = 15f;

        #endregion

        #region Security Options

        /// <summary>
        /// This option enables the instance master to have control over the TV. Leaving enabled should be perfectly acceptable in most cases.
        /// Disable if you don't want random users being able to control the TV.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Allow Master Control"), I18nTooltip("This option enables the instance master to have control over the TV. Leaving enabled should be perfectly acceptable in most cases. Disable if you don't want random users being able to control the TV.")
        ]
        internal bool allowMasterControl = true;

        /// <summary>
        /// This option makes the TV remember the first person to enter the instance and treats them equivalent to the master. It can help alleviate issues with group instances.
        /// </summary>
        [SerializeField, FormerlySerializedAs("rememberFirstMaster"),
         I18nInspectorName("Allow First Master Control"), I18nTooltip("This option makes the TV remember the first person to enter the instance and treats them as an authorized user. It can help alleviate issues with group instances.")
        ]
        internal bool allowFirstMasterControl = true;

        /// <summary>
        /// This option determines whether the first master of the instance should be treated as a super user or not.
        /// For this setting to apply, Allow Master Control and Remember First Master settings MUST be enabled.
        /// </summary>
        [SerializeField,
         I18nInspectorName("First Master Super User"), I18nTooltip("This option determines whether the first master of the instance should be treated as a super user or not. For this setting to apply, Allow First Master Control setting MUST be enabled.")
        ]
        internal bool firstMasterIsSuper = false;

        /// <summary>
        /// This option determines whether the instance owner should be treated as a super user or not.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Instance Owner Super User"), I18nTooltip("This option determines whether the instance owner should be treated as a super user or not.")
        ]
        internal bool instanceOwnerIsSuper = true;

        /// <summary>
        /// This option makes it so that when a super user controls the TV and it is locked, generally authorized users are not able to retake control until unlocked by a super user.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Super User Lock Override"), I18nTooltip("This option makes it so that when a super user controls the TV and it is locked, generally authorized users are not able to retake control until unlocked by a super user.")
        ]
        internal bool superUserLockOverride = false;

        /// <summary>
        /// Determines if the video player starts off as locked down to master only. Good for worlds that do public events and similar.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Locked By Default"), I18nTooltip("Determines if the TV starts off as locked down to master only. Good for worlds that do public events and similar.")
        ]
        internal bool lockedByDefault = false;

        /// <summary>
        /// If desired, the TV will attempt to automatically handle ownership changes for situations where the owner sync is unavailable or the current owner is unauthorized.
        /// </summary>
        [SerializeField,
         I18nInspectorName("[Experimental] Enable Auto-Ownership"), I18nTooltip("If desired, the TV will attempt to automatically handle ownership changes for situations where the owner sync is unavailable or the current owner is unauthorized.")
        ]
        internal bool enableAutoOwnership = false;

        /// <summary>
        /// When enabled, the TV will ONLY allow authorized or super authorized users to interact with the TV.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Disallow Unauthorized Users"), I18nTooltip("When enabled, the TV will ONLY allow authorized or super authorized users to interact with the TV, regardless of if the TV is unlocked.")
        ]
        internal bool disallowUnauthorizedUsers = false;

        [SerializeField, FormerlySerializedAs("pauseTakesOwnership"),
         I18nInspectorName("PlayState Change Takes Ownership"), I18nTooltip("When enabled, the TV will check the auth of the user attempting to play/pause/stop the media, and if valid, will take ownership and pause the media for everyone instead of just locally.")
        ]
        internal bool playStateTakesOwnership = false;

        /// <summary>
        /// Toggles whether to check for domain whitelist access. Will block domains not on the list. Defaults to VRChat's trusted URLs list.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Enable Domain Whitelist"), I18nTooltip("Toggles whether or not to check for domain whitelist access. Will block domains not on the list. Defaults to VRChat's trusted URLs list.")
        ]
        internal bool enforceDomainWhitelist = false;

        /// <summary>
        /// Flag whether an Authorized User is allowed to bypass the domain whitelist. Typically enabled for social worlds and disabled for event worlds.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Authorized User Domain Bypass"), I18nTooltip("Flag whether an Authorized User is allowed to bypass the domain whitelist. Typically enabled for social worlds and disabled for event worlds.")
        ]
        internal bool enableAuthUserDomainBypass = false;

        /// <summary>
        /// Flag for whether to force superusers logging level to Trace so debugging is always available in emergencies.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Authorized Users Log Trace"), I18nTooltip("Flag for whether to force superusers logging level to Trace so debugging is always available in emergencies.")
        ]
        internal bool authorizedUsersAlwaysLogTrace = true;

        /// <summary>
        /// Flag for whether to force superusers logging level to Trace so debugging is always available in emergencies.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Super Users Log Trace"), I18nTooltip("Flag for whether to force superusers logging level to Trace so debugging is always available in emergencies.")
        ]
        internal bool superUsersAlwaysLogTrace = true;

        #endregion

        #region Rendering Options

        /// <summary>
        /// A RenderTexture that will have the active manager's target texture blit-ed into it.
        /// </summary>
        [SerializeField,
         I18nInspectorName("RenderTexture Target"), I18nTooltip("A RenderTexture that will have the active manager's target texture blit-ed into it. Sometimes also referred to as the 'Video Texture'.")
        ]
        internal RenderTexture customTexture;

        /// <summary>
        /// Flag which determines if the RenderTextures should support HDR or not.
        /// Note: When HDR is enabled, the textures will take up around twice the texture memory.
        /// If a RenderTexture Target is provided for LTCGI or similar purposes, it will also increase the world size.
        /// IMPORTANT NOTE: HDR doesn't appear to be supported by AVPro or UnityVideo as of the writing this. The logic will remain for the future whenever HDR video support is added.
        /// </summary>
        [ //SerializeField,
            I18nInspectorName("Support HDR Video"), I18nTooltip("Flag which determines if the RenderTextures should support HDR or not. Be aware it will increase world size (if a RenderTexture Target is provided) and texture memory usage.")
        ]
        internal bool enableHDR = false;

        /// <summary>
        /// Flag which permits the TV to automatically resize the RenderTexture based on the source video and aspect ratio.
        /// Generally don't disable unless you NEED the RenderTexture to be a very specific size.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Allow Texture Resizing"), I18nTooltip("Flag which permits the TV to automatically resize the RenderTexture to match the source video and aspect ratio. Generally don't disable unless you NEED the RenderTexture to be a very specific size.")
        ]
        internal bool autoResizeTexture = true;

        /// <summary>
        /// The desired aspect ratio that the TV should use when updating the render texture. Set to 0 to ignore aspect adjustments.
        /// </summary>
        [SerializeField, Min(0), FormerlySerializedAs("defaultAspectRatio"),
         I18nInspectorName("Texture Aspect Ratio"), I18nTooltip("The desired aspect ratio that the TV should use when updating the render texture. Set to 0 to ignore aspect adjustments.")
        ]
        internal float targetAspectRatio = 0f;

        [SerializeField,
         I18nInspectorName("Aspect Fit Mode"), I18nTooltip("Whether to make the texture fit inside the desired aspect (letterbox/pillarbox) or to fit the outside uv space with the texture (crop fill)")
        ]
        internal TVAspectFitMode aspectFitMode = TVAspectFitMode.FIT_INSIDE;

        /// <summary>
        /// Flag whether to have the aspect ratio applied when handling the texture resizing operation.
        /// When this is active, it will force the texture's physical size to match the aspect ratio.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Apply Aspect to Resize"), I18nTooltip("Flag whether to have the aspect ratio applied when handling the texture resizing operation. When this is active, it will force the texture's physical size to match the aspect ratio.")
        ]
        public bool applyAspectToResize = false;

        /// <summary>
        /// Flag whether to have aspect ratio applied during the internal Blit operation.
        /// When this is active, the TV will attempt to ensure that the resulting video is rendered at the specified aspect ratio, regardless of Blit texture size.
        /// </summary>
        [SerializeField, FormerlySerializedAs("applyAspectRatioToRenderTexture"),
         I18nInspectorName("Bake Aspect into Texture"), I18nTooltip("Flag whether to have aspect ratio applied during the internal Blit operation. When this is active, the TV will attempt to ensure that the resulting video is rendered at the specified aspect ratio, regardless of Blit texture size.")
        ]
        public bool applyAspectToBlit = false;

        [SerializeField,
            I18nInspectorName("Crop Gamma Zone"), I18nTooltip("Whether to trim the RenderTexture Target to exclude content outside of the Custom Gamma Zone. This is useful for handling multiple video systems like LTCGI, VRSL or ShaderMotion, notably it helps prevent VRSL video data from leaking into the LTCGI rendering.")
        ]
        public bool trimToGammaZone = false;

        /// <summary>
        /// Brightness multiplier applied directly to the RenderTexture during the Blit op.
        /// </summary>
        [SerializeField, Range(0f, 8f),
         I18nInspectorName("Brightness Multiplier"), I18nTooltip("Brightness multiplier applied directly to the RenderTexture during the Blit op.")
        ]
        internal float customTextureBrightness = 1f;

        [SerializeField,
         I18nInspectorName("Anti-alias Edges"), I18nTooltip("Anti-aliasing flag for whether to fade the edges out from the video color into transparency. This setting applies to RenderTexture only. To control anti-alias on custom materials, you must update those directly.")
        ]
        internal bool fadeEdges = false;

        // This is auto-populated during the build phase, used by the RenderTexture
        [SerializeField] internal Material blitMaterial;


        /// <summary>
        /// When considering the global video texture, what calculation method should be used for tiling and offset? Normalized is standard 0-1 values. By Pixels will derive normalized values based on exact pixel values.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Texture Transform Mode"), I18nTooltip("When processing the internal blit, this setting will control what zone of the texture has the gamma/color correction applied for the AVPro video engine.")
        ]
        internal TVTextureTransformMode gammaZoneTransformMode = TVTextureTransformMode.ASIS;

        /// <summary>
        /// The offset, in pixels, from the top left of the texture.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Pixel Origin"), I18nTooltip("The offset, in pixels, from the top left of the texture.")
        ]
        internal Vector2Int gammaZonePixelOrigin = new Vector2Int(0, 0);

        /// <summary>
        /// The width and height, in pixels, that will be visible. If either axis is 0, the respective media source size will be used instead for that axis.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Pixel Size"), I18nTooltip("The width and height, in pixels, that will have gamma applied. If either axis is 0, the respective media source size will be used instead for that axis.")
        ]
        internal Vector2Int gammaZonePixelSize = new Vector2Int(0, 0);

        /// <summary>
        /// Standard tiling usage typically present in shaders.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Tiling"), I18nTooltip("Standard tiling usage typically present in shaders.")
        ]
        internal Vector2 gammaZoneTiling = new Vector2(1f, 1f);

        /// <summary>
        /// Standard offset usage typically present in shaders.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Offset"), I18nTooltip("Standard offset usage typically present in shaders.")
        ]
        internal Vector2 gammaZoneOffset = new Vector2(0f, 0f);

        /// <summary>
        /// Optional texture used during the blit operation which bakes the images into the render texture during unloaded states.
        /// This particularly helps with things like LTCGI where the lighting would normally be a blank solid color or unavailable when media was unloaded. 
        /// </summary>
        [SerializeField, FormerlySerializedAs("defaultFallbackTexture"),
         I18nInspectorName("Default Standby"), I18nTooltip("Optional textures used during the blit operation which bakes the images into the render texture. This enables standby texture support for shaders or tooling that doesn't have it built in.")
        ]
        internal Texture defaultStandbyTexture;

        /// <summary>
        /// Optional texture used during the blit operation which bakes the images into the render texture during sound-only media.
        /// This particularly helps with things like LTCGI where the lighting would normally be a blank solid color or unavailable when media was unloaded. 
        /// </summary>
        [SerializeField, FormerlySerializedAs("soundOnlyFallbackTexture"),
         I18nInspectorName("Sound Only")
        ]
        internal Texture soundOnlyTexture;

        /// <summary>
        /// Optional texture used during the blit operation which bakes the images into the render texture when a video error is encountered.
        /// This particularly helps with things like LTCGI where the lighting would normally be a blank solid color or unavailable when media was unloaded. 
        /// </summary>
        [SerializeField,
         I18nInspectorName("Video Error")
        ]
        internal Texture errorTexture;

        /// <summary>
        /// Optional texture used during the blit operation which bakes the images into the render texture while loading media, if no media is currently playing.
        /// This particularly helps with things like LTCGI where the lighting would normally be a blank solid color or unavailable when media was unloaded. 
        /// </summary>
        [SerializeField,
         I18nInspectorName("Loading Media")
        ]
        internal Texture loadingTexture;

        [SerializeField, FormerlySerializedAs("fallback3dMode"),
         I18nInspectorName("3D Mode For Standby Textures"), I18nTooltip("3D mode to use when standby texture is provided.")
        ]
        internal TV3DMode standby3dMode = TV3DMode.NONE;

        [SerializeField,
         I18nInspectorName("3D Mode Width For Standby Textures"), I18nTooltip("3D mode option for whether the standby 3D textures should be pixel perfect.")
        ]
        internal TV3DModeSize standby3dModeSize = TV3DModeSize.Half;

        [SerializeField, FormerlySerializedAs("clearOnMediaEnd"), FormerlySerializedAs("fallbackOnMediaEnd"),
         I18nInspectorName("Show Standby On Media End"), I18nTooltip("This option determines if the last frame of the media should be retained or not. If true, the blit op will not use the last frame and instead will use the fallback if it's present.")
        ]
        internal bool standbyOnMediaEnd = true;

        [SerializeField, FormerlySerializedAs("clearOnMediaPause"), FormerlySerializedAs("fallbackOnMediaPause"),
         I18nInspectorName("Show Standby On Media Pause"), I18nTooltip("This option determines if thw fallback texture should be shown while the media is paused.")
        ]
        internal bool standbyOnMediaPause = false;

        /// <summary>
        /// A custom material that will have the active manager's target texture data injected into it.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Material Targets"), I18nTooltip("Custom materials that will have the active manager's target texture data injected into it via their respective Texture Property.")
        ]
        internal Material[] customMaterials = new Material[0];

        /// <summary>
        /// The property that will have the texture assigned to for the respective custom material.
        /// </summary>
        [SerializeField,
         I18nTooltip("The property that will have the texture assigned to for the respective custom material.")
        ]
        internal string[] customMaterialProperties = new string[0];

        /// <summary>
        /// Activate to assign this TV as the one which will update the global video texture for ProTV. Only one may be active at a time. If you activate this, it will un-assign any other TVs from the global video texture.
        /// </summary>
        [SerializeField,
         I18nInspectorName("[Avatar Support] Global Video Texture"), I18nTooltip("Activate to assign this TV as the one which will update the global shader variables for ProTV. Only one may be active at a time. If you activate this, it will un-assign any other TVs from the global shader variables. This also enables the video texture support for avatar shaders.")
        ]
        public bool enableGSV = false;

        /// <summary>
        /// When considering the global video texture, what calculation method should be used for tiling and offset? Normalized is standard 0-1 values. By Pixels will derive normalized values based on exact pixel values.
        /// </summary>
        [SerializeField, FormerlySerializedAs("textureTransformMode"),
         I18nInspectorName("Texture Transform Mode"), I18nTooltip("When considering the global video texture, what calculation method should be used for tiling and offset? Normalized is standard 0-1 values. By Pixels will derive normalized values based on exact pixel values.")
        ]
        internal TVTextureTransformMode globalTextureTransformMode = TVTextureTransformMode.ASIS;

        /// <summary>
        /// The offset, in pixels, from the top left of the texture.
        /// </summary>
        [SerializeField, FormerlySerializedAs("texturePixelOrigin"),
         I18nInspectorName("Pixel Origin"), I18nTooltip("The offset, in pixels, from the top left of the texture.")
        ]
        internal Vector2Int globalTexturePixelOrigin = new Vector2Int(0, 0);

        /// <summary>
        /// The width and height, in pixels, that will be visible. If either axis is 0, the respective media source size will be used instead for that axis.
        /// </summary>
        [SerializeField, FormerlySerializedAs("texturePixelSize"),
         I18nInspectorName("Pixel Size"), I18nTooltip("The width and height, in pixels, that will be visible. If either axis is 0, the respective media source size will be used instead for that axis.")
        ]
        internal Vector2Int globalTexturePixelSize = new Vector2Int(0, 0);

        /// <summary>
        /// Standard tiling usage typically present in shaders.
        /// </summary>
        [SerializeField, FormerlySerializedAs("textureTiling"),
         I18nInspectorName("Tiling"), I18nTooltip("Standard tiling usage typically present in shaders.")
        ]
        internal Vector2 globalTextureTiling = new Vector2(1f, 1f);

        /// <summary>
        /// Standard offset usage typically present in shaders.
        /// </summary>
        [SerializeField, FormerlySerializedAs("textureOffset"),
         I18nInspectorName("Offset"), I18nTooltip("Standard offset usage typically present in shaders.")
        ]
        internal Vector2 globalTextureOffset = new Vector2(0f, 0f);

        /// <summary>
        /// This bakes the Texture Transform into a new internal texture which is then assigned to the global texture variable. This helps enforce what content is sent to avatars, but it requires an extra Blit call and another texture in memory. Just be aware of the performance impact when enabling this.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Bake Global Texture"), I18nTooltip("This bakes the Texture Transform into a new internal texture which is then assigned to the global texture variable. This helps enforce what content is sent to avatars, but it requires an extra Blit call and another texture in memory. Just be aware of the performance impact when enabling this.")
        ]
        internal bool bakeGlobalVideoTexture = false;

        /// <summary>
        /// Whether to run the pixel extraction logic via AsyncGPUReadback. When enabled you can access the most recent video pixel data via calling GetPixels on this component. NOTE: pixel extraction is triggered at the same time as the normal Blit operation. 
        /// </summary>
        [ //SerializeField,
            I18nInspectorName("Enable Pixel Extraction"), I18nTooltip("Whether to run the pixel extraction logic via AsyncGPUReadback. When enabled you can access the most recent video pixel data via calling GetPixels on this component. NOTE: pixel extraction is triggered at the same time as the normal Blit operation.")
        ]
        internal bool enablePixelExtraction = false;

        #endregion

        #region Misc Options

        /// <summary>
        /// Set this flag to have the TV auto-hide the initial video player after initialization. This is useful for preventing the video player from auto-showing itself when an autoplay video starts.
        /// </summary>
        [ //SerializeField,
            I18nInspectorName("Start Hidden"), I18nTooltip("Set this flag to have the TV auto-hide the initial video player after initialization. This is useful for preventing the video player from auto-showing itself when an autoplay video starts.")
        ]
        internal bool startHidden = false;

        /// <summary>
        /// Set this flag to have the TV auto-disable itself after initialization.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Disable TV after Initialization"), I18nTooltip("Set this flag to have the TV auto-disable itself after initialization. This flag does nothing if the TV is already disabled on start.")
        ]
        internal bool startDisabled = false;

        /// <summary>
        /// Whether to allow media to be loaded while the TV is in the hidden state.
        /// </summary>
        [ //SerializeField,
            I18nInspectorName("Stop Media When Hidden"), I18nTooltip("Whether or not to allow media to be loaded while the TV is in the hidden state.")
        ]
        internal bool stopMediaWhenHidden = false;

        /// <summary>
        /// Determine whether to stop or simply pause any active media when the TV's game object is disabled.
        /// </summary>
        [SerializeField,
         I18nInspectorName("Stop Media When TV Is Disabled"), I18nTooltip("Determine whether or not to stop or simply pause any active media when the TV's game object is disabled.")
        ]
        internal bool stopMediaWhenDisabled = false;

        [SerializeField,
         I18nInspectorName("Enable Local Pausing"), I18nTooltip("This allows the local user to pause the media locally without requiring being the owner of the TV.")
        ]
        internal bool enableLocalPause = true;

        #endregion


        // === Video Manager control ===
        // assigned when the active manager switches to the next one.
        private VPManager prevManager;

        /// <summary>
        /// main manager reference that most everything operates off of.
        /// </summary>
        /// <seealso cref="ActiveManager"/>
        private VPManager activeManager;

        // assigned when the user selects a manager to switch to.
        private VPManager nextManager;

        // === Synchronized variables and their local counterparts ===

        [UdonSynced] internal float syncTime = 0f;
        [NonSerialized] public float seekOffset = 0f;

        /// <summary>
        /// The time that the active media is currently seeked to.
        /// </summary>
        [NonSerialized] public float currentTime;

        [UdonSynced] private int lagCompSync = 0;
        private float lagComp;

        // stateSync is the value that is synced
        // stateOwner is the sync tracking counterpart (used to detect state change from the owner)
        // state is the ACTUAL state that the local video player is in.
        // stateOwner and state are separated to allow for local to not be forced into the owner's state completely
        // The primary reason for this deleniation is to allow for the local to pause without having to desync.
        // For eg: Someone isn't interested in most videos, but still wants to know what is playing, so they pause it and let it do the pausedThreshold resync (every 5 seconds)
        //      One could simply mute the video, yes, but some people might not want the distraction of an active video playing if they happen to be in front of a mirror
        //      where the TV is reflected. This allows a much more comfortable "keep track of" mode for those users.
        [NonSerialized] internal TVPlayState syncState = TVPlayState.WAITING;

        /// <summary>
        /// Play state of the user who is currently in charge of syncing the TV's data
        /// </summary>
        [NonSerialized] public TVPlayState stateOwner = TVPlayState.WAITING;

        /// <summary>
        /// Play state of the local user.
        /// </summary>
        [NonSerialized] public TVPlayState state = TVPlayState.WAITING;


        [NonSerialized] internal TVErrorState syncErrorState = TVErrorState.NONE;

        /// <summary>
        /// Error state of the user who is currently in charge of syncing the TV's data
        /// </summary>
        [NonSerialized] public TVErrorState errorStateOwner = TVErrorState.NONE;

        /// <summary>
        /// Error state of the local user.
        /// </summary>
        [NonSerialized] public TVErrorState errorState = TVErrorState.NONE;

        /// <summary>
        /// The currently active URL being played.
        /// </summary>
        [NonSerialized] public VRCUrl url = VRCUrl.Empty;

        /// <summary>
        /// The local copy of the current main URL
        /// </summary>
        [NonSerialized] public VRCUrl urlMain = VRCUrl.Empty;

        [NonSerialized] internal VRCUrl syncUrlMain = VRCUrl.Empty;

        /// <summary>
        /// The local copy of the current alternate URL
        /// </summary>
        [NonSerialized] public VRCUrl urlAlt = VRCUrl.Empty;

        [NonSerialized] internal VRCUrl syncUrlAlt = VRCUrl.Empty;


        /// <summary>
        /// A miscellaneous string that is used to describe the current video. 
        /// Allows for different plugins to share things like custom video titles in a centralized way.
        /// Is automatically by default the current URL's full domain name.
        /// if this property is set to a new value via SetProgramVariable (eg: from another udon script), the _TvTitleChange event will fire for all subscribers.
        /// </summary>
        [NonSerialized, FieldChangeCallback(nameof(_title))]
        public string title = EMPTYSTR;

        private string _title
        {
            get => title;
            set
            {
                title = value;
                SendManagedVariable(nameof(TVPlugin.OUT_TITLE), title);
                SendManagedEvent(nameof(TVPlugin._TvTitleChange));
            }
        }

        internal string syncTitle = EMPTYSTR;
        internal string lastTitle = EMPTYSTR;

        [NonSerialized] public string addedBy = EMPTYSTR;
        internal string syncAddedBy = EMPTYSTR;

        /// <summary>
        /// The flag which determines whether to use the main url or alternate url.
        /// </summary>
        [NonSerialized] public bool useAlternateUrl = false;

        internal bool syncLocked = false;

        /// <summary>
        /// Whether a privileged user has engaged the lock on the TV.
        /// </summary>
        [NonSerialized] public bool locked = false;

        internal int syncUrlRevision;
        internal int urlRevision;
        internal int syncVideoPlayer = -1;
        internal float syncVolume = 0;
        internal bool syncAudio3d = true;
        internal TV3DMode syncVideo3d = 0;
        internal bool syncVideo3dFull = false;
        internal int syncLoop = 0;
        internal string currentOwner = EMPTYSTR;
        internal string currentMaster = EMPTYSTR;
        internal string firstMaster = EMPTYSTR;
        internal string instanceOwner = EMPTYSTR;

        internal float playbackSpeed = 1f;
        internal float syncPlaybackSpeed = 1f;

        /// <summary>
        /// The video manager index of which VPManager is currently active. Typically 0 but can be another number if multiple VPManagers are available.
        /// Use in conjunction with the <see cref="videoManagers"/> array.
        /// </summary>
        [NonSerialized] public int videoPlayer = -1;

        /// <summary>
        /// Simple flag on whether or not media is attempting to be loaded locally.
        /// </summary>
        [NonSerialized] public bool loading;

        /// <summary>
        /// Flag of whether the TV owner is currently in a loading state or not.
        /// </summary>
        [NonSerialized] internal bool syncLoading;

        // === Fields for tracking internal state ===
        /// <summary>
        /// The timestamp of the source media which the TV begins playing from.
        /// </summary>
        [NonSerialized] public float startTime;

        /// <summary>
        /// The timestamp of the source media which the TV stops playing at.
        /// </summary>
        [NonSerialized] public float endTime;

        /// <summary>
        /// The actual length of the source media. This can be a value greater than endTime if the URL uses the time slicing parameters.
        /// </summary>
        [NonSerialized] public float mediaLength;

        /// <summary>
        /// The amount of the time that the video is set to play for (basically endTime minus startTime)
        /// </summary>
        [NonSerialized] public float videoDuration;

        /// <summary>
        /// Flag notifying that a media reload has been triggered, but it was a delayed trigger.
        /// Works in tandem with the <see cref="waitUntil"/> value.
        /// </summary>
        [NonSerialized] public bool waitingForMediaRefresh;

        /// <summary>
        /// Flag which specifies if the owner has some sort of issue on their end causing syncing to potentially break.
        /// As soon as the owner is able to fix the issues, this flag will be cleared.
        /// </summary>
        [NonSerialized] public bool ownerDisabled = true;

        /// <summary>
        /// The number of replays of the current URL. If the value is -1, it will repeat the media until another URL is manually triggered.
        /// </summary>
        [NonSerialized] public int loop;

        /// <summary>
        /// Whether or not the TV has been muted.
        /// </summary>
        [NonSerialized] public bool mute;

        /// <summary>
        /// Whether or not to attempt making the attached AudioSources into 2D (headphone-style) or 3D (speaker-style)
        /// </summary>
        [NonSerialized] public bool audio3d = true;

        /// <summary>
        /// The internal volume value of the TV
        /// </summary>
        [NonSerialized] public float volume = 0.5f;

        /// <summary>
        /// Flag on whether the current media has been detected as a livestream. 
        /// </summary>
        [NonSerialized] public bool isLive = false;

        /// <summary>
        /// Flag for whether the currently loading media is expected to be a livestream (either detected via protocol or known stream host domain).
        /// This separate value is used to avoid polluting the data related to the CURRENTLY loaded media.
        /// </summary>
        [NonSerialized] public bool isNextLive = false;

        /// <summary>
        /// Value representing which 3D mode is active.
        /// 0 = Not 3D (NONE)
        /// 1 = Side by Side (SBS)
        /// 2 = Side by Side Swapped
        /// 3 = Over / Under (OVUN) [left eye is on top]
        /// 4 = Over / Under Swapped [left eye is on bottom]
        /// </summary>
        [NonSerialized] public TV3DMode video3d = TV3DMode.NONE;

        /// <summary>
        /// Flag that tells shaders if they should treat the video as a double-width video.
        /// This means that each eye is pixel perfect rather than stretched.
        /// </summary>
        [NonSerialized] public bool video3dFull = false;

        /// <summary>
        /// Value representing the aspect ratio adjustment desired.
        /// Is the float value of the expected width / height of the video.
        /// </summary>
        [NonSerialized] public float aspectRatio = 0f;

        /// <summary>
        /// Value representing the aspect ratio adjustment desired.
        /// Is the float value of the expected width / height of the video.
        /// </summary>
        [NonSerialized] public bool force2D = false;

        /// <summary>
        /// Flag for whether to apply a gamma correction to AVPro or not.
        /// Genearlly should be left to false unless the user is on an AMD GPU with software encoding enabled.
        /// </summary>
        [NonSerialized] public bool skipGamma = false;

        /// <summary>
        /// This flag is set to true once the TV has finished all it's initialization and is ready to have media play on it.
        /// </summary>
        [NonSerialized] public bool isReady = false;

        /// <summary>
        /// This is a status flag which specifies if the TV is currently is a retry loop.
        /// This is generally due to some video player error occurring.
        /// </summary>
        [NonSerialized] public bool retrying = false;


        /// <summary>
        /// The list of custom user-provided parameter keys from the given url.
        /// This is treated as a tuple in conjunction with <see cref="urlParamValues"/>.
        /// This is specifically the 'keys' list. The corresponding 'values' list is the <see cref="urlParamValues"/>.
        /// </summary>
        [NonSerialized] public string[] urlParamKeys = new string[0];

        /// <summary>
        /// The list of custom user-provided parameter values from the given url.
        /// This is treated as a tuple in conjunction with <see cref="urlParamKeys"/>.
        /// This is specifically the 'values' list. The corresponding 'keys' list is the <see cref="urlParamKeys"/>.
        /// </summary>
        [NonSerialized] public string[] urlParamValues = new string[0];

        /// <summary>
        /// The domain name for the currently active url
        /// </summary>
        [NonSerialized] public string urlDomain = EMPTYSTR;

        /// <summary>
        /// The protocol for the currently active url
        /// </summary>
        [NonSerialized] public string urlProtocol = EMPTYSTR;

        #region Internal Variables

        /// <summary>
        /// Time delay before allowing the TV to update it's active video
        /// This value is always assigned as: Time.timeSinceLevelLoad + someOffsetValue;
        /// It is checked using this structure: <code>if (Time.timeSinceLevelLoad &lt; waitUntil) { waitIsOver(); }</code>
        /// </summary>
        private float waitUntil = 0f;

        /// <summary>
        /// Time to seek to at time sync check.
        /// This value is set for a couple different reasons.
        /// If the video player is switching locally to a different player, it will use Mathf.Epsilon to signal seemless seek time for the player being swapped to.
        /// If the video URL contains a t= or start= hash params, it will assign that value so to start the video at that point once it's loaded.
        /// </summary>
        private float jumpToTime = 0f;

        private float lastJumpToTime = 0f;

        /// <summary>
        /// This flag simply enables the local player to be paused without forcing hard-sync to the owner's state.
        /// This results in a pause that, when the owner pauses then plays, it won't foroce the local player to unpause unintentionally.
        /// This flag cooperates with the pausedThreshold constant to enable resyncing every 5 seconds without actually having the video playing.
        /// </summary>
        private bool locallyPaused = false;
        private bool locallyStopped = false;
        private bool enforceSyncTime = false;
        private float syncEnforceWait;
        private float loadingWait;
        private bool loadingCatchVideoEndEvent;
        private bool waitForTime;
        private float timeToWaitFor;
        private float liveReloadTimestamp;
        private float autoSyncWait;
        private bool manuallyHidden = false;
        private bool buffering = false;
        private const float syncEnforcementTimeLimit = 3f;
        private float _reloadTime = -1f;
        private float _reloadCache = -1f;
        private int mediaHash;
        private bool _mediaIsStale;
        private bool forceRestartMedia = false;
        private bool videoTextureWasEnabled;
        private float nextUrlAttemptAllowed;
        private int retryCount = 0;
        private bool retryingWithAlt = false;
        private bool retriedWithAlt = false;
        private bool manualLoop = false;
        private bool mediaEnded = true;
        private bool playbackEnabled = false;
        private bool lockedBySuper = false;
        private bool interactionState = true;
        private bool firstDeserialization = true;
        private Color32[] pixels = new Color32[0];

        /// <seealso cref="updateShaderData"/>
        internal Matrix4x4 shaderVideoData = Matrix4x4.zero;

        internal bool disableVideo = false;
        internal bool disableStandby = false;
        private readonly VRCUrl EMPTYURL = VRCUrl.Empty;
        [SerializeField] internal string versionNumber;
        [SerializeField] internal bool gsvfixcheck;
        [SerializeField] private int updateIntervalFrames = 1;

        private bool forceBlitOnce = true;
        private bool deserializationDelayedByLoadingState = false;
        private bool disabled = false;
        private bool dataSyncFailed = false;

        private readonly string[] liveProtocols = { "rtsp", "rtspu", "rtspt" };
        private readonly string[] liveDomains = { "stream.vrcdn.live", "www.twitch.tv", "twitch.tv" };

        #endregion

        #region Data Getters

        /// <summary>
        /// Provides the currently running VPManager. <br/>
        /// when video player is switching (as denoted by the epsilon jump time), use the prevManager reference.
        /// this ensures that any action that might occur during a swap affects only the currently playing manager
        /// then once the swap is complete, the EPSILON trigger is cleared returning to the active manager target.
        /// </summary>
        public VPManager ActiveManager => (jumpToTime == EPSILON ? prevManager : activeManager) ?? nextManager;

        public VPManager NextManager => nextManager;

        /// <summary>
        /// This returns whether the TV is in the process of handling a media state change.
        /// If the TV is loading or is waiting to trigger a reload, it will return true.
        /// </summary>
        public bool IsLoadingMedia => loading || waitingForMediaRefresh;

        /// <summary>
        /// This check is for determining if the TV has ever loaded media before.
        /// Generally used for new-instance initialization of media, such as auto-play or queue pre-loading. 
        /// </summary>
        public bool IsWaitingForMedia => syncUrlRevision == 0 && urlRevision == 0;

        /// <summary>
        /// The normalized (0.0 to 1.0) value of the currently playing seek time.
        /// Takes into account any time slicing applied from the URL.
        /// This will return 1.0 if the current media is detected to be a livestream.
        /// </summary>
        public float SeekPercent
        {
            get
            {
                if (isLive || videoDuration == 0 || currentTime + 0.1f >= endTime) return 1f;
                if (currentTime <= startTime) return 0f;
                return (currentTime - startTime) / videoDuration;
            }
        }

        /// <summary>
        /// Get the current VPManager's playback speed.
        /// </summary>
        public float PlaybackSpeed => ActiveManager.playbackSpeed;

        /// <summary>
        /// Getter for the amount of failure retries remaining to be attempted for the current url.
        /// </summary>
        public int RetryCount => retryCount;

        /// <summary>
        /// Getter for retrieving the volume the TV initializes with
        /// </summary>
        public float DefaultVolume => defaultVolume;

        /// <summary>
        /// Getter that checks if the TV itself is stopped
        /// </summary>
        public bool IsStopped => state == TVPlayState.STOPPED || state == TVPlayState.WAITING;

        /// <summary>
        /// Getter that checks if the TV itself is playing
        /// </summary>
        public bool IsPlaying => state == TVPlayState.PLAYING;

        /// <summary>
        /// Getter that checks if the TV itself is paused
        /// </summary>
        public bool IsPaused => state == TVPlayState.PAUSED;

        /// <summary>
        /// Getter that returns whether the TV sees the media as having reached the end.
        /// </summary>
        /// <seealso cref="IsSkipping"/>
        public bool IsEnded => mediaEnded;

        /// <summary>
        /// Getter that returns if the TV is skipping the current entry.
        /// This value is deterministically only true during the _TVMediaEnd event.
        /// All other times the value is considered non-deterministic (typically false).
        /// </summary>
        /// <seealso cref="IsEnded"/>
        public bool IsSkipping => !mediaEnded && !isLive && currentTime + 0.1f >= endTime;

        /// <summary>
        /// Getter that specifies if the most recent media loop was triggered by a user or automatically.
        /// </summary>
        public bool IsManualLoop => manualLoop;

        /// <summary>
        /// Getter that checks if the owner of the TV is stopped
        /// </summary>
        public bool IsOwnerStopped => stateOwner == TVPlayState.STOPPED;

        /// <summary>
        /// Getter that checks if the owner of the TV is playing
        /// </summary>
        public bool IsOwnerPlaying => stateOwner == TVPlayState.PLAYING;

        /// <summary>
        /// Getter that checks if the owner of the TV is paused
        /// </summary>
        public bool IsOwnerPaused => stateOwner == TVPlayState.PAUSED;

        /// <summary>
        /// Getter that checks if the TV is in a locked state
        /// </summary>
        public bool IsLocked => locked;

        /// <summary>
        /// Getter that checks if the TV is in a locked state triggered by a Super User
        /// </summary>
        public bool IsLockedBySuper => lockedBySuper;

        /// <summary>
        /// Getter to detect when the first media loaded by the TV matches the TV's core autoplay url.
        /// Can be used by plugins to know when the TV implicitly played media.
        /// </summary>
        public bool IsInitialAutoplay => urlRevision == 1 && urlMain.Get() == autoplayMainUrl.Get();

        // AVPro has a playback speed defect. Disable for AVPro until it's fixed.
        // If user sync is active, disable playback speed when video manager selection is not synced as well to avoid disparities between clients
        /// <summary>
        /// Flag check for if the user is an owner if video manager selection sync is enforced.
        /// </summary>
        public bool CanModifySyncVPManagerData
        {
            get
            {
                // if (IsTraceEnabled) Trace($"CanModifySyncVPManagerData: nosync {!syncToOwner} || noVPsync {!syncVideoManagerSelection} || owner {IsOwner}");
                return !syncToOwner || !syncVideoManagerSelection || IsOwner;
            }
        }

        /// <summary>
        /// Check for if the TV's post-media-load buffer period is active.
        /// </summary>
        public bool IsBuffering => loading && buffering;

        /// <summary>
        /// Texture used as the fallback when no other textures are in use. Optional.
        /// </summary>
        public Texture StandbyTexture
        {
            get => defaultStandbyTexture;
            set => defaultStandbyTexture = value;
        }

        /// <summary>
        /// Texture shown when audio is present without video in the currently playing media. Optional.
        /// </summary>
        public Texture SoundOnlyTexture
        {
            get => soundOnlyTexture;
            set => soundOnlyTexture = value;
        }

        /// <summary>
        /// Texture shown when media is loading. Optional.
        /// </summary>
        public Texture LoadingTexture
        {
            get => loadingTexture;
            set => loadingTexture = value;
        }

        /// <summary>
        /// Texture shown when media has failed and produced an error. Optional.
        /// </summary>
        public Texture VideoErrorTexture
        {
            get => errorTexture;
            set => errorTexture = value;
        }

        /// <summary>
        /// Texture that comes directly from the video engine.
        /// Might also be a standby image if conditions are met.
        /// This texture has no corrections applied, so at minimum you must use
        /// the corresponding SourceTextureST value to correct any texture orientation issues.
        /// </summary>
        public Texture SourceTexture => _sourceTexture;

        /// <summary>
        /// Corresponding tiling/offset values for the SourceTexture.
        /// </summary>
        public Vector4 SourceTextureST => _sourceTextureST;

        /// <summary>
        /// Texture that has only video engine corrections applied.
        /// This texture is provided to all Material Targets and is used to derive any
        /// additional textures, such as RenderTexture Target and the global avatar texture.
        /// This texture does not modify anything related to 3D as shaders are what handles 3D display.
        /// </summary>
        public RenderTexture InternalTexture => _internalTexture;

        /// <summary>
        /// Texture that is explicitly provided by the creator to provide a texture to reference throughout the scene.
        /// This texture will strip the right eye of a 3D video
        /// This is for the use of integrating with third party tools like LTCGI and AreaLit.
        /// </summary>
        public RenderTexture CustomTexture => customTexture;

        /// <summary>
        /// Optional texture which will contain the final baked render of the
        /// internal texture with a custom texture transform zone applied.
        /// This is the least commonly used texture of all ProTV textures.
        /// If global texture baking is disabled, it'll just return the internal texture.
        /// </summary>
        public RenderTexture GlobalTexture => _globalTexture != null ? _globalTexture : _internalTexture;

        #endregion

        #region Property Proxies (get/set only)

        public float BufferDelayAfterLoad
        {
            get => bufferDelayAfterLoad;
            set => bufferDelayAfterLoad = value;
        }

        #endregion
    }
}