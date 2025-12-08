using ArchiTech.SDK;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Button = UnityEngine.UI.Button;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;
using VRC.Udon.Common.Enums;

namespace ArchiTech.ProTV
{
    [DefaultExecutionOrder(-1)]
    [HelpURL("https://protv.dev/guides/playlist")]
    public class PlaylistUI : TVPluginUI
    {
        [SerializeField] internal Playlist playlist;

        [SerializeField,
         I18nInspectorName("Playlist ScrollView")
        ]
        internal ScrollRect scrollView;

        [SerializeField, FormerlySerializedAs("content"),
         I18nInspectorName("Playlist Item Container")
        ]
        internal RectTransform listContainer;

        [SerializeField,
         I18nInspectorName("Playlist Header")
        ]
        internal Text headerDisplay;

        [SerializeField] internal TextMeshProUGUI headerDisplayTMP;

        [SerializeField,
         I18nInspectorName("Playlist Item Template")
        ]
        internal GameObject template;

        [SerializeField,
         I18nTooltip("Text display component for the relevant entry URL. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text urlDisplay;

        [SerializeField] internal TextMeshProUGUI urlDisplayTMP;

        [SerializeField,
         I18nTooltip("Text display component for the relevant entry Title. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text titleDisplay;

        [SerializeField] internal TextMeshProUGUI titleDisplayTMP;

        [SerializeField,
         I18nTooltip("Text display component for the relevant entry Description. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text descriptionDisplay;

        [SerializeField] internal TextMeshProUGUI descriptionDisplayTMP;

        [SerializeField,
         I18nTooltip("Interaction component for relevant entry activation. MUST be a child of the Template object.")
        ]
        internal Button selectAction;

        [SerializeField,
         I18nTooltip("Image display component for the relevant entry image. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Image imageDisplay;

        [SerializeField,
         I18nTooltip("Visual component for relevant entry loading progress. MUST be a child of the Template object.")
        ]
        internal Slider loadingBar;

        [SerializeField] internal Button autoplay;

        [SerializeField,
         I18nInspectorName("Icon Display")
        ]
        internal Image autoplayIndicator;

        [SerializeField,
         I18nInspectorName("Default Playlist Image Override"), I18nTooltip("Per-UI image override for the default playlist image, shown when an entry image is not provided.")
        ]
        internal Sprite defaultImage;

        [SerializeField] internal Sprite autoplayOn;
        [SerializeField] internal Sprite autoplayOff;
        [SerializeField] internal Color autoplayOnColor = Color.white;
        [SerializeField] internal Color autoplayOffColor = Color.grey;

        [SerializeField, HideInInspector] internal string urlDisplayTmplPath;
        [SerializeField, HideInInspector] internal string urlDisplayTMPTmplPath;
        [SerializeField, HideInInspector] internal string titleDisplayTmplPath;
        [SerializeField, HideInInspector] internal string titleDisplayTMPTmplPath;
        [SerializeField, HideInInspector] internal string descriptionDisplayTmplPath;
        [SerializeField, HideInInspector] internal string descriptionDisplayTMPTmplPath;
        [SerializeField, HideInInspector] internal string selectActionTmplPath;
        [SerializeField, HideInInspector] internal string loadingBarTmplPath;
        [SerializeField, HideInInspector] internal string imageDisplayTmplPath;


        // entry caches
        private Transform[] entryRefs;
        private Text[] urlDisplayRefs;
        private Text[] titleDisplayRefs;
        private Text[] descriptionDisplayRefs;
        private TextMeshProUGUI[] urlDisplayTMPRefs;
        private TextMeshProUGUI[] titleDisplayTMPRefs;
        private TextMeshProUGUI[] descriptionDisplayTMPRefs;
        private Button[] selectActionRefs;
        private Slider[] loadingBarRefs;
        private Image[] imageDisplayRefs;


        // an array that represents the visible entries shown in the scene based on the filteredView array
        // unlike the previous two, this array's contents corresponds to indexes of the filteredView array
        // eg: to get the actual URL based on a particular entry of the current view, you'd access it via urls[filteredView[currentView[index]]]
        private int[] currentView = new int[0];
        internal int viewOffset = -1;

        private TVManager tv;

        private bool isLoading = false;
        private Slider loading;
        private float loadingBarDamp;
        private float loadingPercent;
        private bool hasLoading;
        private bool hasHeaderDisplay;
        private bool hasHeaderDisplayTMP;
        private bool hasUrlDisplay;
        private bool hasTitleDisplay;
        private bool hasDescriptionDisplay;
        private bool hasUrlDisplayTMP;
        private bool hasTitleDisplayTMP;
        private bool hasDescriptionDisplayTMP;
        private bool hasSelectAction;
        private bool hasLoadingBar;
        private bool hasImageDisplay;
        private bool showUrls;
        private bool hasPlaylist;
        private bool hasAutoplay;
        private bool hasAutoplayIndicator;

        [SerializeField] internal int _EDITOR_templateUpgrade;

        public override void Start()
        {
            if (playlist == null) playlist = GetComponentInParent<Playlist>();
            hasPlaylist = playlist != null && playlist.storage != null;
            if (init || !hasPlaylist) return;
            base.Start();

            playlist._RegisterListener(this);
            tv = playlist.tv;

            cacheEntryRefs();

            if (template != null) template.SetActive(false);
            showUrls = hasPlaylist && playlist.showUrls && (hasUrlDisplay || hasUrlDisplayTMP);

            hasAutoplay = autoplay != null;
            if (hasAutoplay)
            {
                if (autoplayIndicator == null) autoplayIndicator = autoplay.image;
                hasAutoplayIndicator = autoplayIndicator != null;
            }
        }

        public override void _ManagerReady()
        {
            seekView(playlist.sortViewIndexToFilteredViewIndex(playlist.currentSortViewIndex));
            UpdateAutoplay();
        }

        public override void UpdateUI()
        {
            RetargetActive();
        }

        public void SwitchEntry()
        {
            if (!init) return;
            var index = getDetectedEntry(selectActionRefs);
            if (index == -1) return; // no valid index available
            playlist.SwitchEntry(index);
        }

        public void ManualPlay()
        {
            if (playlist != null) playlist.ManualPlay();
        }

        public void AutoPlay()
        {
            if (playlist != null) playlist.AutoPlay();
        }

        public void UpdateLoadingBar()
        {
            if (isLoading)
            {
                SendCustomEventDelayedFrames(nameof(UpdateLoadingBar), 1, EventTiming.LateUpdate);
                if (hasLoading) loadingPercent = loading.value;
                if (loadingPercent > 0.95f) return;
                float dampSpeed = loadingPercent > 0.8f ? 0.4f : 0.3f;
                loadingPercent = Mathf.SmoothDamp(loadingPercent, 1f, ref loadingBarDamp, dampSpeed);
                if (hasLoading) loading.value = loadingPercent;
            }
        }

        public void LoadingStart()
        {
            isLoading = true;
            loadingPercent = 0f;
            if (hasLoading) loading.value = 0f;
            UpdateLoadingBar();
        }

        public void LoadingEnd()
        {
            isLoading = false;
            loadingPercent = 1f;
            if (hasLoading) loading.value = 1f;
        }

        public void LoadingAbort()
        {
            isLoading = false;
            loadingPercent = 0f;
            if (hasLoading) loading.value = 0f;
        }

        public void ToggleAutoPlay()
        {
            playlist.ToggleAutoPlay();
        }

        public void UpdateFilter()
        {
            // since the filtered array changed size, recalculate the total entries height
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            // limit offset to the url max minus the last "page", account for the "extra" overflow row as well.
            var maxRow = (playlist.filteredViewCount - 1) / horizontalCount + 1;
            var contentHeight = maxRow * item.height;

            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            seekView(0);
        }

        public void SeekView()
        {
            if (OUT_INDEX == -1) return;
            seekView(OUT_INDEX);
            OUT_INDEX = -1;
        }

        private void seekView(int filteredViewIndex)
        {
            if (!init) return;
            var filteredViewCount = playlist.filteredViewCount;
            filteredViewIndex = Mathf.Clamp(filteredViewIndex, 0, filteredViewCount - 1);
            var scrollbar = scrollView.verticalScrollbar;
            if (scrollbar != null)
            {
                float value = Mathf.Clamp01(1f - ((float)filteredViewIndex) / filteredViewCount);
                scrollbar.SetValueWithoutNotify(value);
            }

            if (updateView(filteredViewIndex, true)) RetargetActive();
        }

        private void cacheEntryRefs()
        {
            hasUrlDisplay = urlDisplay != null;
            hasTitleDisplay = titleDisplay != null;
            hasDescriptionDisplay = descriptionDisplay != null;
            hasUrlDisplayTMP = urlDisplayTMP != null;
            hasTitleDisplayTMP = titleDisplayTMP != null;
            hasDescriptionDisplayTMP = descriptionDisplayTMP != null;
            hasSelectAction = selectAction != null;
            hasLoadingBar = loadingBar != null;
            hasImageDisplay = imageDisplay != null;
            hasHeaderDisplay = headerDisplay != null;
            hasHeaderDisplayTMP = headerDisplayTMP != null;

            int cacheSize = listContainer.childCount;
            entryRefs = new RectTransform[cacheSize];
            urlDisplayRefs = new Text[cacheSize];
            titleDisplayRefs = new Text[cacheSize];
            descriptionDisplayRefs = new Text[cacheSize];
            urlDisplayTMPRefs = new TextMeshProUGUI[cacheSize];
            titleDisplayTMPRefs = new TextMeshProUGUI[cacheSize];
            descriptionDisplayTMPRefs = new TextMeshProUGUI[cacheSize];
            selectActionRefs = new Button[cacheSize];
            loadingBarRefs = new Slider[cacheSize];
            imageDisplayRefs = new Image[cacheSize];

            for (int i = 0; i < cacheSize; i++)
            {
                Transform entry = listContainer.GetChild(i);
                Transform t;
                entryRefs[i] = entry;

                if (hasUrlDisplay)
                {
                    t = entry;
                    if (urlDisplayTmplPath != EMPTYSTR) t = entry.Find(urlDisplayTmplPath);
                    urlDisplayRefs[i] = t.GetComponent<Text>();
                }

                if (hasTitleDisplay)
                {
                    t = entry;
                    if (titleDisplayTmplPath != EMPTYSTR) t = entry.Find(titleDisplayTmplPath);
                    titleDisplayRefs[i] = t.GetComponent<Text>();
                }

                if (hasDescriptionDisplay)
                {
                    t = entry;
                    if (descriptionDisplayTmplPath != EMPTYSTR) t = entry.Find(descriptionDisplayTmplPath);
                    descriptionDisplayRefs[i] = t.GetComponent<Text>();
                }

                if (hasUrlDisplayTMP)
                {
                    t = entry;
                    if (urlDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(urlDisplayTMPTmplPath);
                    urlDisplayTMPRefs[i] = t.GetComponent<TextMeshProUGUI>();
                }

                if (hasTitleDisplayTMP)
                {
                    t = entry;
                    if (titleDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(titleDisplayTMPTmplPath);
                    titleDisplayTMPRefs[i] = t.GetComponent<TextMeshProUGUI>();
                }

                if (hasDescriptionDisplayTMP)
                {
                    t = entry;
                    if (descriptionDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(descriptionDisplayTMPTmplPath);
                    descriptionDisplayTMPRefs[i] = t.GetComponent<TextMeshProUGUI>();
                }

                if (hasSelectAction)
                {
                    t = entry;
                    if (selectActionTmplPath != EMPTYSTR) t = entry.Find(selectActionTmplPath);
                    selectActionRefs[i] = t.GetComponent<Button>();
                }

                if (hasLoadingBar)
                {
                    t = entry;
                    if (loadingBarTmplPath != EMPTYSTR) t = entry.Find(loadingBarTmplPath);
                    loadingBarRefs[i] = t.GetComponent<Slider>();
                }

                if (hasImageDisplay)
                {
                    t = entry;
                    if (imageDisplayTmplPath != EMPTYSTR) t = entry.Find(imageDisplayTmplPath);
                    imageDisplayRefs[i] = t.GetComponent<Image>();
                }
            }
        }

        public void UpdateView()
        {
            if (!init) return;
            int filteredViewIndex = 0;
            // must have scrollbar and scrollbar must be visible
            var scrollbar = scrollView.verticalScrollbar;
            var viewportRect = scrollView.viewport.rect;
            if (scrollbar != null && scrollView.content.rect.height > viewportRect.height)
            {
                var percent = (1f - scrollbar.value);
                Rect max = viewportRect;
                Rect item = ((RectTransform)template.transform).rect;
                var horizontalCount = Mathf.FloorToInt(max.width / item.width);
                if (horizontalCount == 0) horizontalCount = 1;
                var verticalCount = Mathf.FloorToInt(max.height / item.height);
                filteredViewIndex = Mathf.FloorToInt(percent * (playlist.filteredViewCount - verticalCount * horizontalCount + horizontalCount));
            }

            if (updateView(filteredViewIndex, false)) RetargetActive();
        }

        private bool updateView(int filteredViewIndex, bool forceUpdate)
        {
            // modifies the scope of the view, cache the offset for later use
            var newViewOffset = calculateCurrentViewOffset(filteredViewIndex);
            bool viewChanged = newViewOffset != viewOffset;
            if (viewChanged || forceUpdate)
            {
                viewOffset = newViewOffset;
                updateCurrentView(viewOffset);
            }

            return viewChanged;
        }

        // Takes in the current index and calculates a rounded value based on the horizontal count
        // This ensures that elements don't incidentally shift horizontally and only vertically while scrolling
        private int calculateCurrentViewOffset(int filteredViewIndex)
        {
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "row", account for the "extra" overflow row as well.
            var maxRawRow = playlist.filteredViewCount / horizontalCount + 1;
            // clamp the min/max row to the view area boundaries
            var maxRow = Mathf.Min(maxRawRow, maxRawRow - verticalCount);
            if (maxRow == 0) maxRow = 1;

            var maxOffset = maxRow * horizontalCount;
            var currentRow = filteredViewIndex / horizontalCount; // int DIV causes stepped values, good
            var currentOffset = currentRow * horizontalCount;
            // currentOffset will be smaller than maxOffset when the scroll limit has not yet been reached
            var targetOffset = Mathf.Min(currentOffset, maxOffset);
            return Mathf.Max(0, targetOffset);
        }

        private void updateCurrentView(int filteredViewIndex)
        {
            var count = listContainer.childCount;
            if (currentView.Length != count) currentView = new int[count];
            if (IsDebugEnabled) Debug($"Updating view to index {filteredViewIndex}");
            string _log = "None";
            var mainUrls = playlist.mainUrls;
            var alternateUrls = playlist.alternateUrls;
            var titles = playlist.titles;
            var descriptions = playlist.descriptions;
            // var tags =  playlist.tags;
            var images = playlist.images;
            var header = playlist.header;
            var filteredView = playlist.filteredView;
            var filteredViewCount = playlist.filteredViewCount;
            var fallbackImage = defaultImage != null ? defaultImage : playlist.placeholderImage;

            if (!string.IsNullOrEmpty(header))
            {
                if (hasHeaderDisplay) headerDisplay.text = header;
                if (hasHeaderDisplayTMP) headerDisplayTMP.text = header;
            }

            for (int i = 0; i < count; i++)
            {
                var entry = listContainer.GetChild(i);
                var entryGO = entry.gameObject;
                if (filteredViewIndex >= filteredViewCount)
                {
                    // urls have exceeded count, hide the remaining entries
                    entryGO.SetActive(false);
                    currentView[i] = -1;
                    continue;
                }

                if (IsTraceEnabled)
                {
                    if (i == 0) _log = $"Visible Indexes:\n{filteredView[filteredViewIndex]}";
                    else _log += $", {filteredView[filteredViewIndex]}";
                }

                entryGO.SetActive(true);
                // update entry contents
                var index = filteredView[filteredViewIndex];
                var main = mainUrls[index].Get();
                var alt = alternateUrls[index].Get();
                var rawTitle = titles[index];
                var desc = descriptions[index];

                bool hasOnlyTitle = string.IsNullOrWhiteSpace(main) && string.IsNullOrWhiteSpace(alt);
                var title = rawTitle.StartsWith("~") ? rawTitle.Substring(1) : rawTitle;

                if (hasUrlDisplay && showUrls) urlDisplayRefs[i].text = main;
                if (hasTitleDisplay) titleDisplayRefs[i].text = title;
                if (hasDescriptionDisplay) descriptionDisplayRefs[i].text = desc;
                if (hasUrlDisplayTMP && showUrls) urlDisplayTMPRefs[i].text = main;
                if (hasTitleDisplayTMP) titleDisplayTMPRefs[i].text = title;
                if (hasDescriptionDisplayTMP) descriptionDisplayTMPRefs[i].text = desc;
                // disable the select button action if no urls are available. Effectively treats the entry as a "section header" entry.
                // though if you have shuffle on load enabled, the "section header" concept becomes basically useless. Take note.
                if (hasSelectAction) selectActionRefs[i].interactable = !hasOnlyTitle || rawTitle.StartsWith("~");
                if (hasImageDisplay)
                {
                    var image = imageDisplayRefs[i];
                    var imageEntry = images[index];
                    if (imageEntry == null) imageEntry = fallbackImage;
                    image.sprite = imageEntry;
                    image.gameObject.SetActive(imageEntry != null);
                }

                currentView[i] = filteredViewIndex;
                filteredViewIndex++;
            }

            if (IsTraceEnabled) Trace(_log);
        }

        public void UpdateAutoplay()
        {
            if (hasAutoplayIndicator)
            {
                if (playlist.autoplayList)
                {
                    autoplayIndicator.sprite = autoplayOn;
                    autoplayIndicator.color = autoplayOnColor;
                }
                else
                {
                    autoplayIndicator.sprite = autoplayOff;
                    autoplayIndicator.color = autoplayOffColor;
                }
            }
        }

        public void RetargetActive()
        {
            // if autoplay is disabled, try to see if the current media matches one on the playlist, if so, indicate loading
            if (hasLoading) loading.value = 0f;
            int found = findCurrentViewIndex();
            // cache the found index's Slider component, otherwise null
            if (found > -1)
            {
                if (hasLoadingBar)
                {
                    loading = loadingBarRefs[found];
                    hasLoading = loading != null;
                    loading.value = loadingPercent;
                }

                if (IsTraceEnabled) Trace($"Media index {found} found");
            }
            else
            {
                if (IsTraceEnabled) Trace($"Media index not within view");
                loading = null;
                hasLoading = false;
            }
        }

        private int getDetectedEntry(Selectable[] referencesArray)
        {
            if (IsDebugEnabled) Debug($"Auto-detecting selected index via interaction of possible {referencesArray.Length}");
            var sortView = playlist.sortView;
            for (int i = 0; i < referencesArray.Length; i++)
            {
                var @ref = referencesArray[i];
                if (@ref == null) continue;
                if (!@ref.enabled)
                {
                    var rawIndex = currentViewIndexToRawIndex(i);
                    int index = System.Array.IndexOf(sortView, rawIndex);
                    if (IsDebugEnabled) Debug($"Detected view index {index} from raw {rawIndex}");
                    return index;
                }
            }

            Debug("Index not able to be auto-detected");
            return -1;
        }

        private int findCurrentViewIndex()
        {
            if (tv == null) return -1;
            var url = tv.urlMain.Get();
            // if the current index is playing on the TV and not hidden, 
            //  return either it's position in the current view, or -1 if it's not visible in the current view
            var rawIndex = playlist.CurrentEntryIndex;
            var mainUrls = playlist.mainUrls;
            if (rawIndex > -1)
            {
                if (mainUrls[rawIndex].Get() == url)
                    if (!playlist.hidden[rawIndex])
                        return System.Array.IndexOf(currentView, System.Array.IndexOf(playlist.filteredView, rawIndex));
            }

            // then if the current index IS hidden or IS NOT playing on the TV, 
            // attempt a fuzzy search to find another index that matches that URL
            // do not need to check for hidden here as current view already has that taken into account
            for (int i = 0; i < currentView.Length; i++)
            {
                var listIndex = currentViewIndexToRawIndex(i);
                if (listIndex > -1 && mainUrls[listIndex].Get() == url) return i;
            }

            return -1;
        }


        // take a given index (typically derived from the button a player clicks on in the UI)
        // and reverse the values through the arrays to get the original list index.
        private int currentViewIndexToRawIndex(int currentViewIndex)
        {
            if (currentViewIndex == -1) return -1;
            if (currentViewIndex >= currentView.Length) return -1;
            int filteredViewIndex = currentView[currentViewIndex];
            return playlist.filteredViewIndexToRawIndex(filteredViewIndex);
        }

        private int currentViewIndexToNextSortViewIndex(int currentViewIndex)
        {
            if (currentViewIndex == -1) return -1;
            if (currentViewIndex >= currentView.Length) return -1;
            int filteredViewIndex = currentView[currentViewIndex];
            if (filteredViewIndex == -1) return -1;
            return playlist.filteredViewIndexToNextSortViewIndex(filteredViewIndex);
        }

        [PublicAPI]
        public void SetPlaylist(Playlist plugin)
        {
            if (hasPlaylist) playlist._UnregisterListener(this);
            playlist = plugin;
            hasPlaylist = playlist != null && playlist.storage != null;
            showUrls = hasPlaylist && playlist.showUrls && (hasUrlDisplay || hasUrlDisplayTMP);
            if (hasPlaylist) playlist._RegisterListener(this);
        }
    }
}