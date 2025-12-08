using System;
using ArchiTech.SDK;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ArchiTech.ProTV
{
    public partial class Playlist
    {
        /// <summary>
        /// Use PlaylistUI instead.
        /// </summary>
        [Obsolete("Use PlaylistUI instead")]
        public void _UpdateLoadingBar() { }

        /// <summary>
        /// Use <see cref="FillQueue()"/> instead.
        /// </summary>
        [Obsolete("Use FillQueue() instead")]
        public void _FillQueue() => FillQueue();

        /// <summary>
        /// Use <see cref="FillQueue(int)"/> instead.
        /// </summary>
        [Obsolete("Use FillQueue(int) instead")]
        public void _FillQueue(int amount) => FillQueue(amount);

        /// <summary>
        /// Use <see cref="Next()"/> instead.
        /// </summary>
        [Obsolete("Use Next() instead")]
        public void _Next() => Next();

        /// <summary>
        /// Use <see cref="Previous()"/> instead.
        /// </summary>
        [Obsolete("User Previous() instead")]
        public void _Previous() => Previous();

        /// <summary>
        /// Use PlaylistUI instead.
        /// </summary>
        [Obsolete("User PlaylistUI instead")]
        public void _UpdateView() { }

        /// <summary>
        /// Use <see cref="Shuffle()"/> instead.
        /// </summary>
        [Obsolete("Use Shuffle() instead")]
        public void _Shuffle() => Shuffle();

        /// <summary>
        /// Use <see cref="ResetSort()"/> instead.
        /// </summary>
        [Obsolete("Use ResetSort() instead")]
        public void _ResetSort() => ResetSort();

        /// <summary>
        /// Use <see cref="AutoPlay()"/> instead.
        /// </summary>
        [Obsolete("Use AutoPlay() instead")]
        public void _AutoPlay() => AutoPlay();

        /// <summary>
        /// Use <see cref="ManualPlay()"/> instead.
        /// </summary>
        [Obsolete("Use ManualPlay() instead")]
        public void _ManualPlay() => ManualPlay();

        /// <summary>
        /// Use <see cref="ToggleAutoPlay()"/> instead.
        /// </summary>
        [Obsolete("Use ToggleAutoPlay() instead")]
        public void _ToggleAutoPlay() => ToggleAutoPlay();

        /// <summary>
        /// Use <see cref="SwitchEntry()"/> instead.
        /// </summary>
        [Obsolete("Use SwitchEntry() instead")]
        public void _SwitchEntry() => SwitchEntry();

        /// <summary>
        /// Use <see cref="SwitchToRandomEntry()"/> instead.
        /// </summary>
        [Obsolete("Use SwitchToRandomEntry() instead")]
        public void _SwitchToRandomEntry() => SwitchToRandomEntry();

        /// <summary>
        /// Use <see cref="SwitchToRandomEntry()"/> instead.
        /// </summary>
        [Obsolete("Use SwitchToRandomEntry() instead.")]
        public void _SwitchToRandomFilteredEntry() => SwitchToRandomEntry();

        /// <summary>
        /// Use <see cref="SwitchToRandomUnfilteredEntry()"/> instead.
        /// </summary>
        [Obsolete("Use SwitchToRandomUnfilteredEntry() instead")]
        public void _SwitchToRandomUnfilteredEntry() => SwitchToRandomUnfilteredEntry();

        /// <summary>
        /// Use <see cref="SwitchToRandomEntry(bool)"/> instead.
        /// </summary>
        [Obsolete("Use SwitchToRandomEntry(bool) instead")]
        public void _SwitchToRandomEntry(bool filtered) => SwitchToRandomEntry(filtered);

        /// <summary>
        /// Use <see cref="SwitchEntry(int)"/> instead.
        /// </summary>
        [Obsolete("Use SwitchEntry(int) instead")]
        public void _SwitchEntry(int sortViewIndex) => SwitchEntry(sortViewIndex);

        /// <summary>
        /// Use <see cref="Prioritize()"/> instead.
        /// </summary>
        [Obsolete("Use Prioritize() instead")]
        public void _Prioritize() => Prioritize();

        /// <summary>
        /// Use PlaylistUI instead.
        /// </summary>
        [Obsolete("Use PlaylistUI instead")]
        public void _SeekView(int filteredViewIndex = -1) { }

        /// <summary>
        /// Use <see cref="UpdateFilter(bool[])"/> instead.
        /// </summary>
        [Obsolete("Use UpdateFilter(bool[]) instead")]
        public void _UpdateFilter(bool[] hide) => UpdateFilter(hide);

        /// <summary>
        /// Use <see cref="UpdateSort()"/> instead.
        /// </summary>
        [Obsolete("Use UpdateSort() instead")]
        public void _UpdateSort() => UpdateSort();

        /// <summary>
        /// Use <see cref="ChangeAutoPlay(bool)"/> instead.
        /// </summary>
        [Obsolete("Use ChangeAutoPlay(bool) instead")]
        public void _ChangeAutoPlayTo(bool active) => ChangeAutoPlay(active);

        /// <summary>
        /// Use <see cref="ResetSortView()"/> instead.
        /// </summary>
        [Obsolete("Use ResetSortView() instead")]
        public void _ResetSortView() => ResetSortView();

        // beta.24 playlistUI change

        [SerializeField, Obsolete] internal ScrollRect scrollView;

        [FormerlySerializedAs("content"), SerializeField, Obsolete]
        internal RectTransform listContainer;

        [SerializeField, Obsolete] internal GameObject template;

        [SerializeField, HideInInspector, Obsolete]
        internal Text urlDisplay;

        [SerializeField, HideInInspector, Obsolete]
        internal TextMeshProUGUI urlDisplayTMP;

        [SerializeField, HideInInspector, Obsolete]
        internal Text titleDisplay;

        [SerializeField, HideInInspector, Obsolete]
        internal TextMeshProUGUI titleDisplayTMP;

        [SerializeField, HideInInspector, Obsolete]
        internal Text descriptionDisplay;

        [SerializeField, HideInInspector, Obsolete]
        internal TextMeshProUGUI descriptionDisplayTMP;

        [SerializeField, HideInInspector, Obsolete]
        internal Button selectAction;

        [SerializeField, HideInInspector, Obsolete]
        internal Image imageDisplay;

        [SerializeField, HideInInspector, Obsolete]
        internal Slider loadingBar;

        [SerializeField, HideInInspector, Obsolete]
        internal string urlDisplayTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string urlDisplayTMPTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string titleDisplayTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string titleDisplayTMPTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string descriptionDisplayTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string descriptionDisplayTMPTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string selectActionTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string loadingBarTmplPath;

        [SerializeField, HideInInspector, Obsolete]
        internal string imageDisplayTmplPath;

        // beta.26 playlist config migration
        [SerializeField, HideInInspector, Obsolete, FormerlySerializedAs("_EDITOR_manualToImport")]
        internal bool _EDITOR_importFromFile;


        [SerializeField, HideInInspector, Obsolete]
        internal PlaylistImportMode _EDITOR_importMode;


        [SerializeField, HideInInspector, Obsolete]
        internal string _EDITOR_importUrl;

        [SerializeField, HideInInspector, Obsolete]
        internal string _EDITOR_importPath;

        [SerializeField, HideInInspector, Obsolete]
        internal int _EDITOR_entriesCount;

        [SerializeField, HideInInspector, Obsolete]
        internal int _EDITOR_imagesCount;

        [SerializeField, HideInInspector, Obsolete]
        internal bool _EDITOR_autofillAltURL;

        [SerializeField, HideInInspector, Obsolete]
        internal bool _EDITOR_autofillEscape;

        [SerializeField, HideInInspector, Obsolete]
        internal string _EDITOR_autofillFormat = "$URL";
    }
}