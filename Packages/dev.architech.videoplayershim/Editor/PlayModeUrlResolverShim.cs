using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
#if AVPRO_IMPORTED
using VRC.SDK3.Video.Components.AVPro;
#endif
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace ArchiTech.VideoPlayerShim
{
    /// <summary>
    /// Code originally by Merlin via USharpVideo asset, modified for various improvements and stability.
    /// Allows people to put in links to YouTube videos and other supported video services and have links just work
    /// Hooks into VRC's video player URL resolve callback and uses the VRC installation of YouTubeDL to resolve URLs in the editor.
    /// </summary>
    internal static class PlayModeUrlResolverShim
    {
        private static string youtubeDLPath = "";
        private static readonly HashSet<System.Diagnostics.Process> runningYTDLProcesses = new HashSet<System.Diagnostics.Process>();
        private static readonly HashSet<MonoBehaviour> registeredBehaviours = new HashSet<MonoBehaviour>();
        private static readonly Regex pattern = new Regex(".*(?:youtube|yt)-dl.*\\.exe");
        internal const string userDefinedYTDLPathKey = "YTDL-PATH-CUSTOM";
        internal const string forceVideoErrorKey = "FORCE-VIDEO-ERROR";

        private static readonly string[] possibleExecutableNames =
        {
            "yt-dlp", "ytdlp", "youtube-dlp", "youtubedlp",
            "yt-dl", "ytdl", "youtube-dl", "youtubedl"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SetupURLResolveCallback()
        {
            youtubeDLPath = GetYTDLExecutablePath();
            if (!string.IsNullOrEmpty(youtubeDLPath)) SetupCallbacks();
        }

        private static string GetYTDLExecutablePath()
        {
            // check for a custom install location
            var customPath = EditorPrefs.GetString(userDefinedYTDLPathKey, string.Empty);
            if (!string.IsNullOrEmpty(customPath))
            {
                if (File.Exists(customPath))
                {
                    UnityEngine.Debug.Log($"[VideoPlayerShim] Custom YTDL location found: {customPath}");
                    SetupCallbacks();
                    return customPath;
                }

                UnityEngine.Debug.LogWarning($"[VideoPlayerShim] Custom YTDL location found but does not exist: {customPath}");
                UnityEngine.Debug.Log("[VideoPlayerShim] Checking other locations...");
            }

#if UNITY_EDITOR_WIN
            // check for the default windows location in the VRChat tools directory
            string[] splitPath = Application.persistentDataPath.Split('/');
            string[] files = Directory.GetFiles(string.Join("\\", splitPath.Take(splitPath.Length - 2)) + @"\VRChat\VRChat\Tools");
            foreach (string file in files)
            {
                if (pattern.IsMatch(file))
                {
                    UnityEngine.Debug.Log($"[VideoPlayerShim] Default YTDL location found: {file}");
                    SetupCallbacks();
                    return file;
                }
            }
#endif
            // path not yet found, try to hunt for it via PATH search
            System.Diagnostics.Process ytdlHunt = new System.Diagnostics.Process();
            ytdlHunt.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ytdlHunt.StartInfo.CreateNoWindow = true;
            ytdlHunt.StartInfo.UseShellExecute = false;
            ytdlHunt.StartInfo.RedirectStandardOutput = true;
#if UNITY_EDITOR_WIN
            ytdlHunt.StartInfo.FileName = "where.exe";
#else
            ytdlHunt.StartInfo.FileName = "which";
#if UNITY_EDITOR_OSX
            // M-series Macs use a different location for Homebrew packages to prevent conflicts with x86_64 binaries.
            // As a result, ARM packages are found in the "/opt/homebrew/bin" location, which is not in PATH.
            // However, the VRChat World SDK cannot run in native ARM mode itself, due to a dependency.
            // As such, rather than test for ARM64, test for the "/opt/homebrew/bin" directory, and add to PATH if found.
            // Thanks Azi for this tip.
            if (System.IO.Directory.Exists("/opt/homebrew/bin")) {
                var environment = ytdlHunt.StartInfo.Environment;
                if (!environment.ContainsKey("PATH")) environment.Add("PATH", "");
                if (environment.TryGetValue("PATH", out var path))
                {
                    string[] pathList = { path, "/opt/homebrew/bin/" };
                    environment["PATH"] = string.Join(":", pathList);
                }
            }
#endif
#endif
            ytdlHunt.StartInfo.Arguments = string.Join(" ", possibleExecutableNames);
            ytdlHunt.Start();
            // wait no more than 5 seconds for the process to finish, though it should finish near instantly.
            ytdlHunt.WaitForExit(5000);
            var stdout = ytdlHunt.StandardOutput;
            List<string> lines = new List<string>();
            // grab all possible lines
            while (!stdout.EndOfStream) lines.Add(stdout.ReadLine());
            string resolved = "";
            // find the first possible name that exists across the output options
            // priority is handled by the ordering of the names array
            foreach (var possible in possibleExecutableNames)
            {
                var exeName = possible;
#if UNITY_EDITOR_WIN
                // windows always ends with exe
                exeName += ".exe";
#endif
                resolved = lines.FirstOrDefault(l => l.EndsWith(exeName));
                if (!string.IsNullOrEmpty(resolved)) break;
            }

            var info = string.IsNullOrEmpty(resolved)
                ? $"[VideoPlayerShim] Unable to find YTDL location in the PATH."
                : $"[VideoPlayerShim] YTDL location found in PATH: {resolved}";
            UnityEngine.Debug.Log(info);
            return resolved ?? ""; // don't ever return null
        }

        private static void SetupCallbacks()
        {
            BaseVRCVideoPlayer.InitializeBase = PrepareAutoplay;
            VRCUnityVideoPlayer.StartResolveURLCoroutine = ResolveURLCallback;
#if AVPRO_IMPORTED
            AVProMediaPlayerShim.StartResolveURLCoroutine = ResolveURLCallback;
#endif
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private static void PrepareAutoplay(BaseVRCVideoPlayer player)
        {
            VRCUrl url = null;
            bool autoplay = false;
            if (player is VRCUnityVideoPlayer unity)
            {
                var urlInfo = typeof(VRCUnityVideoPlayer).GetField("videoUrl", BindingFlags.Instance | BindingFlags.NonPublic);
                if (urlInfo != null) url = (VRCUrl)urlInfo.GetValue(unity);
                var autoplayInfo = typeof(VRCUnityVideoPlayer).GetField("autoPlay", BindingFlags.Instance | BindingFlags.NonPublic);
                if (autoplayInfo != null) autoplay = (bool)autoplayInfo.GetValue(unity);
            }
#if AVPRO_IMPORTED
            else if (player is VRCAVProVideoPlayer avpro)
            {
                url = avpro.VideoURL;
                autoplay = avpro.AutoPlay;
            }
#endif

            if (string.IsNullOrWhiteSpace(url?.Get())) return;
            if (autoplay) player.PlayURL(url);
            else player.LoadURL(url);
        }

        /// <summary>
        /// Cleans up any remaining YTDL processes from this play.
        /// In some cases VRC's YTDL has hung indefinitely eating CPU so this is a precaution against that potentially happening.
        /// </summary>
        /// <param name="change"></param>
        private static void PlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                foreach (var process in runningYTDLProcesses.Where(process => !process.HasExited))
                {
                    process.Close();
                }

                runningYTDLProcesses.Clear();

                // Apparently the URLResolveCoroutine will run after this method in some cases magically. So don't because the process will throw an exception.
                foreach (MonoBehaviour behaviour in registeredBehaviours)
                    behaviour.StopAllCoroutines();

                registeredBehaviours.Clear();
            }
        }

        static void ResolveURLCallback(VRCUrl url, int resolution, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            int e = SessionState.GetInt(forceVideoErrorKey, -1);
            if (e > -1)
            {
                errorCallback.Invoke((VideoError)e);
                return;
            }

            System.Diagnostics.Process ytdlProcess = new System.Diagnostics.Process();
            ytdlProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ytdlProcess.StartInfo.CreateNoWindow = true;
            ytdlProcess.StartInfo.UseShellExecute = false;
            ytdlProcess.StartInfo.RedirectStandardOutput = true;
            ytdlProcess.StartInfo.RedirectStandardError = true;
            ytdlProcess.StartInfo.FileName = youtubeDLPath;
            ytdlProcess.StartInfo.Arguments = $"--no-check-certificate --no-cache-dir --rm-cache-dir -f \"mp4[height<=?{resolution}]/best[height<=?{resolution}]\" --get-url \"{url}\"";

            Debug.Log($"[<color=#9C6994>Video Playback</color>] Attempting to resolve URL '{url}'");

            ytdlProcess.Start();
            runningYTDLProcesses.Add(ytdlProcess);

            ((MonoBehaviour)videoPlayer).StartCoroutine(URLResolveCoroutine(url.ToString(), ytdlProcess, videoPlayer, urlResolvedCallback, errorCallback));

            registeredBehaviours.Add((MonoBehaviour)videoPlayer);
        }

        private const string ErrorText = "ERROR:";
        private const string WarningText = "WARNING:";

        static IEnumerator URLResolveCoroutine(string originalUrl, System.Diagnostics.Process ytdlProcess, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            // only process https links through YTDL.
            if (!originalUrl.StartsWith("https://"))
            {
                urlResolvedCallback(originalUrl);
                yield return null;
            }

            while (!ytdlProcess.HasExited)
                yield return new WaitForSeconds(0.1f);

            runningYTDLProcesses.Remove(ytdlProcess);

            // STDOUT handles the resulting URL
            var stdout = ytdlProcess.StandardOutput;
            // STDERR handles any failure messages
            var stderr = ytdlProcess.StandardError;
            string line = "";
            bool foundError = false;
            // check for all possible errors
            while (!stderr.EndOfStream)
            {
                line = stderr.ReadLine();
                // skip empty lines
                if (string.IsNullOrWhiteSpace(line)) continue;
                foundError = line.StartsWith(ErrorText);
                if (foundError) break;
            }

            if (foundError)
            {
                Debug.LogError($"[<color=#9C6994>Video Playback</color>] {line.Substring(ErrorText.Length)}");
                errorCallback(VideoError.PlayerError);
                yield return null;
            }

            while (!foundError && !stdout.EndOfStream)
            {
                line = stdout.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                // only care about the first resolved URL
                if (line.StartsWith("https://")) break;
            }

            // Valid URL was found
            if (!string.IsNullOrWhiteSpace(line))
            {
                Debug.Log($"[<color=#9C6994>Video Playback</color>] URL '{originalUrl}' resolved to '{line}'");
                urlResolvedCallback(line);
            }
            else
            {
                // this usually shouldn't be reached but just in case...
                Debug.LogError($"[<color=#9C6994>Video Playback</color>] Failed to resolved URL '{originalUrl}'. No error detected.");
                errorCallback(VideoError.InvalidURL);
            }
        }
    }
}