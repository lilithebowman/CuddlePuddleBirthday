using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using Newtonsoft.Json;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;

#pragma warning disable CS0612

namespace ArchiTech.ProTV.Editor
{
    public partial class PlaylistEditor
    {
        public const string EntryIndicatorsPrefix = "?";
        public const string EntryStartIndicator = "@";
        public const string EntryAltIndicator = "^";
        public const string EntryImageIndicator = "/";
        public const string EntryTagIndicator = "#";
        public const string EntryTitleIndicator = "~";

        private const string playlistFolder = "Assets/ProTV/RemotePlaylists";

        private PlaylistImportMode importMode;
        private string importPath = "";
        private TextAsset importSrc;
        private string importUrl;
        private bool autofillAltURL;
        private string autofillFormat;
        private bool autofillEscape;
        private bool autoUpdateRemote;

        private ImportState importState = ImportState.READY;
        private WebClient importClient;
        private double importProgress = 0f;

        private enum ImportState
        {
            READY,
            PENDING,
            COMPLETE,
            ERROR
        }


        private void Importer_OnEnable()
        {
            config = ATEditorUtility.GetOrAddComponent<PlaylistConfig>(script.gameObject, out bool componentAdded);
            serializedConfig = new SerializedObject(config);
            if (componentAdded)
            {
                UnityEngine.Debug.Log("PlaylistConfig added. Migrating settings.");
                serializedConfig.SetValue(nameof(config.importMode), script._EDITOR_importMode);
                serializedConfig.SetValue(nameof(config.importUrl), script._EDITOR_importUrl);
                serializedConfig.SetValue(nameof(config.importPath), script._EDITOR_importPath);
                serializedConfig.SetValue(nameof(config.autofillAltURL), script._EDITOR_autofillAltURL);
                serializedConfig.SetValue(nameof(config.autofillEscape), script._EDITOR_autofillEscape);
                serializedConfig.SetValue(nameof(config.autofillFormat), script._EDITOR_autofillFormat);
            }

            importMode = config.importMode;
            importUrl = config.importUrl;
            importPath = config.importPath;
            importSrc = config.importSrc;
            autofillAltURL = config.autofillAltURL;
            autofillFormat = config.autofillFormat;
            autofillEscape = config.autofillEscape;
            autoUpdateRemote = config.autoUpdateRemote;

            // when the asset is not present, but the path is, try getting the asset.
            var src = importSrc;
            if (importSrc == null && !string.IsNullOrWhiteSpace(importPath))
                src = AssetDatabase.LoadAssetAtPath<TextAsset>(importPath);
            if (importSrc != src) serializedConfig.SetValue(nameof(config.importSrc), src);
            importSrc = src;

            // check that the cached path matches the source path, update if it doesn't.
            var path = AssetDatabase.GetAssetPath(importSrc);
            if (importPath != path) serializedConfig.SetValue(nameof(config.importPath), path);
            importPath = path;

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Importer_InitData()
        {
            if (script._EDITOR_importFromFile) ForceSave();
        }

        private void Importer_SaveData()
        {
            if (script._EDITOR_importFromFile)
            {
                script._EDITOR_importFromFile = false;
                importMode = PlaylistImportMode.LOCAL;
            }

            if (!isPrefabMode)
            {
                config = config ?? ATEditorUtility.GetOrAddComponent<PlaylistConfig>(script.gameObject);
                using (new SaveObjectScope(config))
                {
                    config.importMode = importMode;
                    config.importUrl = importUrl;
                    config.importPath = importPath;
                    config.importSrc = importSrc;
                    config.autofillAltURL = autofillAltURL;
                    config.autofillFormat = autofillFormat;
                    config.autofillEscape = autofillEscape;
                }
            }

            if (updateMode == ChangeAction.UPDATEALL)
            {
                var playlists = ATEditorUtility.GetComponentsInScene<Playlist>();
                foreach (var playlist in playlists)
                {
                    if (playlist == script) continue;
                    // other playlists are connected to this data store, update them in the scene as well.
                    if (playlist.storage == script.storage)
                    {
                        RebuildScene(playlist);
                        if (importMode != PlaylistImportMode.NONE)
                        {
                            using (new SaveObjectScope(playlist.storage))
                            {
                                playlist.storage.entriesCount = entriesCount;
                                playlist.storage.imagesCount = imagesCount;
                            }
                        }
                    }
                }
            }
        }


        private void Importer_DrawExportButtons()
        {
            using (HArea)
            {
                EditorGUI.BeginDisabledGroup(importMode != PlaylistImportMode.NONE);
                if (GUILayout.Button(I18n.Tr("Save"), GUILayout.ExpandWidth(false)))
                {
                    // get where to save the file
                    string defaultName = "CustomPlaylist - " + SceneManager.GetActiveScene().name;
                    string directory = "Assets";
                    if (importSrc != null)
                    {
                        defaultName = importSrc.name;
                        directory = AssetDatabase.GetAssetPath(importSrc);
                        directory = Path.GetDirectoryName(Path.GetFullPath(directory));
                    }

                    string destination = EditorUtility.SaveFilePanel(I18n.Tr("Playlist Export"), directory, defaultName, "playlist");
                    if (!string.IsNullOrWhiteSpace(destination))
                    {
                        Debug.Log($"Saving playlist to file {destination}");
                        // write the playlist content
                        File.WriteAllText(destination, Pickle(script), Encoding.UTF8);
                        AssetDatabase.Refresh();
                        TextAsset t = AssetDatabase.LoadAssetAtPath<TextAsset>(ATEditorUtility.ToRelativePath(destination));
                        // if the destination cannot be loaded as an asset, it's not accessible by the project so skip assignment
                        if (t != null)
                        {
                            // load the new playlist file into the import mode
                            importSrc = t;
                            importPath = AssetDatabase.GetAssetPath(importSrc);
                            updateMode = ChangeAction.OTHER;
                            cacheDetectedPlaylists();
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(I18n.Tr("Copy"), GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = Pickle(script);
                if (GUILayout.Button("Json", GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = PickleJson(script);
            }
        }

        private void Importer_DrawPlaylistAssetSelector()
        {
            string detection = "";
            if (!string.IsNullOrWhiteSpace(importPath) && entriesCount > 0)
                detection = $" | {entriesCount} URLs ({imagesCount} Images)";

            using (HArea)
            {
                var mode = (PlaylistImportMode)EditorGUILayout.EnumPopup(importMode);
                EditorGUILayout.LabelField(detection);
                if (mode != importMode)
                {
                    importMode = mode;
                    switch (importMode)
                    {
                        case PlaylistImportMode.NONE:
                            entriesCount = mainUrls.Length;
                            imagesCount = images.Count(i => i != null);
                            break;
                        case PlaylistImportMode.REMOTE:
                        case PlaylistImportMode.LOCAL:
                            if (importSrc != null)
                                PrecomputeEntryCount(importSrc.text, out entriesCount, out imagesCount);
                            break;
                    }

                    updateMode = ChangeAction.OTHER;
                }
            }

            using (HArea)
            {
                switch (importMode)
                {
                    case PlaylistImportMode.NONE:

                        if (GUILayout.Button(I18n.Tr("Add Entry"), GUILayout.MaxWidth(100f)))
                            updateMode = ChangeAction.ADD;

                        using (DisabledScope(rawEntryCount == 0))
                            if (GUILayout.Button(I18n.Tr("Remove All"), GUILayout.MaxWidth(100f)))
                                updateMode = ChangeAction.REMOVEALL;

                        break;
                    case PlaylistImportMode.LOCAL:
                        var src = importSrc;
                        var detectedIndex = string.IsNullOrEmpty(importPath) ? -1 : Array.IndexOf(detectedPlaylists, importPath);
                        float popupSize = 270f;
                        bool hasAnyPlaylists = detectedPlaylists.Length != 0;
                        if (detectedIndex == -1)
                        {
                            popupSize = 30f;
                            src = (TextAsset)EditorGUILayout.ObjectField(src, typeof(TextAsset), false, GUILayout.MaxWidth(hasAnyPlaylists ? 270f : 300f));
                        }

                        if (hasAnyPlaylists)
                        {
                            var newIndex = EditorGUILayout.Popup(GUIContent.none, detectedIndex, detectedPlaylistNames, GUILayout.MaxWidth(popupSize));
                            if (newIndex != detectedIndex) src = AssetDatabase.LoadAssetAtPath<TextAsset>(detectedPlaylists[newIndex]);
                            if (detectedIndex > -1 && GUILayout.Button("?", GUILayout.Width(30))) EditorGUIUtility.PingObject(importSrc);
                        }

                        if (src != importSrc)
                        {
                            importSrc = src;
                            importPath = src == null ? null : AssetDatabase.GetAssetPath(src);
                            updateMode = ChangeAction.OTHER;
                            importState = ImportState.READY; // if local mode changes to a new source, unset any remote url state to force a reset of it
                            entriesCount = 0;
                            imagesCount = 0;
                        }

                        if (importSrc != null)
                        {
                            if (isPrefabMode)
                            {
                                using (DisabledScope())
                                {
                                    GUILayout.Button(I18n.TrContent("Import", "Import is disabled when in prefab edit mode. Exit prefab edit mode to enable importing."), GUILayout.ExpandWidth(false));
                                }
                            }
                            else if (GUILayout.Button(I18n.Tr("Import"), GUILayout.ExpandWidth(false)))
                            {
                                PrecomputeEntryCount(importSrc.text, out entriesCount, out imagesCount);
                                PlaylistData pd = script.storage;
                                if (pd == null) pd = script.GetComponentInChildren<PlaylistData>(true);
                                if (pd == null)
                                {
                                    Debug.Log("Adding playlist data");
                                    var go = new GameObject("Playlist Data");
                                    Undo.RegisterCreatedObjectUndo(go, "Undo Playlist Data add");
                                    go.transform.SetParent(script.transform);
                                    pd = UdonSharpUndo.AddComponent<PlaylistData>(go);
                                }

                                if (pd != script.storage) SetVariableByName(nameof(script.storage), pd);
                                Parse(importSrc.text, entriesCount, out mainUrls, out alternateUrls, out titles, out descriptions, out tags, out images, out header);

                                if (script._EDITOR_autofillAltURL)
                                {
                                    for (var index = 0; index < mainUrls.Length; index++)
                                    {
                                        var targetUrl = mainUrls[index].Get();
                                        if (script._EDITOR_autofillEscape) targetUrl = Uri.EscapeDataString(targetUrl);
                                        alternateUrls[index] = new VRCUrl(script._EDITOR_autofillFormat.Replace("$URL", targetUrl));
                                    }
                                }

                                updateMode = ChangeAction.UPDATEALL;
                            }
                            else if (entriesCount == 0) PrecomputeEntryCount(importSrc.text, out entriesCount, out imagesCount);
                        }

                        break;
                    case PlaylistImportMode.REMOTE:
                        var url = EditorGUILayout.TextField(importUrl, GUILayout.MaxWidth(270f));
                        if (url != importUrl)
                        {
                            importUrl = url;
                            updateMode = ChangeAction.OTHER;
                            if (importClient != null)
                            {
                                importClient.CancelAsync();
                                importClient.Dispose();
                                importClient = null;
                            }

                            importState = ImportState.READY;

                            var playlistFile = GetCacheFilename(importUrl);
                            var playlistPath = Path.Combine(playlistFolder, playlistFile);
                            var path = Path.Combine(Application.dataPath, "..", playlistPath);
                            if (File.Exists(path))
                            {
                                var text = File.ReadAllText(path);
                                PrecomputeEntryCount(text, out entriesCount, out imagesCount);
                                importSrc = AssetDatabase.LoadAssetAtPath<TextAsset>(importPath);
                                importPath = AssetDatabase.GetAssetPath(importSrc);
                                updateMode = ChangeAction.OTHER;
                            }
                        }

                        using (DisabledScope(string.IsNullOrWhiteSpace(importUrl) || importState == ImportState.PENDING || isPrefabMode))
                        {
                            GUIContent buttonTxt = isPrefabMode
                                ? I18n.TrContent("Download", "Import is disabled when in prefab edit mode. Exit prefab edit mode to enable importing.")
                                : importState switch
                                {
                                    ImportState.READY => I18n.TrContent("Download"),
                                    ImportState.PENDING => importProgress < 0.05f ? I18n.TrContent("Fetching...") : new GUIContent($"{importProgress:P1}%..."),
                                    ImportState.COMPLETE => I18n.TrContent("Import"),
                                    ImportState.ERROR => I18n.TrContent("Retry"),
                                    _ => GUIContent.none
                                };

                            if (GUILayout.Button(buttonTxt, GUILayout.ExpandWidth(false)))
                            {
                                switch (importState)
                                {
                                    case ImportState.READY:
                                    case ImportState.ERROR:
                                        var playlistFile = GetCacheFilename(importUrl);
                                        var writeDirectory = Path.Combine(Application.dataPath, "..", playlistFolder);
                                        if (!Directory.Exists(writeDirectory)) Directory.CreateDirectory(writeDirectory);
                                        importPath = ATEditorUtility.ToRelativePath(Path.GetFullPath(Path.Combine(playlistFolder, playlistFile)));
                                        importState = ImportState.PENDING;
                                        importClient = new WebClient();
                                        importClient.DownloadStringCompleted += PlaylistDownloadComplete;
                                        importClient.DownloadProgressChanged += PlaylistDownloadProgress;
                                        importClient.DownloadStringAsync(new Uri(importUrl));
                                        break;
                                    case ImportState.COMPLETE:
                                        if (importSrc == null) importSrc = AssetDatabase.LoadAssetAtPath<TextAsset>(importPath);
                                        UnityEngine.Debug.Log($"{importPath} source check {AssetDatabase.GetAssetPath(importSrc)}");
                                        PrecomputeEntryCount(importSrc.text, out entriesCount, out imagesCount);
                                        PlaylistData pd = script.storage;
                                        if (pd == null) pd = script.GetComponentInChildren<PlaylistData>(true);
                                        if (pd == null)
                                        {
                                            Debug.Log("Adding playlist data");
                                            var go = new GameObject("Playlist Data");
                                            Undo.RegisterCreatedObjectUndo(go, "Undo Playlist Data add");
                                            go.transform.SetParent(script.transform);
                                            pd = UdonSharpUndo.AddComponent<PlaylistData>(go);
                                        }

                                        if (pd != script.storage) SetVariableByName(nameof(script.storage), pd);
                                        Parse(importSrc.text, entriesCount, out mainUrls, out alternateUrls, out titles, out descriptions, out tags, out images, out header);

                                        if (script._EDITOR_autofillAltURL)
                                        {
                                            for (var index = 0; index < mainUrls.Length; index++)
                                            {
                                                var targetUrl = mainUrls[index].Get();
                                                if (script._EDITOR_autofillEscape) targetUrl = Uri.EscapeDataString(targetUrl);
                                                alternateUrls[index] = new VRCUrl(script._EDITOR_autofillFormat.Replace("$URL", targetUrl));
                                            }
                                        }

                                        updateMode = ChangeAction.UPDATEALL;
                                        break;
                                }
                            }
                        }

                        break;
                }
            }
        }

        private void PlaylistDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            importProgress = (double)e.BytesReceived / e.TotalBytesToReceive;
            UnityEngine.Debug.Log($"Playlist DL Progress {importProgress:P}");
        }

        private void PlaylistDownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                UnityEngine.Debug.Log("Playlist retrieval cancelled by user.");
                if (importState == ImportState.PENDING) importState = ImportState.READY;
                importClient.Dispose();
                importClient = null;
                return;
            }

            if (e.Error != null)
            {
                UnityEngine.Debug.LogError(e.Error);
                importState = ImportState.ERROR;
                importClient.Dispose();
                importClient = null;
                return;
            }

            var text = e.Result;

            if (string.IsNullOrEmpty(importPath))
                importPath = Path.Combine(
                    playlistFolder,
                    GetCacheFilename(importUrl)
                );

            var destination = Path.Combine(Application.dataPath, "..", importPath);
            var skipImport = File.Exists(destination);
            File.WriteAllText(destination, text);
            importSrc = null;
            if (!skipImport) AssetDatabase.ImportAsset(importPath, ImportAssetOptions.ForceUpdate);
            else importSrc = AssetDatabase.LoadAssetAtPath<TextAsset>(importPath);
            PrecomputeEntryCount(text, out entriesCount, out imagesCount);
            UnityEngine.Debug.Log($"Playlist downloaded to {importPath} with {entriesCount} entries detected.");
            importState = ImportState.COMPLETE;
            importClient.Dispose();
            importClient = null;
            cacheDetectedPlaylists();
            updateMode = ChangeAction.OTHER;
            using (ChangeCheckScope) SaveData();
        }

        private static void processLines(string text, Func<string, bool> process)
        {
            int currentIndex = 0;
            int endIndex = text.Length;
            while (currentIndex < endIndex)
            {
                int nextIndex = text.IndexOf('\n', currentIndex);
                if (nextIndex == -1) nextIndex = endIndex;
                string line = text.Substring(currentIndex, nextIndex - currentIndex).Trim();
                currentIndex = nextIndex += 1;
                if (!process.Invoke(line)) break;
            }
        }

        private static string[] parseIndicators(string line)
        {
            line = line.Substring(EntryIndicatorsPrefix.Length).Trim();
            var re = new Regex(line.Contains(" ") ? " +" : "");
            string[] indicators = re.Split(line).Where(s => s.Length > 0).ToArray();
            const int reqLength = 5;
            if (indicators.Length != reqLength)
            {
                var sb = new StringBuilder();
                sb.Append("Custom indicators line defined, but not enough indicators are provided.\n");
                sb.Append($"You must have {reqLength} symbols defined for the following indicators in the given order: ");
                sb.Append("Main URL, Alternate URL, Image Location, Tags, Title\n");
                sb.Append($"Eg: <color=green>? {EntryStartIndicator} {EntryAltIndicator} {EntryImageIndicator} {EntryTagIndicator} {EntryTitleIndicator}</color>\n");
                sb.Append($"Detected: [{string.Join(", ", indicators)}]");
                Debug.LogWarning(sb.ToString());
                return null;
            }

            return indicators;
        }

        #region Playlist Importer APIs

        private struct PlaylistJson
        {
            public string header;
            public List<PlaylistJsonEntry> entries;
        }

        private struct PlaylistJsonEntry
        {
            public string mainUrl;
            public string alternateUrl;
            public string title;
            public string description;
            public string tags;
            public string image;
        }

        public static void PrecomputeJsonEntryCount(string json, out int entriesCount, out int imagesCount)
        {
            var playlist = JsonConvert.DeserializeObject<PlaylistJson>(json);
            entriesCount = playlist.entries.Count;
            imagesCount = playlist.entries.Count(entry => !string.IsNullOrEmpty(entry.image));
        }

        /// <summary>
        /// This will read the text input as if it was a playlist file and provide a count of the
        /// total number of unique playlist entries detected, as well as the number of entries
        /// that have images found.
        /// </summary>
        /// <param name="text">Content of the playlist text file</param>
        /// <param name="entriesCount">returned number of detected entries</param>
        /// <param name="imagesCount">returned number of detected entries with images</param>
        public static void PrecomputeEntryCount(string text, out int entriesCount, out int imagesCount)
        {
            if (text.StartsWith('{'))
            {
                PrecomputeJsonEntryCount(text, out entriesCount, out imagesCount);
                return;
            }

            var _entriesCount = 0;
            var _imagesCount = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                entriesCount = 0;
                imagesCount = 0;
                return;
            }

            text = text.Trim();
            var startInd = EntryStartIndicator;
            var imgInd = EntryImageIndicator;
            bool customIndicators = false;
            processLines(text, line =>
            {
                if (!customIndicators && line.StartsWith(EntryIndicatorsPrefix))
                {
                    customIndicators = true;
                    var indicators = parseIndicators(line);
                    startInd = indicators[0];
                    imgInd = indicators[2];
                }
                else if (line.StartsWith(startInd))
                {
                    customIndicators = true;
                    _entriesCount++;
                }
                else if (line.StartsWith(imgInd) && !string.IsNullOrEmpty(line.Substring(imgInd.Length)))
                {
                    customIndicators = true;
                    _imagesCount++;
                }

                return true;
            });

            entriesCount = _entriesCount;
            imagesCount = _imagesCount;
        }

        public static void ParseJson(string json, out VRCUrl[] mainUrls, out VRCUrl[] alternateUrls, out string[] titles, out string[] descriptions, out string[] tags, out Sprite[] images, out string header)
        {
            var playlist = JsonConvert.DeserializeObject<PlaylistJson>(json);
            header = playlist.header;
            mainUrls = playlist.entries.Select(entry => new VRCUrl(entry.mainUrl)).ToArray();
            alternateUrls = playlist.entries.Select(entry => new VRCUrl(entry.alternateUrl)).ToArray();
            titles = playlist.entries.Select(entry => entry.title).ToArray();
            descriptions = playlist.entries.Select(entry => entry.description).ToArray();
            tags = playlist.entries.Select(entry => entry.tags).ToArray();
            images = playlist.entries.Select(entry => entry.image == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(entry.image)).ToArray();
        }

        /// <summary>
        /// This method will run through the fill file text content and cache then return all the
        /// respective parts of each entry. The returned values should be treated as a set of tuple
        /// arrays. All returned arrays will be of the same length.
        /// </summary>
        /// <param name="text">Content of the playlist text file</param>
        /// <param name="mainUrls">returned list of primary urls</param>
        /// <param name="alternateUrls">returned list of secondary urls</param>
        /// <param name="titles">returned list of titles</param>
        /// <param name="descriptions">returned list of descriptions</param>
        /// <param name="tags">returned list of tags</param>
        /// <param name="images">returned list of image sprites</param>
        /// <param name="header">descriptive text for the playlist found prior to the first entry</param>
        public static void Parse(string text, out VRCUrl[] mainUrls, out VRCUrl[] alternateUrls, out string[] titles, out string[] descriptions, out string[] tags, out Sprite[] images, out string header) =>
            Parse(text, -1, out mainUrls, out alternateUrls, out titles, out descriptions, out tags, out images, out header);

        /// <summary>
        /// This method will run through the fill file text content and cache then return all the
        /// respective parts of each entry. The returned values should be treated as a set of tuple
        /// arrays. All returned arrays will be of the same length.
        /// </summary>
        /// <param name="text">Content of the playlist text file</param>
        /// <param name="precomputedEntryCount">precalculated number of entries. If value is -1, <see cref="PrecomputeEntryCount"/> will be used implicitly to determine the size of the playlist.</param>
        /// <param name="mainUrls">returned list of primary urls</param>
        /// <param name="alternateUrls">returned list of secondary urls</param>
        /// <param name="titles">returned list of titles</param>
        /// <param name="descriptions">returned list of descriptions</param>
        /// <param name="tags">returned list of tags</param>
        /// <param name="images">returned list of image sprites</param>
        /// <param name="header">descriptive text for the playlist found prior to the first entry</param>
        public static void Parse(string text, int precomputedEntryCount, out VRCUrl[] mainUrls, out VRCUrl[] alternateUrls, out string[] titles, out string[] descriptions, out string[] tags, out Sprite[] images, out string header)
        {
            if (text.StartsWith('{'))
            {
                ParseJson(text, out mainUrls, out alternateUrls, out titles, out descriptions, out tags, out images, out header);
                return;
            }

            if (precomputedEntryCount < 0) PrecomputeEntryCount(text, out precomputedEntryCount, out _);
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            text = text.Trim();
            var mainInd = EntryStartIndicator;
            var altInd = EntryAltIndicator;
            var imageInd = EntryImageIndicator;
            var tagsInd = EntryTagIndicator;
            var titleInd = EntryTitleIndicator;
            string headerText = null;

            processLines(text, line =>
            {
                // keep searching for the indicators line prefix until it's found
                if (line.StartsWith(EntryIndicatorsPrefix))
                {
                    var indicators = parseIndicators(line);
                    mainInd = indicators[0];
                    altInd = indicators[1];
                    imageInd = indicators[2];
                    tagsInd = indicators[3];
                    titleInd = indicators[4];
                    Debug.Log($"Importing with custom indicators: Main <color=lightblue>{mainInd}</color> Alt <color=lightblue>{altInd}</color> Image <color=lightblue>{imageInd}</color> Tags <color=lightblue>{tagsInd}</color> Title <color=lightblue>{titleInd}</color>");
                    return true;
                }

                bool continueParsing = !line.StartsWith(mainInd);
                // any leading text that isn't indicators line prior to the first valid entry is considered part of the header text
                if (continueParsing && line.Length > 0) headerText += line;
                // until the start indicator is found
                return continueParsing;
            });

            var _mainUrls = new VRCUrl[precomputedEntryCount];
            var _alternateUrls = new VRCUrl[precomputedEntryCount];
            var _titles = new string[precomputedEntryCount];
            var _descriptions = new string[precomputedEntryCount];
            var _tags = new string[precomputedEntryCount];
            var _images = new Sprite[precomputedEntryCount];
            var count = -1;
            bool foundAlt = false;
            bool foundTagString = false;
            bool foundImage = false;
            bool foundTitle = false;
            bool foundDescription = false;
            string currentDescription = "";
            uint missingTitles = 0;

            processLines(text, line =>
            {
                if (line.StartsWith(mainInd))
                {
                    if (count > -1)
                    {
                        if (string.IsNullOrEmpty(_titles[count]))
                        {
                            _titles[count] = "";
                            Debug.Log($"1 Missing title at index {count}");
                            // ReSharper disable once AccessToModifiedClosure
                            missingTitles++;
                        }

                        _descriptions[count] = currentDescription.Trim();
                        currentDescription = "";
                        foundAlt = false;
                        foundTagString = false;
                        foundImage = false;
                        foundTitle = false;
                        foundDescription = false;
                    }

                    count++;
                    _mainUrls[count] = new VRCUrl(line.Substring(mainInd.Length).Trim());
                    return true;
                }

                if (count == -1) return true;
                if (!foundDescription && !foundAlt && line.StartsWith(altInd))
                {
                    _alternateUrls[count] = new VRCUrl(line.Substring(altInd.Length));
                    foundAlt = true;
                    return true;
                }

                if (!foundDescription && !foundImage && line.StartsWith(imageInd))
                {
                    string assetFile = line.Substring(imageInd.Length);
                    _images[count] = (Sprite)AssetDatabase.LoadAssetAtPath(assetFile, typeof(Sprite));
                    foundImage = true;
                    return true;
                }

                if (!foundDescription && !foundTagString && line.StartsWith(tagsInd))
                {
                    _tags[count] = SanitizeTagString(line.Substring(tagsInd.Length));
                    foundTagString = true;
                    return true;
                }

                if (!foundDescription && !foundTitle && line.StartsWith(titleInd))
                {
                    _titles[count] = line.Substring(titleInd.Length);
                    foundTitle = true;
                    return true;
                }

                // for the implicit title and description checks, ignore empty lines if a description has not yet been found
                if (!foundDescription && line.Length == 0) return true;

                // Title is either a line prefixed with the title indicator (defaults to ~)
                // Or the first non-prefixed line encountered
                // For example, this is valid
                //
                // @myurl
                // ^alturl
                // ~Title line 1
                // Finally it's the description of the entry because there is no prefix and the title was already declared
                // ~Because the description is already found, this is just another line of the description
                // ~even though we start it with the indicator for the title
                // with new lines and all that jazz
                if (!foundDescription && !foundTitle)
                {
                    _titles[count] = line;
                    foundTitle = true;
                    // if the implicit title has been found, no other prefix lines should be considered
                    // force the remainder of the entry to be part of the description
                    foundDescription = true;
                    return true;
                }

                // any subsequent line is part of the description
                if (currentDescription.Length > 0) currentDescription += '\n';
                currentDescription += line.Trim();
                foundDescription = true;
                return true;
            });

            if (count > -1)
            {
                _descriptions[count] = currentDescription.Trim();
                if (string.IsNullOrWhiteSpace(_titles[count]))
                {
                    missingTitles++;
                    Debug.Log($"2 Missing title at index {count}");
                }
            }

            if (missingTitles > 0)
            {
                Debug.LogWarning($"Just a heads up, this playlist has {missingTitles} entries that don't have any titles.");
            }

            stopwatch.Stop();
            Debug.Log($"Parsed {_mainUrls.Length} playlist entries in {stopwatch.ElapsedMilliseconds}ms");

            // final out param assignment
            mainUrls = _mainUrls;
            alternateUrls = _alternateUrls;
            titles = _titles;
            descriptions = _descriptions;
            tags = _tags;
            images = _images;
            header = headerText;
        }

        public static void UnPickle(Playlist playlist, TextAsset source) => UnPickle(playlist, source.text);

        /// <summary>
        /// This takes a given playlist text file content, parses it and applies it to the Playlist.
        /// Effectively the same thing as the Playlist Inspector's import function.
        /// This method internally invokes the Parse method.
        /// </summary>
        /// <param name="playlist">The component to update</param>
        /// <param name="text">The file content to process and apply</param>
        /// <seealso cref="Parse(string, out VRCUrl[], out VRCUrl[], out string[], out string[], out string[], out Sprite[])"/>
        public static void UnPickle(Playlist playlist, string text)
        {
            using (new SaveObjectScope(playlist))
            {
                PrecomputeEntryCount(text, out int entriesCount, out _);
                Parse(text, entriesCount,
                    out VRCUrl[] mainUrls,
                    out VRCUrl[] alternateUrls,
                    out string[] titles,
                    out string[] descriptions,
                    out string[] tags,
                    out Sprite[] images,
                    out string header);

                playlist.header = header;

                PlaylistData pd = playlist.storage;
                if (pd == null) pd = playlist.GetComponentInChildren<PlaylistData>(true);
                if (pd == null)
                {
                    var go = new GameObject("Playlist Data");
                    Undo.RegisterCreatedObjectUndo(go, "Undo Playlist Data add");
                    go.transform.SetParent(playlist.transform);
                    pd = UdonSharpUndo.AddComponent<PlaylistData>(go);
                }

                playlist.storage = pd;

                if (playlist._EDITOR_autofillAltURL)
                {
                    for (var index = 0; index < mainUrls.Length; index++)
                    {
                        var targetUrl = mainUrls[index].Get();
                        if (playlist._EDITOR_autofillEscape) targetUrl = Uri.EscapeDataString(targetUrl);
                        alternateUrls[index] = new VRCUrl(playlist._EDITOR_autofillFormat.Replace("$URL", targetUrl));
                    }
                }

                using (new SaveObjectScope(pd))
                {
                    pd.mainUrls = mainUrls;
                    pd.alternateUrls = alternateUrls;
                    pd.titles = titles;
                    pd.descriptions = descriptions;
                    pd.tags = tags;
                    pd.images = images;
                }
            }
        }

        public static string PickleJson(Playlist playlist)
        {
            bool hasStorage = playlist.storage != null;
            var mainUrls = hasStorage ? playlist.storage.mainUrls : playlist.mainUrls;
            var alternateUrls = hasStorage ? playlist.storage.alternateUrls : playlist.alternateUrls;
            var titles = hasStorage ? playlist.storage.titles : playlist.titles;
            var descriptions = hasStorage ? playlist.storage.descriptions : playlist.descriptions;
            var tags = hasStorage ? playlist.storage.tags : playlist.tags;
            var images = hasStorage ? playlist.storage.images : playlist.images;
            var len = mainUrls.Length;
            var entries = new List<PlaylistJsonEntry>();
            for (int i = 0; i < len; i++)
            {
                entries.Add(new PlaylistJsonEntry()
                {
                    mainUrl = mainUrls[i].Get(),
                    alternateUrl = alternateUrls[i].Get(),
                    title = titles[i],
                    description = descriptions[i],
                    tags = tags[i],
                    image = AssetDatabase.GetAssetPath(images[i])
                });
            }

            PlaylistJson json = new PlaylistJson
            {
                header = playlist.header,
                entries = entries
            };
            return JsonConvert.SerializeObject(json, Formatting.Indented);
        }

        /// <summary>
        /// Takes a given playlist and outputs a serialized format as a string for saving into a file.
        /// The output follows the playlist file format specification.
        /// The output of this function can be equally used as input to the <see cref="UnPickle(Playlist, string)"/> method.
        /// </summary>
        /// <param name="playlist">The component to process and serialize</param>
        /// <returns>Serialized output text</returns>
        public static string Pickle(Playlist playlist)
        {
            bool hasStorage = playlist.storage != null;
            var mainUrls = hasStorage ? playlist.storage.mainUrls : playlist.mainUrls;
            var alternateUrls = hasStorage ? playlist.storage.alternateUrls : playlist.alternateUrls;
            var titles = hasStorage ? playlist.storage.titles : playlist.titles;
            var descriptions = hasStorage ? playlist.storage.descriptions : playlist.descriptions;
            var tags = hasStorage ? playlist.storage.tags : playlist.tags;
            var images = hasStorage ? playlist.storage.images : playlist.images;

            StringBuilder s = new StringBuilder();
            s.Append(EntryIndicatorsPrefix)
                .Append(EntryStartIndicator)
                .Append(EntryAltIndicator)
                .Append(EntryImageIndicator)
                .Append(EntryTagIndicator)
                .Append(EntryTitleIndicator)
                .Append("\n\n");

            if (!string.IsNullOrEmpty(playlist.header))
                s.Append(playlist.header).Append("\n\n");

            for (int i = 0; i < mainUrls.Length; i++)
            {
                var url = mainUrls[i];
                s.Append(EntryStartIndicator).Append(url?.Get() ?? string.Empty).Append("\n");

                var alt = alternateUrls[i];
                if (alt != null && !string.IsNullOrWhiteSpace(alt.Get())) s.Append(EntryAltIndicator).Append(alt?.Get() ?? string.Empty).Append("\n");

                var image = images[i];
                if (image != null) s.Append(EntryImageIndicator).Append(AssetDatabase.GetAssetPath(image.texture)).Append("\n");

                var tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) s.Append(EntryTagIndicator).Append(tag).Append("\n");

                var title = titles[i];
                if (!string.IsNullOrWhiteSpace(title))
                {
                    string[] titleLines = title.Split('\n');
                    foreach (string line in titleLines) s.Append(EntryTitleIndicator).Append(line).Append("\n");
                }

                var description = descriptions[i];
                if (!string.IsNullOrWhiteSpace(description)) s.AppendLine(description);

                s.AppendLine("");
            }

            return s.ToString();
        }

        /// <summary>
        /// This is a helper method for cleaning up the comma delimited tags for consistent formatting.
        /// Primarily used by the Parse method.
        /// </summary>
        /// <param name="tagString">Raw tag string, typically from user input</param>
        /// <returns>cleaned up tag string</returns>
        /// <seealso cref="Parse(string, out VRCUrl[], out VRCUrl[], out string[], out string[], out string[], out Sprite[])"/>
        public static string SanitizeTagString(string tagString)
        {
            // sanitize the tags to reduce the number of externs required for udon processing
            var tagList = tagString.Split(',');
            for (int k = 0; k < tagList.Length; k++)
            {
                var tag = tagList[k];
                tag = tag.ToLower();
                if (tag.Contains(":"))
                {
                    int idx = tag.IndexOf(':');
                    if (idx > -1)
                    {
                        var tGroup = tag.Substring(0, idx).Trim();
                        var tValue = tag.Substring(idx + 1).Trim();
                        tag = tGroup + ':' + tValue;
                    }
                    else tag = tag.Trim();

                    tagList[k] = tag;
                }
                else tagList[k] = tag.Trim();
            }

            return string.Join(",", tagList);
        }

        #endregion

        private void OnDestroy()
        {
            if (importClient != null)
            {
                importClient.CancelAsync();
                importClient.Dispose();
                importClient = null;
            }
        }

        private static string GetCacheFilename(string url)
        {
            return $"{SceneManager.GetActiveScene().name}-{Math.Abs(url.GetHashCode())}{url.Length}.playlist";
        }
    }

    [ScriptedImporter(1, "playlist")]
    public class PlaylistFileTypeImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            TextAsset subAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("text", subAsset);
            ctx.SetMainObject(subAsset);
        }
    }
}