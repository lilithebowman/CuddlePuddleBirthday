using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;

#pragma warning disable CS0618

namespace ArchiTech.ProTV
{
    public abstract class TVPlugin : ATEventHandler
    {
        protected readonly VRCUrl EMPTYURL = VRCUrl.Empty;

        [I18nInspectorName("TV"), I18nTooltip("The desired TV reference in the current scene. Plugin will automatically connect to the TV on start to receive events.")]
        public TVManager tv;

        protected bool hasTV;

        /// <summary>
        /// This value is assigned for the <see cref="_TvVideoPlayerError"/> event and contains the enum (int) value of what the backing video player provided.
        /// </summary>
        protected internal VideoError OUT_ERROR;

        /// <summary>
        /// This value is assigned for the <see cref="_TvVolumeChange"/> event and contains the decimal percentage value of the TV's volume.
        /// This is typically used to update some volume display or check if the volume is effectively mute.
        /// </summary>
        protected internal float OUT_VOLUME;

        /// <summary>
        /// This value is assigned for the <see cref="_TvVideoPlayerChange"/> event and contains the array index value of the TV's videoManagers list.
        /// You can use this integer to handle pulling different data from custom arrays you implement.
        /// </summary>
        protected internal int OUT_VIDEOPLAYER;

        /// <summary>
        /// This value is assigned for the <see cref="_Tv3DModeChange"/> event and contains the current enabled 3D mode.
        /// </summary>
        protected internal int OUT_MODE;

        /// <summary>
        /// This value is assigned for the <see cref="_TvOwnerChange"/> event and contains the playerId of the new owner.
        /// </summary>
        protected internal int OUT_OWNER;

        /// <summary>
        /// This value is assigned for the <see cref="_TvSeekChange"/> and <see cref="_TvSeekOffsetChange"/> events.
        /// </summary>
        protected internal float OUT_SEEK;

        /// <summary>
        /// This value is assigned for that <see cref="_TvPlaybackSpeedChange"/> event and contains the relative speed of the current video player.
        /// This value is always clamped between 0.5f and 2f.
        /// </summary>
        protected internal float OUT_SPEED;

        /// <summary>
        /// This value is assigned for the <see cref="_TvTitleChange"/> event and contains the string representation of the current media's title or source.
        /// </summary>
        protected internal string OUT_TITLE;

        /// <summary>
        /// This value is assigned for the <see cref="_TvMediaChange"/> and <see cref="_TvMediaReady"/> events.
        /// </summary>
        protected internal VRCUrl OUT_URL;

        // seal the event manager so that the child classes only use the TV property
        protected sealed override ATEventHandler EventManager
        {
            get => tv;
            set => tv = (TVManager)value;
        }

        public override sbyte Priority => 0;

        /// <summary>
        /// Simple getter which returns a null-safe check on whether the localPlayer is the current TV owner
        /// </summary>
        protected bool IsTVOwner => hasTV && Networking.IsOwner(localPlayer, tv.gameObject);

        public override void Start()
        {
            if (init) return;
            if (tv == null) tv = transform.GetComponentInParent<TVManager>();
            hasTV = tv != null;
            SetLogPrefixLabel(hasTV ? $"{tv.gameObject.name}/{name}" : $"<Missing TV Ref>/{name}");
            base.Start();
            if (!hasTV) Warn("The TV reference was not provided. Please make sure the plugin knows what TV to connect to.");
        }

        [PublicAPI]
        public void _SetTV(TVManager manager)
        {
            ChangeEventManager(manager);
            tv = manager;
            hasTV = tv != null;
        }

        // seal the _ManagerReady call in favor of the _TvReady call which occurs at a different point in time.
        // If listener registers after the initial _TvReady call, implicitly forward this event to the _TvReady call
        public sealed override void _ManagerReady()
        {
            if (tv.isReady) _TvReady();
        }

        /// <summary>
        /// This event is called when the TV has prepared it's internal state and is available to have actions taken, like loading a URL.
        /// </summary>
        public virtual void _TvReady() { }

        /// <summary>
        /// This event is called when the internal playing state has resumed playing the media.
        /// This will be called when the Play action is triggered, or after the <see cref="_TvMediaReady"/> event if the owner has the media playing.
        /// </summary>
        public virtual void _TvPlay() { }

