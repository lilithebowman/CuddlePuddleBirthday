using ArchiTech.SDK;

namespace ArchiTech.ProTV
{
    public abstract class QueueListener : ATEventHandler
    {
        public Queue queue;
        protected bool hasQueue;

        /// <summary>
        /// This value is assigned for any events which involve an index of the queue list in some capacity.
        /// </summary>
        protected internal int OUT_INDEX;

        public override void Start()
        {
            if (init) return;
            base.Start();
            hasQueue = queue != null;
            if (hasQueue) queue._RegisterListener(this);
        }

        /// <summary>
        /// This event is called after the Queue has received the _TvReady event and processed its own ready state.
        /// </summary>
        public virtual void _QueueReady() { }

        /// <summary>
        /// This event is called when an item is added.
        /// The <see cref="OUT_INDEX"/> will contain the index of the new entry.
        /// This event will also be triggered for remote users.
        /// </summary>
        public virtual void _QueueEntryAdded() { }

        /// <summary>
        /// This event is called when an item is removed.
        /// The <see cref="OUT_INDEX"/> will contain the index of the old entry.
        /// Note that the old index will have already had the content removed at this point,
        /// so querying the queue for the index will return inconsistent data.
        /// This event will also be triggered for remote users.
        /// </summary>
        public virtual void _QueueEntryRemoved() { }

        /// <summary>
        /// This event is called when the Queue has a Purge triggered.
        /// It will be called for both PurgeSelf and PurgeAll.
        /// This event will also be triggered for remote users.
        /// </summary>
        public virtual void _QueuePurged() { }

        /// <summary>
        /// This event is called whenever the Queue receives the _TvMediaReady event.
        /// The <see cref="OUT_INDEX"/> value will be the index of the current entry playing in the Queue.
        /// If the <see cref="OUT_INDEX"/> is -1, that means the TV is playing a URL that doesn't match the Queue's.
        /// This happens when a URL is played directly do the TV from some other plugin.
        /// </summary>
        public virtual void _QueuePlaying() { }
    }
}