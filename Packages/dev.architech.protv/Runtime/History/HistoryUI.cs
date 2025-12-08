using ArchiTech.SDK;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;

namespace ArchiTech.ProTV
{
    public class HistoryUI : TVPluginUI
    {
        [SerializeField] internal History history;

        [SerializeField,
         I18nTooltip("Container reference which the history entries will be added to/removed from. It is recommended to have either a VerticalLayoutGroup, HorizontalLayoutGroup or GridLayoutGroup component on this element for easy layout controls.")
        ]
        internal RectTransform listContainer;

        [SerializeField,
         I18nTooltip("Object that will be instantiated for each entry in the history list. Each entry will be parented to the List Container transform upon instantiation.")
        ]
        internal GameObject template;

        [SerializeField,
         I18nTooltip("Text display component for the relevant entry main URL. MUST be a child of the Template object if provided. Supports both UI and TMP.")
        ]
        internal Text urlDisplay;

        [SerializeField,
         I18nTooltip("Text display component for the relevant entry Title. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text titleDisplay;

        [SerializeField,
         I18nTooltip("Text display component for the name of the user who added the entry. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text addedByDisplay;

        [SerializeField] internal TextMeshProUGUI urlDisplayTMP;
        [SerializeField] internal TextMeshProUGUI titleDisplayTMP;
        [SerializeField] internal TextMeshProUGUI addedByDisplayTMP;

        [SerializeField,
         I18nTooltip("Interaction component for triggering the respective entry. MUST be a child of the Template object.")
        ]
        internal Button restoreAction;

        [SerializeField,
         I18nTooltip("Interaction component for triggering an input field copy. MUST be a child of the Template object.")
        ]
        internal InputField copyAction;

        [SerializeField,
         I18nInspectorName("Header Display")
        ]
        internal Text headerDisplay;

        [SerializeField] internal TextMeshProUGUI headerDisplayTMP;

        [SerializeField, HideInInspector] internal string urlDisplayTmplPath;
        [SerializeField, HideInInspector] internal string titleDisplayTmplPath;
        [SerializeField, HideInInspector] internal string addedByDisplayTmplPath;
        [SerializeField, HideInInspector] internal string urlDisplayTMPTmplPath;
        [SerializeField, HideInInspector] internal string titleDisplayTMPTmplPath;
        [SerializeField, HideInInspector] internal string addedByDisplayTMPTmplPath;
        [SerializeField, HideInInspector] internal string restoreActionTmplPath;
        [SerializeField, HideInInspector] internal string copyActionTmplPath;

        private Transform[] entryRefs = new Transform[0];
        private Text[] urlDisplayRefs = new Text[0];
        private Text[] titleDisplayRefs = new Text[0];
        private Text[] addedByDisplayRefs = new Text[0];
        private TextMeshProUGUI[] urlDisplayTMPRefs = new TextMeshProUGUI[0];
        private TextMeshProUGUI[] titleDisplayTMPRefs = new TextMeshProUGUI[0];
        private TextMeshProUGUI[] addedByDisplayTMPRefs = new TextMeshProUGUI[0];
        private Button[] restoreActionRefs = new Button[0];
        private InputField[] copyActionRefs = new InputField[0];

        private bool hasHistory;
        private bool hasUrlDisplay;
        private bool hasUrlDisplayTMP;
        private bool hasTitleDisplay;
        private bool hasTitleDisplayTMP;
        private bool hasAddedByDisplay;
        private bool hasAddedByDisplayTMP;
        private bool hasRestoreAction;
        private bool hasCopyAction;
        private bool hasHeaderDisplay;
        private bool hasHeaderDisplayTMP;

        private int numberOfEntries;

        [SerializeField, HideInInspector] internal int _EDITOR_templateUpgrade;


        public override void Start()
        {
            if (init) return;
            base.Start();
            if (history == null) history = GetComponentInParent<History>(true);
            hasHistory = history != null;
            hasRestoreAction = restoreAction != null;
            hasCopyAction = copyAction != null;
            hasUrlDisplay = urlDisplay != null;
            hasTitleDisplay = titleDisplay != null;
            hasAddedByDisplay = addedByDisplay != null;
            hasUrlDisplayTMP = urlDisplayTMP != null;
            hasTitleDisplayTMP = titleDisplayTMP != null;
            hasAddedByDisplayTMP = addedByDisplayTMP != null;
            hasHeaderDisplay = headerDisplay != null;
            hasHeaderDisplayTMP = headerDisplayTMP != null;

            if (IsDebugEnabled && listContainer == null) Error($"Missing the list container object. This is required for the component to work.");
            if (IsDebugEnabled && listContainer.childCount > 0) Error($"Unexpected child objects within the list container object '{listContainer.name}'. Make sure you have provided a GameObject reference that does not have any children.");

            numberOfEntries = hasHistory ? history.numberOfEntries : 0;
            entryRefs = new Transform[numberOfEntries];
            if (hasRestoreAction) restoreActionRefs = new Button[numberOfEntries];
            if (hasCopyAction) copyActionRefs = new InputField[numberOfEntries];
            if (hasUrlDisplay) urlDisplayRefs = new Text[numberOfEntries];
            if (hasTitleDisplay) titleDisplayRefs = new Text[numberOfEntries];
            if (hasAddedByDisplay) addedByDisplayRefs = new Text[numberOfEntries];
            if (hasUrlDisplayTMP) urlDisplayTMPRefs = new TextMeshProUGUI[numberOfEntries];
            if (hasTitleDisplayTMP) titleDisplayTMPRefs = new TextMeshProUGUI[numberOfEntries];
            if (hasAddedByDisplayTMP) addedByDisplayTMPRefs = new TextMeshProUGUI[numberOfEntries];

            if (template != null) template.SetActive(false);

            if (hasHistory) history._RegisterListener(this);
        }

        public override void _ManagerReady()
        {
            UpdateUI();
        }

        public void SetHistory(History plugin)
        {
            if (hasHistory) history._UnregisterListener(this);
            history = plugin;
            hasHistory = history != null;
            if (hasHistory) history._RegisterListener(this);
        }

        public override void UpdateUI()
        {
            if (!hasHistory) return;
            int index = wrap(history.nextIndex - 1);
            var copyAllowed = history.enableUrlCopy && (!history.protectUrlCopy || history.tv.CanPlayMedia);
            var tv = history.tv;
            var mainUrls = history.mainUrls;
            var alternateUrls = history.alternateUrls;
            var titles = history.titles;
            var addedBy = history.addedBy;
            if (hasHeaderDisplay) headerDisplay.text = history.header;
            if (hasHeaderDisplayTMP) headerDisplayTMP.text = history.header;
            for (int i = 0; i < numberOfEntries; i++)
            {
                var mainUrl = mainUrls[index];
                if (mainUrl == null) continue;
                var mainUrlStr = mainUrl.Get();
                var altUrlStr = alternateUrls[index].Get();
                if (i >= listContainer.childCount) addEntry();
                if (hasUrlDisplay) urlDisplayRefs[i].text = mainUrls[index].Get();
                if (hasTitleDisplay)
                {
                    var title = titles[index];
                    titleDisplayRefs[i].text = string.IsNullOrEmpty(title) ? history.emptyTitlePlaceholder : title;
                }

                if (hasAddedByDisplay) addedByDisplayRefs[i].text = addedBy[index];
                if (hasUrlDisplayTMP) urlDisplayTMPRefs[i].text = mainUrls[index].Get();
                if (hasTitleDisplayTMP)
                {
                    var title = titles[index];
                    titleDisplayTMPRefs[i].text = string.IsNullOrEmpty(title) ? history.emptyTitlePlaceholder : title;
                }

                if (hasAddedByDisplayTMP) addedByDisplayTMPRefs[i].text = addedBy[index];
                // if both URLs match the TV, disable the restore action since that would be ignored internally to the TV
                // doubles as a signal to the user that the entry is currently what is playing on the TV
                if (hasRestoreAction) restoreActionRefs[i].gameObject.SetActive(tv.CanPlayMedia && (mainUrlStr != tv.urlMain.Get() || altUrlStr != tv.urlAlt.Get()));
                if (hasCopyAction)
                {
                    var copyRef = copyActionRefs[i];
                    copyRef.gameObject.SetActive(copyAllowed);
                    copyRef.enabled = copyAllowed;
                    copyRef.text = copyAllowed ? mainUrlStr : EMPTYSTR;
                }

                index = wrap(index - 1);
            }
        }

        private int wrap(int value)
        {
            if (numberOfEntries == 0) return 0;
            value %= numberOfEntries;
            if (value < 0) value += numberOfEntries;
            return value;
        }

        private void addEntry()
        {
            var index = listContainer.childCount;
            var go = Instantiate(template);
            go.name = $"Entry ({index})";
            go.SetActive(true);
            var entry = go.transform;
            entry.SetParent(listContainer, false);
            Debug($"Adding template instance {go.name}");

            entryRefs[index] = entry;
            Transform t;
            if (hasRestoreAction)
            {
                t = entry;
                if (restoreActionTmplPath != EMPTYSTR) t = entry.Find(restoreActionTmplPath);
                restoreActionRefs[index] = t.GetComponent<Button>();
            }

            if (hasCopyAction)
            {
                t = entry;
                if (copyActionTmplPath != EMPTYSTR) t = entry.Find(copyActionTmplPath);
                copyActionRefs[index] = t.GetComponent<InputField>();
            }

            if (hasUrlDisplay)
            {
                t = entry;
                if (urlDisplayTmplPath != EMPTYSTR) t = entry.Find(urlDisplayTmplPath);
                urlDisplayRefs[index] = t.GetComponent<Text>();
            }

            if (hasTitleDisplay)
            {
                t = entry;
                if (titleDisplayTmplPath != EMPTYSTR) t = entry.Find(titleDisplayTmplPath);
                titleDisplayRefs[index] = t.GetComponent<Text>();
            }

            if (hasAddedByDisplay)
            {
                t = entry;
                if (addedByDisplayTmplPath != EMPTYSTR) t = entry.Find(addedByDisplayTmplPath);
                addedByDisplayRefs[index] = t.GetComponent<Text>();
            }

            if (hasUrlDisplayTMP)
            {
                t = entry;
                if (urlDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(urlDisplayTMPTmplPath);
                urlDisplayTMPRefs[index] = t.GetComponent<TextMeshProUGUI>();
            }

            if (hasTitleDisplayTMP)
            {
                t = entry;
                if (titleDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(titleDisplayTMPTmplPath);
                titleDisplayTMPRefs[index] = t.GetComponent<TextMeshProUGUI>();
            }

            if (hasAddedByDisplayTMP)
            {
                t = entry;
                if (addedByDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(addedByDisplayTMPTmplPath);
                addedByDisplayTMPRefs[index] = t.GetComponent<TextMeshProUGUI>();
            }

            if (hasRestoreAction)
            {
                t = entry;
                if (restoreActionTmplPath != EMPTYSTR) t = entry.Find(restoreActionTmplPath);
                restoreActionRefs[index] = t.GetComponent<Button>();
            }
        }

        public void Clear()
        {
            if (!hasHistory) return;
            int count = listContainer.childCount;
            while (count > 1) Destroy(listContainer.GetChild(--count).gameObject);
            UpdateUI();
        }

        public void SelectEntry()
        {
            if (!hasHistory) return;
            history.SelectEntry(getDetectedEntry(restoreActionRefs));
        }

        private int getDetectedEntry(Selectable[] referencesArray)
        {
            if (IsTraceEnabled) Trace("Auto-detecting selected index via interaction");
            for (int i = 0; i < referencesArray.Length; i++)
            {
                var @ref = referencesArray[i];
                if (@ref == null) continue;
                if (!@ref.enabled)
                {
                    var index = entryRefs[i].GetSiblingIndex();
                    if (IsTraceEnabled) Trace($"Detected index {index}");
                    return index;
                }
            }

            if (IsTraceEnabled) Trace("Index not able to be auto-detected");
            return -1;
        }
    }
}