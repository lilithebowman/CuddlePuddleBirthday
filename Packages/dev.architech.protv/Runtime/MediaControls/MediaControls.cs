using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public partial class MediaControls : TVPlugin
    {
        public override sbyte Priority => 2;

        public Queue queue;

        [SerializeField, FormerlySerializedAs("showVideoOwner"),
         I18nInspectorName("Show Media Owner"), I18nTooltip("Flag for whether to prefix the title display with the media owner's name.")
        ]
        internal bool showMediaOwner = true;

        [SerializeField,
         I18nInspectorName("Show Remaining Time"), I18nTooltip("Flag for whether to default to the remaining time as the current time text (represented by currentTime - totalTime)")
        ]
        internal bool showRemainingTime = false;

        [SerializeField,
         I18nInspectorName("Realtime Seek"), I18nTooltip("Whether the seekbar should update every second (disabled) or every frame (enabled)")
        ]
        internal bool realtimeSeek = false;

        [SerializeField,
         I18nInspectorName("Keep Inputs Text"), I18nTooltip("Should the input fields keep their contents when sending the inputs to the TV?")
        ]
        internal bool retainInputText = false;

        [SerializeField,
         I18nInspectorName("Empty Title Placeholder"), I18nTooltip("The text that is displayed when the title info is empty. If this is left empty, it will default to the domain of the current URL.")
        ]
        internal string emptyTitlePlaceholder = "";

        [SerializeField, FormerlySerializedAs("pcUrlInput")]
        internal VRCUrlInputField mainUrlInput;

        [SerializeField, FormerlySerializedAs("questUrlInput"), FormerlySerializedAs("altUrlInput")]
        internal VRCUrlInputField alternateUrlInput;

        [SerializeField] internal InputField titleInput;

        [SerializeField, FormerlySerializedAs("activateUrls")]
        internal Button sendInputs;

        [SerializeField] internal Toggle urlSwitch;
        [SerializeField] internal Button play;
        [SerializeField] internal Button pause;
        [SerializeField] internal Button stop;
        [SerializeField] internal Button skip;
        [SerializeField] internal Button resync;
        [SerializeField] internal Button reload;
        [SerializeField] internal Button audioMode;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image audioModeIndicator;

        [SerializeField] internal Sprite audio3d;
        [SerializeField] internal Sprite audio2d;
        [SerializeField] internal Color audio3dColor = Color.white;
        [SerializeField] internal Color audio2dColor = Color.white;
        [SerializeField] internal Button mute;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image muteIndicator;

        [SerializeField] internal Sprite muted;
        [SerializeField] internal Sprite unmuted;
        [SerializeField] internal Color mutedColor = Color.white;
        [SerializeField] internal Color unmutedColor = Color.white;
        [SerializeField] internal Button colorSpaceCorrection;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image colorSpaceCorrectionIndicator;

        [SerializeField,
         I18nInspectorName("Color Corrected")
        ]
        internal Sprite colorSpaceCorrected;

        [SerializeField,
         I18nInspectorName("Color Raw")
        ]
        internal Sprite colorSpaceRaw;

        [SerializeField] internal Color colorSpaceCorrectedColor = Color.white;
        [SerializeField] internal Color colorSpaceRawColor = Color.white;

        [SerializeField, FormerlySerializedAs("masterLock")]
        internal Button tvLock;

        [SerializeField, FormerlySerializedAs("masterLockIndicator"),
         I18nInspectorName("Icon Display")
        ]
        internal Image tvLockIndicator;

        [SerializeField, FormerlySerializedAs("lockedIcon")]
        internal Sprite locked;

        [SerializeField, FormerlySerializedAs("unlockedIcon")]
        internal Sprite unlocked;

        [SerializeField] internal Color lockedColor = Color.HSVToRGB(0f, 0.75f, 0.75f);
        [SerializeField] internal Color unlockedColor = Color.white;

        [SerializeField] internal Slider volume;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image volumeIndicator;

        [SerializeField] internal Sprite volumeHigh;
        [SerializeField] internal Sprite volumeMed;
        [SerializeField] internal Sprite volumeLow;
        [SerializeField] internal Sprite volumeOff;
        [SerializeField] internal Button syncMode;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image syncModeIndicator;

        [SerializeField, FormerlySerializedAs("syncEnforced")]
        internal Sprite syncEnabled;

        [SerializeField, FormerlySerializedAs("localOnly")]
        internal Sprite syncDisabled;

        [SerializeField] internal Color syncEnabledColor = Color.white;
        [SerializeField] internal Color syncDisabledColor = Color.white;

        [SerializeField] internal Button loopMode;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image loopModeIndicator;

        [SerializeField, FormerlySerializedAs("loopStart"),
         I18nInspectorName("Loop Enabled")
        ]
        internal Sprite loopEnabled;

        [SerializeField, FormerlySerializedAs("loopStop"),
         I18nInspectorName("Loop Disabled")
        ]
        internal Sprite loopDisabled;

        [SerializeField] internal Color loopEnabledColor = Color.white;
        [SerializeField] internal Color loopDisabledColor = Color.white;

        [SerializeField] internal Slider seek;
        [SerializeField] internal Slider seekOffset;
        [SerializeField] internal Text seekOffsetDisplay;
        [SerializeField] internal TextMeshProUGUI seekOffsetDisplayTMP;

        [SerializeField] internal Slider playbackSpeed;

        [SerializeField, FormerlySerializedAs("currentTime"),
         I18nInspectorName("Current Time Display")
        ]
        internal Text currentTimeDisplay;

        [SerializeField, FormerlySerializedAs("currentTimeTMP")]
        internal TextMeshProUGUI currentTimeDisplayTMP;

        [SerializeField, FormerlySerializedAs("endTime"),
         I18nInspectorName("End Time Display")
        ]
        internal Text endTimeDisplay;

        [SerializeField, FormerlySerializedAs("endTimeTMP")]
        internal TextMeshProUGUI endTimeDisplayTMP;

        [SerializeField] internal Slider loadingBar;
        [SerializeField] internal Transform loadingSpinner;
        [SerializeField] internal GameObject loadingSpinnerContainer;

        [SerializeField, FormerlySerializedAs("loadingSpinReverese")]
        internal bool loadingSpinReverse;

        [SerializeField] internal float loadingSpinSpeed = 1f;
        [SerializeField] internal Dropdown videoPlayerSwap;

        [SerializeField,
         I18nInspectorName("Use TMP")
        ]
        internal bool videoPlayerSwapUseTMP;

        [SerializeField] internal Dropdown mode3dSwap;

        [SerializeField,
         I18nInspectorName("Use TMP")
        ]
        internal bool mode3dSwapUseTMP;

        [SerializeField] internal Button width3dMode;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image width3dModeIndicator;

        [SerializeField,
         I18nInspectorName("Half-Width 3d")
        ]
        internal Sprite width3dHalf;

        [SerializeField,
         I18nInspectorName("Full-Width 3d")
        ]
        internal Sprite width3dFull;

        [SerializeField] internal Color width3dHalfColor = Color.white;
        [SerializeField] internal Color width3dFullColor = Color.white;

        [SerializeField, FormerlySerializedAs("info")]
        internal Text infoDisplay;

        [SerializeField, FormerlySerializedAs("infoTMP")]
        internal TextMeshProUGUI infoDisplayTMP;

        [SerializeField, FormerlySerializedAs("localTime"),
         I18nInspectorName("Clock Time Display")
        ]
        internal Text clockTimeDisplay;

        [SerializeField, FormerlySerializedAs("localTimeTMP")]
        internal TextMeshProUGUI clockTimeDisplayTMP;

        [SerializeField,
         I18nInspectorName("Main URL Default"), I18nTooltip("The text that the main url input field resets to after the inputs have been sent.")
        ]
        internal VRCUrl mainUrlDefault = new VRCUrl("");

        [SerializeField,
         I18nInspectorName("Alternate URL Default"), I18nTooltip("The text that the alternate url input field resets to after the inputs have been sent.")
        ]
        internal VRCUrl alternateUrlDefault = new VRCUrl("");

        [SerializeField,
         I18nInspectorName("Title Default"), I18nTooltip("The text that the title input field resets to after the inputs have been sent.")
        ]
        internal string titleDefault = EMPTYSTR;


        private bool MultipleInputs
        {
            get
            {
                byte inputs = 0;
                if (hasMainUrlInput && mainUrlInput.IsActive()) inputs++;
                if (hasAltUrlInput && alternateUrlInput.IsActive()) inputs++;
                if (hasTitleInput && titleInput.IsActive()) inputs++;
                return inputs > 1;
            }
        }

        // boolean checks for the existence of the various public fields
        private bool hasQueue;
        private bool hasMainUrlInput;
        private bool hasAltUrlInput;
        private bool hasTitleInput;
        private bool hasMutlipleInputs;
        private bool hasUrlSwitch;
        private bool hasSendInputs;
        private bool hasPlay;
        private bool hasPause;
        private bool hasStop;
        private bool hasSkip;
        private bool hasResync;
        private bool hasReload;
        private bool hasSyncMode;
        private bool hasSyncModeIndicator;
        private bool hasLoopMode;
        private bool hasLoopModeIndicator;
        private bool hasAudioMode;
        private bool hasAudioModeIndicator;
        private bool hasMute;
        private bool hasMuteIndicator;
        private bool hasGammaCorrection;
        private bool hasColorCorrectionIndicator;
        private bool hasTvLock;
        private bool hasTvLockIndicator;
        private bool hasSeek;
        private bool hasSeekOffset;
        private bool hasSeekOffsetDisplay;
        private bool hasSeekOffsetDisplayTMP;
        private bool hasPlaybackSpeed;
        private bool hasVolume;
        private bool hasVolumeIndicator;
        private bool hasLoadingBar;
        private bool hasLoadingSpinner;
        private bool hasVideoPlayerSwap;
        private bool hasMode3dSwap;
        private bool has3dWidthMode;
        private bool has3dWidthModeIndicator;
        private bool hasInfo;
        private bool hasInfoTMP;
        private bool hasCurrentTime;
        private bool hasCurrentTimeTMP;
        private bool hasEndTimeDisplay;
        private bool hasEndTimeDisplayTMP;
        private bool hasClockTimeDisplay;
        private bool hasClockTimeDisplayTMP;

        private float loadingBarDamp = 0f;
        private float startTime = 0f;
        private float endTime = 0f;
        private float duration = 0f;
        private bool isLive = true;
        private bool isLoading = false;
        private bool isLocked = false;
        private bool checkForDrag = false;
        private bool suppressVolume = false;
        private bool suppressSeek = false;
        private bool suppressSeekOffset = false;
        private bool suppressSpeed = false;

        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#fc7bcc");
            base.Start();
            if (!hasTV) return;

            hasQueue = queue != null;
            hasMainUrlInput = mainUrlInput != null;
            hasAltUrlInput = alternateUrlInput != null;
            hasTitleInput = titleInput != null;
            hasUrlSwitch = urlSwitch != null;
            hasSendInputs = sendInputs != null;
            hasPlay = play != null;
            hasPause = pause != null;
            hasStop = stop != null;
            hasSkip = skip != null;
            hasResync = resync != null;
            hasReload = reload != null;
            hasAudioMode = audioMode != null;
            hasMute = mute != null;
            hasGammaCorrection = colorSpaceCorrection != null;
            hasTvLock = tvLock != null;
            hasSeek = seek != null;
            hasSeekOffset = seekOffset != null;
            hasSeekOffsetDisplay = seekOffsetDisplay != null;
            hasSeekOffsetDisplayTMP = seekOffsetDisplayTMP != null;
            hasPlaybackSpeed = playbackSpeed != null;
            hasSyncMode = syncMode != null;
            hasLoopMode = loopMode != null;
            hasVolume = volume != null;
            hasLoadingBar = loadingBar != null;
            hasLoadingSpinner = loadingSpinner != null;
            hasVideoPlayerSwap = videoPlayerSwap != null;
            hasMode3dSwap = mode3dSwap != null;
            has3dWidthMode = width3dMode != null;
            hasInfo = infoDisplay != null;
            hasInfoTMP = infoDisplayTMP != null;
            hasCurrentTime = currentTimeDisplay != null;
            hasCurrentTimeTMP = currentTimeDisplayTMP != null;
            hasEndTimeDisplay = endTimeDisplay != null;
            hasEndTimeDisplayTMP = endTimeDisplayTMP != null;
            hasClockTimeDisplay = clockTimeDisplay != null;
            hasClockTimeDisplayTMP = clockTimeDisplayTMP != null;

            int count = 0;
            if (hasMainUrlInput)
            {
                count++;
                mainUrlInput.SetUrl(mainUrlDefault);
            }

            if (hasAltUrlInput)
            {
                count++;
                alternateUrlInput.SetUrl(alternateUrlDefault);
            }

            if (hasTitleInput)
            {
                count++;
                titleInput.text = titleDefault;
            }

            hasMutlipleInputs = count > 1;

            // hide the go button until text is entered into the input field
            if (hasSendInputs) sendInputs.gameObject.SetActive(false);

            if (hasMute)
            {
                if (muteIndicator == null) muteIndicator = mute.image;
                hasMuteIndicator = muteIndicator != null;
            }

            if (hasGammaCorrection)
            {
                if (colorSpaceCorrectionIndicator == null) colorSpaceCorrectionIndicator = colorSpaceCorrection.image;
                hasColorCorrectionIndicator = colorSpaceCorrectionIndicator != null;
            }

            if (hasAudioMode)
            {
                if (audioModeIndicator == null) audioModeIndicator = audioMode.image;
                hasAudioModeIndicator = audioModeIndicator != null;
            }

            if (has3dWidthMode)
            {
                if (width3dModeIndicator == null) width3dModeIndicator = width3dMode.image;
                has3dWidthModeIndicator = width3dModeIndicator != null;
            }

            if (hasVolume)
            {
                if (volumeIndicator == null)
                {
                    // volume expects the structure of a default Unity UI slider
                    var imgs = volume.handleRect.GetComponentsInChildren<Image>();
                    foreach (Image img in imgs)
                    {
                        if (volumeIndicator == null) volumeIndicator = img;
                        else if (img.name == "Fill") volumeIndicator = img;
                    }
                }

                hasVolumeIndicator = volumeIndicator != null;
            }

            if (hasTvLock)
            {
                if (tvLockIndicator == null) tvLockIndicator = tvLock.image;
                hasTvLockIndicator = tvLockIndicator != null;
            }

            if (hasLoadingBar) loadingBar.gameObject.SetActive(false);
            if (hasLoadingSpinner)
            {
                if (loadingSpinnerContainer == null)
                    loadingSpinnerContainer = loadingSpinner.gameObject;
            }

            if (hasSeek)
            {
                // cheat cause unity UI is stupid
                hasSeek = false;
                seek.minValue = 0f;
                seek.maxValue = 1f;
                seek.SetValueWithoutNotify(1f);
                seek.interactable = false;
                hasSeek = true;
            }

            if (hasSeekOffset)
            {
                // cheat cause unity UI is stupid
                hasSeekOffset = false;
                seekOffset.minValue = -3f;
                seekOffset.maxValue = 3f;
                seekOffset.SetValueWithoutNotify(0f);
                hasSeekOffset = true;
            }

            if (hasPlaybackSpeed)
            {
                // cheat cause unity UI is stupid
                hasPlaybackSpeed = false;
                playbackSpeed.minValue = 0.5f;
                playbackSpeed.maxValue = 2f;
                playbackSpeed.SetValueWithoutNotify(1f);
                hasPlaybackSpeed = true;
            }

            if (hasSyncMode)
            {
                if (syncModeIndicator == null) syncModeIndicator = syncMode.image;
                hasSyncModeIndicator = syncModeIndicator != null;
            }

            if (hasLoopMode)
            {
                if (loopModeIndicator == null) loopModeIndicator = loopMode.image;
                hasLoopModeIndicator = loopModeIndicator != null;
            }

            if (hasClockTimeDisplay || hasClockTimeDisplayTMP) SendCustomEventDelayedSeconds(nameof(UpdateClock), 1f);
        }


        public void UpdateLoading()
        {
            if (isLoading)
            {
                SendCustomEventDelayedFrames(nameof(UpdateLoading), 1);
                // rotate the spinner while loading a url
                if (hasLoadingSpinner)
                {
                    int dir = loadingSpinReverse ? -1 : 1;
                    loadingSpinner.Rotate(0f, 0f, (-200f * Time.deltaTime * loadingSpinSpeed * dir) % 360f);
                }

                if (hasLoadingBar)
                {
                    // Loading bar "animation"
                    if (loadingBar.value > 0.95f) return;
                    loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, loadingBar.value > 0.8f ? 0.4f : 0.3f);
                }
            }
        }

        public void UpdateSeek()
        {
            // Seek only needs to update once a second
            SendCustomEventDelayedSeconds(nameof(UpdateSeek), realtimeSeek ? 0f : 1f);
            if (suppressSeek) return;
            float timestamp = tv.currentTime;
            updateCurrentTime(timestamp - startTime);
            if (hasSeek)
            {
                if (isLive || duration == 0) { }
                else seek.SetValueWithoutNotify(timestamp + tv.seekOffset); // normalize times to the range of start and end times.
            }
        }

        public void UpdateClock()
        {
            SendCustomEventDelayedSeconds(nameof(UpdateClock), 1f);
            var time = DateTime.Now.ToLongTimeString();
            if (hasClockTimeDisplay) clockTimeDisplay.text = time;
            if (hasClockTimeDisplayTMP) clockTimeDisplayTMP.text = time;
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (value && !checkForDrag) checkForDrag = true;
            else if (!value && checkForDrag)
            {
                checkForDrag = false;
                if (suppressVolume)
                {
                    suppressVolume = false;
                    ChangeVolume();
                }

                if (suppressSeek)
                {
                    suppressSeek = false;
                    ChangeSeek();
                }

                if (suppressSeekOffset)
                {
                    suppressSeekOffset = false;
                    ChangeSeekOffset();
                }

                if (suppressSpeed)
                {
                    suppressSpeed = false;
                    ChangePlaybackSpeed();
                }
            }
        }

        [PublicAPI]
        public void SetQueue(Queue plugin)
        {
            queue = plugin;
            hasQueue = queue != null;
        }

        // =============== UI EVENTS ===================

        #region UI EVENTS

        public void UpdateUrlInput()
        {
            if (hasSendInputs && !tv.IsLoadingMedia)
            {
                bool showGo = false;
                if (hasMainUrlInput)
                    if (mainUrlInput.IsActive() && mainUrlInput.GetUrl().Get() != EMPTYSTR)
                        showGo = true;
                if (hasAltUrlInput)
                    if (alternateUrlInput.IsActive() && alternateUrlInput.GetUrl().Get() != EMPTYSTR)
                        showGo = true;
                if (hasTitleInput)
                    if (titleInput.IsActive() && titleInput.text != EMPTYSTR)
                        showGo = true;
                sendInputs.gameObject.SetActive(showGo);
            }
        }

        public void EndEditUrlInput()
        {
            if (MultipleInputs)
            {
                if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        UpdateMedia();
                    else ChangeMedia();
                }
            }
            else ChangeMedia();
        }

        public void UpdateMedia()
        {
            // queue entries can't be updated, so don't use previous inputs
            if (hasQueue) ChangeMedia();
            else swapMedia(tv.urlMain, tv.urlAlt, tv.title);
        }

        public void ChangeMedia() => swapMedia(EMPTYURL, EMPTYURL, EMPTYSTR);

        private void swapMedia(VRCUrl mainUrl, VRCUrl alternateUrl, string title)
        {
            bool foundUrl = false;
            bool foundTitle = false;
            if (hasMainUrlInput)
            {
                VRCUrl _pcUrl = mainUrlInput.GetUrl();
                if (_pcUrl.Get() != mainUrlDefault.Get())
                {
                    mainUrl = _pcUrl;
                    foundUrl = true;
                    if (!retainInputText) mainUrlInput.SetUrl(mainUrlDefault);
                }
            }

            if (hasAltUrlInput)
            {
                VRCUrl _alternateUrl = alternateUrlInput.GetUrl();
                if (_alternateUrl.Get() != alternateUrlDefault.Get())
                {
                    alternateUrl = _alternateUrl;
                    foundUrl = true;
                    if (!retainInputText) alternateUrlInput.SetUrl(alternateUrlDefault);
                }
            }

            if (hasTitleInput)
            {
                string _title = titleInput.text;
                if (_title != EMPTYSTR)
                {
                    title = _title;
                    foundTitle = true;
                    if (!retainInputText) titleInput.text = titleDefault;
                }
            }

            if (foundUrl)
            {
                if (!tv._CheckDomainWhitelist(mainUrl.Get(), alternateUrl.Get()))
                {
                    OUT_ERROR = VideoError.AccessDenied;
                    updateErrorInfo();
                }
                else if (hasQueue)
                {
                    bool added = queue._AddEntry(mainUrl, alternateUrl, title);
                    if (added) timedMessage("Media successfully added to the Queue.");
                }
                else tv._ChangeMedia(mainUrl, alternateUrl, title);
            }
            else if (foundTitle) tv._ChangeMedia(EMPTYURL, EMPTYURL, title);
        }

        public void Play() => tv._Play();
        public void Pause() => tv._Pause();
        public void Stop() => tv._Stop();
        public void Skip() => tv._Skip();

        public void ReSync()
        {
            UpdateInfo();
            tv._ReSync();
        }

        public void ToggleSync() => tv._ToggleSync();
        public void ToggleLoop() => tv._ToggleLoop();
        public void RefreshMedia() => tv._RefreshMedia();
        public void ToggleAudioMode() => tv._ToggleAudioMode();
        public void ToggleColorCorrection() => tv._ToggleColorCorrection();
        public void ToggleMute() => tv._ToggleMute();
        public void ToggleLock() => tv._ToggleLock();
        public void SeekForward() => tv._SeekForward();
        public void SeekBackward() => tv._SeekBackward();

        public void ChangeSeek()
        {
            if (hasSeek)
            {
                if (!IsTVOwner || !tv.CanPlayMedia) return;
                if (checkForDrag)
                {
                    suppressSeek = true;
                    // Only update the visual timestamp when seek is being suppressed
                    updateCurrentTime(seek.value - tv.seekOffset - startTime);
                }
                else tv._ChangeSeekTime(seek.value - tv.seekOffset);
            }
        }

        public void ChangeSeekOffset()
        {
            if (hasSeekOffset)
            {
                if (checkForDrag) suppressSeekOffset = true;
                else tv._ChangeSeekOffset(seekOffset.value);
            }
        }

        public void ChangeVolume()
        {
            if (!hasVolume) return;
            var val = volume.value;
            if (checkForDrag) suppressVolume = true;
            tv._ChangeVolume(val, suppressVolume);
            if (hasVolumeIndicator)
            {
                if (val == 0f) volumeIndicator.sprite = volumeOff;
                else if (val > 0.9f) volumeIndicator.sprite = volumeHigh;
                else if (val > 0.4f) volumeIndicator.sprite = volumeMed;
                else volumeIndicator.sprite = volumeLow;
            }
        }

        public void ChangeVideoPlayer()
        {
            if (hasVideoPlayerSwap)
            {
                tv._ChangeVideoPlayer(videoPlayerSwap.value);
                // if the swap is rejected, restore the original value
                videoPlayerSwap.SetValueWithoutNotify(tv.videoPlayer);
                if (videoPlayerSwap.captionText != null)
                {
                    // handle optional TMP display label
                    var tmp = videoPlayerSwap.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = videoPlayerSwap.captionText.text;
                }
            }
        }

        public void Change3DMode()
        {
            if (hasMode3dSwap)
            {
                tv._Change3DMode(mode3dSwap.value);
                if (mode3dSwap.captionText != null)
                {
                    // handle optional TMP display label
                    var tmp = mode3dSwap.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = mode3dSwap.captionText.text;
                }
            }
        }

        public void Toggle3DWidth() => tv._Toggle3DWidth();

        public void ToggleUrlMode()
        {
            bool alt = hasUrlSwitch ? urlSwitch.isOn : tv.useAlternateUrl;
            if (isAndroid == alt) tv._UseMainUrl();
            else tv._UseAlternateUrl();
        }

        public void UseMainUrl()
        {
            if (isAndroid != tv.useAlternateUrl) return;
            tv._UseMainUrl();
        }

        public void UseAlternateUrl()
        {
            if (isAndroid == tv.useAlternateUrl) return;
            tv._UseAlternateUrl();
        }

        public void ToggleCurrentRemainingTime()
        {
            showRemainingTime = !showRemainingTime;
            var time = tv.currentTime;
            if (time < startTime) time = startTime;
            updateCurrentTime(time - startTime);
        }

        public void ChangePlaybackSpeed()
        {
            if (hasPlaybackSpeed && IsTVOwner)
            {
                if (checkForDrag) suppressSpeed = true;
                else tv._ChangePlaybackSpeed(playbackSpeed.value);
            }
            else playbackSpeed.SetValueWithoutNotify(tv.playbackSpeed);
        }

        public void ResetPlaybackSpeed() => tv._ResetPlaybackSpeed();

        #endregion

        // =============== TV EVENTS ===================

        #region TV Events

        public override void _TvTitleChange()
        {
            UpdateInfo();
        }

        public override void _TvMediaReady()
        {
            startTime = tv.startTime;
            endTime = tv.endTime;
            duration = tv.videoDuration;
            if (hasEndTimeDisplay) endTimeDisplay.text = _GetReadableTime(duration, showRemainingTime);
            if (hasEndTimeDisplayTMP) endTimeDisplayTMP.text = _GetReadableTime(duration, showRemainingTime);
            if (hasSeek)
            {
                isLive = tv.isLive;
                // cheat cause unity UI is stupid
                hasSeek = false;
                if (isLive)
                {
                    seek.minValue = 0f;
                    seek.maxValue = 1f;
                    seek.SetValueWithoutNotify(1f);
                }
                else
                {
                    seek.minValue = startTime;
                    seek.maxValue = endTime;
                    seek.SetValueWithoutNotify(startTime);
                }

                hasSeek = true;

                UpdateInfo();
            }
        }

        public override void _TvMediaEnd() => _TvPause();

        public override void _TvAuthChange() => _TvOwnerChange();

        public override void _TvOwnerChange()
        {
            if (tv.locked) _TvLock();
            else _TvUnLock();
            UpdateInfo();
        }

        // Once TV has loaded, update certain elements to correctly represent the TV state.
        public override void _TvReady()
        {
            if (hasMainUrlInput) mainUrlInput.SetUrl(mainUrlDefault);
            if (hasAltUrlInput) alternateUrlInput.SetUrl(alternateUrlDefault);

            if (hasMute)
            {
                if (tv.mute) _TvMute();
                else _TvUnMute();
            }

            if (hasGammaCorrection)
            {
                if (tv.skipGamma) _TvColorSpaceRaw();
                else _TvColorSpaceCorrected();
            }

            if (hasAudioMode)
            {
                if (tv.audio3d) _TvAudioMode3d();
                else _TvAudioMode2d();
            }

            if (hasVideoPlayerSwap)
            {
                OUT_VIDEOPLAYER = tv.videoPlayer;
                _TvVideoPlayerChange();
            }

            if (hasMode3dSwap)
            {
                OUT_MODE = (int)tv.video3d;
                _Tv3DModeChange();
            }

            if (has3dWidthMode)
            {
                if (tv.video3dFull) _Tv3DWidthFull();
                else _Tv3DWidthHalf();
            }

            if (hasVolume)
            {
                OUT_VOLUME = tv.volume;
                _TvVolumeChange();
            }

            if (hasTvLock)
            {
                if (tv.locked) _TvLock();
                else _TvUnLock();
            }

            if (hasSyncMode)
            {
                if (tv.syncToOwner) _TvSync();
                else _TvDeSync();
            }

            if (hasLoopMode)
            {
                if (tv.loop > 0) _TvEnableLoop();
                else _TvDisableLoop();
            }

            var state = tv.stateOwner;
            if (state == TVPlayState.WAITING)
            {
                if (hasPlay) play.gameObject.SetActive(false);
                if (hasPause) pause.gameObject.SetActive(false);
                if (hasStop) stop.gameObject.SetActive(false);
                if (hasReload) reload.gameObject.SetActive(false);
            }
            else if (state == TVPlayState.STOPPED) _TvStop();
            else
            {
                _TvMediaReady();
                if (state == TVPlayState.PLAYING) _TvPlay();
                else if (state == TVPlayState.PAUSED) _TvPause();
            }

            if (tv.loading) _TvLoading();

            UpdateSeek();
        }

        public override void _TvPlay()
        {
            if (hasPlay) play.gameObject.SetActive(false);
            if (hasPause) pause.gameObject.SetActive(true);
            if (hasStop) stop.gameObject.SetActive(true);
            if (hasReload) reload.gameObject.SetActive(true);
            UpdateInfo();
        }


        public override void _TvPause()
        {
            if (hasPlay) play.gameObject.SetActive(true);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(true);
            if (hasReload) reload.gameObject.SetActive(true);
        }

        public override void _TvStop()
        {
            if (hasPlay) play.gameObject.SetActive(true);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(false);
            if (hasReload) reload.gameObject.SetActive(true);
            if (tv.errorState == TVErrorState.FAILED) UpdateInfo();
        }

        public override void _TvMute()
        {
            if (hasMuteIndicator)
            {
                muteIndicator.sprite = muted;
                muteIndicator.color = mutedColor;
            }
        }

        public override void _TvUnMute()
        {
            if (hasMuteIndicator)
            {
                muteIndicator.sprite = unmuted;
                muteIndicator.color = unmutedColor;
            }
        }

        public override void _TvAudioMode3d()
        {
            if (hasAudioModeIndicator)
            {
                audioModeIndicator.sprite = audio3d;
                audioModeIndicator.color = audio3dColor;
            }
        }

        public override void _TvAudioMode2d()
        {
            if (hasAudioModeIndicator)
            {
                audioModeIndicator.sprite = audio2d;
                audioModeIndicator.color = audio2dColor;
            }
        }

        public override void _TvColorSpaceCorrected()
        {
            if (hasColorCorrectionIndicator)
            {
                colorSpaceCorrectionIndicator.sprite = colorSpaceCorrected;
                colorSpaceCorrectionIndicator.color = colorSpaceCorrectedColor;
            }
        }

        public override void _TvColorSpaceRaw()
        {
            if (hasColorCorrectionIndicator)
            {
                colorSpaceCorrectionIndicator.sprite = colorSpaceRaw;
                colorSpaceCorrectionIndicator.color = colorSpaceRawColor;
            }
        }

        public override void _Tv3DModeChange()
        {
            if (hasMode3dSwap && mode3dSwap.value != OUT_MODE)
            {
                mode3dSwap.SetValueWithoutNotify(OUT_MODE);
                var tmp = mode3dSwap.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = mode3dSwap.captionText.text;
            }

            if (has3dWidthMode)
            {
                width3dMode.gameObject.SetActive(OUT_MODE != (int)TV3DMode.NONE);
            }
        }

        public override void _Tv3DWidthHalf()
        {
            if (has3dWidthModeIndicator)
            {
                width3dModeIndicator.sprite = width3dHalf;
                width3dModeIndicator.color = width3dHalfColor;
            }
        }

        public override void _Tv3DWidthFull()
        {
            if (has3dWidthModeIndicator)
            {
                width3dModeIndicator.sprite = width3dFull;
                width3dModeIndicator.color = width3dFullColor;
            }
        }

        public override void _TvLoading()
        {
            if (hasPlay) play.gameObject.SetActive(false);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(true);
            if (hasLoadingBar)
            {
                loadingBar.gameObject.SetActive(true);
                loadingBar.SetValueWithoutNotify(0f);
            }

            if (hasLoadingSpinner) loadingSpinnerContainer.SetActive(true);
            bool keepInputsVisible = hasMainUrlInput && mainUrlInput.GetUrl().Get() != EMPTYSTR
                                     || hasAltUrlInput && alternateUrlInput.GetUrl().Get() != EMPTYSTR
                                     || hasTitleInput && titleInput.text != EMPTYSTR;
            if (hasMainUrlInput) mainUrlInput.gameObject.SetActive(keepInputsVisible);
            if (hasAltUrlInput) alternateUrlInput.gameObject.SetActive(keepInputsVisible);
            if (hasTitleInput) titleInput.gameObject.SetActive(keepInputsVisible);
            if (hasSendInputs) sendInputs.gameObject.SetActive(false);
            isLoading = true;
            // with loading enabled, trigger the loading bar animation logic
            UpdateLoading();
        }

        public override void _TvLoadingEnd()
        {
            if (hasPlay) play.gameObject.SetActive(true);
            if (hasPause) pause.gameObject.SetActive(true);
            if (hasLoadingBar)
            {
                loadingBar.gameObject.SetActive(false);
                loadingBar.SetValueWithoutNotify(0f);
            }

            if (hasLoadingSpinner) loadingSpinnerContainer.SetActive(false);
            if (!isLocked || hasQueue && queue.enableAddWhileLocked)
            {
                if (hasMainUrlInput) mainUrlInput.gameObject.SetActive(true);
                if (hasAltUrlInput) alternateUrlInput.gameObject.SetActive(true);
                if (hasTitleInput) titleInput.gameObject.SetActive(true);
                UpdateUrlInput();
            }

            isLoading = false;
        }

        public override void _TvLoadingAbort() => _TvLoadingEnd();

        public override void _TvLock()
        {
            bool canControl = tv.CanPlayMedia || hasQueue && queue.enableAddWhileLocked;
            isLocked = !canControl;
            if (hasMainUrlInput) mainUrlInput.gameObject.SetActive(canControl);
            if (hasAltUrlInput) alternateUrlInput.gameObject.SetActive(canControl);
            if (hasTitleInput) titleInput.gameObject.SetActive(canControl);
            if (hasTvLockIndicator)
            {
                tvLockIndicator.color = lockedColor;
                tvLockIndicator.sprite = locked;
            }
        }

        public override void _TvUnLock()
        {
            isLocked = false;
            if (hasMainUrlInput) mainUrlInput.gameObject.SetActive(true);
            if (hasAltUrlInput) alternateUrlInput.gameObject.SetActive(true);
            if (hasTitleInput) titleInput.gameObject.SetActive(true);
            if (hasTvLockIndicator)
            {
                tvLockIndicator.color = unlockedColor;
                tvLockIndicator.sprite = unlocked;
            }
        }

        public override void _TvVolumeChange()
        {
            if (!hasVolume) return;

            var val = volume.value;
            if (val != OUT_VOLUME)
            {
                val = OUT_VOLUME;
                volume.SetValueWithoutNotify(val);
                if (hasVolumeIndicator)
                {
                    if (val == 0f) volumeIndicator.sprite = volumeOff;
                    else if (val == 1f) volumeIndicator.sprite = volumeHigh;
                    else if (val > 0.5f) volumeIndicator.sprite = volumeMed;
                    else volumeIndicator.sprite = volumeLow;
                }
            }
        }

        public override void _TvVideoPlayerChange()
        {
            if (hasVideoPlayerSwap && videoPlayerSwap.value != OUT_VIDEOPLAYER)
            {
                videoPlayerSwap.SetValueWithoutNotify(OUT_VIDEOPLAYER);
                var tmp = videoPlayerSwap.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = videoPlayerSwap.captionText.text;
            }

            if (hasPlaybackSpeed)
            {
                var manager = tv.videoManagers[OUT_VIDEOPLAYER];
                playbackSpeed.gameObject.SetActive(manager.ValidMediaController);
                playbackSpeed.interactable = tv.CanModifySyncVPManagerData;
            }
        }

        public override void _TvVideoPlayerError()
        {
            if (hasPlay) play.gameObject.SetActive(false);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(true);
            updateErrorInfo();
            if (hasLoadingBar && OUT_ERROR != VideoError.RateLimited)
            {
                loadingBar.gameObject.SetActive(false);
                loadingBar.value = 0f;
            }

            if (hasLoadingSpinner) loadingSpinnerContainer.SetActive(false);
        }

        public override void _TvSync()
        {
            if (hasSyncModeIndicator)
            {
                syncModeIndicator.color = syncEnabledColor;
                syncModeIndicator.sprite = syncEnabled;
            }
        }

        public override void _TvDeSync()
        {
            if (hasSyncModeIndicator)
            {
                syncModeIndicator.color = syncDisabledColor;
                syncModeIndicator.sprite = syncDisabled;
            }
        }

        public override void _TvEnableLoop()
        {
            if (hasLoopModeIndicator)
            {
                loopModeIndicator.color = loopEnabledColor;
                loopModeIndicator.sprite = loopEnabled;
            }
        }

        public override void _TvDisableLoop()
        {
            if (hasLoopModeIndicator)
            {
                loopModeIndicator.color = loopDisabledColor;
                loopModeIndicator.sprite = loopDisabled;
            }
        }

        public override void _TvSeekChange()
        {
            updateCurrentTime(OUT_SEEK - startTime);
            if (hasSeek)
            {
                if (isLive || duration == 0) { }
                else
                {
                    var offset = tv.seekOffset;
                    seek.SetValueWithoutNotify(OUT_SEEK + offset);
                    if (hasSeekOffset)
                    {
                        // cheat cause unity UI is stupid
                        hasSeekOffset = false;
                        seekOffset.minValue = offset - 3f;
                        seekOffset.maxValue = offset + 3f;
                        seekOffset.SetValueWithoutNotify(offset);
                        hasSeekOffset = true;
                    }
                }
            }
        }

        public override void _TvSeekOffsetChange()
        {
            if (hasSeekOffset)
            {
                // cheat cause unity UI is stupid
                hasSeekOffset = false;
                seekOffset.minValue = Mathf.Max(OUT_SEEK - 3f, -5f);
                seekOffset.maxValue = Mathf.Min(OUT_SEEK + 3f, 5f);
                hasSeekOffset = true;
            }

            if (hasSeekOffsetDisplay) seekOffsetDisplay.text = $"{OUT_SEEK}s";
            if (hasSeekOffsetDisplayTMP) seekOffsetDisplayTMP.text = $"{OUT_SEEK}s";
        }

        public override void _TvPlaybackSpeedChange()
        {
            if (hasPlaybackSpeed)
            {
                playbackSpeed.SetValueWithoutNotify(OUT_SPEED);
            }
        }

        #endregion

        // === Helpers ===

        [HideInInspector] public string ERRORMSG_STREAMINACTIVE = "(Stream Error) Stream not active. Rechecking...";
        [HideInInspector] public string ERRORMSG_STREAMUNAVAILABLE = "(Stream Error) Stream has ended or is unavailable.";
        [HideInInspector] public string ERRORMSG_URLFAIL = "(Invalid URL) Could not resolve URL properly. Ensure there are no typos.";
        [HideInInspector] public string ERRORMSG_STREAMSTOPPED = "(Stream Error) Stream has stopped or failed. Rechecking...";
        [HideInInspector] public string ERRORMSG_STREAMENDED = "(Stream Error) Stream has ended.";
        [HideInInspector] public string ERRORMSG_VIDEOFAIL = "(Video Error) Unable to load video.";
        [HideInInspector] public string ERRORMSG_ACCESSDENIED = "(Access Denied) URL is not permitted or 'Enable Untrusted URLs' is disabled.";
        [HideInInspector] public string ERRORMSG_RATELIMITED = "(Rate Limited) Waiting 5 seconds to retry.";
        [HideInInspector] public string ERRORMSG_RTSPNOTSUPPORTED = "RTSP and RTMP protocols are not supported by VRChat. You must use RTSPT instead.";

        public void UpdateInfo()
        {
            if (hasInfo || hasInfoTMP)
            {
                var player = Networking.GetOwner(tv.gameObject);
                var t = "";
                if (showMediaOwner && tv.syncToOwner && VRC.SDKBase.Utilities.IsValid(player))
                {
                    t += player.displayName;
                    if (IsTraceEnabled) t += $" {player.playerId}";
                    t = $"[{t}] ";
                }

                var title = tv.title;
                if (title == EMPTYSTR)
                    title = emptyTitlePlaceholder != EMPTYSTR ? emptyTitlePlaceholder : tv._GetUrlDomain();
                t += title;
                if (hasInfo) infoDisplay.text = t;
                if (hasInfoTMP) infoDisplayTMP.text = t;
            }

            var canPlay = tv.CanPlayMedia;
            if (hasSeek) seek.interactable = IsTVOwner && canPlay;
            if (hasSkip) skip.gameObject.SetActive(canPlay);
            if (hasUrlSwitch) urlSwitch.SetIsOnWithoutNotify(tv.useAlternateUrl);
        }

        private void updateErrorInfo()
        {
            if (hasInfo || hasInfoTMP)
            {
                string t = "";
                var tvOwner = Networking.GetOwner(tv.gameObject);
                if (showMediaOwner && tv.syncToOwner && VRC.SDKBase.Utilities.IsValid(tvOwner))
                {
                    t = IsTraceEnabled ? $"[{tvOwner.displayName} {tvOwner.playerId}] " : $"[{tvOwner.displayName}] ";
                }

                var protocol = tv.urlProtocol;
                switch (OUT_ERROR)
                {
                    case VideoError.InvalidURL:
                        if (protocol == "rtsp" || protocol == "rtmp") t += ERRORMSG_RTSPNOTSUPPORTED;
                        else if (tv.isLive)
                            if (tv.RetryCount > 0)
                                t += ERRORMSG_STREAMINACTIVE;
                            else t += ERRORMSG_STREAMUNAVAILABLE;
                        else t += ERRORMSG_URLFAIL;
                        break;
                    case VideoError.PlayerError:
                        if (protocol == "rtsp" || protocol == "rtmp") t += ERRORMSG_RTSPNOTSUPPORTED;
                        else if (tv.isLive)
                            if (tv.RetryCount > 0)
                                t += ERRORMSG_STREAMSTOPPED;
                            else t += ERRORMSG_STREAMENDED;
                        else t += ERRORMSG_VIDEOFAIL;
                        break;
                    case VideoError.AccessDenied:
                        t += ERRORMSG_ACCESSDENIED;
                        break;
                    case VideoError.RateLimited:
                        t += ERRORMSG_RATELIMITED;
                        break;
                    default:
                        t += $"(ERROR) {OUT_ERROR}";
                        break;
                }

                if (hasInfo) infoDisplay.text = t;
                if (hasInfoTMP) infoDisplayTMP.text = t;
            }
        }

        private void timedMessage(string msg, float seconds = 5f)
        {
            if (hasInfo) infoDisplay.text = msg;
            if (hasInfoTMP) infoDisplayTMP.text = msg;
            SendCustomEventDelayedSeconds(nameof(UpdateInfo), seconds);
        }

        private void updateCurrentTime(float timestamp)
        {
            // convert from current time to time remaining if flag is set
            // disallow conversion if duration is not valid
            if (isLive || duration == 0) { }
            else if (showRemainingTime) timestamp -= duration;

            if (hasCurrentTime) currentTimeDisplay.text = _GetReadableTime(timestamp, showRemainingTime);
            if (hasCurrentTimeTMP) currentTimeDisplayTMP.text = _GetReadableTime(timestamp, showRemainingTime);
        }

        public static string _GetReadableTime(float _time, bool negativeZero = false)
        {
            if (_time == INF) return "Live";
            if (float.IsNaN(_time)) _time = 0f;
            string early = _time < 0 ? "-" : "";
            if (negativeZero && _time == 0) early = "-";
            _time = Mathf.Abs(_time);
            int seconds = (int)_time % 60;
            int minutes = (int)(_time / 60) % 60;
            int hours = (int)(_time / 60 / 60) % 60;
            return hours > 0 ? $"{early}{hours}:{minutes:D2}:{seconds:D2}" : $"{early}{minutes:D2}:{seconds:D2}";
        }

        public static bool _TryParseReadableTime(string input, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.EndsWith("Infinity"))
            {
                seconds = float.PositiveInfinity;
                return true;
            }

            var segments = input.Split(':');
            var len = segments.Length;
            bool hasSeconds = float.TryParse(segments[len - 1], out seconds);
            if (hasSeconds) seconds = Mathf.Abs(seconds);
            if (len > 1 && int.TryParse(segments[len - 2], out int minutes))
                seconds += Mathf.Abs(minutes) * 60;
            if (len > 2 && int.TryParse(segments[len - 3], out int hours))
                seconds += Mathf.Abs(hours) * 60 * 60;

            return hasSeconds;
        }
    }
}