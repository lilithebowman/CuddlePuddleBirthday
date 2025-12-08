using ArchiTech.SDK;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Toggle = UnityEngine.UI.Toggle;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    public class TVManagedWhitelistUI : TVPluginUI
    {
        [SerializeField,
         I18nTooltip("Reference to the whitelist component the UI should represent.")
        ]
        internal TVManagedWhitelist whitelist;

        [SerializeField,
         I18nTooltip("Container reference which the list will be added to/removed from. It is recommended to have either a VerticalLayoutGroup, HorizontalLayoutGroup or GridLayoutGroup component on this element for easy layout controls.")
        ]
        internal RectTransform listContainer;

        [SerializeField,
         I18nTooltip("Object that will be instantiated for each player in the queue. Each item will be parented to the List Container transform upon instantiation.")
        ]
        internal GameObject template;

        [FormerlySerializedAs("selectAction")]
        [SerializeField,
         I18nTooltip("Interaction component for authorizing the respective player. MUST be a child of the Template object.")
        ]
        internal Toggle authAction;

        [SerializeField,
         I18nTooltip("Text display component for the relevant player name. MUST be a child of the Template object. Supports both UI and TMP.")
        ]
        internal Text nameDisplay;

        [SerializeField] internal TextMeshProUGUI nameDisplayTMP;

        [SerializeField,
         I18nTooltip("Visual component for the relevant player for if they are in the world currently. MUST be a child of the Template object.")
        ]
        internal Toggle hereIndicator;


        [SerializeField, HideInInspector] internal string authActionTmplPath = null;
        [SerializeField, HideInInspector] internal string hereIndicatorTmplPath = null;
        [SerializeField, HideInInspector] internal string nameDisplayTmplPath = null;
        [SerializeField, HideInInspector] internal string nameDisplayTMPTmplPath = null;

        private Transform[] entryRefs = new Transform[0];
        private Toggle[] authActionRefs = new Toggle[0];
        private Toggle[] hereIndicatorRefs = new Toggle[0];
        private Text[] nameDisplayRefs = new Text[0];
        private TextMeshProUGUI[] nameDisplayTMPRefs = new TextMeshProUGUI[0];


        private bool hasAuthAction = false;
        private bool hasHereIndicator = false;
        private bool hasNameDisplay = false;
        private bool hasNameDisplayTMP = false;

        [SerializeField, HideInInspector] internal int _EDITOR_templateUpgrade;

        public override void Start()
        {
            if (init) return;
            base.Start();

#if UNITY_2022_3_OR_NEWER
            if (whitelist == null) whitelist = GetComponentInParent<TVManagedWhitelist>(true);
#else
            if (whitelist == null) whitelist = GetComponentInParent<TVManagedWhitelist>();
#endif
            if (whitelist != null) whitelist._RegisterListener(this);
            hasAuthAction = authAction != null;
            hasHereIndicator = hereIndicator != null;
            hasNameDisplay = nameDisplay != null;
            hasNameDisplayTMP = nameDisplayTMP != null;

            const int commonPlayerMax = 82;
            entryRefs = new Transform[commonPlayerMax];
            if (hasAuthAction) authActionRefs = new Toggle[commonPlayerMax];
            if (hasHereIndicator) hereIndicatorRefs = new Toggle[commonPlayerMax];
            if (hasNameDisplay) nameDisplayRefs = new Text[commonPlayerMax];
            if (hasNameDisplayTMP) nameDisplayTMPRefs = new TextMeshProUGUI[commonPlayerMax];


            if (template != null) template.SetActive(false);
            var count = listContainer.childCount;
            while (count-- > 0) DestroyImmediate(listContainer.GetChild(0).gameObject);
        }

        public override void _ManagerReady() => UpdateUI();

        public void AuthorizeEntry()
        {
            bool detected = false;
            int index = getDetectedEntry(authActionRefs);
            bool state = false;
            var playerNames = whitelist.playerNames;
            if (index > -1)
            {
                detected = true;
                state = authActionRefs[index].isOn;
                if (IsDebugEnabled) Debug($"Detected Entry -1 < {index} < {playerNames.Length}");
            }

            if (-1 < index && index < playerNames.Length)
            {
                var playerName = playerNames[index];
                if (whitelist.tv._IsSuperAuthorized()) whitelist._Authorize(playerName, state);
                else if (detected)
                    authActionRefs[index].SetIsOnWithoutNotify(System.Array.IndexOf(whitelist.authorizedList, playerName) > -1);
            }

        }

        public override void UpdateUI()
        {
            if (IsDebugEnabled) Debug("Updating UI");
            var tv = whitelist.tv;
            var isLocalSuper = tv._IsSuperAuthorized();
            var children = listContainer.childCount;
            var playerNames = whitelist.playerNames;
            var count = playerNames.Length;
            resizeEntryRefs(count);

            if (IsTraceEnabled) Trace($"Player names: {string.Join(", ", playerNames)}");
            var displayed = 0;
            for (int i = 0; i < count; i++)
            {
                string playerName = playerNames[i];
                if (string.IsNullOrWhiteSpace(playerName)) continue;
                if (i >= children)
                {
                    var go = Instantiate(template);
                    go.name = $"Player ({i})";
                    if (IsTraceEnabled) Trace($"Creating new entry child {go.name}");
                    go.SetActive(true);
                    Transform entry = go.transform;
                    entry.SetParent(listContainer, false);

                    entryRefs[i] = entry;
                    Transform t;
                    if (hasAuthAction)
                    {
                        t = entry;
                        if (authActionTmplPath != EMPTYSTR) t = entry.Find(authActionTmplPath);
                        var authAct = t.GetComponent<Toggle>();
                        authAct.interactable = false;
                        authAct.SetIsOnWithoutNotify(false);
                        authActionRefs[i] = authAct;
                    }

                    if (hasAuthAction)
                    {
                        t = entry;
                        if (hereIndicatorTmplPath != EMPTYSTR) t = entry.Find(hereIndicatorTmplPath);
                        var hereInd = t.GetComponent<Toggle>();
                        hereInd.interactable = false;
                        hereInd.SetIsOnWithoutNotify(false);
                        hereIndicatorRefs[i] = hereInd;
                    }

                    if (hasNameDisplay)
                    {
                        t = entry;
                        if (nameDisplayTmplPath != EMPTYSTR) t = entry.Find(nameDisplayTmplPath);
                        nameDisplayRefs[i] = t.GetComponent<Text>();
                    }

                    if (hasNameDisplayTMP)
                    {
                        t = entry;
                        if (nameDisplayTMPTmplPath != EMPTYSTR) t = entry.Find(nameDisplayTMPTmplPath);
                        nameDisplayTMPRefs[i] = t.GetComponent<TextMeshProUGUI>();
                    }

                    children++;
                }

                VRCPlayerApi playerApi = whitelist.playerApis[i];
                bool isPlayerHere = VRC.SDKBase.Utilities.IsValid(playerApi);
                bool isPlayerInternalAuthorized = System.Array.IndexOf(whitelist.authorizedList, playerName) > -1;
                bool isPlayerTvAuthorized = tv._IsAuthorized(playerApi, true);
                bool isPlayerExternalAuthorized = !isPlayerInternalAuthorized && isPlayerTvAuthorized;
                bool isPlayerSuper = isPlayerHere && tv._IsSuperAuthorized(playerApi, true);

                if (IsTraceEnabled) Trace($"Player state {playerName}: \nindex {i} here {isPlayerHere} auth {isPlayerInternalAuthorized} || {isPlayerTvAuthorized} super {isPlayerSuper}");

                // update the contents of the respective references
                if (hasNameDisplay) nameDisplayRefs[i].text = playerName;
                if (hasNameDisplayTMP) nameDisplayTMPRefs[i].text = playerName;
                if (hasHereIndicator) hereIndicatorRefs[i].SetIsOnWithoutNotify(isPlayerHere);
                if (hasAuthAction)
                {
                    var authRef = authActionRefs[i];
                    var interactable = isLocalSuper && !isPlayerExternalAuthorized && !isPlayerSuper;
                    var btnGraphic = authRef.targetGraphic;
                    if (btnGraphic != null) btnGraphic.enabled = interactable;
                    authRef.interactable = interactable;
                    authRef.SetIsOnWithoutNotify(isPlayerSuper || isPlayerInternalAuthorized || isPlayerTvAuthorized);
                }

                displayed++;
            }

            removeExtraEntries(displayed);
        }


        private void resizeEntryRefs(int newSize)
        {
            var oldSize = entryRefs.Length;
            if (oldSize == newSize) return;
            var copySize = System.Math.Min(oldSize, newSize);
            var _entryRefs = entryRefs;
            var _authActionRefs = authActionRefs;
            var _hereIndicatorRefs = hereIndicatorRefs;
            var _nameDisplayRefs = nameDisplayRefs;
            var _nameDisplayTMPRefs = nameDisplayTMPRefs;
            if (IsDebugEnabled) Debug($"Resize reference entries {oldSize} -> {newSize}");
            entryRefs = new Transform[newSize];
            if (hasAuthAction) authActionRefs = new Toggle[newSize];
            if (hasHereIndicator) hereIndicatorRefs = new Toggle[newSize];
            if (hasNameDisplay) nameDisplayRefs = new Text[newSize];
            if (hasNameDisplayTMP) nameDisplayTMPRefs = new TextMeshProUGUI[newSize];
            if (copySize > 0)
            {
                System.Array.Copy(_entryRefs, 0, entryRefs, 0, copySize);
                if (hasAuthAction) System.Array.Copy(_authActionRefs, 0, authActionRefs, 0, copySize);
                if (hasHereIndicator) System.Array.Copy(_hereIndicatorRefs, 0, hereIndicatorRefs, 0, copySize);
                if (hasNameDisplay) System.Array.Copy(_nameDisplayRefs, 0, nameDisplayRefs, 0, copySize);
                if (hasNameDisplayTMP) System.Array.Copy(_nameDisplayTMPRefs, 0, nameDisplayTMPRefs, 0, copySize);
            }
        }

        private void removeExtraEntries(int size)
        {
            var children = listContainer.childCount;
            if (children > size && IsDebugEnabled) Debug($"Removing {children - size} extra child entries");
            while (children > size)
            {
                children--;
                DestroyImmediate(listContainer.GetChild(children).gameObject);
                entryRefs[children] = null;
                if (hasAuthAction) authActionRefs[children] = null;
                if (hasHereIndicator) hereIndicatorRefs[children] = null;
                if (hasNameDisplay) nameDisplayRefs[children] = null;
                if (hasNameDisplayTMP) nameDisplayTMPRefs[children] = null;
            }
        }

        private int entryIndexToRefIndex(int entryIndex)
        {
            if (entryIndex == -1) return -1;
            if (entryIndex >= listContainer.childCount) return -1;
            return System.Array.IndexOf(entryRefs, listContainer.GetChild(entryIndex));
        }

        private int getDetectedEntry(Selectable[] referencesArray)
        {
            if (IsDebugEnabled) Debug("Auto-detecting selected index via interaction");
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

            if (IsDebugEnabled) Debug("Index not able to be auto-detected");
            return -1;
        }
    }
}