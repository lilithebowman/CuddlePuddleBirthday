using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArchiTech.SDK;
using UnityEditor;
using UnityEngine;

#pragma warning disable CS0649

namespace ArchiTech.ProTV.Editor
{
    public class ProTVGeneratorsWindow : EditorWindow
    {
        internal enum GenerationMode
        {
            YT_PLAYLIST,
            YT_CHAPTERS
        }

        private string[] generationModeLabels =
        {
            "Youtube Playlist",
            "Youtube Video Chapters"
        };

        private static string youtubeDLPath = "";
        private static string youtubeDLTarget = "";
        private static System.Net.WebClient _webClient;
        private static System.Diagnostics.Process ytdlProcess;
        private static Task<string> readData;
        private static Task<string> readError;
        private static string ytdlpWIN = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        private static string ytdlpOSX = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos";
        private static string ytdlpLNX = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux";
        private static readonly Regex youtubeVideoPattern = new Regex("^https:\\/\\/(?:(?:www\\.)?youtube\\.com\\/watch\\/?\\?v=|youtu.be\\/)[\\w\\d]+$");
        private static readonly Regex youtubePlaylistPattern = new Regex("^https:\\/\\/(?:www\\.)?youtube\\.com\\/playlist\\/?\\?list=[\\w\\d_-]+$");

        internal GenerationMode mode;
        private string selectedUrl = "";
        private string currentUrl = "";
        private string currentHashParams = "";
        private string requestedUrl = "";
        private string requestedHashParams = "";
        private string cachedJsonResponse = "";
        private string generatedPlaylist = "";
        private string generatedPlaylistTitle = "";

        private static bool fetchingYTDL = false;
        private bool fetchingJson = false;
        private bool lastRequestFailed = false;
        private string lastRequestError = "";
        private bool patternFailure = false;

        private Vector2 scrollPos = Vector2.zero;
        private bool autofillMain = false;
        private bool autofillAlt = false;
        private bool autofillEscape = false;
        private string autofillFormatStr = "$URL";
        private bool includePlaylistTitle = true;


        private void OnDestroy()
        {
            youtubeDLPath = "";
            if (ytdlProcess != null && !ytdlProcess.HasExited)
                ytdlProcess.Close();
        }


        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(fetchingJson);
            if (GUILayout.Button(generationModeLabels[(int)mode], GUILayout.ExpandWidth(true)))
            {
                mode = mode == GenerationMode.YT_CHAPTERS ? 0 : mode + 1;
            }

            var _selectedUrl = EditorGUILayout.TextField(I18n.Tr("URL"), selectedUrl);
            if (_selectedUrl != selectedUrl)
            {
                requestedUrl = _selectedUrl;
                requestedHashParams = "";
                // extract hash params
                if (requestedUrl.Contains("#"))
                {
                    var split = requestedUrl.Split('#');
                    requestedUrl = split[0];
                    requestedHashParams = split[1];
                }
            }

            selectedUrl = _selectedUrl;

            if (mode == GenerationMode.YT_PLAYLIST)
            {
                patternFailure = requestedUrl.Length > 0 && !youtubePlaylistPattern.IsMatch(requestedUrl);
                if (patternFailure) EditorGUILayout.HelpBox(I18n.Tr("URL must be in the format of https://youtube.com/playlist?list=PLAYLIST_ID"), MessageType.Error);
            }
            else if (mode == GenerationMode.YT_CHAPTERS)
            {
                patternFailure = requestedUrl.Length > 0 && !youtubeVideoPattern.IsMatch(requestedUrl);
                if (patternFailure) EditorGUILayout.HelpBox(I18n.Tr("URL must be in the format of https://youtube.com/watch?v=VIDEO_ID"), MessageType.Error);
            }
            else patternFailure = false;

