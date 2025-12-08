using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public partial class History : TVPlugin
    {
        [NonSerialized] public int IN_INDEX = -1;

        [SerializeField] internal Queue queue;

        [Range(5, 50), SerializeField,
         I18nTooltip("The maximum number of entries the list will retain. All older entries will be removed.")
        ]
        internal int numberOfEntries = 15;

        [SerializeField,
         I18nTooltip("This option allows a URL to be copied IF the corresponding reference is available on the Template object.")
        ]
        internal bool enableUrlCopy;

        [SerializeField,
         I18nTooltip("This option ensures that a URL can only be copied by an authorized user.")
        ]
        internal bool protectUrlCopy;

        [SerializeField,
         I18nTooltip("The label that will be shown in place of an empty title.")
        ]
        internal string emptyTitlePlaceholder = "No Title";

        [SerializeField,
         I18nInspectorName("Header Text"), I18nTooltip("Optional header text to display next to the playlist. If the value is empty, no modification occurs.")
        ]
        internal string header = "";

        internal VRCUrl[] mainUrls;
        internal VRCUrl[] alternateUrls;
        internal string[] titles;
        internal string[] addedBy;
        internal int nextIndex = 0;
        internal bool newEntryExpected = false;
        internal bool hasQueue;

        // wait until all other plugins have finished before considering the history
        public override sbyte Priority => 127;

        private int CurrentIndex => wrap(nextIndex - 1);

        public override void Start()
        {
            if (init) return;
            base.Start();

            mainUrls = new VRCUrl[numberOfEntries];
            alternateUrls = new VRCUrl[numberOfEntries];
            titles = new string[numberOfEntries];
            addedBy = new string[numberOfEntries];

            hasQueue = queue != null;
        }

        #region TV Events

        public override void _TvMediaReady()
        {
            // short circuit for generic refreshes or video player swaps
            var main = tv.urlMain;
            var localMain = mainUrls[CurrentIndex];
            if (!newEntryExpected || localMain != null && localMain.Get() == main.Get()) return;
            mainUrls[nextIndex] = tv.urlMain;
            alternateUrls[nextIndex] = tv.urlAlt;
            titles[nextIndex] = tv.title;
            addedBy[nextIndex] = tv.addedBy;
            nextIndex = wrap(nextIndex + 1);
            updateUI();
        }

        public override void _TvTitleChange()
        {
            var currentIndex = CurrentIndex;
            // update the title of the current entry if available and no new entry is expected
            if (!newEntryExpected && mainUrls[currentIndex] != null)
            {
                titles[currentIndex] = OUT_TITLE;
            }
        }

        public override void _TvMediaChange()
        {
            newEntryExpected = true;
        }

        public override void _TvAuthChange() => updateUI();

        public override void _TvLock() => updateUI();

        public override void _TvUnLock() => updateUI();

        #endregion

        private int wrap(int value)
        {
            value %= numberOfEntries;
            if (value < 0) value += numberOfEntries;
            return value;
        }

        private void updateUI() => SendManagedEvent(nameof(HistoryUI.UpdateUI));

        [PublicAPI]
        public void Clear()
        {
            // purge all except the latest media which would still be referenced interally to the TV
            // since the whole list is being purged, reset the ring buffer index
            var currentIndex = CurrentIndex;
            var mainUrl = mainUrls[currentIndex];
            var altUrl = alternateUrls[currentIndex];
            var title = titles[currentIndex];
            System.Array.Clear(mainUrls, 0, mainUrls.Length);
            System.Array.Clear(alternateUrls, 0, alternateUrls.Length);
            System.Array.Clear(titles, 0, titles.Length);
            mainUrls[0] = mainUrl;
            alternateUrls[0] = altUrl;
            titles[0] = title;
            nextIndex = 1;
            SendManagedEvent(nameof(HistoryUI.Clear));
        }

        public void SelectEntry()
        {
            if (IN_INDEX == -1) return; // bad index value;
            SelectEntry(IN_INDEX);
            IN_INDEX = -1;
        }

        [PublicAPI]
        public void SelectEntry(int index)
        {
            if (index == -1) return; // bad index value
            index = wrap(CurrentIndex - index); // convert the entry index into the ring-buffer index
            if (hasQueue) queue._AddEntry(mainUrls[index], alternateUrls[index], titles[index]);
            else tv._ChangeMedia(mainUrls[index], alternateUrls[index], titles[index]);
        }

        [PublicAPI]
        public void SetQueue(Queue plugin)
        {
            queue = plugin;
            hasQueue = queue != null;
        }

        [PublicAPI]
        public void UpdateHeader(string text)
        {
            header = text;
            updateUI();
        }
    }
}