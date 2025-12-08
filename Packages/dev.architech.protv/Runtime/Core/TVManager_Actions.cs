using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech.ProTV
{
    // This partial contains all the events for controlling the state of the TV
    // that are exposed for public consumption such as for other udon behaviours or UI event calls
    public partial class TVManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_RefreshMedia"/>
        [PublicAPI]
        public void _ReloadMedia()
        {
            forceRestartMedia = false;
            triggerRefresh(0f, nameof(_ReloadMedia));
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_ChangeMedia(VRCUrl, VRCUrl, string, string)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_MAINURL"/>, <see cref="IN_ALTURL"/> and <see cref="IN_TITLE"/> variables.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_ChangeMedia(VRCUrl, VRCUrl, string, string)"/>
        /// <seealso cref="_RefreshMedia"/>
        [PublicAPI]
        public void _ChangeMedia()
        {
            // when explicitly changing the URL, even if it's the same, the media should restart.
            // This flag ensures that the jumpToTime value is 0 for the media.
            forceRestartMedia = !IN_MAINURL.Equals(EMPTYURL) || !IN_ALTURL.Equals(EMPTYURL);
            // refresh next frame
            triggerRefresh(0f, nameof(_ChangeMedia));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_mainUrl">Primary URL for users to resolve</param>
        /// <seealso cref="_ChangeMedia(VRCUrl, VRCUrl, string, string)"/>
        /// <seealso cref="_RefreshMedia"/>
        public void _ChangeMedia(VRCUrl _mainUrl)
        {
            if (_mainUrl != null) IN_MAINURL = _mainUrl;
            // when explicitly changing the URL, even if it's the same, the media should restart.
            // This flag ensures that the jumpToTime value is 0 for the media.
            forceRestartMedia = !IN_MAINURL.Equals(EMPTYURL) || !IN_ALTURL.Equals(EMPTYURL);
            // refresh next frame
            triggerRefresh(0f, nameof(_ChangeMedia));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainUrl">Primary URL for users to resolve</param>
        /// <param name="alternateUrl">Secondary URL as backup, alternate use or use by non PC platforms, depending on the configuration of the TV</param>
        /// <param name="titleStr">Custom title, pass in empty string or null to use default</param>
        /// <param name="adder"></param>
        /// <seealso cref="_RefreshMedia"/>
        [PublicAPI]
        public void _ChangeMedia(VRCUrl mainUrl, VRCUrl alternateUrl, string titleStr, string adder = null)
        {
            if (mainUrl != null) IN_MAINURL = mainUrl;
            if (alternateUrl != null) IN_ALTURL = alternateUrl;
            if (titleStr != null) IN_TITLE = titleStr;
            if (adder != null) IN_NAME = adder;
            // when explicitly changing the URL, even if it's the same, the media should restart.
            // This flag ensures that the jumpToTime value is 0 for the media.
            forceRestartMedia = !IN_MAINURL.Equals(EMPTYURL) || !IN_ALTURL.Equals(EMPTYURL);
            // refresh next frame
            triggerRefresh(0f, nameof(_ChangeMedia));
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ChangeTitle(string titleStr)
        {
            IN_MAINURL = EMPTYURL;
            IN_ALTURL = EMPTYURL;
            if (titleStr != null) IN_TITLE = titleStr;
            IN_NAME = EMPTYSTR;
            triggerRefresh(0f, nameof(_ChangeTitle));
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _TogglePlay()
        {
            if (!isReady) return;
            if (state == TVPlayState.PLAYING) _Pause();
            else if (state == TVPlayState.PAUSED) _Play();
            else if ((int)state <= (int)TVPlayState.STOPPED) triggerRefresh();
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Play()
        {
            if (!isReady || loading) return;

            // if owner is paused, prevent non-owner from playing video if they are syncing to owner
            if (syncToOwner && stateOwner == TVPlayState.PAUSED && !IsOwner) return;
            if (playStateTakesOwnership && CanPlayMedia) takeOwnership();
            play();
        }

        /// <summary>
        /// 
        /// </summary>
        private void play()
        {
            if ((int)state <= (int)TVPlayState.STOPPED)
            {
                if (IsDebugEnabled) Debug("Refresh video via Play");
                triggerRefresh(0f, nameof(play));
                return;
            }

            var vp = activeManager.videoPlayer;
            RequestSync();
            // if media is at end and user forces play, force loop the media one time if the media isn't in a stopped state
            if (mediaEnded)
            {
                mediaEnded = false;
                manualLoop = true;
                currentTime = syncTime = startTime;
                setTime(vp, startTime);
                SendManagedEvent(nameof(TVPlugin._TvMediaLoop));
            }

            vp.Play();
            state = TVPlayState.PLAYING;
            forceBlitOnce = true;
            locallyPaused = false;
            SendManagedEvent(nameof(TVPlugin._TvPlay));

            if (!playbackEnabled)
            {
                playbackEnabled = true;
                SendManagedEvent(nameof(TVPlugin._TvPlaybackStart));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Pause()
        {
            if (!isReady || loading) return;
            if ((int)state <= (int)TVPlayState.STOPPED) return; // nothing to pause
            if (playStateTakesOwnership && CanPlayMedia) takeOwnership();
            if (playStateTakesOwnership)
            {
                if (CanPlayMedia) takeOwnership();
                else if (!enableLocalPause) return;
            }

            if (enableLocalPause) locallyPaused = !IsOwner; // flag to determine if pause was locally triggered by a non-owner
            pause();
        }

        /// <summary>
        /// 
        /// </summary>
        private void pause()
        {
            if ((int)state <= (int)TVPlayState.STOPPED) return; // nothing to pause
            var vp = activeManager.videoPlayer;
            vp.Pause();
            RequestSync();
            // only run a delayed resync when it's not locally paused
            if (!locallyPaused) triggerSync(0.2f);
            state = TVPlayState.PAUSED;
            forceBlitOnce = true;
            SendManagedEvent(nameof(TVPlugin._TvPause));
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Stop()
        {
            if (!isReady) return;
            if (playStateTakesOwnership && CanPlayMedia) takeOwnership();
            stop();
        }

        /// <summary>
        /// 
        /// </summary>
        private void stop(bool force = false)
        {
            locallyPaused = false;
            locallyStopped = true;

            if (loading)
            {
                Info($"Stop called while loading");
                // if stop is called while loading a video, the video loading will be halted instead of the active player
                if (errorState == TVErrorState.NONE) nextManager.Stop();
                else errorState = TVErrorState.NONE;
                loading = false;
                SendManagedEvent(nameof(TVPlugin._TvLoadingAbort));
                if (!disabled && !force && state != TVPlayState.STOPPED) return;
            }

            Debug("Stopping current media");
            activeManager.Stop();
            state = TVPlayState.STOPPED;
            if (IsOwner)
            {
                setTime(activeManager.videoPlayer, startTime);
                RequestSync();
            }

            setLoadingState(false);
            forceBlitOnce = true;
            waitingForMediaRefresh = false; // halt any queued refreshes
            retryCount = 0;
            SendManagedEvent(nameof(TVPlugin._TvStop));
            errorState = TVErrorState.NONE;

            if (playbackEnabled)
            {
                playbackEnabled = false;
                SendManagedEvent(nameof(TVPlugin._TvPlaybackEnd));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Skip()
        {
            if (!isReady) return;
            if (mediaEnded || loading) return; // nothing to skip currently
            if (IsOwner) syncTime = -1f;
            else if (CanPlayMedia)
            {
                takeOwnership();
                syncTime = -1f;
            }
            // else user doesn't have enough privilege
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Hide()
        {
            if (!isReady) return;
            if (stopMediaWhenHidden)
            {
                if (IsOwner) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerDisabled));
                _Stop();
            }
            else activeManager.Hide();

            manuallyHidden = true;
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Show()
        {
            if (!isReady) return;
            manuallyHidden = false;
            if (stopMediaWhenHidden)
            {
                if (IsOwner) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerEnabled));
                _Play();
            }
            else activeManager.Show();
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ToggleHidden()
        {
            if (manuallyHidden) _Show();
            else _Hide();
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _UseMainUrl() => _ChangeUrlMode(false);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _UseAlternateUrl() => _ChangeUrlMode(true);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ToggleUrlMode() => _ChangeUrlMode(!useAlternateUrl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="useAlternate"></param>
        public void _ChangeUrlMode(bool useAlternate)
        {
            if (!isReady || useAlternateUrl == useAlternate) return;
            useAlternateUrl = useAlternate;
            triggerRefresh(0f, nameof(_ChangeUrlMode));
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_ChangeVideoPlayer(int)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_VIDEOPLAYER"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_ChangeVideoPlayer(int)"/>
        [PublicAPI]
        public void _ChangeVideoPlayer()
        {
            _ChangeVideoPlayer(IN_VIDEOPLAYER);
            IN_VIDEOPLAYER = -1;
        }

        // equivalent to: udonBehavior.SetProgramVariable("IN_VIDEOPLAYER", (int) index); udonBehavior.SendCustomEvent("_ChangeVideoPlayer");
        /// <summary>
        /// 
        /// </summary>
        /// <param name="useVideoPlayer"></param>
        [PublicAPI]
        public void _ChangeVideoPlayer(int useVideoPlayer)
        {
            if (!isReady) return;

            // no need to change if same is picked
            if (useVideoPlayer == videoPlayer) return;
            // invalid data provided
            if (useVideoPlayer < 0 || useVideoPlayer >= videoManagers.Length)
            {
                Error($"Invalid Video Player index value: Expected between 0 and {videoManagers.Length - 1} - Requested {useVideoPlayer}");
                return;
            }

            if (syncVideoManagerSelection)
            {
                if (syncToOwner && !IsOwner && _IsAuthorized())
                {
                    Trace($"Taking ownership via _ChangeVideoPlayer({useVideoPlayer})");
                    takeOwnership();
                }

                if (IsOwner) RequestSync();
            }

            // do not allow changing resolution while a video is loading.
            bool changeNotAllowed = loading || (!allowLocalTweaks && syncVideoManagerSelection && !IsOwner);

            changeVideoPlayer(useVideoPlayer, changeNotAllowed);

            if (useVideoPlayer == videoPlayer && IsInfoEnabled)
                Info($"Switching {nameof(activeManager)} to: [{nextManager.gameObject.name}]");
        }

        /// <seealso cref="prepareMedia"/>
        private void changeVideoPlayer(int useVideoPlayer, bool revert = false)
        {
            // When player is loading, but stopped, force stop any loading and prioritize the player swap
            if (!revert)
            {
                videoPlayer = useVideoPlayer;
                if (loading)
                    if (state == TVPlayState.STOPPED)
                        stop(true);
                prevManager = activeManager;
                nextManager = videoManagers[videoPlayer];
                if (prevManager == null)
                    prevManager = activeManager;
                nextManager.gameObject.SetActive(true);
                if (IsTraceEnabled)
                {
                    string nMan = nextManager == null ? "null" : nextManager.gameObject.name;
                    string aMan = activeManager == null ? "null" : activeManager.gameObject.name;
                    string pMan = prevManager == null ? "null" : prevManager.gameObject.name;
                    Trace($"Manager swap: Next '{nMan}' -> Active '{aMan}' -> Prev '{pMan}'");
                }
            }

            SendManagedVariable(nameof(TVPlugin.OUT_VIDEOPLAYER), videoPlayer);
            SendManagedEvent(nameof(TVPlugin._TvVideoPlayerChange));

            if (!revert && !loading && state != TVPlayState.WAITING)
            {
                if (state == TVPlayState.STOPPED)
                {
                    nextManager.videoPlayer.Stop();
                    return;
                }

                if (IsTraceEnabled) Trace("Changing video player. Jumping via EPSILON.");
                jumpToTime = EPSILON;
                forceRestartMedia = false;
                triggerRefresh(0f, nameof(changeVideoPlayer));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Mute() => _ChangeMute(true);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _UnMute() => _ChangeMute(false);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ToggleMute() => _ChangeMute(!mute);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isMute"></param>
        [PublicAPI]
        public void _ChangeMute(bool isMute)
        {
            if (!isReady || isMute == mute) return;
            mute = isMute;
            changeMute(mute);
        }

        private void changeMute(bool isMute)
        {
            activeManager.ChangeMute(isMute);
            SendManagedEvent(isMute ? nameof(TVPlugin._TvMute) : nameof(TVPlugin._TvUnMute));
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_ChangeVolume(float, bool)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_VOLUME"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_ChangeVolume(float, bool)"/>
        [PublicAPI]
        public void _ChangeVolume()
        {
            _ChangeVolume(IN_VOLUME);
            IN_VOLUME = 0f;
        }

        // equivalent to: udonBehavior.SetProgramVariable("IN_VOLUME", (float) volumePercent); udonBehavior.SendCustomEvent("_ChangeVolume");
        /// <summary>
        /// 
        /// </summary>
        /// <param name="useVolume">Value to set the volume to (0 to 1)</param>
        /// <param name="suppress">Pass as true to prevent the managed events from firing off. Generally used during a drag action for a slider.</param>
        [PublicAPI]
        public void _ChangeVolume(float useVolume, bool suppress = false)
        {
            if (!isReady || useVolume == volume) return;
            var isOwner = IsOwner;
            var changeNotAllowed = !allowLocalTweaks && syncVolumeControl && !isOwner;
            changeVolume(useVolume, suppress, changeNotAllowed);
            if (!suppress && syncVolumeControl && isOwner) RequestSync();
        }

        private void changeVolume(float useVolume, bool suppress = false, bool revert = false)
        {
            if (!revert)
            {
                activeManager.ChangeVolume(useVolume, suppress);
                if (suppress) return;
                volume = useVolume;
            }
            else if (suppress) return;

            SendManagedVariable(nameof(TVPlugin.OUT_VOLUME), volume);
            SendManagedEvent(nameof(TVPlugin._TvVolumeChange));
        }

        /// <summary>
        /// This event updates the flag that is used to determine whether to render a 3D video in stereo or not.
        /// Does not apply during the blit operation, but instead is passed as a "request" type of value to the shaders
        /// via the shader _VideoData matrix.
        /// </summary>
        /// <seealso cref="updateShaderData"/>
        public void _ToggleVideoForce2d() => force2D = !force2D;

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_ChangeColorCorrection"/>
        public void _ToggleColorCorrection() => _ChangeColorCorrection(skipGamma);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_ChangeColorCorrection"/>
        public void _EnableColorCorrection() => _ChangeColorCorrection(true);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_ChangeColorCorrection"/>
        public void _DisableColorCorrection() => _ChangeColorCorrection(false);

        /// <summary>
        /// Method that can be called to disable the blit operation's gamma correction for AVPro.
        /// AMD GPUs running in software rendering use linear space already, so disable gamma correction to prevent videos being too dim.
        /// </summary>
        public void _ChangeColorCorrection(bool enable)
        {
            skipGamma = !enable;
            forceBlitOnce = true;
            SendManagedEvent(enable ? nameof(TVPlugin._TvColorSpaceCorrected) : nameof(TVPlugin._TvColorSpaceRaw));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        public void _Change3DMode(int mode)
        {
            if (!isReady || mode == (int)video3d) return;
            var isOwner = IsOwner;
            var changeNotAllowed = !allowLocalTweaks && syncVideoMode && !isOwner;
            changeVideo3dMode(mode, changeNotAllowed);
            if (isOwner && syncVideoMode) RequestSync();
        }

        private void changeVideo3dMode(int mode, bool revert = false)
        {
            if (!revert)
            {
                video3d = mode > 4 ? TV3DMode.NONE : (TV3DMode)mode;
                forceBlitOnce = true;
            }

            SendManagedVariable(nameof(TVPlugin.OUT_MODE), video3d);
            SendManagedEvent(nameof(TVPlugin._Tv3DModeChange));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_Change3DWidth(bool)"/>
        public void _Toggle3DWidth() => _Change3DWidth(!video3dFull);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_Change3DWidth(bool)"/>
        public void _Width3DFull() => _Change3DWidth(true);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_Change3DWidth(bool)"/>
        public void _Width3DHalf() => _Change3DWidth(false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullWidth"></param>
        public void _Change3DWidth(bool fullWidth)
        {
            if (!isReady || fullWidth == video3dFull) return;
            var isOwner = IsOwner;
            var changeNotAllowed = !allowLocalTweaks && syncVideoMode && !isOwner;
            changeVideo3dWidth(fullWidth, changeNotAllowed);
            if (isOwner && syncVideoMode) RequestSync();
        }

        private void changeVideo3dWidth(bool fullWidth, bool revert = false)
        {
            if (!revert)
            {
                video3dFull = fullWidth;
                forceBlitOnce = true;
            }

            SendManagedEvent(video3dFull ? nameof(TVPlugin._Tv3DWidthFull) : nameof(TVPlugin._Tv3DWidthHalf));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_ChangeAudioMode(bool)"/>
        [PublicAPI]
        public void _AudioMode3d() => _ChangeAudioMode(true);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_ChangeAudioMode(bool)"/>
        [PublicAPI]
        public void _AudioMode2d() => _ChangeAudioMode(false);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="_ChangeAudioMode(bool)"/>
        [PublicAPI]
        public void _ToggleAudioMode() => _ChangeAudioMode(!audio3d);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="use3dAudio"></param>
        [PublicAPI]
        public void _ChangeAudioMode(bool use3dAudio)
        {
            if (!isReady || use3dAudio == audio3d) return;
            var isOwner = IsOwner;
            var changeNotAllowed = !allowLocalTweaks && syncAudioMode && !isOwner;
            changeAudioMode(use3dAudio, changeNotAllowed);
            if (isOwner && syncAudioMode) RequestSync();
        }

        private void changeAudioMode(bool use3dAudio, bool revert = false)
        {
            if (!revert)
            {
                audio3d = use3dAudio;
                activeManager.ChangeAudioMode(use3dAudio);
            }

            SendManagedEvent(audio3d ? nameof(TVPlugin._TvAudioMode3d) : nameof(TVPlugin._TvAudioMode2d));
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ReSync()
        {
            if (!isReady) return;
            if (syncToOwner) triggerSync(0f);
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _Sync() => _ChangeSync(true);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _DeSync() => _ChangeSync(false);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ToggleSync() => _ChangeSync(!syncToOwner);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sync"></param>
        [PublicAPI]
        public void _ChangeSync(bool sync)
        {
            if (!isReady) return;
            syncToOwner = sync;
            enforceSyncTime = sync;
            SendManagedEvent(sync ? nameof(TVPlugin._TvSync) : nameof(TVPlugin._TvDeSync));
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _LoopStart() => _ChangeLoop(true);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _LoopStop() => _ChangeLoop(false);

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ToggleLoop() => _ChangeLoop(loop == 0);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loopMedia"></param>
        [PublicAPI]
        public void _ChangeLoop(bool loopMedia)
        {
            if (!isReady) return;
            bool noChangeAllowed = !IsOwner;
            int loopCount = loopMedia ? int.MaxValue : 0;
            changeLoop(loopCount, noChangeAllowed);
            if (IsOwner) RequestSync();
        }

        private void changeLoop(int loopCount, bool revert = false)
        {
            var isChange = (loop > 0) != (loopCount > 0) || revert;
            if (!revert) loop = loopCount;
            Debug($"Loop Check: revert {revert} is change {isChange} loop count {loopCount}");
            if (isChange) SendManagedEvent(loop > 0 ? nameof(TVPlugin._TvEnableLoop) : nameof(TVPlugin._TvDisableLoop));
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in SeeAlso.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_SEEK"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_ChangeSeekPercent(float, bool)"/>
        [PublicAPI]
        public void _ChangeSeekPercent()
        {
            _ChangeSeekPercent(IN_SEEK);
            IN_SEEK = 0f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seekPercent"></param>
        /// <param name="suppress"></param>
        [PublicAPI]
        public void _ChangeSeekPercent(float seekPercent, bool suppress = false)
        {
            if (!isReady || isLive) return;
            // map the percent value to the range of the start and end time to the target timestamp
            var seekTime = (endTime - startTime) * Mathf.Clamp01(seekPercent) + startTime;
            _ChangeSeekTime(seekTime, suppress);
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in SeeAlso.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_SEEK"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_ChangeSeekTime(float, bool)"/>
        [PublicAPI]
        public void _ChangeSeekTime()
        {
            _ChangeSeekTime(IN_SEEK);
            IN_SEEK = 0f;
        }

        // equivalent to: udonBehavior.SetProgramVariable("IN_SEEK", (float) seekPercent); udonBehavior.SendCustomEvent("_ChangeSeekTime");
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="suppress"></param>
        [PublicAPI]
        public void _ChangeSeekTime(float seconds, bool suppress = false)
        {
            if (!isReady || isLive || loading) return;
            var oldLevel = LoggingLevel;
            if (suppress) LoggingLevel = ATLogLevel.ALWAYS;
            if (!IsOwner || !CanPlayMedia)
            {
                if (suppress) LoggingLevel = oldLevel;
                return;
            }

            var vp = ActiveManager.videoPlayer;
            setTime(vp, Mathf.Clamp(seconds + seekOffset, startTime, endTime));
            currentTime = Mathf.Clamp(seconds, startTime, endTime);
            forceBlitOnce = true;
            if (!suppress && !runningEvents)
            {
                SendManagedVariable(nameof(TVPlugin.OUT_SEEK), currentTime);
                SendManagedEvent(nameof(TVPlugin._TvSeekChange));
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_QuickReSync));
            if (suppress) LoggingLevel = oldLevel;
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _SeekForward()
        {
            if (!isReady) return;
            if (isLive) enforceSyncTime = true;
            else _ChangeSeekTime(currentTime + 10f);
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _SeekBackward()
        {
            if (!isReady) return;
            if (isLive) enforceSyncTime = true;
            else _ChangeSeekTime(currentTime - 10f);
        }

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        public void _ChangeSeekOffset()
        {
            _ChangeSeekOffset(IN_SEEK);
            IN_SEEK = 0f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        [PublicAPI]
        public void _ChangeSeekOffset(float offset)
        {
            var lastOffset = seekOffset;
            seekOffset = offset;
            setTime(activeManager.videoPlayer, Mathf.Clamp(currentTime + offset, startTime, endTime));
            if (seekOffset != lastOffset)
            {
                SendManagedVariable(nameof(TVPlugin.OUT_SEEK), seekOffset);
                SendManagedEvent(nameof(TVPlugin._TvSeekOffsetChange));
            }
        }

        /// <summary>
        /// Update the current manager's playback speed.
        /// Value will be clamped between 0.5f and 2f.
        /// </summary>
        [PublicAPI]
        public void _ChangePlaybackSpeed()
        {
            _ChangePlaybackSpeed(IN_SPEED);
            IN_SPEED = 1f;
        }

        /// <summary>
        /// Update the current manager's playback speed.
        /// Value will be clamped between 0.5f and 2f.
        /// </summary>
        /// <param name="speed">The relative speed adjustment desired (allows 0.5f to 2f)</param>
        [PublicAPI]
        public void _ChangePlaybackSpeed(float speed)
        {
            if (!isReady || isLive || !CanModifySyncVPManagerData || !ActiveManager.ValidMediaController) return;
            changePlaybackSpeed(speed);
            if (IsOwner) RequestSync();
        }

        /// <summary>
        /// Update the playback speed back to normal speed.
        /// </summary>
        /// <seealso cref="_ChangePlaybackSpeed(float)"/>
        public void _ResetPlaybackSpeed() => _ChangePlaybackSpeed(1f);

        private void changePlaybackSpeed(float speed)
        {
            var manager = ActiveManager;
            if (IsTraceEnabled) Trace($"Setting playback speed: {manager.playbackSpeed} -> {speed}");
            if (manager.playbackSpeed == speed) return;
            manager.ChangePlaybackSpeed(speed);
            playbackSpeed = manager.playbackSpeed;
            if (IsTraceEnabled) Trace($"Playback speed set to {playbackSpeed}");
            SendManagedVariable(nameof(TVPlugin.OUT_SPEED), playbackSpeed);
            SendManagedEvent(nameof(TVPlugin._TvPlaybackSpeedChange));
        }

        /// <summary>
        /// Explicitly update the lock state to on/enabled when calling this event.
        /// </summary>
        /// <seealso cref="_ChangeLock"/>
        [PublicAPI]
        public void _Lock() => _ChangeLock(true);

        /// <summary>
        /// Explicitly update the lock state to off/disabled when calling this event.
        /// </summary>
        /// <seealso cref="_ChangeLock"/>
        [PublicAPI]
        public void _UnLock() => _ChangeLock(false);

        /// <summary>
        /// Each time this event is called, it will alternate between the locked and unlocked state.
        /// </summary>
        /// <seealso cref="_ChangeLock"/>
        [PublicAPI]
        public void _ToggleLock() => _ChangeLock(!locked);

        /// <summary>
        /// Method for changing the locked flag to the desired state.
        /// Will check for authorization and update necessary conditions for ownership.
        /// If locked by a super user, a special super user flag is enabled
        /// and will limit the TV to super users only until released.  
        /// </summary>
        /// <param name="lockActive">Specify if the TV should be in a locked state or not</param>
        [PublicAPI]
        public void _ChangeLock(bool lockActive)
        {
            if (!isReady) return;
            if (_IsAuthorized())
            {
                // if locked when not the owner, first steal ownership back
                // call this method again to subsequently do the actual unlock
                if (locked && !IsOwner)
                {
                    lockedBySuper = _IsSuperAuthorized();
                    takeOwnership();
                    RequestSync();
                }
                else if (takeOwnership())
                {
                    lockedBySuper = lockActive && _IsSuperAuthorized();
                    locked = lockActive;
                    RequestSync();
                    SendManagedEvent(locked ? nameof(TVPlugin._TvLock) : nameof(TVPlugin._TvUnLock));
                }
            }
        }

        /// <summary>
        /// Explicitly disable the interactions of UI elements that are children of
        /// any listener scripts when calling this event.
        /// </summary>
        /// <seealso cref="_ChangeInteractions"/>
        [PublicAPI]
        public void _EnableInteractions() => _ChangeInteractions(true);

        /// <summary>
        /// Explicitly disable the interactions of UI elements that are children of
        /// any listener scripts when calling this event.
        /// </summary>
        /// <seealso cref="_ChangeInteractions"/>
        [PublicAPI]
        public void _DisableInteractions() => _ChangeInteractions(false);

        /// <summary>
        /// Each time this event is called, it will alternate between the enabled and disabled state for the
        /// interactions of UI elements that are children of any listener scripts.
        /// </summary>
        /// <seealso cref="_ChangeInteractions"/>
        [PublicAPI]
        public void _ToggleInteractions() => _ChangeInteractions(!interactionState);

        /// <summary>
        /// This method searches through all attached listener scripts and hunts for any child objects that have
        /// a VRCUiShape component on them. Then for each of those it finds any attached collider(s)
        /// and either disables or enables the component. This prevents/allows the VRC raycast being able to
        /// 'hit' the elements, thus modifying the interactability of a given UI.
        /// </summary>
        /// <param name="newState">Explicitly pass the desired enable/disable state for the interactions.</param>
        [PublicAPI]
        public void _ChangeInteractions(bool newState)
        {
            interactionState = newState;
            if (!isReady || !_sendEvents) return;
            foreach (UdonSharpBehaviour target in _eventListeners)
            {
                if (target == null) continue;
                var uiShapes = target.gameObject.GetComponentsInChildren(typeof(VRCUiShape), true);
                foreach (Component uiShape in uiShapes)
                {
                    var interactables = uiShape.GetComponents<Collider>();
                    foreach (Collider interactable in interactables)
                        interactable.enabled = newState;
                }
            }
        }

        public void _DisableVideoTexture()
        {
            disableVideo = true;
            forceBlitOnce = true;
        }

        public void _EnableVideoTexture()
        {
            disableVideo = false;
            forceBlitOnce = true;
        }

        public void _ToggleVideoTexture()
        {
            disableVideo = !disableVideo;
            forceBlitOnce = true;
        }


        public void _EnableGlobalTexture()
        {
            enableGSV = true;
            forceBlitOnce = true;
        }

        public void _DisableGlobalTexture()
        {
            enableGSV = false;
            forceBlitOnce = true;
        }

        public void _ToggleGlobalTexture()
        {
            enableGSV = !enableGSV;
            forceBlitOnce = true;
        }

        public bool _IsManagedSpeaker(AudioSource source)
        {
            foreach (var manager in videoManagers)
            {
                var isManaged = manager.IsManagedSpeaker(source);
                if (isManaged) return true;
            }

            return false;
        }

        [PublicAPI]
        public void _AddCustomMaterialTarget(Material mat, string materialProperty)
        {
            if (mat == null) return;
            var index = System.Array.IndexOf(customMaterials, (object)null);
            if (index == -1)
            {
                // no empty material slots in array, expand array
                index = customMaterials.Length;
                var newLen = index + 1;
                Material[] mats = new Material[newLen];
                string[] props = new string[newLen];
                int[] propIds = new int[newLen];
                System.Array.Copy(customMaterials, mats, index);
                System.Array.Copy(customMaterialProperties, props, index);
                System.Array.Copy(shaderIDs_MaterialProperties, propIds, index);
                customMaterials = mats;
                customMaterialProperties = props;
                shaderIDs_MaterialProperties = propIds;
            }

            customMaterials[index] = mat;
            customMaterialProperties[index] = materialProperty;
            shaderIDs_MaterialProperties[index] = VRCShader.PropertyToID(materialProperty);
            _hasCustomMaterials = customMaterials.Length > 0;
        }

        [PublicAPI]
        public void _RemoveCustomMaterialTarget(Material mat)
        {
            var index = System.Array.IndexOf(customMaterials, mat);
            if (index > -1)
            {
                customMaterials[index] = null;
                customMaterialProperties[index] = null;
                shaderIDs_MaterialProperties[index] = 0;
            }
            // otherwise ignore call if material isn't in the list
        }

        #region Pixel Extraction (Broken Currently)

        // Feature is currently broken. Needs revisited.
        /// <summary>
        /// Retrives the entire array of pixels from the most recent successful pixel extraction.
        /// Array will be empty if the pixel extraction flag has never ran or been enabled.
        /// </summary>
        /// <returns>the full source array of pixels</returns>
        // [PublicAPI]
        private Color32[] _GetPixels()
        {
            pixels = ActiveManager.pixels;
            return pixels;
        }

        /// <summary>
        /// Retrieves a 1D slice of the pixel array from the most recent successful pixel extraction.
        /// Array will be empty if the pixel extraction flag has never ran or been enabled.
        /// </summary>
        /// <param name="offset">The start position from the beinging of the pixels array</param>
        /// <param name="length">The size of the array to extract from the offset</param>
        /// <returns>newly sliced array of the desired pixels</returns>
        // [PublicAPI]
        private Color32[] _GetPixels(int offset, int length)
        {
            // if length 0 is requested, return empty array.
            if (length == 0) return new Color32[0];
            var srcPixels = ActiveManager.pixels;
            var srcLength = srcPixels.Length;
            // if the source array is empty, just return plainly.
            // generally means that the flag for pixel extraction was never enabled.
            if (srcLength == 0) return srcPixels;
            Color32[] targetPixels = new Color32[length];
            if (offset > srcLength)
            {
                Warn("Length exceeds the source array size. Returning empty pixels.");
                pixels = targetPixels;
                return targetPixels;
            }

            // to avoid array out of bounds issues, trim the array copy size per row to the smallest width value.
            var copyLength = Math.Min(srcLength - offset, length);
            System.Array.Copy(srcPixels, offset, targetPixels, 0, copyLength);
            pixels = targetPixels;
            return targetPixels;
        }

        /// <seealso cref="_GetPixels(int, int, int, int)"/>
        private Color32[] _GetPixels(Rect area) => _GetPixels((int)area.x, (int)area.y, (int)area.width, (int)area.height);

        /// <summary>
        /// Retrieves a 2D slice of the pixel array from the most recent successful pixel extraction.
        /// Array will be empty if the pixel extraction flag has never ran or been enabled.
        /// Any pixels expected by the slice that fall outside the source texture's width/height
        /// will be left as the default 'clear' color (r=0 b=0 g=0 a=0).
        /// You will need to keep track of the target width/height yourself if you intend to operate on it
        /// as 2D array. This method only returns a 1D array.
        /// </summary>
        /// <example>
        ///     Source Texture is 128x128y, target slice is 64x64y offset from origin by 96x96y
        ///     This makes the resulting array contain the following:
        ///     Source data from 96x96y through 128x128y (which fills only 25% of the desired slice aka a 32x32y slice)
        ///     The remaining 75% contain 'clear' pixels since there was no source data overlapping that part of the slice.
        /// </example>
        /// <param name="x">The horizontal start position from the origin of the texture</param>
        /// <param name="y">The vertical start position from the origin of the texture</param>
        /// <param name="width">The horizontal size to extract</param>
        /// <param name="height">The vertical size to extract</param>
        /// <returns>newly sliced array of the desired pixels</returns>
        [PublicAPI]
        private Color32[] _GetPixels(int x, int y, int width, int height)
        {
            // if either desired dimension is 0, there is nothing to return.
            if (width == 0 || height == 0) return new Color32[0];
            var srcPixels = ActiveManager.pixels;
            // if the source array is empty, just return plainly.
            // generally means that the flag for pixel extraction was never enabled.
            if (srcPixels.Length == 0) return srcPixels;
            var srcDims = ActiveManager.pixelDims;
            var srcWidth = srcDims.x;
            var srcHeight = srcDims.y;
            Color32[] targetPixels = new Color32[width * height];
            if (x >= srcWidth || y >= srcHeight)
            {
                Warn("X exceeds the source Width or Y exceeds the source Height. Returning empty pixels.");
                pixels = targetPixels;
                return targetPixels;
            }

            // to avoid array out of bounds issues, trim the array copy size per row to the smallest width value.
            var copyWidth = Math.Min(srcWidth - x, width);
            // copying from rows that don't exist won't work, so pick the smaller value between the two.
            var copyHeight = Math.Min(srcHeight - y, height);
            var srcOffset = srcWidth * y + x;
            var targetOffset = 0;
            for (var i = 0; i < copyHeight; i++)
            {
                System.Array.Copy(srcPixels, srcOffset, targetPixels, targetOffset, copyWidth);
                // go to next row
                srcOffset += srcWidth;
                targetOffset += width;
            }

            pixels = targetPixels;
            return targetPixels;
        }

        #endregion
    }
}