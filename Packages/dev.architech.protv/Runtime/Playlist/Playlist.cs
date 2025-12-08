using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using Random = UnityEngine.Random;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace ArchiTech.ProTV
{
    internal enum PlaylistImportMode
    {
        [I18nInspectorName("Manual")] NONE,
        [I18nInspectorName("In Project")] LOCAL,
        [I18nInspectorName("Remote URL")] REMOTE
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None), DefaultExecutionOrder(-2)]
    [HelpURL("https://protv.dev/guides/playlist")]
    public partial class Playlist : TVPlugin
    {
        public Queue queue;

        [I18nInspectorName("Shuffle Playlist on Load"), I18nTooltip("Will reorder the list locally upon joining the world. Note: For non-owner users, this will appear as if the playlist is playing in a random order.")]
        public bool shuffleOnLoad = false;

        [Min(0),
         I18nInspectorName("Preload Queue Amount"), I18nTooltip("If a Queue is connected, this will make the instance master automatically add the specific number of items to the queue when they join. This only happens once per instance.")
        ]
        public int queuePreloadAmount = 0;

        [I18nInspectorName("Autoplay Next"), I18nTooltip("Whether autoplay should be active. Checked for when media is considered done (finished/skipped/error/etc). This is considered the 'enable' state for most autoplay settings.")]
        public bool autoplayList = false;

        [I18nInspectorName("Autoplay On Load"), I18nTooltip("Whether autoplay should activate immediately upon joining the world.")]
        public bool autoplayOnLoad = true;

        [I18nInspectorName("Enable Autoplay on Interact"), I18nTooltip("When interacting with a playlist entry, this will make the interaction enable autoplay. Mutually exclusive to its disable counterpart.")]
        public bool enableAutoplayOnInteract;

        [I18nInspectorName("Disable Autoplay on Interact"), I18nTooltip("When interacting with a playlist entry, this will make the interaction disable autoplay. Mutually exclusive to its enable counterpart.")]
        public bool disableAutoplayOnInteract;

        [I18nInspectorName("Enable Autoplay on Custom Media"), I18nTooltip("When enabled, if the playlist detects a url has been entered by a user that is not present in itself, it will ENABLE autoplay.")]
        public bool enableAutoplayOnCustomMedia;

        [I18nInspectorName("Disable Autoplay on Custom Media"), I18nTooltip("When enabled, if the playlist detects a url has been entered by a user that is not present in itself, it will DISABLE autoplay.")]
        public bool disableAutoplayOnCustomMedia;

        [I18nInspectorName("Prioritize on Interact"), I18nTooltip("When interacting with a playlist entry, this flag causes the playlist to be evaluated first when processing media ending logic.")]
        public bool prioritizeOnInteract = true;

        [I18nInspectorName("Loop Playlist"), I18nTooltip("Whether the playlist should continue playing from the top once it reaches the end.")]
        public bool loopPlaylist = true;

        [I18nInspectorName("Start Autoplay From Random Entry"), I18nTooltip("Applies to Autoplay On Load, will pick a random entry in the playlist when starting the autoplay.")]
        public bool startFromRandomEntry = false;

        [I18nInspectorName("Continue From Last Known Entry"), I18nTooltip("This makes the playlist remember it's last played entry, so if autoplay logic runs, it'll continue from where it left off.")]
        public bool continueWhereLeftOff = true;

        [I18nInspectorName("Show Urls in Playlist?"), I18nTooltip("Whether the URLs should be allowed to be displayed in the playlist at all, regardless of the specific UI setup.")]
        public bool showUrls;

        [FormerlySerializedAs("pcUrls"), FormerlySerializedAs("urls")]
        public VRCUrl[] mainUrls = new VRCUrl[0];

        [FormerlySerializedAs("questUrls"), FormerlySerializedAs("alts")]
        public VRCUrl[] alternateUrls = new VRCUrl[0];

        public string[] titles = new string[0];
        public string[] descriptions = new string[0];
        public string[] tags = new string[0];
        public Sprite[] images = new Sprite[0];

        [I18nInspectorName("Header Text"), I18nTooltip("Optional header text to display next to the playlist. If the value is empty, no modification occurs.")]
        public string header = "";

        [I18nInspectorName("Default Playlist Image"), I18nTooltip("Optional image used as the placeholder image for playlist entries when that entry does not have a valid image available.")]
        public Sprite placeholderImage;

        [SerializeField,
         I18nInspectorName("Playlist Storage"), I18nTooltip("If present, the playlist will store the entries data on the specified component instead of itself. This helps with editor performance when dealing with very large playlists.")
        ]
        internal PlaylistData storage;

        [NonSerialized] public int IN_INDEX = -1;

        public override sbyte Priority => -10;

        // A 1 to 1 array corresponding to each original entry specifying whether the element should be filtered (aka hidden) in the views.
        internal bool[] hidden = new bool[0];

        // an array of the same size as the urls that stores corresponding references to the indexes within the urls array.
        // the order of this array is what gets modified when the playlist gets sorted.
        internal int[] sortView = new int[0];

        // an array of a variable size (length of sortView or less) that represents the list of url indexes that are visible for rendering in the current view
        // this array also contains values which correspond to the indexes of the URL list, which may be non-sequential based on the sortView order
        internal int[] filteredView = new int[0];
        internal int filteredViewCount = 0;
        internal int currentSortViewIndex = -1;

        private int realSortViewIndex = -1;
        private bool hasQueue;
        private bool loadAutoplay;
        private bool internalSwitch { get; set; }
        private PlaylistRPC rpc;

        private bool hasRPC;

        // Getter Helpers
        public bool[] Hidden => hidden;
        public int[] SortView => sortView;
        public int[] FilteredView => filteredView;

        internal int nextSortViewIndex => findNextSortViewIndex(false);
        internal int prevSortViewIndex => findNextSortViewIndex(true);

        public int CurrentEntryIndex => currentSortViewIndex == -1 ? -1 : sortView[currentSortViewIndex];
        public int NextEntryIndex => currentSortViewIndex == -1 ? -1 : sortView[nextSortViewIndex];
        public int PrevEntryIndex => currentSortViewIndex == -1 ? -1 : sortView[prevSortViewIndex];

        [PublicAPI] public VRCUrl CurrentEntryMainUrl => currentSortViewIndex == -1 ? VRCUrl.Empty : mainUrls[sortView[currentSortViewIndex]];

        [PublicAPI] public VRCUrl CurrentEntryAlternateUrl => currentSortViewIndex == -1 ? VRCUrl.Empty : alternateUrls[sortView[currentSortViewIndex]];

        [PublicAPI] public string CurrentEntryTags => currentSortViewIndex == -1 ? string.Empty : tags[sortView[currentSortViewIndex]];

        [PublicAPI] public string CurrentEntryTitle => currentSortViewIndex == -1 ? string.Empty : titles[sortView[currentSortViewIndex]];

        [PublicAPI] public string CurrentEntryDescription => currentSortViewIndex == -1 ? string.Empty : descriptions[sortView[currentSortViewIndex]];

        [PublicAPI] public Sprite CurrentEntryImage => currentSortViewIndex == -1 ? null : images[sortView[currentSortViewIndex]];


        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#ff8811");
            base.Start();
            // copy storage array references
            mainUrls = storage.mainUrls;
            alternateUrls = storage.alternateUrls;
            titles = storage.titles;
            descriptions = storage.descriptions;
            tags = storage.tags;
            images = storage.images;

            var len = mainUrls.Length;
            hidden = new bool[len];
            sortView = new int[len];
            filteredView = new int[len];
            ResetSortView();
            if (shuffleOnLoad) shuffle(sortView, 3);
            cacheFilteredView();
            hasQueue = queue != null;

            if (titles.Length != len) Warn($"Titles count ({titles.Length}) doesn't match Urls count ({len}).");
            if (len == 0) Warn("No entries in the playlist.");
            else
            {
                if (startFromRandomEntry) currentSortViewIndex = Mathf.FloorToInt(Random.Range(0f, 1f) * (sortView.Length - 1));
                if (autoplayOnLoad) loadAutoplay = hasLocalPlayer && localPlayer.isMaster;

                var idx = sortViewIndexToFilteredViewIndex(currentSortViewIndex);
                SendManagedVariable(nameof(PlaylistUI.OUT_INDEX), idx);
                SendManagedEvent(nameof(PlaylistUI.SeekView));
            }

            rpc = GetComponentInChildren<PlaylistRPC>(true);
            hasRPC = rpc != null;
            if (hasRPC) rpc.gameObject.SetActive(true);
        }

        [PublicAPI]
        public void FillQueue()
        {
            if (hasQueue) FillQueue(queue.AvailableSize);
        }

        [PublicAPI]
        public void FillQueue(int amount)
        {
            if (hasQueue && amount > 0)
            {
                queue.Start(); // ensure queue has been initialized
                while (amount > 0 && !queue.IsFull)
                {
                    currentSortViewIndex = nextSortViewIndex;
                    var nextIndex = sortView[currentSortViewIndex];
                    queue._AddEntry(mainUrls[nextIndex], alternateUrls[nextIndex], titles[nextIndex]);
                    amount--;
                }
            }
        }

        [PublicAPI]
        public void SetQueue(Queue plugin)
        {
            queue = plugin;
            hasQueue = queue != null;
        }

        // === TV EVENTS ===

        #region TV Events

        public override void _TvReady()
        {
            // short-circuit the method if the user cannot play media or there has been a URL already loaded in the past.
            if (!IsTVOwner || !tv.IsWaitingForMedia) return;
            if (hasQueue && queuePreloadAmount > 0) FillQueue(queuePreloadAmount);
            else if (loadAutoplay)
            {
                int nextIndex = sortView[nextSortViewIndex];
                if (hasQueue)
                {
                    queue.Start(); // ensure queue has been initialized
                    // if something else has not yet prepared an entry for the queue, go ahead
                    if (queue._CheckEntry(0, false))
                    {
                        if (hasRPC && prioritizeOnInteract) rpc.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlaylistRPC.ALL_PRIORITIZE));
                        queue._AddEntry(mainUrls[nextIndex], alternateUrls[nextIndex], titles[nextIndex]);
                    }
                }
                else if (hasTV && !tv.IsLoadingMedia)
                {
                    if (hasRPC && prioritizeOnInteract) rpc.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlaylistRPC.ALL_PRIORITIZE));
                    tv._ChangeMedia(mainUrls[nextIndex], alternateUrls[nextIndex], titles[nextIndex]);
                }
            }
        }

        public override void _TvMediaEnd()
        {
            bool willPlay = autoplayList && tv.IsOwner;
            if (IsTraceEnabled) Trace($"Autoplay with Owner: {willPlay}");
            if (willPlay)
            {
                willPlay = !tv.IsLoadingMedia; // check general TV state
                if (IsTraceEnabled) Trace($"General TV State: {willPlay}");
            }

            if (willPlay)
            {
                willPlay = loopPlaylist || currentSortViewIndex != mainUrls.Length - 1; // check if playlist can trigger the next entry 
                if (IsTraceEnabled) Trace($"Next Entry Available: {willPlay}");
            }

            if (willPlay)
            {
                willPlay = !hasQueue || queue.WillBeEmpty; // if queue is attached, make sure it's completely empty before queueing
                if (IsTraceEnabled) Trace($"Playable if Queue: {willPlay}");
            }

            if (willPlay)
            {
                // when the playlist should not continue at the last known entry,
                // and the active media is not part of the playlist,
                // reset the playlist to the start of the list,
                // otherwise switch to the next entry in the list.
                internalSwitch = true;
                if (!continueWhereLeftOff && currentSortViewIndex == -1)
                    SwitchEntry(0);
                else SwitchEntry(nextSortViewIndex);
                internalSwitch = false;
            }
        }

        public override void _TvMediaChange()
        {
            // Examine the logic of findActualSortViewIndex to make sure it's finding things correctly.
            realSortViewIndex = findActualSortViewIndex();
            if (autoplayList)
            {
                if (realSortViewIndex > -1) currentSortViewIndex = realSortViewIndex;
                else if (!continueWhereLeftOff) currentSortViewIndex = -1;
            }
            else currentSortViewIndex = realSortViewIndex;

            if (IsTraceEnabled) Trace($"Media Change: new sort view index {currentSortViewIndex}");
            SendManagedEvent(nameof(PlaylistUI.RetargetActive));
        }

        public override void _TvMediaReady()
        {
            if (realSortViewIndex == -1 && !tv.IsInitialAutoplay)
            {
                if (disableAutoplayOnCustomMedia) ManualPlay();
                if (enableAutoplayOnCustomMedia) AutoPlay();
            }
        }

        public override void _TvVideoPlayerError()
        {
            if (!autoplayList) return;
            if (!tv.IsOwner && !tv.ownerDisabled) return; // when the owner not the local user but is avaialable, ignore the event.
            if (tv.errorState != TVErrorState.FAILED) return; // only proceed if the tv signals that an error has actually occurred.
            if (tv.IsLoadingMedia) return; // skip if another plugin has already switched to another entry.
            if (loopPlaylist || currentSortViewIndex != mainUrls.Length - 1)
            {
                var nextIndex = nextSortViewIndex;
                if (IsInfoEnabled) Info($"Error detected. Switching to next entry {nextIndex}.");
                internalSwitch = true;
                SwitchEntry(nextIndex);
                internalSwitch = false;
            }
        }

        public override void _TvLoading() => SendManagedEvent(nameof(PlaylistUI.LoadingStart));
        public override void _TvLoadingEnd() => SendManagedEvent(nameof(PlaylistUI.LoadingEnd));
        public override void _TvLoadingAbort() => SendManagedEvent(nameof(PlaylistUI.LoadingAbort));

        #endregion

        #region UI EVents

        public void Next()
        {
            SwitchEntry(nextSortViewIndex);
        }

        public void Previous()
        {
            SwitchEntry(prevSortViewIndex);
        }

        public void Shuffle()
        {
            Info("Randomizing sort");
            shuffle(sortView, 3);
            cacheFilteredView(); // must recache the filtered view after a shuffle to update to the new sortView order
            SendManagedVariable(nameof(PlaylistUI.OUT_INDEX), 0);
            SendManagedEvent(nameof(PlaylistUI.SeekView));
        }

        public void ResetSort()
        {
            Info("Resetting sort to default");
            ResetSortView();
            cacheFilteredView(); // must recache the filtered view after a shuffle to update to the new sortView order
            SendManagedVariable(nameof(PlaylistUI.OUT_INDEX), 0);
            SendManagedEvent(nameof(PlaylistUI.SeekView));
        }

        public void AutoPlay()
        {
            if (autoplayList) return; // already autoplay, skip
            if (IsDebugEnabled) Debug($"Playlist autoplay enabled via AutoPlay.");
            if (startFromRandomEntry) currentSortViewIndex = Mathf.FloorToInt(Random.Range(0f, 1f) * sortView.Length - 1);
            if (tv.stateOwner != TVPlayState.PLAYING && !tv.IsLoadingMedia) SwitchEntry(currentSortViewIndex);
            autoplayList = true;
            SendManagedEvent(nameof(PlaylistUI.UpdateAutoplay));
        }

        public void ManualPlay()
        {
            if (IsDebugEnabled) Debug($"Playlist autoplay disabled via ManualPlay.");
            autoplayList = false;
            SendManagedEvent(nameof(PlaylistUI.UpdateAutoplay));
        }

        public void ToggleAutoPlay()
        {
            if (autoplayList) ManualPlay();
            else AutoPlay();
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="SwitchEntry(int)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_INDEX"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="SwitchEntry(int)"/>
        [PublicAPI]
        public void SwitchEntry()
        {
            if (!init) return;
            if (IN_INDEX == -1) return; // no valid index available
            SwitchEntry(IN_INDEX);
            IN_INDEX = -1;
        }

        public void SwitchToRandomEntry() => SwitchToRandomEntry(true);

        public void SwitchToRandomUnfilteredEntry() => SwitchToRandomEntry(false);

        public void Prioritize()
        {
            Start();
            if (!hasTV) return;
            if (!hasQueue && tv.IsLoadingMedia) return; // wait until the current video loading finishes/fails
            if (hasRPC && prioritizeOnInteract) rpc.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlaylistRPC.ALL_PRIORITIZE));
        }

        #endregion

        // === Public Helper Methods

        public void SwitchToRandomEntry(bool filtered)
        {
            int max = filtered ? filteredViewCount : sortView.Length;
            int index = Random.Range(0, max);
            if (filtered) index = filteredViewIndexToSortViewIndex(index);
            SwitchEntry(index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sortViewIndex">the index of the entry to switch to based on the sorted list</param>
        [PublicAPI]
        public void SwitchEntry(int sortViewIndex)
        {
            Start();
            if (!hasTV) return;
            if (!hasQueue && tv.IsLoadingMedia) return; // wait until the current video loading finishes/fails
            if (!hasQueue && !internalSwitch && !tv.CanPlayMedia) return; // if the playlist is swapping via autoplay from media end or media error, allow skipping the auth check.
            if (sortViewIndex >= sortView.Length) Error($"Playlist Item {sortViewIndex} doesn't exist.");
            else if (sortViewIndex > -1)
            {
                if (hasRPC && prioritizeOnInteract) rpc.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlaylistRPC.ALL_PRIORITIZE));
                currentSortViewIndex = sortViewIndex;
                if (IsInfoEnabled) Info($"Switching to playlist item {sortViewIndex}");
                int index = sortView[currentSortViewIndex];
                var title = titles[index];
                if (title.StartsWith("~")) title = title.Substring(1);
                if (hasQueue) queue._AddEntry(mainUrls[index], alternateUrls[index], title);
                else tv._ChangeMedia(mainUrls[index], alternateUrls[index], title);
            }
        }

        public void UpdateFilter(bool[] hide)
        {
            if (hide.Length != mainUrls.Length)
            {
                if (IsDebugEnabled) Info("Filter array must be the same size as the list of urls in the playlist");
                return;
            }

            hidden = hide;
            cacheFilteredView();
            SendManagedEvent(nameof(PlaylistUI.UpdateFilter));
        }

        public void UpdateSort()
        {
            cacheFilteredView();
            SendManagedEvent(nameof(PlaylistUI.SeekView));
        }

        public void ChangeAutoPlay(bool active)
        {
            if (active) AutoPlay();
            else ManualPlay();
        }

        public void UpdateHeader(string text)
        {
            header = text;
            SendManagedEvent(nameof(PlaylistUI.UpdateView));
        }

        // === Helper Methods ===

        private static void shuffle(int[] view, int cycles)
        {
            for (int j = 0; j < cycles; j++)
                VRC.SDKBase.Utilities.ShuffleArray(view);
        }

        // prepare the sortView with the default index mapping
        public void ResetSortView()
        {
            for (int i = 0; i < sortView.Length; i++) sortView[i] = i;
        }

        private void cacheFilteredView()
        {
            var count = 0;
            // repopulate the filteredView with visible items
            foreach (var index in sortView)
                if (!hidden[index])
                {
                    filteredView[count] = index;
                    count++;
                }

            // cache the visible items count
            filteredViewCount = count;
            // remove the remainder of the filteredView entries
            for (; count < sortView.Length; count++)
                filteredView[count] = -1;
        }

        // take a given index (typically derived from the button a player clicks on in the UI)
        // and reverse the values through the arrays to get the original list index.
        internal int filteredViewIndexToRawIndex(int filteredViewIndex)
        {
            if (filteredViewIndex == -1) return -1;
            if (filteredViewIndex >= filteredViewCount) return -1;
            return filteredView[filteredViewIndex];
        }

        internal int filteredViewIndexToSortViewIndex(int filteredViewIndex)
        {
            if (filteredViewIndex == -1) return -1;
            return Array.IndexOf(sortView, filteredView[filteredViewIndex]);
        }

        internal int sortViewIndexToFilteredViewIndex(int sortViewIndex)
        {
            if (sortViewIndex == -1) return -1;
            return Array.IndexOf(filteredView, sortView[sortViewIndex]);
        }

        internal int filteredViewIndexToNextSortViewIndex(int filteredViewIndex)
        {
            if (filteredViewIndex == -1) return -1;
            int sortViewIndex = Array.IndexOf(sortView, filteredView[filteredViewIndex], nextSortViewIndex);
            if (sortViewIndex == -1) sortViewIndex = Array.IndexOf(sortView, filteredView[filteredViewIndex]);
            return sortViewIndex;
        }

        private int findActualSortViewIndex()
        {
            var url = tv.urlMain;
            // check for exact instances (only really applies to the person who selected the entry)
            int rawIndex = Array.IndexOf(mainUrls, url);
            if (rawIndex == -1)
            {
                // otherwise scan the playlist for the first matching URL string
                var len = mainUrls.Length;
                var str = url.Get();
                // Note: This loop can become performance heavy in Udon1 with extremely large playlists.
                for (int i = 0; i < len; i++)
                {
                    if (mainUrls[i].Get() == str)
                    {
                        rawIndex = i;
                        break;
                    }
                }

                if (rawIndex == -1) return -1;
            }

            return Array.IndexOf(sortView, rawIndex);
        }

        private int findNextSortViewIndex(bool reverse)
        {
            int direction = reverse ? -1 : 1;
            int value = currentSortViewIndex;
            int len = sortView.Length;
            do
            {
                value += direction;
                if (value < 0) value = len - 1; // loops start to end
                else if (value >= len) value -= len; // loops end to start
                int rawIndex = sortView[value];
                if (mainUrls[rawIndex].Get() != EMPTYSTR || alternateUrls[rawIndex].Get() != EMPTYSTR) return value;
            } while (value != currentSortViewIndex);

            return currentSortViewIndex;
        }
    }
}