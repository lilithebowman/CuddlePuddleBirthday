using System;
using UnityEditor;
using VRC.SDK3.Components.Video;

namespace ArchiTech.VideoPlayerShim
{
    public static class ShimMenus
    {
        internal const string userDefinedYTDLPathMenu = "Tools/VideoPlayerShim/Select Custom YTDL Install";
        internal const string forceVideoErrorMenu = "Tools/VideoPlayerShim/Force Video Error/";

        [MenuItem(userDefinedYTDLPathMenu, priority = 1)]
        private static void SelectYTDLInstall()
        {
            var ytdlPath = EditorPrefs.GetString(PlayModeUrlResolverShim.userDefinedYTDLPathKey, "");
            var tpath = ytdlPath.Substring(0, ytdlPath.LastIndexOf("/", StringComparison.Ordinal) + 1);
            var path = EditorUtility.OpenFilePanel("Select YTDL Install", tpath, "");
            EditorPrefs.SetString(PlayModeUrlResolverShim.userDefinedYTDLPathKey, path ?? string.Empty);
        }

        [MenuItem(userDefinedYTDLPathMenu, true, priority = 1)]
        private static bool validateSelectYTDLInstall()
        {
            Menu.SetChecked(userDefinedYTDLPathMenu, EditorPrefs.GetString(PlayModeUrlResolverShim.userDefinedYTDLPathKey, string.Empty) != string.Empty);
            return true;
        }

        [MenuItem(forceVideoErrorMenu + "Unknown", false, priority = 10)]
        private static void ForceErrorUnknown() => ToggleForceErrorOption(VideoError.Unknown);

        [MenuItem(forceVideoErrorMenu + "Unknown", true, priority = 10)]
        private static bool validateForceErrorUnknown() => UpdateForceErrorMenu(VideoError.Unknown);

        [MenuItem(forceVideoErrorMenu + "InvalidURL", false, priority = 11)]
        private static void ForceErrorInvalidURL() => ToggleForceErrorOption(VideoError.InvalidURL);

        [MenuItem(forceVideoErrorMenu + "InvalidURL", true, priority = 11)]
        private static bool validateForceErrorInvalidURL() => UpdateForceErrorMenu(VideoError.InvalidURL);

        [MenuItem(forceVideoErrorMenu + "AccessDenied", false, priority = 12)]
        private static void ForceErrorAccessDenied() => ToggleForceErrorOption(VideoError.AccessDenied);

        [MenuItem(forceVideoErrorMenu + "AccessDenied", true, priority = 12)]
        private static bool validateForceErrorAccessDenied() => UpdateForceErrorMenu(VideoError.AccessDenied);

        [MenuItem(forceVideoErrorMenu + "PlayerError", false, priority = 13)]
        private static void ForceErrorPlayerError() => ToggleForceErrorOption(VideoError.PlayerError);

        [MenuItem(forceVideoErrorMenu + "PlayerError", true, priority = 13)]
        private static bool validateForceErrorPlayerError() => UpdateForceErrorMenu(VideoError.PlayerError);

        [MenuItem(forceVideoErrorMenu + "RateLimited", false, priority = 14)]
        private static void ForceErrorRateLimited() => ToggleForceErrorOption(VideoError.RateLimited);

        [MenuItem(forceVideoErrorMenu + "RateLimited", true, priority = 14)]
        private static bool validateForceErrorRateLimited() => UpdateForceErrorMenu(VideoError.RateLimited);

        private static void ToggleForceErrorOption(VideoError error)
        {
            int target = (int)error;
            int e = SessionState.GetInt(PlayModeUrlResolverShim.forceVideoErrorKey, -1);
            if (target == e) target = -1;
            SessionState.SetInt(PlayModeUrlResolverShim.forceVideoErrorKey, target);
        }

        private static bool UpdateForceErrorMenu(VideoError error)
        {
            Menu.SetChecked(
                forceVideoErrorMenu + Enum.GetName(typeof(VideoError), error),
                SessionState.GetInt(PlayModeUrlResolverShim.forceVideoErrorKey, -1) == (int)error
            );
            return true;
        }
    }
}