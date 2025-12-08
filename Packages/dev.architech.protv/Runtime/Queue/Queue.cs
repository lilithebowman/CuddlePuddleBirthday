using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech.ProTV
{
    public enum QueueChangeMode
    {
        NONE,
        ADD,
        REMOVE,
        PURGE
    }

    public class Queue : TVPluginWithReliableSync
    {
        [NonSerialized] public VRCUrl IN_MAINURL = new VRCUrl("");
        [NonSerialized] public VRCUrl IN_ALTURL = new VRCUrl("");
        [NonSerialized] public string IN_TITLE = string.Empty;
        [NonSerialized] public int IN_INDEX = -1;
        [NonSerialized] public bool IN_STATE = false;

        [SerializeField, Min(0),
         I18nInspectorName("Max Added Entries Per-User"), I18nTooltip("Number of entries a single user can have in the Queue at any given point in time. To add more, one of their own entries must be cleared either by the queue's natural pruning or by manual removal. Authorized users ignore this limit. Set to 0 to prevent unauthorized users from adding to the Queue.")
        ]
        internal byte maxEntriesPerPlayer = 3;

        [SerializeField, Min(0),
         I18nInspectorName("Burst Entries Per-User"), I18nTooltip("Number of entries a single user can add to the Queue within a rolling period of time (see Burst Entries Timeout). Effectively an anti-spam measure to 'give everyone a turn'. 0 = feature disabled. Authorized users ignore this limit.")
        ]
        internal byte maxBurstEntriesPerPlayer = 0;

        [SerializeField, Min(5),
         I18nInspectorName("Burst Entries Timeout"), I18nTooltip("How long (in seconds) a single user has to wait to add entries after being throttled for hitting the Max Burst Entries threshold.")
        ]
        internal int burstThrottleTime = 60;

        [SerializeField, Range(5, 100),
         I18nInspectorName("Max Queue Length"), I18nTooltip("The total maximum number of entries allowed for the queue. Switch inspector to debug mode to increase the limit past the slider's range.")
        ]
        internal byte maxQueueLength = 20;

        [SerializeField,
         I18nInspectorName("Prevent Duplicate Media"), I18nTooltip("Disallow the a url to be added to the list when it is already present in another entry.")
        ]
        internal bool preventDuplicateVideos = true;

        [SerializeField,
         I18nInspectorName("Allow Adding While Locked"), I18nTooltip("If enabled and the TV is locked, unauthorized users will still be able to add to the Queue, but cannot modify or manipulate entries.")
        ]
        internal bool enableAddWhileLocked = false;

        [SerializeField,
         I18nInspectorName("Allow Entry Selection by Anyone"), I18nTooltip("If enabled, it will allow any un-authorized users to switch to other entries.")
        ]
        internal bool openEntrySelection = false;

        [SerializeField,
         I18nInspectorName("Show URLs in Queue"), I18nTooltip("Whether to allow the URLs to be visible in the Queue entries.")
        ]
        internal bool showUrlsInQueue = true;

        [SerializeField,
         I18nInspectorName("Loop Queue"), I18nTooltip("Should the Queue attempt to continue playing from the start of the list once the end has been reached?")
        ]
        internal bool loop = false;

        [SerializeField,
         I18nInspectorName("Header Text"), I18nTooltip("Optional header text to display next to the playlist. If the value is empty, no modification occurs.")
        ]
        internal string header = "";

        [UdonSynced] internal VRCUrl[] mainUrls = new VRCUrl[0];
        [UdonSynced] internal VRCUrl[] alternateUrls = new VRCUrl[0];
        [UdonSynced] internal string[] titles = new string[0];
        [UdonSynced] internal string[] addedBy = new string[0];
        [UdonSynced] internal int[] owners = new int[0];
        [UdonSynced] internal bool[] persistence = new bool[0];
        [UdonSynced] internal int currentEntry = -1;
        [UdonSynced] internal int currentQueueLength = 0;
        [UdonSynced] internal QueueChangeMode changeMode = QueueChangeMode.NONE;
        [UdonSynced] internal int changeIndex = -1;

        private VRCUrl _syncingMainUrl;
        private VRCUrl _syncingAlternateUrl;
        private string _syncingTitle;
        private string _syncingAddedBy;
        private int _syncingOwner;
        private bool _syncingPersistence;

        internal int lastEntry = -1;
        internal bool requestedByMe = false;
        private double burstTimeWait = 0;
        private int burstCount = 0;

        private int NextEntry
        {
            get
            {
                var index = currentEntry + 1;
                if (index >= currentQueueLength) index = -1;
                return index;
            }
        }

        private int NextWrappedEntry
        {
            get
            {
                var index = currentEntry + 1;
                if (loop) index = wrap(index);
                if (index >= currentQueueLength) index = -1;
                return index;
            }
        }


        /// <summary>
        /// Getter for how many entries the Queue currently contains.
        /// </summary>
        public int CurrentSize => currentQueueLength;

        /// <summary>
        /// Getter for the maximum number of entries the Queue can have.
        /// </summary>
        public int MaxSize => maxQueueLength;

        /// <summary>
        /// Getter for the number of entries that can be added before the Queue is full.
        /// </summary>
        public int AvailableSize => maxQueueLength - currentQueueLength;

        /// <summary>
        /// Getter for whether the Queue has any entries or not.
        /// </summary>
        public bool IsEmpty => currentQueueLength == 0;

        /// <summary>
        /// Getter for whether the Queue can accept new entries or not.
        /// </summary>
        public bool IsFull => currentQueueLength == maxQueueLength;

        /// <summary>
        /// Getter for detecting if the Queue will be empty on the next frame.
        /// Useful for predictively adding a new entry into the Queue. 
        /// </summary>
        public bool WillBeEmpty => IsEmpty || currentEntry == -1 || _CheckEntry(loop ? NextWrappedEntry : NextEntry, false);

        // this plugin's priority should be earlier than most other plugins
        public override sbyte Priority => -20;

        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#ffff00");
            base.Start();
            if (!hasTV) return;

            mainUrls = new VRCUrl[maxQueueLength];
            alternateUrls = new VRCUrl[maxQueueLength];
            titles = new string[maxQueueLength];
            addedBy = new string[maxQueueLength];
            owners = new int[maxQueueLength];
            persistence = new bool[maxQueueLength];

            for (int i = 0; i < maxQueueLength; i++)
            {
                mainUrls[i] = EMPTYURL;
                alternateUrls[i] = EMPTYURL;
                titles[i] = EMPTYSTR;
                addedBy[i] = EMPTYSTR;
                owners[i] = -1;
                // persistence[i] always defaults to false anyways.
            }
        }

        protected override void _PreSerialization() { }

        protected override void _PostSerialization()
        {
            Debug("Sync delivered.");
        }

        protected override void _DeserializationOutOfDate()
        {
            _Deserialization();
        }

        protected override void _Deserialization()
        {
            switch (changeMode)
            {
                case QueueChangeMode.ADD:
                    SendManagedVariable(nameof(QueueListener.OUT_INDEX), changeIndex);
                    SendManagedEvent(nameof(QueueListener._QueueEntryAdded));
                    break;
                case QueueChangeMode.REMOVE:
                    SendManagedVariable(nameof(QueueListener.OUT_INDEX), changeIndex);
                    SendManagedEvent(nameof(QueueListener._QueueEntryRemoved));
                    break;
                case QueueChangeMode.PURGE:
                    SendManagedEvent(nameof(QueueListener._QueuePurged));
                    break;
            }

            updateUI();
        }

        public override void _RequestData()
        {
            if (tv.locked && !tv._IsAuthorized()) return;
            base._RequestData();
            if (retryingSync)
            {
                mainUrls[currentQueueLength] = _syncingMainUrl;
                alternateUrls[currentQueueLength] = _syncingAlternateUrl;
                titles[currentQueueLength] = _syncingTitle;
                addedBy[currentQueueLength] = _syncingAddedBy;
                owners[currentQueueLength] = _syncingOwner;
                persistence[currentQueueLength] = _syncingPersistence;
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi p)
        {
            var pid = p.playerId;
            var pname = p.displayName;
            bool delta = false;
            for (int i = 0; i < addedBy.Length; i++)
                if (addedBy[i] == pname)
                {
                    owners[i] = pid;
                    delta = true;
                }

            if (delta) updateUI();
        }

        public override void OnPlayerLeft(VRCPlayerApi p)
        {
            if (!IsTVOwner) return;
            // make sure the TV owner owns the queue before processing the player leave action
            Owner = localPlayer;
            var pid = localPlayer.playerId;
            var oldpid = p.playerId;
            bool shouldRetain = true;
            bool dataUpdate = false;
            // the entry should only be retained if authorization matches the lock level
            if (tv.IsLockedBySuper) shouldRetain = tv._IsSuperAuthorized(p);
            else if (tv.IsLocked) shouldRetain = tv._IsAuthorized(p);
            for (int i = 0; i < owners.Length; i++)
            {
                if (oldpid == owners[i])
                {
                    dataUpdate = true;
                    if (!shouldRetain && _MatchCurrentEntry(false))
                        clearEntry(i);
                    else owners[i] = 0;
                }
            }

            if (dataUpdate)
            {
                cleanupSyncData();
                updateUI();
            }
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_AddEntry(VRCUrl, VRCUrl, string, bool)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_MAINURL"/>, <see cref="IN_ALTURL"/> and <see cref="IN_TITLE"/> variables.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_AddEntry(VRCUrl, VRCUrl, string, bool)"/>
        [PublicAPI]
        public void _AddEntry()
        {
            _AddEntry(IN_MAINURL, IN_ALTURL, IN_TITLE, IN_STATE);
            IN_MAINURL = EMPTYURL;
            IN_ALTURL = EMPTYURL;
            IN_TITLE = EMPTYSTR;
            IN_STATE = false;
        }

        /// <summary>
        /// Use this method for appending an entry to the queue.
        /// Will validate queue limits, user permissions and some other settings before allowing media to be added.
        /// If validation passes, the passed information will be inserted into the synced data and trigger playing if appropriate.
        /// </summary>
        /// <param name="mainUrl">The main URL to use for the entry</param>
        /// <param name="alternateUrl">Optional secondary/alternative URL</param>
        /// <param name="title">Optional title you can provide for the entry</param>
        /// <param name="persist">Optionally specify the persistence state of the entry, defaults to false</param>
        [PublicAPI]
        public bool _AddEntry(VRCUrl mainUrl, VRCUrl alternateUrl, string title, bool persist = false)
        {
            Start();
            if (mainUrl == null) mainUrl = EMPTYURL;
            if (alternateUrl == null) alternateUrl = EMPTYURL;
            if (title == null) title = EMPTYSTR;
            string urlMainStr = mainUrl.Get();
            string urlAltStr = alternateUrl.Get();
            if (string.IsNullOrEmpty(urlMainStr))
            {
                if (string.IsNullOrEmpty(urlAltStr)) return false; // no url present
                // make sure the PC url has a value
                mainUrl = alternateUrl;
                urlMainStr = urlAltStr;
            }

            bool validationFailed = false;
            string validationMsg = null;

            if (!tv._CheckDomainWhitelist(urlMainStr, urlAltStr))
            {
                validationMsg = "URL is blocked by TV. You do not have enough authorization for this domain.";
                validationFailed = true;
            }
            else if (currentQueueLength >= maxQueueLength)
            {
                validationMsg = "Queue is full. Wait until another media has been cleared.";
                validationFailed = true;
            }
            else if (!enableAddWhileLocked && tv.locked && !tv.CanPlayMedia)
            {
                validationMsg = "TV is locked. You must be an authorized user to queue media while TV is locked.";
                validationFailed = true;
            }
            else if (!tv._IsAuthorized() && personalVideosQueued() >= maxEntriesPerPlayer)
            {
                validationMsg = "Personal queue limit reached. Either remove one or wait for the next one to play.";
                validationFailed = true;
            }
            else if (!tv._IsAuthorized() && burstCount >= maxBurstEntriesPerPlayer && burstTimeWait >= Time.timeSinceLevelLoad)
            {
                validationMsg = $"Throttled. Please wait {(burstTimeWait - Time.timeSinceLevelLoad):N0} seconds before attempting to queue another entry.";
                validationFailed = true;
            }
            else if (preventDuplicateVideos && videoIsQueued(urlMainStr))
            {
                validationMsg = "Media is already in queue. Duplicate media are not allowed.";
                validationFailed = true;
            }

            if (validationFailed)
            {
                Warn(validationMsg);
                updateToaster(validationMsg);
                return false;
            }

            if (title == EMPTYSTR)
                title = showUrlsInQueue ? tv._GetUrlDomain(mainUrl.Get()) : "No Title";

            var newIndex = currentQueueLength;
            mainUrls[newIndex] = _syncingMainUrl = mainUrl;
            alternateUrls[newIndex] = _syncingAlternateUrl = alternateUrl;
            titles[newIndex] = _syncingTitle = title;
            addedBy[newIndex] = _syncingAddedBy = localPlayer.displayName;
            owners[newIndex] = _syncingOwner = localPlayer.playerId;
            persistence[newIndex] = _syncingPersistence = persist;

            Owner = localPlayer;
            cleanupSyncData();
            if (currentEntry == -1) currentEntry = newIndex;
            lastEntry = currentEntry;
            updateUI();
            updateBurstDebounce();
            // for the queue to trigger an instant play when an item is added,
            // the newly added item must be the first item (checked by entry == new index)
            // and that the TV does not have another active video which has yet to end.
            if (tv.isReady && !tv.IsLoadingMedia && currentEntry == newIndex && (tv.IsStopped || tv.IsEnded || tv.IsSkipping)) play();
            if (IsOwner)
            {
                changeMode = QueueChangeMode.ADD;
                changeIndex = newIndex;
            }

            SendManagedVariable(nameof(QueueListener.OUT_INDEX), newIndex);
            SendManagedEvent(nameof(QueueListener._QueueEntryAdded));
            return true;
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_SwitchEntry(int)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_INDEX"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_SwitchEntry(int)"/>
        [PublicAPI]
        public void _SwitchEntry()
        {
            if (IN_INDEX == -1) return;
            _SwitchEntry(IN_INDEX);
            IN_INDEX = -1;
        }

        /// <summary>
        /// Use this event to activate any given available entry in the queue.
        /// If the queue's current entry is actively playing in the TV, it will remove it from the queue.
        /// Otherwise it will simply start playing the requested entry given the current permissions are valid.
        /// If the TV is locked, only authorized users may switch entries, otherwise it's open to the public.
        /// </summary>
        /// <param name="index">the entry to switch to. Will noop when an invalid index is provided.</param>
        [PublicAPI]
        public void _SwitchEntry(int index)
        {
            if (index == -1) return; // bad index value
            if (index >= currentQueueLength) return; // bad index value
            if (index == currentEntry) return; // same entry, noop
            if (tv.loading) return; // disallow switching while loading
            if (!tv.IsLocked && openEntrySelection || tv._IsAuthorized())
            {
                Debug($"Switching to entry {index}");
                Owner = localPlayer;
                int removedIndex = -1;
                if (_MatchCurrentEntry(true) && !persistence[currentEntry])
                {
                    clearEntry(currentEntry);
                    // trim the entry that was just removed
                    if (index > currentEntry) index--;
                    removedIndex = currentEntry;
                }

                // cleanup the sync data with the currentEntry before updating it
                cleanupSyncData();
                currentEntry = index;
                updateUI();
                play();
                if (removedIndex > -1)
                {
                    if (IsOwner)
                    {
                        changeMode = QueueChangeMode.REMOVE;
                        changeIndex = removedIndex;
                    }

                    SendManagedVariable(nameof(QueueListener.OUT_INDEX), removedIndex);
                    SendManagedEvent(nameof(QueueListener._QueueEntryRemoved));
                }
            }
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_PersistEntry(int, bool)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_INDEX"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_PersistEntry(int, bool)"/>
        [PublicAPI]
        public void _PersistEntry()
        {
            if (IN_INDEX == -1) return;
            if (IN_INDEX > -1 && IN_INDEX < currentQueueLength) _PersistEntry(IN_INDEX, IN_STATE);
            IN_INDEX = -1;
        }

        /// <summary>
        /// This will set whether or not the given entry should be in a persistent state.
        /// When persistence is active, the queue will never implicilty remove the entry.
        /// While persistent, only explicitly deleting the specific entry with <see cref="_RemoveEntry(int)"/> works.
        /// </summary>
        /// <param name="index">the entry to persist. Will noop when an invalid index is provided.</param>
        /// <param name="state">the state of persistence for the given entry.</param>
        [PublicAPI]
        public void _PersistEntry(int index, bool state)
        {
            Start();
            if (index == -1) return;
            if (index >= currentQueueLength) return;
            Debug($"Switching persistence state for entry {index}");
            if (tv._IsAuthorized())
            {
                Owner = localPlayer;
                persistence[index] = state;
                RequestSerialization();
            }
        }

        /// <summary>
        /// Convenience proxy event. Check the overload method in <see cref="_RemoveEntry(int)"/>.<br/>
        /// Compatible with UdonGraph/CyanTriggers when used with <see cref="IN_INDEX"/> variable.<br/>
        /// Compatible with UIEvents via Template object usage.
        /// </summary>
        /// <seealso cref="_RemoveEntry(int)"/>
        [PublicAPI]
        public void _RemoveEntry()
        {
            if (IN_INDEX == -1) return;
            _RemoveEntry(IN_INDEX);
            IN_INDEX = -1;
        }

        /// <summary>
        /// Call this to remove a given entry.
        /// If the entry is currenty active on the TV, it will attempt to play the next entry if one exists.
        /// </summary>
        /// <param name="index">the entry to remove. Will noop when an invalid index is provided.</param>
        [PublicAPI]
        public void _RemoveEntry(int index)
        {
            Start();
            if (index == -1) return;
            if (index >= currentQueueLength) return;
            Debug($"Removing entry {index}");
            if (localPlayer.playerId == owners[index] || tv._IsAuthorized())
            {
                Owner = localPlayer;
                if (index == currentEntry && _MatchCurrentEntry(true))
                {
                    if (tv.loading) return; // do not allow removal of active entry if it's loading
                    Info("Removing active queue item.");
                    tv._Stop();
                    requestNext();
                }
                else
                {
                    Info($"Removing queue item {index}");
                    removeGivenEntry(index);
                }
            }
        }

        /// <summary>
        /// Remove all media in the queue that the calling user has added.
        /// </summary>
        [PublicAPI]
        public void _PurgeSelf()
        {
            Start();
            int purgeCount = 0;
            for (int i = 0; i < currentQueueLength; i++)
            {
                if (persistence[i]) continue;
                if (localPlayer.playerId == owners[i])
                {
                    if (i == currentEntry && _CheckCurrentEntry(true))
                        tv._Stop();
                    clearEntry(i);
                    purgeCount++;
                }
            }

            if (purgeCount > 0)
            {
                Owner = localPlayer;
                cleanupSyncData();
                changeMode = QueueChangeMode.PURGE;
                updateUI();
                Info($"Purged {purgeCount} queue entries");
                SendManagedEvent(nameof(QueueListener._QueuePurged));
            }
        }

        /// <summary>
        /// Remove all media from the queue. Only authorized users can call this event.
        /// </summary>
        [PublicAPI]
        public void _PurgeAll()
        {
            Start();
            if (!tv._IsAuthorized()) return;
            Info("Purging the queue.");
            int purgeCount = 0;
            if (_CheckCurrentEntry(true)) tv._Stop();
            for (int i = 0; i < currentQueueLength; i++)
            {
                if (persistence[i]) continue;
                clearEntry(i);
                purgeCount++;
            }

            if (purgeCount > 0)
            {
                Owner = localPlayer;
                cleanupSyncData();
                changeMode = QueueChangeMode.PURGE;
                updateUI();
                Info($"Purged {purgeCount} queue entries");
                SendManagedEvent(nameof(QueueListener._QueuePurged));
            }
        }

        /// <summary>
        /// Tells the TV to run the skip logic.
        /// </summary>
        [PublicAPI]
        public void _Skip()
        {
            Start();
            if (hasTV) tv._Skip();
        }

        [PublicAPI]
        public void _UpdateHeader(string text)
        {
            header = text;
            updateUI();
        }


        // ======== NETWORK METHODS ===========

        private void requestNext()
        {
            if (!tv.IsLocked || IsTVOwner)
            {
                Trace("Requesting next available video");
                requestedByMe = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_RequestNext));
            }
        }

        public void ALL_RequestNext() // PUT ALL Next BUTTON CHECKS HERE
        {
            if (!hasLocalPlayer) return;
            // ignore any requests for next while loading to prevent next spamming
            if (tv.IsLoadingMedia) return;
            if (tv.IsLocked)
            {
                if (IsDebugEnabled) Debug($"Next requested (by me {requestedByMe} is owner {IsTVOwner}), but tv is locked.");
                // only allow the TV owner to act if the tv is locked
                // only allow self-requested NEXT calls when the TV is locked.
                if (requestedByMe && IsTVOwner)
                {
                    // if the current url is in the TV, switch to the next URL
                    if (_MatchCurrentEntry(true))
                    {
                        if (IsTraceEnabled) Trace("Entry matched, is owner and is switching to next entry.");
                        Owner = localPlayer;
                        activateNextEntry();
                    }

                    play();
                }
            }
            else
            {
                if (IsDebugEnabled) Debug($"Next Requested, tv unlocked (by me {requestedByMe})");
                // if the current url is in the TV
                var isOwner = IsOwner;
                if (_MatchCurrentEntry(true))
                {
                    var nextEntryIndex = NextWrappedEntry;
                    var hasNextEntry = _CheckEntry(nextEntryIndex, true);
                    bool ownerMatch = false;
                    if (hasNextEntry)
                    {
                        var entryOwner = owners[nextEntryIndex];
                        var entryOwnerMatches = localPlayer.playerId == entryOwner;
                        var ownerFallback = VRCPlayerApi.GetPlayerById(entryOwner) == null && isOwner;
                        ownerMatch = entryOwnerMatches || ownerFallback;
                    }

                    if (hasNextEntry && ownerMatch)
                    {
                        // update owner of queue to the owner of next queued media
                        // if for some reason the entryOwner playerId is not valid, the owner should immediately take action.
                        Owner = localPlayer;
                        activateNextEntry();
                        play();
                    }
                    else if (!hasNextEntry && isOwner)
                    {
                        // allow pass through for queue owner if there isn't another media in queue
                        //      This allows for media end to clear the last url from the queue
                        activateNextEntry();
                    }
                }
                else
                {
                    var entryOwner = currentEntry > -1 ? owners[currentEntry] : 0;
                    var entryOwnerMatches = localPlayer.playerId == entryOwner;
                    var ownerFallback = VRCPlayerApi.GetPlayerById(entryOwner) == null && isOwner;
                    if (_CheckCurrentEntry(true) && (entryOwnerMatches || ownerFallback))
                    {
                        // if there is a URL in the queue, make the owner of that queue entry play the video
                        play();
                    }
                }
            }

            requestedByMe = false;
        }

        // === TV Events ===

        public override void _TvReady()
        {
            if (IsOwner) play();
            updateUI();
            SendManagedEvent(nameof(QueueListener._QueueReady));
        }

        public override void _TvAuthChange() => updateUI();

        public override void _TvLock() => updateUI();

        public override void _TvUnLock() => updateUI();

        public override void _TvMediaReady()
        {
            var matched = _MatchCurrentEntry(true);
            if (IsOwner && !matched)
            {
                if (_MatchEntry(lastEntry, false)) clearEntry(currentEntry);
                cleanupSyncData();
            }

            lastEntry = matched ? currentEntry : -1;
            updateUI();
            SendManagedVariable(nameof(QueueListener.OUT_INDEX), lastEntry);
            SendManagedEvent(nameof(QueueListener._QueuePlaying));
        }

        public override void _TvMediaEnd()
        {
            if (IsTVOwner)
            {
                if (_MatchCurrentEntry(true)) // current entry matches the tv url
                    requestNext(); // attempt queueing the next media
                else play(); // attempt to play current entry
            }
        }

        public override void _TvVideoPlayerError()
        {
            // only proceed if tv signal an error actually occurred
            if (tv.errorState != TVErrorState.FAILED) return;
            SendManagedEvent(nameof(QueueUI.VideoError));
            if (!tv.IsOwner && !tv.ownerDisabled) return;
            if (_MatchCurrentEntry(true)) requestNext();
        }

        public override void _TvLoading()
        {
            SendManagedEvent(nameof(QueueUI.Loading));
        }

        public override void _TvLoadingEnd()
        {
            SendManagedEvent(nameof(QueueUI.LoadingEnd));
        }

        public override void _TvLoadingAbort()
        {
            SendManagedEvent(nameof(QueueUI.LoadingAbort));
        }

        // ======== HELPER METHODS ============

        private void play()
        {
            if (currentEntry > -1 && owners[currentEntry] > -1)
            {
                if (IsDebugEnabled) Debug($"New URL - {mainUrls[currentEntry]} | Title '{titles[currentEntry]}'");
                updateToaster(EMPTYSTR);
                tv._ChangeMedia(mainUrls[currentEntry], alternateUrls[currentEntry], titles[currentEntry], addedBy[currentEntry]);
            }
        }

        /// <summary>
        /// A check to see if the given entry url matches the TV's currently active url
        /// </summary>
        /// <param name="entryIndex">entry index to check</param>
        /// <param name="shouldMatch">expectation for if the match should be true or not</param>
        /// <returns>if the url matches or not</returns>
        public bool _MatchEntry(int entryIndex, bool shouldMatch)
        {
            if (entryIndex == -1) return shouldMatch == false;
            var check = mainUrls[entryIndex];
            check = check ?? EMPTYURL;
            bool matches = check.Get() == tv.urlMain.Get();
            if (IsTraceEnabled) Trace($"Checking entry match {entryIndex}: should {shouldMatch} == does {matches}");
            return matches == shouldMatch;
        }

        /// <summary>
        /// A check to see if the current entry url matches the TV's currently active url
        /// </summary>
        /// <param name="shouldMatch">expectation for if the match should be true or not</param>
        /// <returns>if the url matches or not</returns>
        public bool _MatchCurrentEntry(bool shouldMatch) => _MatchEntry(currentEntry, shouldMatch);

        public bool _CheckEntry(int entryIndex, bool shouldExist)
        {
            if (entryIndex == -1) return shouldExist == false;
            var check = mainUrls[entryIndex];
            check = check ?? EMPTYURL;
            bool exists = check.Get() != EMPTYSTR;
            if (IsTraceEnabled) Trace($"Checking entry exists {entryIndex}: should {shouldExist} == does {exists}");
            return exists == shouldExist;
        }

        public bool _CheckCurrentEntry(bool shouldExist) => _CheckEntry(currentEntry, shouldExist);

        public bool _CheckNextEntry(bool shouldExist) => _CheckEntry(NextEntry, shouldExist);

        public bool TryGetEntry(int index, out VRCUrl main, out VRCUrl alt, out string title, out int owner, out bool persist)
        {
            main = EMPTYURL;
            alt = EMPTYURL;
            title = EMPTYSTR;
            owner = -1;
            persist = false;
            if (index < 0 || index >= currentQueueLength) return false;
            main = mainUrls[index];
            alt = alternateUrls[index];
            title = titles[index];
            owner = owners[index];
            persist = persistence[index];
            return true;
        }

        public bool TryGetCurrentEntry(out VRCUrl main, out VRCUrl alt, out string title, out int owner, out bool persist) =>
            TryGetEntry(currentEntry, out main, out alt, out title, out owner, out persist);

        /// <summary>
        /// This method updates the synced array data. Should only be ever be called by the current object owner.
        /// It effectively collapses all entries into the top of the list so there are no "empty" entries within the currentQueueLength selection.
        /// </summary>
        private void cleanupSyncData()
        {
            int index = 0;
            for (int i = 0; i < maxQueueLength; i++)
            {
                // Skip entries that have their owner removed as they are considered "empty" entries.
                if (owners[i] == -1) continue;
                // If the index and entry count diverge, movement is required.
                if (index != i)
                {
                    // move entry to new index
                    mainUrls[index] = mainUrls[i];
                    alternateUrls[index] = alternateUrls[i];
                    titles[index] = titles[i];
                    addedBy[index] = addedBy[i];
                    owners[index] = owners[i];
                    persistence[index] = persistence[i];
                    // remove old entry index
                    clearEntry(i);
                }

                index++;
            }

            changeMode = QueueChangeMode.NONE;
            currentQueueLength = index;
            if (currentEntry >= currentQueueLength) currentEntry = loop && currentQueueLength > 0 ? 0 : -1;
            if (IsDebugEnabled) Debug($"Updated to {currentQueueLength} entries. Current entry is {currentEntry}");
            RequestSerialization();
        }

        private int wrap(int value)
        {
            if (currentQueueLength == 0) return 0;
            value %= currentQueueLength;
            if (value < 0) value += currentQueueLength;
            return value;
        }

        private void removeGivenEntry(int entryIndex)
        {
            clearEntry(entryIndex);
            cleanupSyncData();
            changeMode = QueueChangeMode.REMOVE;
            updateUI();
            SendManagedVariable(nameof(QueueListener.OUT_INDEX), entryIndex);
            SendManagedEvent(nameof(QueueListener._QueueEntryRemoved));
        }

        private void activateNextEntry()
        {
            if (currentEntry == -1) return;
            if (persistence[currentEntry])
            {
                currentEntry++;
                cleanupSyncData();
                updateUI();
            }
            else removeGivenEntry(currentEntry);
        }

        private void clearEntry(int index)
        {
            if (index == -1) return;
            mainUrls[index] = EMPTYURL;
            alternateUrls[index] = EMPTYURL;
            titles[index] = EMPTYSTR;
            addedBy[index] = EMPTYSTR;
            owners[index] = -1;
            persistence[index] = false;
        }

        private int personalVideosQueued()
        {
            int count = 0;
            for (int i = 0; i < currentQueueLength; i++)
                if (localPlayer.playerId == owners[i])
                    count++;
            return count;
        }

        private bool videoIsQueued(string url)
        {
            foreach (VRCUrl queued in mainUrls)
                if (queued != null && queued.Get() == url)
                    return true;
            return false;
        }

        private void updateUI() => SendManagedEvent(nameof(QueueUI.UpdateUI));

        private void updateToaster(string msg)
        {
            SendManagedVariable(nameof(QueueUI.OUT_TEXT), msg);
            SendManagedEvent(nameof(QueueUI.UpdateToaster));
        }

        private void updateBurstDebounce()
        {
            if (maxBurstEntriesPerPlayer == 0) return;
            var time = Time.timeSinceLevelLoad;
            if (time >= burstTimeWait)
            {
                burstTimeWait = time + burstThrottleTime;
                burstCount = 0;
            }

            burstCount++;
        }
    }
}