        /// <summary>
        /// This event is called when the internal playing state has been paused.
        /// This will be called when the Pause action is triggered, or after the <see cref="_TvMediaReady"/> event if the owner has the media paused.
        /// </summary>
        public virtual void _TvPause() { }

        /// <summary>
        /// This event is called when the Stop action is triggered, typically (though not exclusively) by an explicit user input.
        /// </summary>
        public virtual void _TvStop() { }

        /// <summary>
        /// Deprecated. Use the <see cref="_TvMediaReady"/> event name instead. 
        /// </summary>
        [Obsolete("Use _TvMediaReady instead")]
        protected virtual void _TvMediaStart() { }

        /// <summary>
        /// This event is called upon successfully loading a URL without error.
        /// </summary>
        public virtual void _TvMediaReady()
        {
            _TvMediaStart(); // backwards compat
        }

        /// <summary>
        /// This event is called whenever the media has finished and come to a complete stop.
        /// If the TV's loop flag is enabled, this event will NOT fire.
        /// </summary>
        public virtual void _TvMediaEnd() { }

        /// <summary>
        /// This event is called as soon as playback is detected to be enabled upon it's first attempt to play.
        /// This event may be called multiple times per media load, but <see cref="_TvPlaybackEnd"/> must have been triggered
        /// before this event is allowed to be called again (such as the media reaching the end of its duration).
        /// If the event is allowed to trigger, it will occur immediately after the <see cref="_TvPlay"/> event
        /// The TV will effectively toggle between the two events.
        /// </summary>
        public virtual void _TvPlaybackStart() { }

        /// <summary>
        /// This event is called whenever the playback is considered to have been completed/ended.
        /// This event can only occur if the playback start has already been triggered for some media.
        /// If the event is allowed to trigger, it will occur immediately after the <see cref="_TvMediaEnd"/>, <see cref="_TvStop"/>, <see cref="_TvVideoPlayerError"/> events.
        /// Pausing media is NOT considered to be playback ending.
        /// </summary>
        public virtual void _TvPlaybackEnd() { }

        /// <summary>
        /// This event is called when the current media has started playing again without a reload.
        /// This can happen automatically when the media ends if the TV's loop flag is enabled,
        /// or if the media has ended and the current owner has activated the Play action, triggering a manual one-off loop.
        /// </summary>
        public virtual void _TvMediaLoop() { }

        /// <summary>
        /// This event is called when a new URL is attempting to be loaded.
        /// This occurs prior to any success or failure.
        /// If you want to know when a URL successfully loaded, use the <see cref="_TvMediaReady"/> event.
        /// </summary>
        public virtual void _TvMediaChange() { }

        /// <summary>
        /// This event is called whenever the TV's internal media title data is changed.
        /// It will have the <see cref="OUT_TITLE"/> value available to it.
        /// </summary>
        public virtual void _TvTitleChange() { }

        /// <summary>
        /// This event is called anytime the ownership has successfully been changed on the TV.
        /// It will have the <see cref="OUT_OWNER"/> value available to it
        /// </summary>
        public virtual void _TvOwnerChange() { }

        /// <summary>
        /// This event is called anytime the video player selection has changed.
        /// It will have the <see cref="OUT_VIDEOPLAYER"/> value available to it as the current array index of the TV's videoManager list.
        /// </summary>
        public virtual void _TvVideoPlayerChange() { }

        /// <summary>
        /// This event is called when the current video player has encountered an error.
        /// It will have the <see cref="OUT_ERROR"/> value available to it as the enum of the particular error provided by the video player.
        /// </summary>
        public virtual void _TvVideoPlayerError() { }

        /// <summary>
        /// This event is called when the user has requested the TV to mute itself.
        /// </summary>
        public virtual void _TvMute() { }

        /// <summary>
        /// This event is called when the user has requested the TV to un-mute itself.
        /// </summary>
        public virtual void _TvUnMute() { }

        /// <summary>
        /// This event is called when the TV has modified the volume of the current media.
        /// It will have the <see cref="OUT_VOLUME"/> value available to it.
        /// 
        /// </summary>
        public virtual void _TvVolumeChange() { }

        /// <summary>
        /// This event is called when the TV has attempted to swap any audio setup to a 3D (aka spatialized) audio.
        /// </summary>
        public virtual void _TvAudioMode3d() { }