            if (!fetchingYTDL) CheckYTDLExecutable();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(I18n.Tr("Autofill Options"));
                using (new EditorGUILayout.VerticalScope())
                {
                    bool hasAutofill = autofillMain || autofillAlt;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        autofillMain = EditorGUILayout.ToggleLeft(I18n.Tr("Main URL"), autofillMain, GUILayout.Width(100));
                        autofillAlt = EditorGUILayout.ToggleLeft(I18n.Tr("Alternate URL"), autofillAlt, GUILayout.Width(100));
                        hasAutofill = autofillMain || autofillAlt;
                        using (new EditorGUI.DisabledScope(!hasAutofill))
                        {
                            if (hasAutofill)
                                autofillEscape = EditorGUILayout.ToggleLeft(I18n.Tr("Escape URL"), autofillEscape, GUILayout.Width(100));
                            else
                                EditorGUILayout.ToggleLeft(I18n.Tr("Escape URL"), false, GUILayout.Width(100));
                        }

                        includePlaylistTitle = EditorGUILayout.ToggleLeft(I18n.Tr("Include Playlist Title"), includePlaylistTitle, GUILayout.Width(150));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(!hasAutofill))
                        {
                            EditorGUILayout.LabelField(I18n.Tr("URL Format"), GUILayout.Width(100));
                            if (hasAutofill)
                                autofillFormatStr = EditorGUILayout.TextField(autofillFormatStr, GUILayout.ExpandWidth(true));
                            else EditorGUILayout.TextField("", GUILayout.ExpandWidth(true));
                        }
                    }
                }
            }

            if (autofillMain || autofillAlt)
                EditorGUILayout.HelpBox(I18n.Tr("Put $URL (uppercase is important) wherever you want the main url to be inserted. Eg: https://mydomain.tld/?url=$URL"), MessageType.Info);

            using (new EditorGUI.DisabledScope(patternFailure || selectedUrl.Length == 0))
            {
                if (GUILayout.Button(I18n.Tr("Generate Playlist"))) RequestUrlJsonDump();
            }


            GUIStyle infoStyle = new GUIStyle() { richText = true };
            if (fetchingJson)
                EditorGUILayout.LabelField(I18n.Tr("Fetching JSON data. Please be patient, as this can take a while to complete depending on the number of videos involved..."));
            else if (fetchingYTDL)
                EditorGUILayout.LabelField(I18n.Tr("YTDL not found. Retrieving latest version..."));
            else if (lastRequestFailed)
                EditorGUILayout.LabelField($"<color=orange>{I18n.Tr("Last fetch failed")} - {lastRequestError}</color>", infoStyle);
            else
            {
                string playlistTitle = I18n.Tr("No playlist generated...");
                if (generatedPlaylistTitle.Length > 0) playlistTitle = generatedPlaylistTitle;
                EditorGUILayout.LabelField($"{I18n.Tr("Playlist Preview")} | {playlistTitle}");
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(400));
            generatedPlaylist = EditorGUILayout.TextArea(generatedPlaylist, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(generatedPlaylist.Length == 0))
                {
                    if (GUILayout.Button(I18n.Tr("Save")))
                    {
                        // get where to save the file
                        string destination = EditorUtility.SaveFilePanelInProject(I18n.Tr("Saving Playlist"), generatedPlaylistTitle, "playlist", I18n.Tr("Save the playlist in your assets folder"));
                        if (!string.IsNullOrWhiteSpace(destination))
                        {
                            Debug.Log($"Saving playlist to file: {destination}");
                            // write the playlist content
                            File.WriteAllText(destination, generatedPlaylist, Encoding.UTF8);
                            AssetDatabase.Refresh();
                            ShowNotification(new GUIContent(I18n.Tr("Playlist saved to file.")), 5f);
                        }
                    }

                    if (GUILayout.Button("Copy"))
                    {
                        GUIUtility.systemCopyBuffer = generatedPlaylist;
                        ShowNotification(new GUIContent(I18n.Tr("Playlist copied to clipboard.")), 5f);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        private void CheckYTDLExecutable()
        {
            if (!string.IsNullOrEmpty(youtubeDLPath) && File.Exists(youtubeDLPath)) return;

            youtubeDLTarget = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar + "yt-dlp-full";
            if (File.Exists(youtubeDLTarget))
            {
                youtubeDLPath = youtubeDLTarget;
                return;
            }

            var source = "";
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    source = ytdlpWIN;
                    break;
                case RuntimePlatform.OSXEditor:
                    source = ytdlpOSX;
                    break;
                case RuntimePlatform.LinuxEditor:
                    source = ytdlpLNX;
                    break;
                default:
                    return;
            }

            fetchingYTDL = true;
            _webClient = new System.Net.WebClient();
            _webClient.DownloadFileCompleted += YoutubDLRetrievalComplete;
            _webClient.DownloadFileAsync(new Uri(source), youtubeDLTarget);
        }

        private void YoutubDLRetrievalComplete(object obj, AsyncCompletedEventArgs args)
        {
            UnityEngine.Debug.Log("YTDL retrieval finished");
            fetchingYTDL = false;
            if (args.Error != null)
            {
                lastRequestFailed = true;
                lastRequestError = "YTDL retrieval failed: " + args.Error.Message;
                UnityEngine.Debug.LogError(lastRequestError);
                return;
            }

            lastRequestFailed = false;
            youtubeDLPath = youtubeDLTarget;
            UnityEngine.Debug.Log("YTDL path - " + youtubeDLPath);
        }

        private void Update()
        {
            if (fetchingJson)
                if (readData != null && readData.IsCompleted || readError != null && readError.IsCompleted)
                {
                    fetchingJson = false;
                    YoutubeDLJsonDumpComplete();
                    Repaint();
                }
        }

        private void RequestUrlJsonDump()
        {
            if (string.IsNullOrEmpty(youtubeDLPath))
            {
                lastRequestFailed = true;
                lastRequestError = "Unable to find VRC YouTube-dl installation, cannot generate.";
                UnityEngine.Debug.LogError(lastRequestError);
                return;
            }

            lastRequestFailed = false;
            lastRequestError = "";

            // url hasn't changed. Use existing cached Json.
            if (generatedPlaylist.Length > 0 && currentUrl == requestedUrl)
            {
                currentHashParams = requestedHashParams;
                switch (mode)
                {
                    case GenerationMode.YT_PLAYLIST:
                        GenerateFromYTPlaylist();
                        break;
                    case GenerationMode.YT_CHAPTERS:
                        GenerateFromYTChapters();
                        break;
                }

                return;
            }

            if (ytdlProcess != null && !ytdlProcess.HasExited) ytdlProcess.Close();
            ytdlProcess = new System.Diagnostics.Process();
            ytdlProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ytdlProcess.StartInfo.CreateNoWindow = true;
            ytdlProcess.StartInfo.UseShellExecute = false;
            ytdlProcess.StartInfo.RedirectStandardOutput = true;
            ytdlProcess.StartInfo.RedirectStandardError = true;
            ytdlProcess.StartInfo.FileName = youtubeDLPath;
            ytdlProcess.StartInfo.Arguments = $"-J --flat-playlist \"{requestedUrl}\"";
            fetchingJson = true;
            ytdlProcess.Start();
            readData = ytdlProcess.StandardOutput.ReadToEndAsync();
            readError = ytdlProcess.StandardError.ReadToEndAsync();
        }

        private void YoutubeDLJsonDumpComplete()
        {
            currentUrl = requestedUrl;
            currentHashParams = requestedHashParams;
            var error = readError.Result;
            var response = readData.Result;
            if (!string.IsNullOrWhiteSpace(error))
            {
                if (string.IsNullOrWhiteSpace(response) || response.Trim() == "null")
                {
                    lastRequestFailed = true;
                    lastRequestError = error;
                    UnityEngine.Debug.LogError(error);
                    return;
                }

                UnityEngine.Debug.LogWarning(error);
            }

            lastRequestFailed = false;
            lastRequestError = "";
            cachedJsonResponse = response;
            switch (mode)
            {
                case GenerationMode.YT_PLAYLIST:
                    GenerateFromYTPlaylist();
                    break;
                case GenerationMode.YT_CHAPTERS:
                    GenerateFromYTChapters();
                    break;
            }
        }

        private void GenerateFromYTPlaylist()
        {
            Playlist playlist = null;
            try
            {
                playlist = JsonUtility.FromJson<Playlist>(cachedJsonResponse);
            }
            catch (ArgumentException)
            {
                lastRequestFailed = true;
                lastRequestError = "Requested playlist URL returned invalid JSON content.";
                UnityEngine.Debug.LogError($"isNull {cachedJsonResponse == null} length {cachedJsonResponse?.Length ?? 0} isStrNull {string.CompareOrdinal(cachedJsonResponse, "null")}");
                UnityEngine.Debug.LogError(lastRequestError + "\n" + cachedJsonResponse);
                return;
            }

            if (playlist?.entries == null || playlist.entries.Length == 0)
            {
                lastRequestFailed = true;
                lastRequestError = "Requested playlist URL does not have any videos associated with it.";
                UnityEngine.Debug.LogError(lastRequestError);
                return;
            }

            lastRequestFailed = false;
            lastRequestError = "";

            generatedPlaylistTitle = $"Playlist - {playlist.title} [{playlist.id}]";

            StringBuilder build = new StringBuilder();
            var targetFormat = autofillFormatStr;
            string hashParams = currentHashParams;
            if (targetFormat.Contains("#"))
            {
                var split = targetFormat.Split('#');
                targetFormat = split[0];
                if (split[1].Length > 0)
                {
                    if (hashParams.Length > 0) hashParams += ";";
                    hashParams += split[1];
                }
            }

            build.Append(PlaylistEditor.EntryIndicatorsPrefix)
                .Append(PlaylistEditor.EntryStartIndicator)
                .Append(PlaylistEditor.EntryAltIndicator)
                .Append(PlaylistEditor.EntryImageIndicator)
                .Append(PlaylistEditor.EntryTagIndicator)
                .Append(PlaylistEditor.EntryTitleIndicator)
                .Append("\n\n");

            foreach (Video video in playlist.entries)
            {
                if (string.IsNullOrWhiteSpace(video.id)) continue; // invalid video, probably a hidden/private video in the playlist
                var videoUrl = "https://youtube.com/embed/" + video.id;
                var autofillUrl = videoUrl;
                if (autofillMain || autofillAlt)
                {
                    if (autofillEscape) autofillUrl = Uri.EscapeDataString(autofillUrl);
                    if (autofillUrl.Contains("$URL")) autofillUrl = targetFormat.Replace("$URL", autofillUrl);
                    else if (autofillUrl.Contains("$ID")) autofillUrl = targetFormat.Replace("$ID", video.id);
                }

                if (hashParams.Length > 0) hashParams = "#" + hashParams;
                build.Append(PlaylistEditor.EntryStartIndicator).Append(autofillMain ? autofillUrl : videoUrl).Append(hashParams).Append("\n");
                if (autofillAlt) build.Append(PlaylistEditor.EntryAltIndicator).Append(autofillUrl).Append(hashParams).Append("\n");
                build.Append(PlaylistEditor.EntryTitleIndicator);
                if (includePlaylistTitle) build.AppendFormat("{0} | {1}", playlist.title, video.title);
                else build.Append(video.title);
                build.Append("\n\n");
            }

            generatedPlaylist = build.ToString();
        }

        private void GenerateFromYTChapters()
        {
            Video video = JsonUtility.FromJson<Video>(cachedJsonResponse);
            if (video?.chapters == null || video.chapters.Length == 0)
            {
                lastRequestFailed = true;
                lastRequestError = "Requested video URL does not have any chapters associated with it.";
                UnityEngine.Debug.LogError(lastRequestError);
                return;
            }

            lastRequestFailed = false;
            lastRequestError = "";

            generatedPlaylistTitle = $"Playlist - {video.title} [{video.id}]";

            StringBuilder build = new StringBuilder();
            var targetFormat = autofillFormatStr;
            string hashParams = currentHashParams;
            if (targetFormat.Contains("#"))
            {
                var split = targetFormat.Split('#');
                targetFormat = split[0];
                if (split[1].Length > 0)
                {
                    if (hashParams.Length > 0) hashParams += ";";
                    hashParams += split[1];
                }
            }

            var targetUrl = "https://youtu.be/" + video.id;
            var autofillUrl = targetUrl;
            if (autofillMain || autofillAlt)
            {
                if (autofillEscape) autofillUrl = Uri.EscapeDataString(autofillUrl);
                if (autofillUrl.Contains("$URL")) autofillUrl = targetFormat.Replace("$URL", autofillUrl);
                else if (autofillUrl.Contains("$ID")) autofillUrl = targetFormat.Replace("$ID", video.id);
            }

            build.Append(PlaylistEditor.EntryIndicatorsPrefix)
                .Append(PlaylistEditor.EntryStartIndicator)
                .Append(PlaylistEditor.EntryAltIndicator)
                .Append(PlaylistEditor.EntryImageIndicator)
                .Append(PlaylistEditor.EntryTagIndicator)
                .Append(PlaylistEditor.EntryTitleIndicator)
                .Append("\n\n");

            foreach (Chapter chapter in video.chapters)
            {
                var chapterHashParams = $"#start={chapter.start_time};end={chapter.end_time}";
                if (hashParams.Length > 0) chapterHashParams += ";" + hashParams;
                build.Append(PlaylistEditor.EntryStartIndicator).Append(autofillMain ? autofillUrl : targetUrl).Append(chapterHashParams).Append("\n");
                if (autofillAlt) build.Append(PlaylistEditor.EntryAltIndicator).Append(autofillUrl).Append(chapterHashParams).Append("\n");
                build.Append(PlaylistEditor.EntryTitleIndicator);
                if (includePlaylistTitle) build.AppendFormat("{0} | {1}", video.title, chapter.title);
                else build.Append(video.title);
                build.Append("\n\n");
            }

            generatedPlaylist = build.ToString();
        }

        [Serializable]
        private class Playlist
        {
            public string id;

            public string title;

            public string description;
            public Video[] entries;
        }

        [Serializable]
        private class Video
        {
            public string id;
            public string title;
            public string channel;
            public string[] tags;
            public Chapter[] chapters;
        }

        [Serializable]
        private class Chapter
        {
            public float start_time;
            public float end_time;
            public string title;
        }
    }
}