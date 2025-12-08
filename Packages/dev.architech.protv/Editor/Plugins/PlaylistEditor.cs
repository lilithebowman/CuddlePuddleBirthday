using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.Core;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.AssetImporters;

#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(Playlist))]
    public partial class PlaylistEditor : TVPluginEditor
    {
        private Playlist script;
        private VRCUrl[] mainUrls = new VRCUrl[0];
        private VRCUrl[] alternateUrls = new VRCUrl[0];
        private string[] titles = new string[0];
        private string[] descriptions = new string[0];
        private string[] tags = new string[0];
        private Sprite[] images = new Sprite[0];
        private string header;
        private Vector2 scrollPos;
        private ChangeAction updateMode = ChangeAction.NOOP;
        private const int perPage = 10;
        internal int currentFocus;
        private int lastFocus;
        private int targetEntry;
        private string[] detectedPlaylists = new string[0];
        private string[] detectedPlaylistNames = new string[0];

        private VRCUrl[] currentPageUrls;
        private VRCUrl[] currentPageAlts;
        private string[] currentPageTitles;
        private string[] currentPageDescriptions;
        private string[] currentPageTags;
        private Sprite[] currentPageImages;
        private int currentPageStart;
        private int currentPageEnd;
        private bool recachePage = true;

        private PlaylistUI[] detectedUIs;
        private Texture linkIcon;
        private Texture checkmarkIcon;

        private int entriesCount;
        private int imagesCount;
        private int rawEntryCount;
        private bool isPrefabMode;

        private PlaylistConfig config;
        private SerializedObject serializedConfig;

        protected override bool autoRenderVariables => false;

        internal enum ChangeAction
        {
            NOOP,
            OTHER,
            MOVEUP,
            MOVEDOWN,
            ADD,
            REMOVE,
            REMOVEALL,
            UPDATESELF,
            UPDATEALL,
            UPDATEVIEW
        }

        private void OnEnable()
        {
            script = (Playlist)target;
            if (script.storage == null) SetVariableByName(nameof(script.storage), script.GetComponentInChildren<PlaylistData>(true));
            SetupTVReferences();
            isPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;
            linkIcon = AssetDatabase.LoadAssetAtPath<Texture>(ProTVEditorUtility.linkIconPath);
            checkmarkIcon = AssetDatabase.LoadAssetAtPath<Texture>(ProTVEditorUtility.checkmarkIconPath);
            // this might cause certain behaviour confusion for autofillAltURL options in prefab editing mode?
            // fix when someone complains about it.
            if (!isPrefabMode) Importer_OnEnable();

            detectedUIs = ATEditorUtility.GetComponentsInScene<PlaylistUI>();
            if (detectedUIs.Length > 0)
                detectedUIs = detectedUIs
                    .OrderBy(plugin => plugin.playlist != script)
                    .ThenBy(plugin => plugin.playlist == null ? sbyte.MaxValue : plugin.playlist.Priority)
                    .ToArray();
        }

        protected override void InitData()
        {
            Checkpoint("Setup");
            header = script.header;

            EnforcePlaylistData(script, isPrefabMode);
            if (!isPrefabMode)
            {
                var storage = script.storage;
                mainUrls = storage.mainUrls;
                alternateUrls = storage.alternateUrls;
                titles = storage.titles;
                descriptions = storage.descriptions;
                tags = storage.tags;
                images = storage.images;
                entriesCount = storage.entriesCount > 0 ? storage.entriesCount : mainUrls.Length;
                imagesCount = storage.imagesCount > 0 ? storage.imagesCount : images.Count(i => i != null);
            }

            mainUrls = NormalizeArray(mainUrls, 0);
            rawEntryCount = mainUrls.Length;
            alternateUrls = NormalizeArray(alternateUrls, rawEntryCount);
            titles = NormalizeArray(titles, rawEntryCount);
            descriptions = NormalizeArray(descriptions, rawEntryCount);
            tags = NormalizeArray(tags, rawEntryCount);
            images = NormalizeArray(images, rawEntryCount);

            Importer_InitData();

            cacheDetectedPlaylists();
        }

        private void cacheDetectedPlaylists()
        {
            var inPackages = AssetDatabase.FindAssets("t:TextAsset", new[] { "Packages" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".playlist")).ToArray();

            var inAssets = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".playlist")).ToArray();

            IEnumerable<string> list = new string[0].AsEnumerable();
            list = list.Append("-");
            if (inAssets.Length > 0) list = list.Append(null).Union(inAssets);
            if (inPackages.Length > 0) list = list.Append("").Union(inPackages);

            detectedPlaylists = list.ToArray();
            detectedPlaylistNames = detectedPlaylists.Select(p =>
            {
                if (p == null) return null;
                var slash = p.LastIndexOf("/", StringComparison.Ordinal);
                var dot = p.LastIndexOf(".", StringComparison.Ordinal);
                return slash == -1 ? p : p.Substring(slash + 1).Substring(0, dot - slash - 1);
            }).ToArray();
        }

        protected override void SaveData()
        {
            if (updateMode == ChangeAction.NOOP) return; // don't save the data if current op was none

            script.header = header;

            Importer_SaveData();

            if (updateMode != ChangeAction.OTHER)
            {
                // data changes modify the storage data, so must occur before saving
                updateModeDataCheck();
                init = false;
                recachePage = true;
            }

            var storage = script.storage;
            if (storage != null)
            {
                using (new SaveObjectScope(storage))
                {
                    storage.mainUrls = mainUrls;
                    storage.alternateUrls = alternateUrls;
                    storage.titles = titles;
                    storage.descriptions = descriptions;
                    storage.tags = tags;
                    storage.images = images;
                    storage.entriesCount = mainUrls.Length;
                    storage.imagesCount = images.Count(i => i != null);
                }
            }

            if (updateMode != ChangeAction.OTHER)
            {
                // scene changes needs the updated storage data, so must occur after saving.
                updateModeSceneCheck();
                init = false;
                recachePage = true;
            }

            updateMode = ChangeAction.NOOP;
        }

        protected override void RenderChangeCheck()
        {
#pragma warning disable CS0612
            if (script.listContainer || script.scrollView || script.template)
            {
                Spacer(15);
                EditorGUILayout.HelpBox(I18n.Tr("Deprecated UI components detected. Click to manually migrate to the new PlaylistUI component. This will automatically happen on build."), MessageType.Info);
                if (GUILayout.Button(I18n.Tr("Migrate UI")))
                {
                    var playlistUI = PlaylistUIEditor.MigrateUI(script);
                    using (new SaveObjectScope(script.gameObject))
                    {
                        var playlistIndex = System.Array.IndexOf(script.GetComponents<Component>(), script);
                        ATEditorUtility.MoveComponentToIndex(playlistUI, playlistIndex + 1);
                    }
                }
            }
#pragma warning restore CS0612

            Checkpoint("TV Refs");
            DrawTVReferences();

            Checkpoint("General Settings");
            DrawCustomHeaderLarge("General Settings");
            using (VBox) DrawGeneralSettings();

            Checkpoint("Autoplay Options");
            DrawCustomHeaderLarge("Autoplay Options");
            using (VBox) DrawAutoplayOptions();

            BeginCheckpointGroup("Media Entries");
            DrawCustomHeaderLarge("Media Entries");
            using (VBox)
            {
                if (EditorApplication.isPlaying)
                    EditorGUILayout.HelpBox(I18n.Tr("Playlist entry editing is disabled during playmode."), MessageType.None);
                else
                {
                    Checkpoint("Header");
                    DrawListHeader();
                    Checkpoint("Entries");
                    DrawListEntries();
                }
            }
            EndCheckpointGroup();

            BeginCheckpointGroup("Detected UIs");
            DrawRelatedComponents(I18n.Tr("Detected UIs"), typeof(PlaylistUI), "playlist", script);
            EndCheckpointGroup();
        }

        private void DrawGeneralSettings()
        {
            if (DrawAndGetVariableByName(nameof(script.header), out header))
                UpdateHeader(script);

            if (DrawAndGetVariableByName(nameof(script.storage), out PlaylistData storage))
            {
                mainUrls = storage.mainUrls;
                alternateUrls = storage.alternateUrls;
                titles = storage.titles;
                descriptions = storage.descriptions;
                tags = storage.tags;
                images = storage.images;
                updateMode = ChangeAction.UPDATESELF;
            }

            DrawVariablesByName(nameof(script.placeholderImage));

            if (script.queue != null)
            {
                var amount = script.queuePreloadAmount;
                amount = Math.Min(amount, script.queue.maxQueueLength);
                amount = Math.Min(amount, mainUrls.Length);
                if (amount != script.queuePreloadAmount)
                    SetVariableByName(nameof(script.queuePreloadAmount), amount);
                DrawVariablesByName(nameof(script.queuePreloadAmount));
            }

            if (DrawVariablesByName(nameof(script.shuffleOnLoad)))
                updateMode = ChangeAction.OTHER;

            using (HArea)
            {
                if (DrawVariablesByName(nameof(script.showUrls)))
                    updateMode = ChangeAction.OTHER;
            }

            using (HArea)
            {
                if (DrawVariablesByName(nameof(autofillAltURL)))
                {
                    updateMode = ChangeAction.OTHER;
                    if (!autofillAltURL) SetVariableByName(serializedConfig, nameof(config.autofillFormat), "$URL"); // reset to default
                }
            }

            if (autofillAltURL)
            {
                EditorGUILayout.HelpBox(I18n.Tr("Put $URL (uppercase is important) wherever you want the main url to be inserted. Eg: https://mydomain.tld/?url=$URL"), MessageType.Info);
                using (HArea)
                {
                    if (DrawVariablesByName(serializedConfig, nameof(config.autofillFormat)))
                        updateMode = ChangeAction.OTHER;
                }

                if (DrawVariablesByName(serializedConfig, nameof(config.autofillEscape)))
                    updateMode = ChangeAction.OTHER;
            }
        }

        private void DrawAutoplayOptions()
        {
            using (HArea)
            {
                if (DrawVariablesByName(nameof(script.autoplayOnLoad))) updateMode = ChangeAction.OTHER;
                if (DrawVariablesByName(nameof(script.loopPlaylist))) updateMode = ChangeAction.OTHER;
            }

            using (HArea)
            {
                if (DrawVariablesByName(nameof(script.autoplayList))) updateMode = ChangeAction.OTHER;
                if (DrawVariablesByName(nameof(script.startFromRandomEntry))) updateMode = ChangeAction.OTHER;
            }

            using (HArea)
            {
                if (DrawVariablesByName(nameof(script.enableAutoplayOnInteract)))
                {
                    script.disableAutoplayOnInteract = false;
                    updateMode = ChangeAction.OTHER;
                }

                if (DrawVariablesByName(nameof(script.disableAutoplayOnInteract)))
                {
                    script.enableAutoplayOnInteract = false;
                    updateMode = ChangeAction.OTHER;
                }
            }

            using (HArea)
            {
                if (DrawVariablesByName(nameof(script.enableAutoplayOnCustomMedia)))
                {
                    script.disableAutoplayOnCustomMedia = false;
                    updateMode = ChangeAction.OTHER;
                }

                if (DrawVariablesByName(nameof(script.disableAutoplayOnCustomMedia)))
                {
                    script.enableAutoplayOnCustomMedia = false;
                    updateMode = ChangeAction.OTHER;
                }
            }

            using (HArea)
            {
                if (
                    DrawVariablesByName(nameof(script.prioritizeOnInteract))
                    || DrawVariablesByName(nameof(script.continueWhereLeftOff))
                ) updateMode = ChangeAction.OTHER;
            }
        }


        private void DrawListHeader()
        {
            Spacer();
            EditorGUILayout.BeginHorizontal(); // 1
            EditorGUILayout.BeginVertical(); // 2
            EditorGUILayout.LabelField(I18n.Tr("Video Playlist Items"), GUILayout.Width(120f), GUILayout.ExpandWidth(true));
            if (GUILayout.Button(I18n.Tr("Update Scene"), GUILayout.MaxWidth(100f)))
                updateMode = ChangeAction.UPDATEALL;

            Importer_DrawExportButtons();

            EditorGUILayout.EndVertical(); // end 2
            Spacer();
            EditorGUILayout.BeginVertical(); // 2

            Importer_DrawPlaylistAssetSelector();

            DrawPlaylistNavigation();

            EditorGUILayout.EndVertical(); // end 2
            EditorGUILayout.EndHorizontal(); // end 1
        }

        private void DrawPlaylistNavigation()
        {
            var urlCount = rawEntryCount;
            var currentPage = currentFocus / perPage;
            var maxPage = urlCount / perPage;
            var oldFocus = currentFocus;
            using (HArea)
            {
                using (DisabledScope(currentPage == 0))
                    if (GUILayout.Button("<<"))
                        currentFocus -= perPage;
                using (DisabledScope(currentFocus == 0))
                    if (GUILayout.Button("<"))
                        currentFocus -= 1;
                currentFocus = EditorGUILayout.IntSlider(currentFocus, 0, urlCount - 1, GUILayout.ExpandWidth(true));
                if (currentFocus != lastFocus)
                {
                    recachePage = true;
                    lastFocus = currentFocus;
                }

                GUILayout.Label($"/ {urlCount}");

                using (DisabledScope(currentFocus == urlCount))
                    if (GUILayout.Button(">"))
                        currentFocus += 1;
                using (DisabledScope(currentPage == maxPage))
                    if (GUILayout.Button(">>"))
                        currentFocus += perPage;
            }

            if (oldFocus != currentFocus)
            {
                updateMode = ChangeAction.UPDATEVIEW;
            }
        }

        private int recacheListPage()
        {
            var currentPage = currentFocus / perPage;
            var maxPage = rawEntryCount / perPage;
            currentPageStart = currentPage * perPage;
            currentPageEnd = Math.Min(rawEntryCount, currentPageStart + perPage);

            var pageLength = currentPageEnd - currentPageStart;
            currentPageUrls = new VRCUrl[pageLength];
            currentPageAlts = new VRCUrl[pageLength];
            currentPageTitles = new string[pageLength];
            currentPageDescriptions = new string[pageLength];
            currentPageTags = new string[pageLength];
            currentPageImages = new Sprite[pageLength];
            System.Array.Copy(mainUrls, currentPageStart, currentPageUrls, 0, pageLength);
            System.Array.Copy(alternateUrls, currentPageStart, currentPageAlts, 0, pageLength);
            System.Array.Copy(titles, currentPageStart, currentPageTitles, 0, pageLength);
            System.Array.Copy(descriptions, currentPageStart, currentPageDescriptions, 0, pageLength);
            System.Array.Copy(tags, currentPageStart, currentPageTags, 0, pageLength);
            System.Array.Copy(images, currentPageStart, currentPageImages, 0, pageLength);
            recachePage = false;
            return currentPageStart;
        }

        private static void DrawUILine(Color color, int thickness = 2, int padding = 4)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x += 16;
            r.width -= 32;
            EditorGUI.DrawRect(r, color);
        }

        private void DrawListEntries()
        {
            if (recachePage) recacheListPage();
            var height = Mathf.Min(330f, perPage * 55f) + 15f; // cap size at 330 + 15 for spacing for the horizontal scroll bar
            Spacer();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height)); // 1
            EditorGUI.BeginDisabledGroup(importMode != PlaylistImportMode.NONE); // 2
            for (var pageIndex = 0; pageIndex < currentPageUrls.Length; pageIndex++)
            {
                int rawIndex = currentPageStart + pageIndex;
                if (pageIndex > 0) DrawUILine(Color.gray);
                EditorGUILayout.BeginHorizontal(); // 3
                EditorGUILayout.BeginVertical(); // 4
                bool mainUrlUpdated = false;
                // URL field management
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{rawIndex}) PC Url", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var oldUrl = currentPageUrls[pageIndex] ?? VRCUrl.Empty;
                    var url = new VRCUrl(EditorGUILayout.TextField(oldUrl.Get(), GUILayout.ExpandWidth(true)));
                    if (url.Get() != oldUrl.Get())
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        mainUrls[rawIndex] = url;
                        mainUrlUpdated = true;
                    }
                }

                // ALT field management
                using (HArea)
                {
                    EditorGUILayout.LabelField($"     Android Url", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var oldAlt = currentPageAlts[pageIndex] ?? VRCUrl.Empty;
                    var alt = new VRCUrl(EditorGUILayout.TextField(oldAlt.Get(), GUILayout.ExpandWidth(true)));
                    if (mainUrlUpdated && autofillAltURL)
                    {
                        var targetUrl = mainUrls[rawIndex].Get();
                        if (autofillEscape) targetUrl = Uri.EscapeDataString(targetUrl);
                        alt = new VRCUrl(autofillFormat.Replace("$URL", targetUrl));
                    }

                    if (alt.Get() != oldAlt.Get())
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        alternateUrls[rawIndex] = alt;
                    }
                }

                // TITLE field management
                using (HArea)
                {
                    EditorGUILayout.LabelField("     Title", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var title = EditorGUILayout.TextArea(currentPageTitles[pageIndex], GUILayout.Width(250f), GUILayout.ExpandWidth(true));
                    if (title != currentPageTitles[pageIndex])
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        titles[rawIndex] = title.Trim();
                    }
                }

                // TODO make this a less stupid layout, make it look nicer
                // DESCRIPTION field management
                using (HArea)
                {
                    EditorGUILayout.LabelField("     Description", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var description = EditorGUILayout.TextArea(currentPageDescriptions[pageIndex], GUILayout.Width(250f), GUILayout.ExpandWidth(true));
                    if (description != currentPageDescriptions[pageIndex])
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        descriptions[rawIndex] = description.Trim();
                    }
                }

                // TAGS field management
                using (HArea)
                {
                    EditorGUILayout.LabelField("     Tags", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var tagString = EditorGUILayout.TextArea(currentPageTags[pageIndex], GUILayout.Width(250f), GUILayout.ExpandWidth(true));
                    if (tagString != currentPageTags[pageIndex])
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        tags[rawIndex] = SanitizeTagString(tagString);
                    }
                }

                EditorGUILayout.EndVertical(); // end 4
                var image = (Sprite)EditorGUILayout.ObjectField(currentPageImages[pageIndex], typeof(Sprite), false, GUILayout.Height(75), GUILayout.Width(60));
                if (image != currentPageImages[pageIndex])
                {
                    updateMode = ChangeAction.UPDATESELF;
                    images[rawIndex] = image;
                }

                if (importMode == PlaylistImportMode.NONE)
                {
                    // Playlist entry actions
                    EditorGUILayout.BeginVertical(); // 4
                    if (GUILayout.Button(I18n.Tr("Remove")))
                    {
                        // Cannot modify urls list within loop else index error occurs
                        targetEntry = rawIndex;
                        updateMode = ChangeAction.REMOVE;
                    }

                    // Playlist entry ordering
                    using (HArea)
                    {
                        EditorGUI.BeginDisabledGroup(rawIndex == 0);
                        if (GUILayout.Button(I18n.Tr("Up")))
                        {
                            targetEntry = rawIndex;
                            updateMode = ChangeAction.MOVEUP;
                        }

                        EditorGUI.EndDisabledGroup();
                        EditorGUI.BeginDisabledGroup(rawIndex + 1 == rawEntryCount);
                        if (GUILayout.Button(I18n.Tr("Down")))
                        {
                            targetEntry = rawIndex;
                            updateMode = ChangeAction.MOVEDOWN;
                        }

                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUILayout.EndVertical(); // end 4
                }

                EditorGUILayout.EndHorizontal(); // end 3
                GUILayout.Space(3f);
            }

            EditorGUI.EndDisabledGroup(); // end 2
            EditorGUILayout.EndScrollView(); // end 1
        }

        internal static void EnforcePlaylistData(Playlist playlist, bool isPrefab)
        {
            if (playlist == null) return;
            PlaylistData storage = playlist.storage;
            if (storage == null) storage = playlist.GetComponentInChildren<PlaylistData>(true);

            if (isPrefab)
            {
                // Due to unity serialization BULLSHIT with prefabs, DO NOT ALLOW PLAYLIST DATA WHEN VIEWING PLAYLIST AS A PREFAB
                if (storage != null)
                {
                    Undo.DestroyObjectImmediate(storage.gameObject);
                    using (new SaveObjectScope(playlist))
                        playlist.storage = null;
                }

                return;
            }

            if (storage == null)
            {
                Debug.Log($"Adding playlist data for {playlist.gameObject.name}");
                var go = new GameObject("Playlist Data");
                Undo.RegisterCreatedObjectUndo(go, "Undo Playlist Data add");
                go.transform.SetParent(playlist.transform);
                storage = UdonSharpUndo.AddComponent<PlaylistData>(go);
            }

            if (storage != playlist.storage)
            {
                using (new SaveObjectScope(playlist))
                    playlist.storage = storage;
            }

            if (storage.mainUrls == null && playlist.mainUrls != null)
            {
                using (new SaveObjectScope(storage))
                {
                    storage.mainUrls = playlist.mainUrls;
                    storage.alternateUrls = playlist.alternateUrls;
                    storage.titles = playlist.titles;
                    storage.descriptions = playlist.descriptions;
                    storage.tags = playlist.tags;
                    storage.images = playlist.images;
                }
            }

            if (playlist.mainUrls != null)
            {
                using (new SaveObjectScope(playlist))
                {
                    playlist.mainUrls = null;
                    playlist.alternateUrls = null;
                    playlist.titles = null;
                    playlist.descriptions = null;
                    playlist.tags = null;
                    playlist.images = null;
                }
            }
        }

        #region Update Mode Handling

        private void updateModeDataCheck()
        {
            // Actions involving modification of the internal data
            switch (updateMode)
            {
                case ChangeAction.ADD:
                    addItems();
                    break;
                case ChangeAction.MOVEUP:
                    moveItems(targetEntry, targetEntry - 1);
                    break;
                case ChangeAction.MOVEDOWN:
                    moveItems(targetEntry, targetEntry + 1);
                    break;
                case ChangeAction.REMOVE:
                    removeItems(targetEntry);
                    break;
                case ChangeAction.REMOVEALL:
                    removeAll();
                    break;
            }

            targetEntry = -1;
        }

        private void updateModeSceneCheck()
        {
            // Actions involving refreshing of the UIs displaying the data
            switch (updateMode)
            {
                case ChangeAction.UPDATEVIEW:
                case ChangeAction.UPDATESELF:
                    UpdateContents(script, currentFocus);
                    break;
                case ChangeAction.UPDATEALL:
                    RebuildScene(script, currentFocus);
                    break;
                default:
                    RebuildScene(script, currentFocus);
                    break;
            }
        }

        private void addItems()
        {
            var newIndex = mainUrls.Length;
            Debug.Log($"Adding playlist item. New size {newIndex + 1}");
            mainUrls = AddArrayItem(mainUrls);
            alternateUrls = AddArrayItem(alternateUrls);
            tags = AddArrayItem(tags);
            titles = AddArrayItem(titles);
            descriptions = AddArrayItem(descriptions);
            images = AddArrayItem(images);
            // Make sure the urls default to an empty instead of null
            mainUrls[newIndex] = VRCUrl.Empty;
            alternateUrls[newIndex] = VRCUrl.Empty;
        }

        private void removeItems(int index)
        {
            Debug.Log($"Removing playlist item {index}: {titles[index]}");
            mainUrls = RemoveArrayItem(mainUrls, index);
            alternateUrls = RemoveArrayItem(alternateUrls, index);
            tags = RemoveArrayItem(tags, index);
            titles = RemoveArrayItem(titles, index);
            descriptions = RemoveArrayItem(descriptions, index);
            images = RemoveArrayItem(images, index);
        }

        private void moveItems(int from, int to)
        {
            // no change needed
            if (from == to) return;
            Debug.Log($"Moving playlist item {from} -> {to}");

            mainUrls = MoveArrayItem(mainUrls, from, to);
            alternateUrls = MoveArrayItem(alternateUrls, from, to);
            tags = MoveArrayItem(tags, from, to);
            titles = MoveArrayItem(titles, from, to);
            descriptions = MoveArrayItem(descriptions, from, to);
            images = MoveArrayItem(images, from, to);
        }

        private void removeAll()
        {
            Debug.Log($"Removing all {mainUrls.Length} playlist items");
            mainUrls = new VRCUrl[0];
            alternateUrls = new VRCUrl[0];
            tags = new string[0];
            titles = new string[0];
            descriptions = new string[0];
            images = new Sprite[0];
        }

        public static void RebuildScene(Playlist playlist, int offset = 0)
        {
            if (playlist == null) return;
            var playlistUIs = ATEditorUtility.GetComponentsInScene<PlaylistUI>();
            // Find all playlistUIs that are targeting this playlist and rebuild the hierarchy for them.
            foreach (PlaylistUI playlistUI in playlistUIs)
                if (playlistUI.playlist == playlist)
                    PlaylistUIEditor.RebuildScene(playlistUI, offset);
        }

        public static void UpdateHeader(Playlist script)
        {
            if (script == null) return;
            // Find all playlistUIs that are targeting this playlist and update the header for them.
            var uis = ATEditorUtility.GetComponentsInScene<PlaylistUI>();
            Undo.RecordObjects(uis, "Updating header text");
            foreach (var ui in uis)
                if (ui.playlist == script)
                    PlaylistUIEditor.UpdateHeader(ui);
        }

        public static void UpdateContents(Playlist playlist, int focus)
        {
            if (playlist == null) return;
            var playlistUIs = ATEditorUtility.GetComponentsInScene<PlaylistUI>();
            // Find all playlistUIs that are targeting this playlist and update the hierarchy for them.
            foreach (PlaylistUI playlistUI in playlistUIs)
                if (playlistUI.playlist == playlist)
                    PlaylistUIEditor.UpdateContents(playlistUI, focus);
        }

        #endregion
    }
}