        /// <summary>
        /// This event is called when the TV has attempted to swap any audio setup to a 2D (aka global) audio.
        /// </summary>
        public virtual void _TvAudioMode2d() { }

        /// <summary>
        /// This event is called when the TV has been told to enable looping for the current media.
        /// </summary>
        public virtual void _TvEnableLoop() { }

        /// <summary>
        /// This event is called when the TV has been told to stop looping the current media.
        /// </summary>
        public virtual void _TvDisableLoop() { }

        /// <summary>
        /// This event is called when the TV's internal state is restored for sync data continutity
        /// This restores the TV to a synchronized state and will automatically attempt to resync and catch up with the current owner.
        /// </summary>
        public virtual void _TvSync() { }

        /// <summary>
        /// This event is called when the TV's internal state is modified to ignore any sync data.
        /// This effectively turns the TV into a local-only media player.
        /// </summary>
        public virtual void _TvDeSync() { }

        /// <summary>
        /// This event is called when the TV's internal protections have been enabled.
        /// If the user is not authorized to enable these protections, this does nothing.
        /// </summary>
        public virtual void _TvLock() { }

        /// <summary>
        /// This event is called whenever the TV's protected state has be lifted.
        /// If the user is not authorized to lift these protections, this does nothing.
        /// </summary>
        public virtual void _TvUnLock() { }

        /// <summary>
        /// This event is called anytime the TV enters a loading state
        /// </summary>
        public virtual void _TvLoading() { }

        /// <summary>
        /// This event is called anytime the TV exits a loading state
        /// </summary>
        public virtual void _TvLoadingEnd() { }

        /// <summary>
        /// This event is called anytime the TV's loading state is manually cancelled by the user
        /// </summary>
        public virtual void _TvLoadingAbort() { }

        /// <summary>
        /// Deprecated. Use the <see cref="_TvLoadingAbort"/> event name instead. 
        /// </summary>
        [Obsolete("Use _TvLoadingAbort instead")]
        public virtual void _TvLoadingStop() { }

        /// <summary>
        /// This event is called whenever media's seek position has been adjusted by a user.
        /// It will have the <see cref="OUT_SEEK"/> value available to it as the actual timestamp of the source media.
        /// </summary>
        public virtual void _TvSeekChange() { }

        /// <summary>
        /// This event is called whenever the seek offset has been modified.
        /// It will have the <see cref="OUT_SEEK"/> value available containing the offset value (between 0.5 and 2)
        /// </summary>
        public virtual void _TvSeekOffsetChange() { }

        /// <summary>
        /// This event is generally triggered by auth plugins to request that any regular plugins recheck authentication requirements.
        /// </summary>
        public virtual void _TvAuthChange() { }

        /// <summary>
        /// This event is called when the tv's playback speed has been modified.
        /// It will have the <see cref="OUT_SPEED"/> value available containing the offset value (between 0.5 and 2)
        /// </summary>
        public virtual void _TvPlaybackSpeedChange() { }

        /// <summary>
        /// This event is called whenever the 3D mode has been modified.
        /// It will have the <see cref="OUT_MODE"/> value available for the <see cref="TV3DMode"/> option currently active.
        /// </summary>
        public virtual void _Tv3DModeChange() { }

        /// <summary>
        /// This event is called whenever the 3D width is updated to half.
        /// This is the default width mode.
        /// Each eye uses the full video resolution, thus half the quality due to scaling.
        /// </summary>
        public virtual void _Tv3DWidthHalf() { }

        /// <summary>
        /// This event is called whenever the 3D width is updated to full.
        /// Each eye uses half the video resolution (depending on 3D mode), thus gets full quality as no extra scaling is involved.
        /// </summary>
        public virtual void _Tv3DWidthFull() { }

        /// <summary>
        /// This event is called when the TV enables gamma correction.
        /// </summary>
        public virtual void _TvColorSpaceCorrected() { }

        /// <summary>
        /// This event is called when the TV disables gamma correction.
        /// </summary>
        public virtual void _TvColorSpaceRaw() { }

        /// <summary>
        /// Special method which can be overridden to return wheter
        /// </summary>
        /// <param name="main"></param>
        /// <param name="alt"></param>
        /// <returns></returns>
        public virtual bool _IsPreApprovedUrl(VRCUrl main, VRCUrl alt) => false;
    }
}