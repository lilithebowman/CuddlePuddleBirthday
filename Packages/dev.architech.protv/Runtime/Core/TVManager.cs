using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using ArchiTech.SDK;
using VRC.Udon.Common.Enums;

// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace ArchiTech.ProTV
{
    [
        UdonBehaviourSyncMode(BehaviourSyncMode.Manual),
        DefaultExecutionOrder(-9999), // needs to initialize before anything else if possible
        HelpURL("https://protv.dev/guides/tvmanager")
    ]
    public partial class TVManager : ATEventHandler
    {
        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#00ff00");
            base.Start();
            if (syncData == null) syncData = GetComponentInChildren<TVManagerData>(true);
            syncData.Logger = Logger;
            Log(ATLogLevel.ALWAYS, $"Starting TV (v{versionNumber})");
            if (repeatingRetryDelay < 5f) repeatingRetryDelay = 5f;
            if (videoManagers == null || videoManagers.Length == 0)
                videoManagers = GetComponentsInChildren<VPManager>(true);
            if (videoManagers.Length == 0)
            {
                Error("No video managers available. Make sure any desired video managers are a child of the TV, otherwise the TV will not work.");
                return;
            }

            // Quest always default to the alternate URL
#pragma warning disable CS0162
            // ReSharper disable once HeuristicUnreachableCode
            if (isAndroid) useAlternateUrl = preferAlternateUrlForQuest;
#pragma warning restore CS0162
            // load initial video player
            videoPlayer = defaultVideoManager;
            // make the object active at start, then when the TV is ready, hide all but the active one.
            foreach (VPManager m in videoManagers) m.gameObject.SetActive(true);
            prevManager = activeManager = nextManager = videoManagers[videoPlayer];
            volume = defaultVolume;
            nextManager.ChangeVolume(volume);
            audio3d = !startWith2DAudio;
            nextManager.ChangeAudioMode(audio3d);
            if (startWithVideoDisabled) disableVideo = true;

            RequestSync();
            SetupSecurity();
            SetupBlitData();
            // implicitly have minimum 1 second as the buffer delay to guarantee that 
            // the syncTime continuous sync will be able to transmit prior to playing the media
            // This prevents non-owners from trying to sync too early to the wrong part of the media
            if (bufferDelayAfterLoad < 1f) bufferDelayAfterLoad = 1f;
            if (automaticResyncInterval == 0f) automaticResyncInterval = Mathf.Infinity;
            autoSyncWait = Time.timeSinceLevelLoad + automaticResyncInterval;
            syncEnforceWait = waitUntil + syncEnforcementTimeLimit;
            aspectRatio = targetAspectRatio;
            if (startHidden) manuallyHidden = true;
        }

        private void OnEnable()
        {
            Start();
            // trigger the update loops
            SendCustomEventDelayedFrames(nameof(_InternalTimeSync), 1);
            SendCustomEventDelayedFrames(nameof(_InternalUpdate), 1);
            SendCustomEventDelayedFrames(nameof(_InternalLateUpdate), 1, EventTiming.LateUpdate);
            if (!VRC.SDKBase.Utilities.IsValid(activeManager) || !VRC.SDKBase.Utilities.IsValid(activeManager.videoPlayer)) return;

            Debug("Enabling TV");
            bool isOwner = IsOwner;
            // Setup the request for getting data from the owner
            // if deserialization does not happen in time (ie: user is master or the owner has the object disabled)
            // force the local player to run the ready up logic anyways
            // give it plenty of breathing room before forcing internal ready up so the world can load in
            if (isOwner)
            {
                if (!isReady) SendCustomEventDelayedFrames(nameof(_InternalSyncDataRequest), 2);
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerEnabled));
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(_InternalSyncDataRequest), 2f);
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OWNER_RequestSyncData));
            }

            disabled = false;
            if (videoTextureWasEnabled) _EnableVideoTexture();
            // if local state has not initialized yet, skip
            // if owner state is not initialized or has no media loaded, skip
            // if local media has ended, skip because that'll trigger a manual loop thus restarting the media
            if (state != TVPlayState.WAITING && (int)stateOwner > (int)TVPlayState.STOPPED && !mediaEnded)
            {
                processReloadCache();
                if (IsLoadingMedia)
                {
                    // technical limitation: OnVideoReady is not received when object is disabled.
                    // If media is in the middle of loading, and object becomes disabled, and media finishes loaded while disabled,
                    // loading state becomes orphaned from the actual state. To mitigate this issue, if the loading state is active
                    // when the game object is enabled, call stop to halt the loading state and activate a refresh.
                    // This expedites the implicit loading wait timeout refresh to an immediate refresh.
                    stop();
                    triggerRefresh(0f, nameof(OnEnable));
                }
                else play();
            }
        }

        private void OnDisable()
        {
            if (!VRC.SDKBase.Utilities.IsValid(activeManager) || !VRC.SDKBase.Utilities.IsValid(activeManager.videoPlayer)) return;

            Debug("Disabling TV");

            // In order to prevent a loop glitch due to owner not updating syncTime when the object is disabled
            // send a command as owner to everyone to signal that the owner is disabled.
            if (IsOwner) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerDisabled));

            if (!isLive)
            {
                _reloadCache = currentTime;
                _reloadTime = Time.timeSinceLevelLoad;
            }

            // if game object is enabled and then ever gets disabled by some external means,
            // disable the flag so internalReadyUp doesn't re-disable the object 
            startDisabled = false;
            disabled = true;
            videoTextureWasEnabled = !disableVideo;
            _DisableVideoTexture();
            if (state != TVPlayState.WAITING)
            {
                if (stopMediaWhenDisabled) stop(true);
                else pause();
            }
        }

        private bool dataSyncCheck = false;

        public void _InternalSyncDataRequest()
        {
            if (!dataSyncFailed && dataSyncCheck)
            {
                dataSyncCheck = false;
                return;
            }

            bool isOwner = IsOwner;
            if (IsTraceEnabled) Trace($"_InternalSyncDataRequest called: ({dataSyncCheck} || {isOwner})");
            if (dataSyncCheck || isOwner)
            {
                dataSyncCheck = false;
                dataSyncFailed = false;
                postDataSync(isOwner);
            }
            else
            {
                // if not the owner, try to force the owner to resync the current data
                if (IsTraceEnabled) Trace("Deserialization not received in time, requesting sync data.");
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OWNER_RequestSyncData));
                // trigger another delayed force attempt in case deserialization STILL doesn't happen
                // this is usually because the owner has the tv object disabled.
                dataSyncFailed = true;
                dataSyncCheck = true;
                SendCustomEventDelayedSeconds(nameof(_InternalSyncDataRequest), 1f);
            }
        }

        private void processAutoOwnership()
        {
            if (CanPlayMedia && AutoOwnershipAvailable)
            {
                if (IsTraceEnabled) Trace($"AutoOwnership change to: {localPlayer.displayName} (state {stateOwner} time {syncTime} ownerDisabled {ownerDisabled})");
                takeOwnership();
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerEnabled));
            }
        }

        private void postDataSync(bool success)
        {
            bool isOwner = IsOwner;

            if (!isReady)
            {
                if (IsTraceEnabled)
                {
                    if (isOwner) Trace($"Readying up data for Owner");
                    else if (success) Trace("Deserialization received, readying up");
                    else Trace("Deserialization not received in time, forcing ready up");
                }

                internalReadyUp();
                if (isOwner) processReloadCache();
            }
            else if (!isOwner)
            {
                // assume owner is disabled if the data sync was not successful
                ownerDisabled = !success;
                if (IsTraceEnabled) Trace("Non-owner AutoOwnership activated");
                processAutoOwnership();
                // ensure the playback time is set to the latest known owner playback time
                if (IsOwner) jumpToTime = syncTime;
            }
        }

        private void processReloadCache()
        {
            if (_reloadCache > 0f)
            {
                var diff = Time.timeSinceLevelLoad - _reloadTime;
                jumpToTime = _reloadCache + diff;
                if (IsTraceEnabled) Trace($"Cached time jump to: {jumpToTime}s");
                if (jumpToTime > endTime) jumpToTime = endTime;
                if (!stopMediaWhenDisabled) setTime(activeManager.videoPlayer, jumpToTime);
            }

            _reloadCache = 0f; // clear after use
        }

        /// <summary>
        /// Handles preparing the internal data after the initial wait period is complete.
        /// Triggers refresh, handles autoplay and notifies plugins the TV is ready to be used.
        /// Gets called automatically for the instance master.
        /// Gets called via OnDeserialization for everyone else.
        /// </summary>
        /// <seealso cref="_PostDeserialization"/>
        private void internalReadyUp()
        {
            if (isReady) return;
            isReady = true;
            // with the TV now ready, disable all but the default video manager
            foreach (VPManager m in videoManagers) m.gameObject.SetActive(m == nextManager);

            // authorization checks depend on the auth plugin,
            // so let the auth plugin run its setup before checking for any authorization checks
            if (IsTraceEnabled) Trace("Auth check for ReadyUp");
            if (hasAuthPlugin) authPlugin._TvReady();

            bool activeMedia = (int)stateOwner > (int)TVPlayState.STOPPED;
            bool neverMedia = urlRevision == 0;

            if (neverMedia)
            {
                if (IsTraceEnabled) Trace("AutoOwnership from internalReadyUp()");
                processAutoOwnership();
            }

            if (activeMedia)
            {
                // if there is media already active when ownership is taken,
                // setup the data for a continuity sync reload
                if (IsTraceEnabled) Trace($"Setting jump time to EPSILON. old jumptime {jumpToTime}");
                jumpToTime = EPSILON;
                _reloadCache = syncTime;
                _reloadTime = Time.timeSinceLevelLoad;

                if (!startDisabled)
                    triggerRefresh(autoplayStartOffset, nameof(internalReadyUp));
            }

            if (neverMedia && (!syncToOwner || IsOwner))
            {
                if (autoplayMainUrl == null) autoplayMainUrl = EMPTYURL;
                if (autoplayAlternateUrl == null) autoplayAlternateUrl = EMPTYURL;
                if (string.IsNullOrEmpty(IN_MAINURL.Get()) && string.IsNullOrEmpty(IN_ALTURL.Get()))
                {
                    IN_MAINURL = autoplayMainUrl;
                    IN_ALTURL = autoplayAlternateUrl;
                    IN_TITLE = autoplayTitle;
                    if (IsTraceEnabled) Trace($"Preparing implicit TV autoplay media: {IN_MAINURL.Get()}");
                }

                // when no TV autoplay is present, do not trigger a refresh
                // this allows plugins to respect loading state and still be able to handle their own autoplay setup via _TvReady
                if (!string.IsNullOrWhiteSpace(IN_MAINURL.Get()))
                {
                    if (!startDisabled)
                        triggerRefresh(autoplayStartOffset, nameof(internalReadyUp));
                }
            }

            Info("TV is now ready");
            SendManagedEvent(nameof(TVPlugin._TvReady));

            if (startWithAudioMuted)
            {
                if (IsTraceEnabled) Trace($"Start mute option enabled. Muting after ready up.");
                mute = true;
                changeMute(mute);
            }

            if (startDisabled)
            {
                Trace($"Start disabled option enabled. Disabling after ready up.");
                gameObject.SetActive(false);
            }
        }

        public void _InternalLateUpdate()
        {
            if (!gameObject.activeInHierarchy && !forceBlitOnce) return;
            // trigger the next update loop
            SendCustomEventDelayedFrames(nameof(_InternalLateUpdate), 1, EventTiming.LateUpdate);
            // clear the auth cache for the current frame
            localAuthCacheUser = -1;
            authCacheUser = -1;
            superAuthCacheUser = -1;
            // tv has not been fully init'd yet, skip current cycle.
            if (!isReady) return;
            VPManager manager = ActiveManager;
            // when video player is switching (as denoted by the epsilon jump time), use the prevManager reference.
            if (state == TVPlayState.PLAYING)
            {
                forceBlitOnce = false;
                updateShaderData();
                _Blit(manager);
            }
            // when TV is not playing, run the blit once then wait until it plays again.
            // this enables updating the blit data only as needed
            // this will also run a single time once the tv is ready.
            else if (forceBlitOnce)
            {
                forceBlitOnce = false;
                updateShaderData();
                if (IsTraceEnabled) Trace($"Latest VideoData:\n{shaderVideoData.ToString()}");
                _Blit(manager);
            }
        }

        public void _InternalUpdate()
        {
            if (!gameObject.activeInHierarchy) return;
            // trigger the next update loop
            SendCustomEventDelayedFrames(nameof(_InternalUpdate), updateIntervalFrames);
            // has not yet been initialized or is playmode without cyanemu
            if (!init) return;
            if (!hasLocalPlayer) return;
            if (buffering) return; // shortcut the loop while media is being prepared.

            var waitTime = Time.timeSinceLevelLoad;
            // wait until the timeout has cleard
            if (waitTime < waitUntil) return;
            if (!isReady) return; // wait until either deserialization triggers ready state or the force ready timeout expires
            if (waitingForMediaRefresh)
            {
                Info("Refresh media via delay");
                waitingForMediaRefresh = false;
                // For some reason when rate limiting happens, the auto reload causes loading to be enabled unexpectely
                // This might cause unexpected edgecases at some point. Keep a close eye on media change issues related to loading states.
                loading = false;
                _RefreshMedia();
                return;
            }

            VPManager manager = ActiveManager;
            if (!VRC.SDKBase.Utilities.IsValid(manager)) return; // manager has been destroyed/unavailable, exiting world or application
            BaseVRCVideoPlayer vp = manager.videoPlayer;
            if (!VRC.SDKBase.Utilities.IsValid(vp)) return; // video player has been destroyed, exiting world or application

            if (mediaEnded) { } // media has ended, nothing to skip and time doesn't need to be updated
            else if (syncTime == -1)
            {
                // skip the current media
                if (IsTraceEnabled) Trace($"Skip detected. Current media ending. time {syncTime}");
                endMedia(vp);
                return;
            }
            // wait until manual loop has cleared before checking for the currentTime
            else if (!manualLoop)
            {
                // update time and cache the mediaEnd check flag
                currentTime = Mathf.Clamp(vp.GetTime() - seekOffset, startTime, endTime);
                if (waitForTime && Mathf.Abs(currentTime - timeToWaitFor) < 0.65f) waitForTime = false;
                // do not enable media end if the local player has not loaded a video previously or if the video is live media
                if (!waitForTime && state != TVPlayState.WAITING && !isLive) mediaEnded = currentTime + 0.1f >= endTime;
            }

            if (errorState == TVErrorState.FAILED) return; // blocking error has occurred

            // skip time checks when waiting for seek to finish.
            if (waitForTime)
            {
                // unsetting this flag here is probably a hack fix, might need a better solution if edge cases crop up... for now it fixes the manual loop soft lock cause by the waitForTime flag.
                manualLoop = false;
                return;
            }

            if (enableReloadKeybind && !IsLoadingMedia)
            {
                // shift + Function keys are avatar gesture bindings, ignore when shift is pressed
                if (Input.GetKeyDown(reloadKey) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                    triggerRefresh(autoplayStartOffset, nameof(internalReadyUp));
            }

            if (vp.IsPlaying && waitTime > autoSyncWait)
            {
                // every so often trigger an automatic resync
                enforceSyncTime = true;
                autoSyncWait = waitTime + automaticResyncInterval;
            }

            mediaSyncCheck(vp, waitTime);
            // live media does not do time checks for ending.
            if (isLive)
            {
                liveMediaReloadCheck(waitTime);
                return;
            }

            mediaEndCheck(vp);
        }

        private void mediaSyncCheck(BaseVRCVideoPlayer vp, float waitTime)
        {
            if (!syncToOwner || isLive)
            {
                // if TV is local or if livestream detected, do sync check logic and skip the rest
                // skip the enforcement when currentTime is infinity (a rare circumstance)
                if (enforceSyncTime && waitTime >= syncEnforceWait && currentTime != INF)
                {
                    // single time update for local-only mode.
                    // Also helps fix audio/video desync/drift in most cases.
                    enforceSyncTime = false;
                    if (IsInfoEnabled) Info($"Sync enforcement requested for live or local. Updating to {currentTime}");
                    if (isLive) currentTime += 10f;
                    else currentTime += 0.05f; // anything smaller than this won't be registered by AVPro for some reason...
                    setTime(vp, Mathf.Max(0, currentTime + seekOffset));
                }

                return;
            }

            // This handles updating the sync time from owner to others.
            if (IsOwner)
            {
                // check for loading timeout and trigger PlayerError if exceeded
                // Skip check if a delayed refresh is active
                if (loading && !waitingForMediaRefresh && maxAllowedLoadingTime > 0 && waitTime >= loadingWait)
                {
                    Warn($"Loading timeout of {maxAllowedLoadingTime}s reached.");
                    _OnVideoPlayerError(VideoError.PlayerError);
                }

                if (enforceSyncTime && waitTime >= syncEnforceWait)
                {
                    // single time update for owner. 
                    // Also helps fix audio/video desync/drift in most cases.
                    enforceSyncTime = false;
                    if (IsInfoEnabled) Info($"Sync enforcement requested for owner. Updating to {currentTime}");
                    currentTime += 0.05f; // anything smaller than this won't be registered by AVPro for some reason...
                    setTime(vp, Mathf.Clamp(currentTime + seekOffset, startTime, endTime));
                }

                syncTime = currentTime;
            }
            else if (loading) // skip other time checks if TV is loading a video
            {
                // check for loading timeout and trigger PlayerError if exceeded
                // Skip check if a delayed refresh is active
                if (!waitingForMediaRefresh && maxAllowedLoadingTime > 0 && waitTime >= loadingWait)
                {
                    Warn($"Loading timeout of {maxAllowedLoadingTime}s reached.");
                    _OnVideoPlayerError(VideoError.PlayerError);
                }
            }
            else if ((int)state > (int)TVPlayState.STOPPED && (int)errorStateOwner <= (int)TVErrorState.BLOCKED)
            {
                var compSyncTime = syncTime + lagComp;
                if (compSyncTime > endTime) compSyncTime = endTime;
                float syncDelta = Mathf.Abs(currentTime - compSyncTime);
                if (enforceSyncTime || state == TVPlayState.PLAYING && syncDelta > playDriftThreshold && !ownerDisabled)
                {
                    // sync time enforcement check should ONLY be for when the video is playing
                    // Also helps fix audio/video desync/drift in most cases.
                    // delay enforcing sync until the owner's sync time is greater than the start time
                    // syncTime being less than startTime is an artifact of combining auto-ownership with a sliced video
                    if (waitTime >= syncEnforceWait && syncTime >= startTime)
                    {
                        if (IsTraceEnabled) Trace($"Sync info:\nsyncTime {syncTime} + lag {lagComp} = comp {compSyncTime}\nseekOffset {seekOffset} currentTime {currentTime}");
                        currentTime = compSyncTime;
                        if (IsInfoEnabled) Info($"Sync enforcement. Updating to {currentTime}");
                        enforceSyncTime = false;
                        setTime(vp, Mathf.Clamp(currentTime + seekOffset, startTime, endTime));
                        SendManagedVariable(nameof(TVPlugin.OUT_SEEK), currentTime);
                        SendManagedEvent(nameof(TVPlugin._TvSeekChange));
                    }
                }
                // video sync enforcement will always occur for paused mode as the user expects the video to not be active, so we can skip forward as needed.
                else if (syncDelta > pauseDriftThreshold)
                {
                    currentTime = compSyncTime;
                    if (IsDebugEnabled) Debug($"Paused drift threshold exceeded. Updating to {currentTime}");
                    setTime(vp, Mathf.Clamp(currentTime + seekOffset, startTime, endTime));
                    SendManagedVariable(nameof(TVPlugin.OUT_SEEK), currentTime);
                    SendManagedEvent(nameof(TVPlugin._TvSeekChange));
                }
            }
        }

        private void mediaEndCheck(BaseVRCVideoPlayer vp)
        {
            // loop/media end check
            if (mediaEnded)
            {
                bool shouldLoop = loop > 0;
                if (IsOwner && shouldLoop)
                {
                    // owner when loop is active
                    setTime(vp, startTime);
                    mediaEnded = false;
                    syncTime = currentTime = startTime;
                    SendManagedEvent(nameof(TVPlugin._TvMediaLoop));
                    if (loop < int.MaxValue) changeLoop(loop - 1);
                    if (IsTraceEnabled) Trace($"Looping owner to start time {startTime}.");
                }
                else if (state == TVPlayState.PLAYING && endTime > 0f)
                {
                    if (shouldLoop)
                    {
                        if (syncToOwner && syncTime > currentTime)
                        {
                            if (IsDebugEnabled) Debug("Sync is enabled but sync time hasn't been passed, skip");
                        }
                        // sync is enabled but sync time hasn't been passed, skip
                        else
                        {
                            // non-owner when owner has loop (causing the sync time to start over)
                            setTime(vp, startTime);
                            mediaEnded = false;
                            // update current time to start time so this only executes once, prevents accidental spam
                            currentTime = startTime;
                            SendManagedEvent(nameof(TVPlugin._TvMediaLoop));
                        }
                    }
                    else if (!manualLoop)
                    {
                        endMedia(vp);
                    }
                }
            }
            else if (manualLoop) manualLoop = false;
        }

        private void setTime(BaseVRCVideoPlayer vp, float time)
        {
            waitForTime = true;
            timeToWaitFor = time;
            vp.SetTime(time);
        }

        private void liveMediaReloadCheck(float time)
        {
            if (liveMediaAutoReloadInterval > 0 && time > liveReloadTimestamp)
            {
                liveReloadTimestamp = time + liveMediaAutoReloadInterval * 60f;
                triggerRefresh(0f, nameof(liveMediaReloadCheck));
            }
        }

        private void endMedia(BaseVRCVideoPlayer vp)
        {
            if (IsDebugEnabled) Debug("Ending media playback.");
            var cTime = currentTime;
            // force times to tne end for media end detection and to prevent accidental skips from INF check
            currentTime = syncTime = endTime;
            // once media has finished, any reload info should be zeroed
            jumpToTime = 0f;
            _reloadCache = 0f;
            SendManagedEvent(nameof(TVPlugin._TvMediaEnd));
            // if no plugins triggered a new URL or refresh, force end the actual media
            if (mediaEnded || !waitingForMediaRefresh)
            {
                if (IsTraceEnabled) Trace($"Media full ending: actual {mediaEnded} or not waiting {!waitingForMediaRefresh}");
                vp.Pause();
                setTime(vp, endTime);
                state = TVPlayState.PAUSED;
                if (IsOwner)
                {
                    syncState = stateOwner = state;
                    RequestSync();
                }

                mediaEnded = true;
                forceBlitOnce = true; // force blit once more just to be sure
            }
            // if the ending is not a real ending, restore the original timestamp
            else currentTime = syncTime = cTime;

            if (playbackEnabled)
            {
                playbackEnabled = false;
                SendManagedEvent(nameof(TVPlugin._TvPlaybackEnd));
            }
        }

        public void _InternalTimeSync()
        {
            if (!gameObject.activeInHierarchy) return;
            SendCustomEventDelayedSeconds(nameof(_InternalTimeSync), 0.2f);
            if (IsOwner && state == TVPlayState.PLAYING) RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            lagCompSync = Networking.GetServerTimeInMilliseconds();
            syncTime = currentTime;
        }

        public override void OnDeserialization()
        {
            lagComp = (Networking.GetServerTimeInMilliseconds() - lagCompSync) * 0.001f;
        }

        private void RequestSync()
        {
            if (!init) return;
            if (IsOwner)
            {
                RequestSerialization();
                syncData._RequestData();
            }
        }

        /// <summary>
        /// Any changes to the owner's synced data will be monitored by this method and update the local player data as needed.
        /// </summary>
        public void _PostDeserialization()
        {
            dataSyncFailed = false;
            if (!syncToOwner) return;
            ownerDisabled = false; // ensure that owner is marked enabled since the deserialization doesn't happen when owner is disabled.
            if (!isReady && firstDeserialization)
            {
                // if this is the first time deserialization happens,
                // delay to allow any other first-time deserialization scripts to finish
                // before triggering the ready state
                firstDeserialization = false;
                SendCustomEventDelayedFrames(nameof(_PostDeserialization), 2);
                return;
            }

            // Wait to process deserialization data until the current loading state has cleared.
            // Not doing this can cause users to be playing a previous video if the owner switches videos too fast.
            deserializationDelayedByLoadingState = IsLoadingMedia;
            if (deserializationDelayedByLoadingState) return;

            // grab the delta states
            bool _deltaAny = false;
            bool _deltaVP = syncVideoManagerSelection && videoPlayer != syncVideoPlayer;
            bool _deltaVolume = syncVolumeControl && volume != syncVolume;
            bool _deltaAudio3d = syncAudioMode && audio3d != syncAudio3d;
            bool _deltaVideo3d = syncVideoMode && video3d != syncVideo3d;
            bool _deltaVideo3dWide = syncVideoMode && video3dFull != syncVideo3dFull;
            bool _deltaPlaybackSpeed = playbackSpeed != syncPlaybackSpeed;
            bool _deltaLocked = locked != syncLocked;
            bool _deltaErrorState = errorStateOwner != syncErrorState;
            bool _deltaTitle = title != syncTitle;
            bool _deltaAddedBy = addedBy != syncAddedBy;
            bool _deltaUrlRevision = urlRevision != syncUrlRevision;
            bool _deltaState = stateOwner != syncState;
            bool _deltaLoop = loop != syncLoop;

            _deltaAny |= _deltaVP;
            _deltaAny |= _deltaVolume;
            _deltaAny |= _deltaAudio3d;
            _deltaAny |= _deltaVideo3d;
            _deltaAny |= _deltaVideo3dWide;
            _deltaAny |= _deltaPlaybackSpeed;
            _deltaAny |= _deltaLocked;
            _deltaAny |= _deltaErrorState;
            _deltaAny |= _deltaTitle;
            _deltaAny |= _deltaAddedBy;
            _deltaAny |= _deltaUrlRevision;
            _deltaAny |= _deltaState;
            _deltaAny |= _deltaLoop;
            var oldVolume = volume;
            var oldVideoPlayer = videoPlayer;
            var oldAudio3d = audio3d;
            var oldLoop = loop;
            var oldVideo3d = video3d;
            var oldVideo3dWide = video3dFull;

            // update the valid deltas + log
            string _changes = "Deserialization Changes";
            if (_deltaVP)
            {
                if (IsDebugEnabled) _changes += $"\nVideo Player swap {videoPlayer} -> {syncVideoPlayer}";
                videoPlayer = syncVideoPlayer;
            }

            if (_deltaVolume)
            {
                if (IsDebugEnabled) _changes += $"\nVolume update {volume} -> {syncVolume}";
                volume = syncVolume;
            }

            if (_deltaAudio3d)
            {
                if (IsDebugEnabled) _changes += $"\nAudio3D update {audio3d} -> {syncAudio3d}";
                audio3d = syncAudio3d;
            }

            if (_deltaVideo3d)
            {
                if (IsDebugEnabled) _changes += $"\nVideo3D update {video3d} -> {syncVideo3d}";
                video3d = syncVideo3d;
            }

            if (_deltaVideo3dWide)
            {
                if (IsDebugEnabled) _changes += $"\nVideo3DWide update {video3dFull} -> {syncVideo3dFull}";
                video3dFull = syncVideo3dFull;
            }

            if (_deltaLoop)
            {
                if (IsDebugEnabled) _changes += $"\nLoop update {loop} -> {syncLoop}";
                loop = syncLoop;
            }

            if (_deltaPlaybackSpeed)
            {
                if (IsDebugEnabled) _changes += $"\nPlayback Speed update {playbackSpeed} -> {syncPlaybackSpeed}";
                playbackSpeed = syncPlaybackSpeed;
            }

            if (_deltaLocked)
            {
                if (IsDebugEnabled) _changes += $"\nLock change {locked} -> {syncLocked}";
                lockedBySuper = syncLocked && _IsSuperAuthorized(Owner);
                locked = syncLocked || disallowUnauthorizedUsers && !_IsAuthorized();
            }

            if (_deltaErrorState)
            {
                if (IsDebugEnabled) _changes += $"\nOwner error state change {errorStateOwner} -> {syncErrorState}";
                errorStateOwner = syncErrorState;
                _deltaTitle = false;
                _deltaAddedBy = false;
                _deltaUrlRevision = false;
                _deltaState = false;
            }
            else // if owner has an error skip syncing the following data
            {
                if (_deltaTitle)
                {
                    if (IsDebugEnabled) _changes += $"\nTitle change \"{title}\" -> \"{syncTitle}\"";
                    title = syncTitle;
                }

                if (_deltaAddedBy)
                {
                    if (IsDebugEnabled) _changes += $"\nAdded by change {addedBy} -> {syncAddedBy}";
                    addedBy = syncAddedBy;
                }

                if (_deltaUrlRevision)
                {
                    if (IsDebugEnabled) _changes += $"\nURL change {urlRevision} -> {syncUrlRevision}";
                    urlRevision = syncUrlRevision;
                }

                if (_deltaState)
                {
                    if (IsDebugEnabled) _changes += $"\nState change {stateOwner} -> {syncState}";
                    stateOwner = syncState;
                }
            }

            if (IsDebugEnabled) Debug(_changes);

            postDataSync(true);

            // extended testing needed to check for unexpected side-effects of this early exit.
            if (!_deltaAny) return; // no changes, skip

            // Run actions based on detected deltas
            if (_deltaVP)
            {
                // restore old value to pass the conditional in the change method.
                videoPlayer = oldVideoPlayer;
                changeVideoPlayer(syncVideoPlayer);
            }

            if (_deltaVolume)
            {
                // restore old value to pass the conditional in the change method.
                volume = oldVolume;
                changeVolume(syncVolume);
            }

            if (_deltaAudio3d)
            {
                // restore old value to pass the conditional in the change method.
                audio3d = oldAudio3d;
                changeAudioMode(syncAudio3d);
            }

            if (_deltaVideo3d)
            {
                // restore old value to pass the conditional in the change method.
                video3d = oldVideo3d;
                changeVideo3dMode((int)syncVideo3d);
            }

            if (_deltaVideo3dWide)
            {
                video3dFull = oldVideo3dWide;
                changeVideo3dWidth(syncVideo3dFull);
            }

            if (_deltaLoop)
            {
                // restore old value to pass the conditional in the change method.
                loop = oldLoop;
                changeLoop(syncLoop);
            }

            if (_deltaPlaybackSpeed) changePlaybackSpeed(playbackSpeed);

            if (_deltaLocked) SendManagedEvent(locked ? nameof(TVPlugin._TvLock) : nameof(TVPlugin._TvUnLock));

            if (_deltaErrorState)
            {
                if ((int)errorStateOwner >= (int)TVErrorState.BLOCKED)
                    Warn("Current TV Owner has an error. Media will not sync until the owner no longer has an error.");
            }

            if (_deltaTitle)
            {
                SendManagedVariable(nameof(TVPlugin.OUT_TITLE), title);
                SendManagedEvent(nameof(TVPlugin._TvTitleChange));
            }

            // ReSharper disable once RedundantCheckBeforeAssignment
            if (_deltaAddedBy) { }

            if (_deltaUrlRevision)
            {
                // if the owner is stopped, do NOT force a refresh.
                if ((int)syncState > (int)TVPlayState.STOPPED)
                {
                    triggerRefresh(0f, nameof(_PostDeserialization));
                    triggerSync(0.2f);
                    jumpToTime = 0;
                    return;
                }

                if (!manualLoop && stateOwner != TVPlayState.WAITING)
                    triggerSync(0.3f);
            }

            if (_deltaState)
            {
                // if loading, skip state change actions as those actions will be triggered once loading is done.
                if (IsLoadingMedia)
                {
                    if (IsTraceEnabled) Trace("Media is currently loading. Deferring state change action.");
                    return;
                }

                switch (stateOwner)
                {
                    // always enforce stopping
                    case TVPlayState.STOPPED:
                        // when local mode is waiting, the initial video load is required.
                        if (state == TVPlayState.WAITING) stop(true);
                        else stop(syncLoading);
                        break;
                    // allow the local player to be paused if owner is playing
                    case TVPlayState.PLAYING:
                        if (!locallyPaused) play();
                        break;
                    // pause for the local player
                    case TVPlayState.PAUSED:
                        // the owner should not be able to trigger the locallyPaused
                        // flag, so use the internal pause method instead of the public
                        // _Pause event.
                        if (Mathf.Abs(syncTime - endTime) < 0.65f)
                        {
                            VPManager manager = ActiveManager;
                            // manager has been destroyed/unavailable, exiting world or application
                            if (!VRC.SDKBase.Utilities.IsValid(manager)) break;
                            endMedia(manager.videoPlayer);
                        }
                        else pause();

                        break;
                }
            }
        }


        // === VPManager events ===

        private readonly string[] rtspProtocols = { "rtsp", "rtspt", "rtspu" };

        public void _OnVideoPlayerEnd()
        {
            if (IsTraceEnabled) Trace($"Media triggered built-in OnVideoEnd.");
            if (loading) return; // short circuit when loading is active cause that means that media is changing and no automatic video end correction is required.

            // this catches when video end happens immediately after a new url has been loaded, but the OnVideoReady event was already triggered.
            if (loadingCatchVideoEndEvent)
            {
                loadingCatchVideoEndEvent = false;
                return;
            }

            // RTSPT will trigger this event before the media even starts, so do not trigger a refresh or mediaEnd if it's live media and protocol is rtsp-based.
            // If it IS rtspt, make sure a reasonable amount of time has passed to trigger an auto-refresh, as this method can be called within a few seconds of starting... for some ungodly reason... thanks avpro. 
            // MPEG-TS will trigger this event after a while with current time being infinity, even when the stream isn't yet dead. Implicitly trigger a refresh to recover.
            if (isLive && (System.Array.IndexOf(rtspProtocols, urlProtocol) == -1 || currentTime > 10f))
            {
                if (IsTraceEnabled) Trace($"Was live media. Retrying once media just to make sure it's actually done.\n(ctime: {currentTime} etime {endTime} stime {syncTime} actual time {activeManager.videoPlayer.GetTime()}");
                retryCount = 0;
                triggerRefresh(0f, nameof(_OnVideoPlayerEnd));
            }
            // non-live media forces the mediaEnd logic when this event triggers.
            else if (!isLive)
            {
                if (IsTraceEnabled) Trace($"Media is ending.");
                mediaEnded = true;
            }
        }

        public void _OnVideoPlayerError(VideoError error)
        {
            if (string.IsNullOrWhiteSpace(url.Get()))
            {
                if (IsTraceEnabled) Trace($"Error occured for empty URL. This should be an unreachable condition. Skipping error logic.");
                return;
            }

            errorState = TVErrorState.FAILED;
            Error($"Video Error: {error}");
            if (IsTraceEnabled) Trace($"Error occured for url '{url}'");
            if (error == VideoError.RateLimited)
            {
                Warn("Refresh via rate limit error, retrying in 5 seconds...");
                errorState = TVErrorState.RETRY;
                triggerRefresh(6f, nameof(_OnVideoPlayerError) + " - " + nameof(VideoError.RateLimited)); // 5+1 seconds just to avoid any race condition issues with the global rate limit
            }
            else if (error == VideoError.PlayerError || error == VideoError.InvalidURL)
            {
                if (error == VideoError.InvalidURL)
                {
                    if (urlProtocol == "rtsp" || urlProtocol == "rtmp") Warn("RTSP protocol is not supported by VRChat. You must use RTSPT instead.");
                    else if (loading && isNextLive || isLive) Warn("Stream is either offline or the URL is incorrect.");
                    else Warn("Unable to load. Media maybe unavailable, protected, region-locked or the URL is wrong.");
                }
                else //if (error == VideoError.PlayerError)
                {
                    if (loading && isNextLive || isLive) Warn("Livestream has stopped.");
                    else Warn("Unexpected error with the media playback.");
                }

                if (retryCount > 0)
                {
                    // the first retry should be very short.
                    float retryDelay = 6f;
                    // any subsequent retries (meaning 2 or more times the url failed to load) use the repeating delay value
                    if (errorState == TVErrorState.RETRY) retryDelay = repeatingRetryDelay;
                    else errorState = TVErrorState.RETRY;
                    // do not decrement retry count if count is "effectively infinite"
                    if (retryCount < int.MaxValue)
                    {
                        retryCount--;
                        if (IsDebugEnabled) Debug($"{retryCount} retries remaining.");
                    }

                    state = TVPlayState.PAUSED;
                    // if video is ended, but subsequent video is failing, force another blit op just to make sure things are rendered correctly
                    forceBlitOnce = true;
                    // if flag is enabled, flip-flop the useAlternateUrl flag once.
                    // if that again fails, flip-flop once more and then don't flip any more until success or a new URL is input
                    if (retryUsingAlternateUrl && (!retryingWithAlt || !retriedWithAlt))
                    {
                        useAlternateUrl = !useAlternateUrl;
                        if (retryingWithAlt) retriedWithAlt = true;
                        else retryingWithAlt = true;
                    }

                    triggerRefresh(retryDelay, nameof(_OnVideoPlayerError) + " - AutoRetry");
                }

                setLoadingState(false);
            }
            else setLoadingState(false);

            // if error does not trigger a reload of some kind, restore the url data of the previous url
            RequestSync();
            if (!waitingForMediaRefresh)
            {
                parseUrl(url.Get(), out urlProtocol, out urlDomain, out urlParamKeys, out urlParamValues);
                mediaEnded = true;
            }

            SendManagedVariable(nameof(TVPlugin.OUT_ERROR), error);
            SendManagedEvent(nameof(TVPlugin._TvVideoPlayerError));
            if (!waitingForMediaRefresh)
            {
                mediaEnded = false;
                // if video-player swap failed, revert the swap attempt.
                if (activeManager != nextManager)
                {
                    nextManager.gameObject.SetActive(false);
                    nextManager = activeManager;
                    jumpToTime = 0f; // would be epsilon for timestamp continuity, but needs reset since the swap failed.
                    videoPlayer = System.Array.IndexOf(videoManagers, activeManager);
                    SendManagedVariable(nameof(TVPlugin.OUT_VIDEOPLAYER), videoPlayer);
                    SendManagedEvent(nameof(TVPlugin._TvVideoPlayerChange));
                }
                // do not trigger playback end when manager swap was occurring.
                else if (playbackEnabled)
                {
                    playbackEnabled = false;
                    SendManagedEvent(nameof(TVPlugin._TvPlaybackEnd));
                }
            }
        }

        public void _OnVideoPlayerPlay()
        {
            triggerSync(0.3f);
        }

        // general media info
        // Once the active manager detects the player has finished loading, get video information and log
        public void _OnVideoPlayerReady()
        {
            if (IsTraceEnabled && !buffering) Trace($"Media preparing via built-in OnVideoReady.");
            forceRestartMedia = false;
            // if player ready is called when a video is not loading and the internal state is stopped, ignore the load.
            // This occurs when a user stops a media in the middle of a loading action and then the video finishes resolving AFTERWARDS.
            if (locallyStopped) return;
            if (!loading)
            {
                // if this is called again and loading is done, just rerun the media start actions to continue.
                // generally caused by some UnityVideo bullshit when swapping back and forth between UnityVideo and another video player option.
                jumpToTime = lastJumpToTime;
                startMedia();
                return;
            }

            if (!buffering)
            {
                // video has successfully loaded, make sure the active manager is updated to the target manager
                activeManager = nextManager;
                mediaLength = activeManager.videoPlayer.GetDuration();
                isLive = mediaLength == INF || mediaLength == 0f;
                mediaEnded = false;
            }

            if (isLive)
            {
                // livestreams should just start immediately
                liveReloadTimestamp = Time.timeSinceLevelLoad + liveMediaAutoReloadInterval * 60f;
                prepareMedia();
                startMedia();
            }
            // non-owner buffering will continue to wait for owner loading to finish or owner to be disabled, or owner to have a failed load
            // Owner always ends the buffer after the configured delay
            else if (buffering && (IsOwner || !syncLoading || ownerDisabled || errorStateOwner == TVErrorState.FAILED))
            {
                if (IsInfoEnabled) Info("Buffering complete.");
                buffering = false;
                startMedia();
            }
            else if (bufferDelayAfterLoad > 0)
            {
                // timeout is exceeded while the buffer flag is unset. Buffering has started, call delayed event
                if (!buffering)
                {
                    if (IsInfoEnabled) Info($"Allowing video to buffer for {bufferDelayAfterLoad} seconds.");
                    prepareMedia();
                }

                SendCustomEventDelayedSeconds(nameof(_OnVideoPlayerReady), buffering ? 1f : bufferDelayAfterLoad);
                buffering = true;
            }
            else
            {
                // no buffering, start immediately
                prepareMedia();
                startMedia();
            }
        }

        private void prepareMedia()
        {
            var urlStr = url.Get();
            var newMediaHash = urlStr.GetHashCode();
            _mediaIsStale = mediaHash == newMediaHash && jumpToTime == EPSILON;
            mediaHash = newMediaHash;

            if (!_mediaIsStale)
            {
                cacheMediaReadyInfo();
                if (autoplayLoop && (urlStr == autoplayMainUrl.Get() || urlStr == autoplayAlternateUrl.Get()))
                {
                    loop = int.MaxValue;
                    autoplayLoop = false;
                }
            }

            if (!activeManager.isVisible && !manuallyHidden) activeManager.Show();

            bool vpSwap = prevManager != activeManager;
            if (vpSwap)
            {
                if (prevManager != null)
                {
                    if (IsDebugEnabled) Debug($"Hiding previous manager {prevManager.gameObject.name}");
                    activeManager.UpdateState();
                    prevManager.Stop();
                    prevManager.ChangePlaybackSpeed(1f);
                    prevManager.gameObject.SetActive(false);
                }

                prevManager = activeManager;
            }

            // only do epsilon jump when media is actively loaded
            if (jumpToTime == EPSILON && (int)state > (int)TVPlayState.STOPPED)
            {
                // If jumptime is still epsilon, a non-switching reload occurred. Jump to last known media time.
                jumpToTime = _reloadCache;
                // if the media is actively playing, include the diff of how long the media took to load.
                if (state == TVPlayState.PLAYING)
                {
                    float diff = Time.timeSinceLevelLoad - _reloadTime + bufferDelayAfterLoad;
                    jumpToTime += diff;
                }

                if (jumpToTime > endTime) jumpToTime = endTime;
                if (IsTraceEnabled) Trace($"Jump to time is Epsilon, Jumping to {jumpToTime} from start {_reloadTime} and cache {_reloadCache}");
            }
            else if (!IsOwner && !ownerDisabled)
            {
                // when a non-owner loads a video (and owner is actually available),
                // it should do a resync to catch up to the owner and ignore any jumpToTime to avoid a double timeskip
                jumpToTime = 0f;
                triggerSync(0f);
            }

            if (IsInfoEnabled)
            {
                var added = string.IsNullOrEmpty(addedBy) ? Owner.displayName : addedBy;
                Info($"[{activeManager.gameObject.name}] ({added}) Now Playing: {url}");
            }

            activeManager.ChangeMute(mute || manuallyHidden);

            if (endTime < startTime)
            {
                if (IsTraceEnabled) Trace($"endTime {endTime} precedes startTime {startTime}. Updating.");
                startTime = 0f; // invalid start time given, zero-out
            }

            if (currentTime + 0.1f >= endTime)
            {
                if (IsTraceEnabled) Trace($"last playing time {currentTime} exceeds the new media end time {endTime}. Updating.");
                jumpToTime = startTime;
            }

            if (jumpToTime < startTime)
            {
                if (IsTraceEnabled) Trace($"jumpToTime {jumpToTime} precedes startTime {startTime}. Updating.");
                jumpToTime = startTime;
            }

            locallyPaused = false;
            // clear the retry flags on successful video load
            retrying = false;
            retryingWithAlt = false;
            retriedWithAlt = false;
            // after a successful load, always ensure that a livestream will retry at least once upon failure/ending.
            if (isLive && retryCount == 0) retryCount = 1;
            errorState = TVErrorState.NONE;
            if (IsOwner) errorStateOwner = TVErrorState.NONE;

            if (lastTitle != title || string.IsNullOrEmpty(title))
            {
                SendManagedVariable(nameof(TVPlugin.OUT_TITLE), title);
                SendManagedEvent(nameof(TVPlugin._TvTitleChange));
            }
        }

        private void startMedia()
        {
            if (IsDebugEnabled && jumpToTime > 0f) Debug($"Jumping [{activeManager.gameObject.name}] to timestamp: {jumpToTime}");
            setTime(activeManager.videoPlayer, jumpToTime);
            lastJumpToTime = jumpToTime;
            jumpToTime = 0f;

            setLoadingState(false);

            if (!_mediaIsStale && IsOwner)
            {
                stateOwner = syncState = playVideoAfterLoad || isLive ? TVPlayState.PLAYING : TVPlayState.PAUSED;
                RequestSync();
            }

            var checkState = _mediaIsStale ? state : stateOwner;
            // if (!_mediaIsStale && (isLive || lastJumpToTime > 0f)) checkState = TVPlayState.PLAYING;
            SendManagedVariable(nameof(TVPlugin.OUT_URL), url);
            SendManagedEvent(nameof(TVPlugin._TvMediaReady));
            if (!isLive && (checkState == TVPlayState.PAUSED || locallyPaused))
            {
                if (IsDebugEnabled) Debug($"Media starting paused. (local pause {locallyPaused})");
                state = TVPlayState.PAUSED;
                activeManager.videoPlayer.Pause();
                SendManagedEvent(nameof(TVPlugin._TvPause));
            }
            else
            {
                if (IsDebugEnabled) Debug("Media starting playing.");
                state = TVPlayState.PLAYING;
                activeManager.videoPlayer.Play();
                SendManagedEvent(nameof(TVPlugin._TvPlay));
            }

            if (!playbackEnabled)
            {
                playbackEnabled = true;
                SendManagedEvent(nameof(TVPlugin._TvPlaybackStart));
            }
        }

        private void cacheMediaReadyInfo()
        {
            // grab parameters
            float value = 0f;
            int check = 0;
            string param = null;
            if (isLive)
            {
                startTime = 0f;
                endTime = INF;
                videoDuration = INF;
                loop = 0;
                // always have at least 1 retry for any live content
                if (retryCount == 0) retryCount = 1;
            }
            else
            {
                // check for start param
                param = getUrlParam("start", EMPTYSTR);
                if (float.TryParse(param, out value)) startTime = value;
                else startTime = 0f;

                // check for end param
                param = getUrlParam("end", EMPTYSTR);
                if (float.TryParse(param, out value)) endTime = value;
                else endTime = mediaLength;
                videoDuration = endTime - startTime;

                // loop is synced, so only extract it from the owner
                if (IsOwner)
                {
                    // if loop is present without value, default to -1
                    check = 0;
                    param = getUrlParam("loop", "-1");
                    bool parsed = int.TryParse(param, out check);
                    // if loop is not explicitly provided but duration is up to 15 seconds, implicilty loop it once
                    // generally helpful for really short meme clips in-case someone takes too long to load the video the first time
                    if (!parsed && videoDuration <= implicitReplayThreshold) check = 1;
                    bool oldState = loop != 0;
                    bool newState = check != 0;
                    loop = check;
                    if (loop < 0) loop = int.MaxValue;
                    if (oldState != newState) SendManagedEvent(loop > 0 ? nameof(TVPlugin._TvEnableLoop) : nameof(TVPlugin._TvDisableLoop));
                }

                // check for t or start params, only update jumpToTime if start or t succeeds
                // only parse if another jumpToTime value has not been set.
                if (jumpToTime <= startTime)
                {
                    param = getUrlParam("t", EMPTYSTR);
                    param = param.Trim('s');
                    if (float.TryParse(param, out value)) jumpToTime = value;
                }
            }

            // check for end param
            param = getUrlParam("aspect", EMPTYSTR);
            if (param.Contains(":"))
            {
                var pair = param.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (int.TryParse(pair[0], out int aspectWidth) && int.TryParse(pair[1], out int aspectHeight))
                    aspectRatio = (float)aspectWidth / aspectHeight;
                else aspectRatio = targetAspectRatio;
            }
            else if (float.TryParse(param, out value)) aspectRatio = value;
            else aspectRatio = targetAspectRatio;

            param = getUrlParam("3D", "1");
            var lastFull = video3dFull;
            var lastMode = video3d;
            if (int.TryParse(param, out check))
            {
                video3dFull = check < 0;
                check = Math.Abs(check);
                video3d = check > 4 ? TV3DMode.NONE : (TV3DMode)check;
            }
            else
            {
                video3dFull = false;
                video3d = TV3DMode.NONE;
            }

            if (video3d != lastMode)
            {
                SendManagedVariable(nameof(TVPlugin.OUT_MODE), video3d);
                SendManagedEvent(nameof(TVPlugin._Tv3DModeChange));
            }

            if (video3dFull != lastFull) SendManagedEvent(video3dFull ? nameof(TVPlugin._Tv3DWidthFull) : nameof(TVPlugin._Tv3DWidthHalf));

            if (IsDebugEnabled) Debug("Params set after video is ready");
            if (IsTraceEnabled) Trace($"Media Ready info loaded: start={startTime}, end={endTime}, t={jumpToTime}, loop={loop}, 3D={video3d}, 3D[Full]={video3dFull}");
        }

        private void cacheMediaChangeInfo()
        {
            string param = EMPTYSTR;
            // if retry is present without value, default to -1
            _TryGetUrlParam("retry", "-1", out param);
            int value;
            if (int.TryParse(param, out value)) retryCount = value;
            if (retryCount < 0) retryCount = int.MaxValue;

            // next live is used by video error to predict how a failure-before-successful-load should be handled.
            isNextLive = _HasUrlParam("live") || System.Array.IndexOf(liveProtocols, urlProtocol) > -1 || System.Array.IndexOf(liveDomains, urlDomain) > -1;

            if (IsTraceEnabled) Trace($"Change info loaded: retry={retryCount}, live={isNextLive}");
        }

        // === Public events to control the TV from user interfaces ===

        /// <summary>
        /// The nexus event/method which drives the logic for handling how the TV deals with changes to the active media.
        /// </summary>
        [PublicAPI]
        public void _RefreshMedia()
        {
            if (!init) return;

            if (loading)
            {
                if (IsWarnEnabled) Warn("Cannot change to another media while loading.");
                return; // disallow refreshing media while TV is loading another video
            }

            // disallow non-owners from changing media while the TV is running a managed or targeted events.
            if (runningEvents && !IsOwner) return;

            bool preApprovedUrl = CheckPreApprovedUrls(IN_MAINURL, IN_ALTURL);

            if (!CanPlayMedia && !(IsOwner && preApprovedUrl))
            {
                // if TV is locked without being privileged, force unset any requested URLs
                // This converts the command into a simple video refresh
                if (IsWarnEnabled) Warn("TV is locked. Cannot change media for un-privileged users.");
                IN_MAINURL = EMPTYURL;
                IN_ALTURL = EMPTYURL;
                IN_TITLE = EMPTYSTR;
                IN_NAME = EMPTYSTR;
            }

            // compare input URL and previous URL
            if (IN_MAINURL == null) IN_MAINURL = EMPTYURL;
            if (IN_ALTURL == null) IN_ALTURL = EMPTYURL;
            if (IN_TITLE == null) IN_TITLE = EMPTYSTR;
            string urlMainStr = IN_MAINURL.Get();
            string urlAltStr = IN_ALTURL.Get();
            bool hasMainUrl = !string.IsNullOrWhiteSpace(urlMainStr);
            bool hasAltUrl = !string.IsNullOrWhiteSpace(urlAltStr);
            bool hasTitle = !string.IsNullOrWhiteSpace(IN_TITLE);
            bool newMainUrl = hasMainUrl && urlMainStr != urlMain.Get();
            bool newAltUrl = hasAltUrl && urlAltStr != urlAlt.Get();
            bool newTitle = hasTitle && IN_TITLE != title;
            bool hasUrl = hasMainUrl || hasAltUrl;
            bool newUrl = newMainUrl || newAltUrl;

            lastTitle = title;

            if (hasUrl)
            {
                if (newUrl && IsDebugEnabled) Debug("New URL(s) detected.");

                if (!_CheckDomainWhitelist(urlMainStr, urlAltStr) && !preApprovedUrl)
                {
                    errorState = TVErrorState.BLOCKED;
                    _OnVideoPlayerError(VideoError.AccessDenied);
                    // deny access and exit logic
                    IN_MAINURL = EMPTYURL;
                    IN_ALTURL = EMPTYURL;
                    IN_TITLE = EMPTYSTR;
                    IN_NAME = EMPTYSTR;
                    return;
                }

                // when new URLs are detected, grab ownership to handle the sync data
                takeOwnership();
                RequestSync();
                // update relevant URL data
                urlMain = syncUrlMain = IN_MAINURL;
                urlAlt = syncUrlAlt = IN_ALTURL;
                title = syncTitle = IN_TITLE;
                addedBy = string.IsNullOrEmpty(IN_NAME) ? localPlayer.displayName : IN_NAME;
                urlRevision++;
                syncUrlRevision = urlRevision;
                // reset the alternate URL flag back to default
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                useAlternateUrl = preferAlternateUrlForQuest ? isAndroid : false;
                if (state == TVPlayState.WAITING)
                    state = syncState = stateOwner = TVPlayState.STOPPED;
                errorState = TVErrorState.NONE;
                // new URL, reset the retry flags
                retryingWithAlt = false;
                retriedWithAlt = false;
            }
            else if (newTitle)
            {
                if (IsDebugEnabled) Debug("Explicit title change detected.");
                // just the title is changing, skip reloading and just notify about the title change
                takeOwnership();
                RequestSync();
                title = syncTitle = IN_TITLE;
                IN_TITLE = EMPTYSTR;
                SendManagedVariable(nameof(TVPlugin.OUT_TITLE), title);
                SendManagedEvent(nameof(TVPlugin._TvTitleChange));
                return;
            }
            else
            {
                if (IsDebugEnabled) Debug("No URL change. Running generic reload.");
                // nothing is changing, thus a reload is taking place, pull from the synced variables
                urlMain = syncUrlMain;
                urlAlt = syncUrlAlt;
                title = syncTitle;
                addedBy = syncAddedBy;
            }

            IN_MAINURL = EMPTYURL;
            IN_ALTURL = EMPTYURL;
            IN_TITLE = EMPTYSTR;
            IN_NAME = EMPTYSTR;

            urlMainStr = urlMain.Get();
            urlAltStr = urlAlt.Get();

            // sanity checks
            if (urlMainStr == null)
            {
                urlMain = EMPTYURL;
                urlMainStr = EMPTYSTR;
            }

            if (urlAltStr == null)
            {
                urlAlt = EMPTYURL;
                urlAltStr = EMPTYSTR;
            }

            // graceful fallback checks
            if (urlAltStr == EMPTYSTR)
            {
                urlAlt = urlMain;
                urlAltStr = urlMainStr;
            }

            if (urlMainStr == EMPTYSTR)
            {
                urlMain = urlAlt;
                urlMainStr = urlAltStr;
            }

            if (urlMainStr == EMPTYSTR)
            {
                if (IsDebugEnabled) Debug("No URLs present. Skip.");
                return;
            }

            url = useAlternateUrl ? urlAlt : urlMain;
            if (stopMediaWhenHidden && manuallyHidden) return; // skip URL loading if media is force hidden
            string urlStr = url.Get();
            bool newMedia = urlStr.GetHashCode() != mediaHash;
            if (IsInfoEnabled) Info($"[{nextManager.gameObject.name}] loading URL by user '{addedBy}': {urlStr}");

            // when the media is not actually changing links, and it's currently loaded, run timestamp continuity
            if (!newMedia && (int)state > (int)TVPlayState.STOPPED)
            {
                _reloadTime = Time.timeSinceLevelLoad;
                // if epsilon is set prior to this point, there is a video swap going on. Use previous manager time instead.
                _reloadCache = syncTime;
                // skip timestamp continuity if the video is a retry attempt after an error
                jumpToTime = retrying || forceRestartMedia ? 0f : EPSILON;
                if (IsTraceEnabled) Trace($"Refresh, jump time: {jumpToTime} | retry {retrying} || force {forceRestartMedia}");
            }

            // if alternate URL is provided without a main URL and the user isn't assigned to use the alternate url, skip.
            if (IsTraceEnabled) Trace($"useAlt {useAlternateUrl} hasAlt {hasAltUrl} hasMain {hasMainUrl}");
            bool urlChange = !(!hasMainUrl && hasAltUrl && !useAlternateUrl);
            if (urlChange)
            {
                parseUrl(urlStr, out urlProtocol, out urlDomain, out urlParamKeys, out urlParamValues);

                // only cache once per url
                if (newMedia && errorState != TVErrorState.RETRY)
                {
                    retryCount = defaultRetryCount;
                    if (retryUsingAlternateUrl)
                        if (retryCount == 0)
                            if (urlMainStr != urlAltStr)
                                retryCount = 1;
                    cacheMediaChangeInfo();
                }

                loading = true;
                loadingCatchVideoEndEvent = true;
                waitingForMediaRefresh = false; // halt any queued refreshes
                locallyStopped = false;
                if (errorState == TVErrorState.BLOCKED) errorState = TVErrorState.NONE;
                nextManager.videoPlayer.LoadURL(url);
                // rate limit stuff
                nextUrlAttemptAllowed = Time.timeSinceLevelLoad + 6f;
            }

            if (urlChange)
            {
                SendManagedVariable(nameof(TVPlugin.OUT_URL), url);
                SendManagedEvent(nameof(TVPlugin._TvMediaChange));
            }

            if (urlChange && (errorState != TVErrorState.FAILED || waitingForMediaRefresh))
            {
                setLoadingState(true);
            }
        }

        public Texture _GetVideoTexture() => customTexture;


        // === Networked methods ===

        public void OWNER_RequestSyncData() => RequestSync();

        public void ALL_QuickReSync()
        {
            if (syncToOwner && !IsOwner)
            {
                if (IsTraceEnabled) Trace($"Resync triggered by network. Waiting 0.2f seconds");
                triggerSync(0.3f);
            }
        }

        public void ALL_ManualReSync()
        {
            if (syncToOwner)
            {
                if (IsTraceEnabled) Trace($"Resync triggered by network. Waiting {syncEnforcementTimeLimit} seconds");
                triggerSync(syncEnforcementTimeLimit);
            }
        }

        public void ALL_OwnerEnabled()
        {
            Debug("Enabling owner via Network call");
            ownerDisabled = false;
        }

        public void ALL_OwnerDisabled()
        {
            Debug("Disabling owner via Network call");
            ownerDisabled = true;
        }
    }
}