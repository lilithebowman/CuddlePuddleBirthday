using ArchiTech.SDK;

namespace ArchiTech.ProTV
{
    public abstract class PlaylistListener : ATEventHandler
    {
        public Playlist playlist;
        protected bool hasPlaylist;

        /// <summary>
        /// This value is assigned for any events which involve an index of the playlist list in some capacity.
        /// </summary>
        protected internal int OUT_INDEX;

        public override void Start()
        {
            if (init) return;
            base.Start();
            hasPlaylist = playlist != null;
            if (hasPlaylist) playlist._RegisterListener(this);
        }

        /// <summary>
        /// This event is called after the Playlist has received the _TvReady event and processed its own ready state.
        /// </summary>
        public virtual void _PlaylistReady() { }

        /// <summary>
        /// This event is called whenever the Playlist receives the _TvMediaReady event.
        /// The <see cref="OUT_INDEX"/> value will be the index of the current entry playing in the Playlist.
        /// If the <see cref="OUT_INDEX"/> is -1, that means the TV is playing a URL that doesn't match the Playlist's.
        /// This happens when a URL is played directly do the TV from some other plugin.
        /// </summary>
        public virtual void _PlaylistPlaying() { }
    }
}