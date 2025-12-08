using System;
using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(-1)]
    public class PlaylistSearch : ATBehaviour
    {
        public bool searchHiddenPlaylists = false;
        public bool searchOnEachKeypress = true;

        [Min(1), Tooltip("The number of milliseconds that the playlist search will occupy per frame. The higher the number the faster the playlist search goes, but the higher the frame lag is.")]
        public int searchAggressionLevel = 5;

        public InputField titleInput;
        public Text titleText;
        public InputField tagsInput;
        public Text tagsText;
        public Text searchDisplay;
        public Playlist[] playlistsToSearch;
        [NonSerialized] public string IN_SEARCH_TITLE = "";
        [NonSerialized] public string IN_SEARCH_TAGS = "";

        private System.Diagnostics.Stopwatch searchDuration;
        private int currentSearchIndex;
        private bool[] currentSearchCache;
        private string currentSearchTitle;
        private string currentSearchTagString;
        private float currentSearchTotal;
        private int currentSearchCount;
        private int currentSearchPlaylistIndex;
        private Playlist currentSearchPlaylist;
        private bool currentSearchActive;
        private bool hasTitleInput;
        private bool hasTitleText;
        private bool hasTagsInput;
        private bool hasTagsText;
        private bool hasSearchDisplay;
        private bool hasSearchTargets = false;

        public override void Start()
        {
            if (init) return;
            base.Start();

            hasSearchTargets = playlistsToSearch != null && playlistsToSearch.Length > 0;
            hasTitleInput = titleInput != null;
            hasTitleText = titleText != null;
            hasTagsInput = tagsInput != null;
            hasTagsText = tagsText != null;
            hasSearchDisplay = searchDisplay != null;
        }

        public void _UpdateSearchOnKeypress()
        {
            if (searchOnEachKeypress) _UpdateSearch();
        }

        // Input field event that sanitizes the input before calling the generic event.
        public void _UpdateSearch()
        {
            // the text component variation is for handling the fact that quest doesn't have inputfield access
            string title = EMPTYSTR;
            string tags = EMPTYSTR;
            if (hasTitleInput) title = titleInput.text.Trim().ToLower();
            else if (hasTitleText) title = titleText.text.Trim().ToLower();
            if (hasTagsInput) tags = tagsInput.text.Trim().ToLower();
            else if (hasTagsText) tags = tagsText.text.Trim().ToLower();
            _Search(title, tags);
        }

        // Generic event for allowing udon behaviours to trigger if desired.
        public void _Search()
        {
            _Search(IN_SEARCH_TITLE, IN_SEARCH_TAGS);
            IN_SEARCH_TITLE = EMPTYSTR;
            IN_SEARCH_TAGS = EMPTYSTR;
        }

        public void _Search(string title, string tags)
        {
            bool validationPassed = true;
            if (playlistsToSearch.Length == 0)
            {
                Warn("No playlists attached to search.");
                validationPassed = false;
            }
            else if (currentSearchTitle == title && currentSearchTagString == tags)
            {
                Info("Title and tags are the same as previous. Skip redundant work.");
                validationPassed = false;
            }

            if (IsDebugEnabled)
                Debug(string.Format(
                    "Searching {0} playlists for title '{1}' and tags '{2}'",
                    new object[] { playlistsToSearch.Length, title, tags }
                ));
            if (validationPassed) startSearch(title, tags);
        }

        private bool playlistIsSearchable(Playlist playlist) =>
            playlist != null && (searchHiddenPlaylists || playlist.gameObject.activeInHierarchy);

        private void startSearch(string searchTitle, string searchTagString)
        {
            currentSearchIndex = -1;
            bool hasNextPlaylist = nextPlaylist();
            if (hasNextPlaylist)
            {
                currentSearchActive = true;
                currentSearchTotal = 0;
                currentSearchCount = 0;
                currentSearchTitle = searchTitle;
                currentSearchTagString = searchTagString;
                // get the total expected count for all connected playlists
                foreach (Playlist playlist in playlistsToSearch)
                    if (playlistIsSearchable(playlist))
                        currentSearchTotal += playlist.mainUrls.Length;
                searchDuration = System.Diagnostics.Stopwatch.StartNew();
                SendCustomEventDelayedFrames(nameof(_ProcessSearch), 1);
            }
        }

        public void _ProcessSearch()
        {
            if (currentSearchActive) { }
            else return; // must trigger a proper search to be allowed.

            var timeout = System.Diagnostics.Stopwatch.StartNew();
            var playlistTitles = currentSearchPlaylist.titles;
            var playlistTags = currentSearchPlaylist.tags;
            var urls = currentSearchPlaylist.mainUrls;
            int len = urls.Length;
            bool hasSearchTitle = !string.IsNullOrWhiteSpace(currentSearchTitle);
            bool hasSearchTagString = !string.IsNullOrWhiteSpace(currentSearchTagString);
            // treat as a fixed update speed
            while (timeout.ElapsedMilliseconds <= searchAggressionLevel)
            {
                if (currentSearchPlaylistIndex >= len)
                {
                    currentSearchPlaylist.UpdateFilter(currentSearchCache);
                    if (!nextPlaylist())
                    {
                        currentSearchActive = false;
                        currentSearchCount = (int)currentSearchTotal;
                        break;
                    }
                }

                bool shown = false;

                if (hasSearchTagString)
                {
                    string tagString = playlistTags[currentSearchPlaylistIndex];
                    if (string.IsNullOrWhiteSpace(tagString)) { }
                    else
                    {
                        string[] searchORs = currentSearchTagString.Split(',');
                        string[] tags = tagString.Split(',');
                        // strip sort prefixes from tags for searching
                        for (int i = 0; i < tags.Length; i++)
                        {
                            var t = tags[i];
                            var idx = t.IndexOf(':');
                            if (idx > -1) tags[i] = t.Substring(idx + 1);
                        }

                        foreach (string or in searchORs)
                        {
                            // skip blank search tags
                            if (string.IsNullOrWhiteSpace(or)) continue;
                            string[] searchANDs = or.Split('+');
                            foreach (string and in searchANDs)
                            {
                                // skip blank entry tags
                                if (string.IsNullOrWhiteSpace(and)) continue;
                                int idx = Array.IndexOf(tags, and);
                                shown = idx > -1;
                                // if ANY of the ANDs are not present, the entry should be hidden
                                if (!shown) break;
                            }

                            // if ANY of the ORs are present, entry is shown and can skip the rest of the OR options
                            if (shown) break;
                        }
                    }
                }
                // if there's no tag search, assume visible by default and then check for title search
                else shown = true;

                // if entry is visible, ensure that the title search matches as well
                if (hasSearchTitle && shown)
                {
                    string title = playlistTitles[currentSearchPlaylistIndex];
                    if (string.IsNullOrWhiteSpace(title)) { }
                    else
                    {
                        title = title.Trim().ToLower();
                        // title search splits by spaces because that is a natural word delimiter
                        string[] searchANDs = currentSearchTitle.Split(' ');
                        foreach (string and in searchANDs)
                        {
                            if (string.IsNullOrWhiteSpace(and)) continue;
                            shown = title.Contains(and.Trim().ToLower());
                            // if any of the words dont' match, entry is hidden and can skip the rest of the AND options
                            if (!shown) break;
                        }
                    }
                }

                currentSearchCache[currentSearchPlaylistIndex] = !shown;
                currentSearchPlaylistIndex++;
                currentSearchCount++;
            }

            if (currentSearchCount > currentSearchTotal) currentSearchCount = (int)currentSearchTotal;
            if (hasSearchDisplay)
            {
                string _title = "";
                if (hasSearchTitle) _title = $"Search {currentSearchCount / currentSearchTotal:P2} Complete ({searchDuration.ElapsedMilliseconds:D3}ms)";
                searchDisplay.text = _title;
            }

            if (currentSearchActive) SendCustomEventDelayedFrames(nameof(_ProcessSearch), 1);
        }

        private bool nextPlaylist()
        {
            currentSearchIndex++;
            if (IsDebugEnabled) Debug("Next Playlist pre-check: " + currentSearchIndex);
            if (currentSearchIndex >= playlistsToSearch.Length) return false; // end of list
            currentSearchPlaylist = playlistsToSearch[currentSearchIndex];
            while (!playlistIsSearchable(currentSearchPlaylist))
            {
                currentSearchIndex++;
                if (currentSearchIndex >= playlistsToSearch.Length) return false; // end of list
                currentSearchPlaylist = playlistsToSearch[currentSearchIndex];
            }

            currentSearchPlaylistIndex = 0;
            currentSearchPlaylist.Start();
            currentSearchCache = currentSearchPlaylist.Hidden;
            if (IsDebugEnabled) Debug($"Playlist size: {currentSearchCache.Length}");
            return true;
        }
    }
}