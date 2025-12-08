using ArchiTech.SDK;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace ArchiTech.ProTV
{
    public class QueueUI : TVPluginUI
    {
        private const string TSTR_QUEUE_LIMIT_REACHED = "Queue Limit Reached";
        private const string TSTR_PLAYER_LIMIT_REACHED = "Personal Limit Reached";
        private const string TSTR_URL_IS_BLOCKED = "URL Domain Is Blocked";

        [SerializeField] internal Queue queue;

        [SerializeField,
         I18nInspectorName("Show Count"), I18nTooltip("Should the number of queue items be appended to the header text?")
        ]
        internal bool showCountInHeader = true;

        [SerializeField,
         I18nInspectorName("List Container"), I18nTooltip("Container reference which the queue entries will be added to/removed from. It is recommended to have either a VerticalLayoutGroup, HorizontalLayoutGroup or GridLayoutGroup component on this element for easy layout controls.")
        ]
        internal RectTransform listContainer;

        [SerializeField,
         I18nInspectorName("Template Object"), I18nTooltip("Object that will be instantiated for each entry in the queue. Each entry will be parented to the List Container transform upon instantiation.")
        ]
        internal GameObject template;

        [SerializeField,
         I18nInspectorName("URL Display"), I18nTooltip("Text display component for the relevant entry URL. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text urlDisplay;

        [SerializeField] internal TextMeshProUGUI urlDisplayTMP;

        [SerializeField,
         I18nInspectorName("Title Display"), I18nTooltip("Text display component for the relevant entry Title. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text titleDisplay;

        [SerializeField] internal TextMeshProUGUI titleDisplayTMP;

        [SerializeField,
         I18nInspectorName("Owner Display"), I18nTooltip("Text display component for the relevant entry Owner name. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text ownerDisplay;

        [SerializeField] internal TextMeshProUGUI ownerDisplayTMP;

        [SerializeField,
         I18nInspectorName("Entry Select Button"), I18nTooltip("Interaction component for triggering the respective entry. MUST be a child of the Template object.")
        ]
        internal Button selectAction;

        [SerializeField,
         I18nInspectorName("Entry Remove Button"), I18nTooltip("Interaction component for relevant entry removal. MUST be a child of the Template object.")
        ]
        internal Button removeAction;

        [SerializeField,
         I18nInspectorName("Persist Entry Toggle"), I18nTooltip("Interaction component for relevant persistence toggle. MUST be a child of the Template object.")
        ]
        internal Toggle persistenceAction;

        [SerializeField,
         I18nInspectorName("Loading Bar"), I18nTooltip("Visual component for relevant entry loading progress. MUST be a child of the Template object.")
        ]
        internal Slider loadingBar;

        [SerializeField,
         I18nInspectorName("Info Notification"), I18nTooltip("This text element will be populated with any error/limit messages related to the queue. Supports standard UI and TMP.")
        ]
        internal Text toasterMsg;

        [SerializeField] internal TextMeshProUGUI toasterMsgTMP;

        [SerializeField,
         I18nInspectorName("Header Display")
        ]
        internal Text headerDisplay;

        [SerializeField] internal TextMeshProUGUI headerDisplayTMP;

        [SerializeField, HideInInspector] internal string selectActionTmplPath = null;
        [SerializeField, HideInInspector] internal string urlDisplayTmplPath = null;
        [SerializeField, HideInInspector] internal string titleDisplayTmplPath = null;
        [SerializeField, HideInInspector] internal string ownerDisplayTmplPath = null;
        [SerializeField, HideInInspector] internal string urlDisplayTMPTmplPath = null;
        [SerializeField, HideInInspector] internal string titleDisplayTMPTmplPath = null;
        [SerializeField, HideInInspector] internal string ownerDisplayTMPTmplPath = null;
        [SerializeField, HideInInspector] internal string removeActionTmplPath = null;
        [SerializeField, HideInInspector] internal string persistenceToggleTmplPath = null;
        [SerializeField, HideInInspector] internal string loadingBarTmplPath = null;

        private Transform[] entryRefs;
        private Button[] selectActionRefs;
        private Text[] urlDisplayRefs;
        private Text[] titleDisplayRefs;
        private Text[] ownerDisplayRefs;
        private TextMeshProUGUI[] urlDisplayTMPRefs;
        private TextMeshProUGUI[] titleDisplayTMPRefs;
        private TextMeshProUGUI[] ownerDisplayTMPRefs;
        private Button[] removeActionRefs;
        private Toggle[] persistenceToggleRefs;
        private Slider[] loadingBarRefs;

        private bool hasQueue;
        private bool hasSelectAction;
        private bool hasUrlDisplay;
        private bool hasTitleDisplay;
        private bool hasOwnerDisplay;
        private bool hasUrlDisplayTMP;
        private bool hasTitleDisplayTMP;
        private bool hasOwnerDisplayTMP;
        private bool hasRemoveAction;
        private bool hasPersistenceToggle;
        private bool hasLoadingBar;
        private bool hasToaster;
        private bool hasToasterTMP;
        private bool hasHeaderDisplay;
        private bool hasHeaderDisplayTMP;

        private int maxQueueLength;
        private int maxEntriesPerPlayer;
        private bool showUrlsInQueue;

        private Slider activeLoadingBar = null;
        private bool isLoading;
        private float loadingBarDamp;

        [SerializeField] internal int _EDITOR_templateUpgrade;

        public override void Start()
        {
            if (queue == null) queue = GetComponentInParent<Queue>();
            hasQueue = queue != null;
            if (init || !hasQueue) return;
            base.Start();

            queue._RegisterListener(this);

            hasSelectAction = selectAction != null;
            hasUrlDisplay = urlDisplay != null;
            hasTitleDisplay = titleDisplay != null;
            hasOwnerDisplay = ownerDisplay != null;
            hasUrlDisplayTMP = urlDisplayTMP != null;
            hasTitleDisplayTMP = titleDisplayTMP != null;
            hasOwnerDisplayTMP = ownerDisplayTMP != null;
            hasRemoveAction = removeAction != null;
            hasPersistenceToggle = persistenceAction != null;
            hasLoadingBar = loadingBar != null;
            hasToaster = toasterMsg != null;
            hasToasterTMP = toasterMsgTMP != null;
            hasHeaderDisplay = headerDisplay != null;
            hasHeaderDisplayTMP = headerDisplayTMP != null;

            if (!hasQueue) return;
            maxQueueLength = queue.maxQueueLength;
            maxEntriesPerPlayer = queue.maxEntriesPerPlayer;

            if (IsDebugEnabled && listContainer == null) Error($"Missing the list container object. This is required for the component to work.");
            if (IsDebugEnabled && listContainer.childCount > 0) Error($"Unexpected child objects within the list container object '{listContainer.name}'. Make sure you have provided a GameObject reference that does not have any children.");

            entryRefs = new Transform[maxQueueLength];
            if (hasSelectAction) selectActionRefs = new Button[maxQueueLength];
            if (hasUrlDisplay) urlDisplayRefs = new Text[maxQueueLength];
            if (hasTitleDisplay) titleDisplayRefs = new Text[maxQueueLength];
            if (hasOwnerDisplay) ownerDisplayRefs = new Text[maxQueueLength];
            if (hasUrlDisplayTMP) urlDisplayTMPRefs = new TextMeshProUGUI[maxQueueLength];
            if (hasTitleDisplayTMP) titleDisplayTMPRefs = new TextMeshProUGUI[maxQueueLength];
            if (hasOwnerDisplayTMP) ownerDisplayTMPRefs = new TextMeshProUGUI[maxQueueLength];
            if (hasRemoveAction) removeActionRefs = new Button[maxQueueLength];
            if (hasPersistenceToggle) persistenceToggleRefs = new Toggle[maxQueueLength];
            if (hasLoadingBar) loadingBarRefs = new Slider[maxQueueLength];

            if (template != null) template.SetActive(false);
            showUrlsInQueue = queue.showUrlsInQueue && (hasUrlDisplay || hasUrlDisplayTMP);

            var count = listContainer.childCount;
            while (count-- > 0) DestroyImmediate(listContainer.GetChild(0).gameObject);
            UpdateUI();
        }

        public void UpdateLoadingBar()
        {
            if (isLoading)
            {
                SendCustomEventDelayedFrames(nameof(UpdateLoadingBar), 1, EventTiming.LateUpdate);
                if (activeLoadingBar)
                {
                    var val = activeLoadingBar.value;
                    if (val > 0.95f) return;
                    activeLoadingBar.value = Mathf.SmoothDamp(val, 1f, ref loadingBarDamp, val > 0.8f ? 0.4f : 0.3f);
                }
            }
        }

        public void SwitchEntry()
        {
            if (!hasQueue) return;
            var index = getDetectedEntry(selectActionRefs);
            if (index > -1) queue._SwitchEntry(index);
        }

        public void PersistEntry()
        {
            if (!hasQueue) return;
            var index = getDetectedEntry(persistenceToggleRefs);
            if (index > -1 && index < queue.currentQueueLength)
                queue._PersistEntry(index, persistenceToggleRefs[entryIndexToRefIndex(index)].isOn);
        }

        public void RemoveEntry()
        {
            if (!hasQueue) return;
            var index = getDetectedEntry(removeActionRefs);
            if (index > -1) queue._RemoveEntry(index);
        }

        public void Skip() => queue._Skip();

        private int refIndexToEntryIndex(int refIndex)
        {
            if (refIndex == -1) return -1;
            return entryRefs[refIndex].GetSiblingIndex();
        }

        private int entryIndexToRefIndex(int entryIndex)
        {
            if (entryIndex == -1) return -1;
            if (entryIndex >= listContainer.childCount) return -1;
            return System.Array.IndexOf(entryRefs, listContainer.GetChild(entryIndex));
        }

        private int getDetectedEntry(Selectable[] referencesArray)
        {
            Debug("Auto-detecting selected index via interaction");
            for (int i = 0; i < referencesArray.Length; i++)
            {
                var @ref = referencesArray[i];
                if (@ref == null) continue;
                if (!@ref.enabled)
                {
                    var index = entryRefs[i].GetSiblingIndex();
                    if (IsDebugEnabled) Debug($"Detected index {index}");
                    return index;
                }
            }

            Debug("Index not able to be auto-detected");
            return -1;
        }

        public override void UpdateUI()
        {
            if (!hasQueue) return;
            var currentQueueLength = queue.currentQueueLength;
            var tv = queue.tv;
            var hasTV = tv != null;
            if (hasToaster) toasterMsg.text = EMPTYSTR;
            if (hasToasterTMP) toasterMsgTMP.text = EMPTYSTR;
            var controlBypass = hasTV && tv._IsAuthorized();
            int personalCount = 0;
            var count = listContainer.childCount;
            if (hasHeaderDisplay) headerDisplay.text = showCountInHeader ? $"{queue.header} ({currentQueueLength})" : queue.header;
            if (hasHeaderDisplayTMP) headerDisplayTMP.text = showCountInHeader ? $"{queue.header} ({currentQueueLength})" : queue.header;
            // remove old entries
            while (count > currentQueueLength)
            {
                var refIndex = entryIndexToRefIndex(count - 1);
                // manually purge the refs for easy null check later cause unity likes to lie to us about if a destroyed object == null randomly
                DestroyImmediate(entryRefs[refIndex].gameObject);
                entryRefs[refIndex] = null;
                if (hasSelectAction) selectActionRefs[refIndex] = null;
                if (hasUrlDisplay) urlDisplayRefs[refIndex] = null;
                if (hasTitleDisplay) titleDisplayRefs[refIndex] = null;
                if (hasOwnerDisplay) ownerDisplayRefs[refIndex] = null;
                if (hasUrlDisplayTMP) urlDisplayTMPRefs[refIndex] = null;
                if (hasTitleDisplayTMP) titleDisplayTMPRefs[refIndex] = null;
                if (hasOwnerDisplayTMP) ownerDisplayTMPRefs[refIndex] = null;
                if (hasRemoveAction) removeActionRefs[refIndex] = null;
                if (hasPersistenceToggle) persistenceToggleRefs[refIndex] = null;
                if (hasLoadingBar) loadingBarRefs[refIndex] = null;
                count--;
            }

            var mains = queue.mainUrls;
            var alts = queue.alternateUrls;
            var titles = queue.titles;
            var persists = queue.persistence;
            var owners = queue.owners;
            var addedBy = queue.addedBy;
            var lastEntry = queue.lastEntry;
            var currentEntry = queue.currentEntry;

            for (int i = 0; i < currentQueueLength; i++)
            {
                var url = isAndroid && hasTV && tv.preferAlternateUrlForQuest ? alts[i] : mains[i];
                var title = titles[i];
                var persist = persists[i];
                // if URLS are hidden, but no title is available, use the url as the title when appropriate.
                string urlStr, titleStr;
                if (showUrlsInQueue)
                {
                    urlStr = url.Get();
                    titleStr = title == EMPTYSTR ? "No Title" : title;
                }
                else
                {
                    urlStr = EMPTYSTR;
                    titleStr = title == EMPTYSTR ? url.Get() : title;
                }

                var children = listContainer.childCount;
                var refsEntry = -1;
                // add new entries
                Transform entry = null;
                if (i >= children)
                {
                    var go = Instantiate(template);
                    go.name = $"Entry ({children})";
                    go.SetActive(true);
                    entry = go.transform;
                    entry.SetParent(listContainer, false);
                    refsEntry = System.Array.IndexOf(entryRefs, null);
                    entryRefs[refsEntry] = entry;
                    Transform t;
                    if (hasSelectAction)
                    {
                        t = entry;
                        if (selectActionTmplPath != EMPTYSTR) t = entry.Find(selectActionTmplPath);
                        selectActionRefs[refsEntry] = t.GetComponent<Button>();
                    }

                    if (hasUrlDisplay)
                    {
                        t = entry;
                        if (urlDisplayTmplPath != EMPTYSTR) t = entry.Find(urlDisplayTmplPath);
                        urlDisplayRefs[refsEntry] = t.GetComponent<Text>();
                    }

                    if (hasTitleDisplay)
                    {
                        t = entry;
                        if (titleDisplayTmplPath != EMPTYSTR) t = entry.Find(titleDisplayTmplPath);
                        titleDisplayRefs[refsEntry] = t.GetComponent<Text>();
                    }

                    if (hasOwnerDisplay)
                    {
                        t = entry;
                        if (ownerDisplayTmplPath != EMPTYSTR) t = entry.Find(ownerDisplayTmplPath);
                        ownerDisplayRefs[refsEntry] = t.GetComponent<Text>();
                    }

                    if (hasUrlDisplayTMP)
                    {
                        t = entry;
                        if (urlDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(urlDisplayTMPTmplPath);
                        urlDisplayTMPRefs[refsEntry] = t.GetComponent<TextMeshProUGUI>();
                    }

                    if (hasTitleDisplayTMP)
                    {
                        t = entry;
                        if (titleDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(titleDisplayTMPTmplPath);
                        titleDisplayTMPRefs[refsEntry] = t.GetComponent<TextMeshProUGUI>();
                    }

                    if (hasOwnerDisplayTMP)
                    {
                        t = entry;
                        if (ownerDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(ownerDisplayTMPTmplPath);
                        ownerDisplayTMPRefs[refsEntry] = t.GetComponent<TextMeshProUGUI>();
                    }

                    if (hasRemoveAction)
                    {
                        t = entry;
                        if (removeActionTmplPath != EMPTYSTR) t = entry.Find(removeActionTmplPath);
                        removeActionRefs[refsEntry] = t.GetComponent<Button>();
                    }

                    if (hasPersistenceToggle)
                    {
                        t = entry;
                        if (persistenceToggleTmplPath != EMPTYSTR) t = entry.Find(persistenceToggleTmplPath);
                        persistenceToggleRefs[refsEntry] = t.GetComponent<Toggle>();
                    }

                    if (hasLoadingBar)
                    {
                        t = entry;
                        if (loadingBarTmplPath != EMPTYSTR) t = entry.Find(loadingBarTmplPath);
                        var bar = t.GetComponent<Slider>();
                        bar.value = 0f;
                        loadingBarRefs[refsEntry] = bar;
                    }
                }
                else
                {
                    entry = listContainer.GetChild(i);
                    refsEntry = System.Array.IndexOf(entryRefs, entry);
                }

                // should be technically impossible, but is here just in case to prevent crashing
                if (refsEntry == -1) continue;

                var owner = VRCPlayerApi.GetPlayerById(owners[i]);
                var validOwner = VRC.SDKBase.Utilities.IsValid(owner);
                var ownerIsLocal = validOwner && owners[i] == localPlayer.playerId;
                if (ownerIsLocal) personalCount++;
                var _addedBy = addedBy[i];

                // update the contents of the respective references
                if (hasUrlDisplay) urlDisplayRefs[refsEntry].text = urlStr;
                if (hasTitleDisplay) titleDisplayRefs[refsEntry].text = titleStr;
                if (hasOwnerDisplay) ownerDisplayRefs[refsEntry].text = _addedBy;
                if (hasUrlDisplayTMP) urlDisplayTMPRefs[refsEntry].text = urlStr;
                if (hasTitleDisplayTMP) titleDisplayTMPRefs[refsEntry].text = titleStr;
                if (hasOwnerDisplayTMP) ownerDisplayTMPRefs[refsEntry].text = _addedBy;
                if (hasRemoveAction) removeActionRefs[refsEntry].gameObject.SetActive(ownerIsLocal || controlBypass);
                if (hasPersistenceToggle)
                {
                    var _ref = persistenceToggleRefs[refsEntry];
                    // do not hide the object for privileged users when persist is off
                    _ref.gameObject.SetActive(persist || controlBypass);
                    // only enable interaction for privileged users
                    _ref.interactable = controlBypass;
                    var graphic = _ref.targetGraphic;
                    if (graphic != null) graphic.enabled = controlBypass;
                    // update the toggle state without notify to prevent recursive calls
                    _ref.SetIsOnWithoutNotify(persist);
                }
            }

            if (hasLoadingBar)
            {
                if (lastEntry == -1 && hasTV && !tv.IsLoadingMedia)
                {
                    if (activeLoadingBar) activeLoadingBar.value = 0f;
                    activeLoadingBar = null;
                }
                else
                {
                    if (currentEntry > -1)
                    {
                        var currentBar = loadingBarRefs[currentEntry];
                        if (activeLoadingBar && activeLoadingBar != currentBar)
                            activeLoadingBar.value = 0f;
                        activeLoadingBar = currentBar;
                    }
                    else activeLoadingBar = null;
                }
            }

            // TODO rework toaster usage
            if (hasToaster)
            {
                if (currentQueueLength >= maxQueueLength)
                    toasterMsg.text = TSTR_QUEUE_LIMIT_REACHED;
                else if (!controlBypass && personalCount >= maxEntriesPerPlayer)
                    toasterMsg.text = TSTR_PLAYER_LIMIT_REACHED;
                else toasterMsg.text = EMPTYSTR;
            }

            if (hasToasterTMP)
            {
                if (currentQueueLength >= maxQueueLength)
                    toasterMsgTMP.text = TSTR_QUEUE_LIMIT_REACHED;
                else if (!controlBypass && personalCount >= maxEntriesPerPlayer)
                    toasterMsgTMP.text = TSTR_PLAYER_LIMIT_REACHED;
                else toasterMsgTMP.text = EMPTYSTR;
            }
        }

        public void UpdateToaster()
        {
            if (OUT_TEXT == null) return;
            if (hasToaster) toasterMsg.text = OUT_TEXT;
            if (hasToasterTMP) toasterMsgTMP.text = OUT_TEXT;
            OUT_TEXT = null;
        }

        public void VideoError()
        {
            isLoading = false;
        }

        public void Loading()
        {
            // ONLY enable loading if the current url matches
            if (activeLoadingBar) activeLoadingBar.value = 0f;
            if (queue._MatchCurrentEntry(true))
            {
                Debug("Now loading");
                isLoading = true;
                SendCustomEventDelayedFrames(nameof(UpdateLoadingBar), 1);
            }
        }

        public void LoadingEnd()
        {
            isLoading = false;
            if (activeLoadingBar) activeLoadingBar.value = 1f;
        }

        public void LoadingAbort()
        {
            isLoading = false;
            if (activeLoadingBar) activeLoadingBar.value = 0f;
        }

        [PublicAPI]
        public void SetQueue(Queue plugin)
        {
            if (hasQueue) queue._UnregisterListener(this);
            queue = plugin;
            hasQueue = queue != null;
            if (hasQueue) queue._RegisterListener(this);
        }
    }
}