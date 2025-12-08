using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ArchiTech.VideoPlayerShim.ImportHandler
{
    #region Scripting Define Control

    internal static class AVProImportHandler
    {
        private const string vrchatAVProVersion = "2.8.5";

        // private const string vrchatAVProVersion = "3.0.0";
        private const string filePathCheck = "Assets/AVProVideo/Runtime/Scripts/Internal/Helper.cs";
        private const string guidCheck = "79e446998599e1647804321292c80f42";
        private const string avproPackageSrc = "https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases/download/{0}/UnityPlugin-AVProVideo-v{0}-Trial.unitypackage";
        private const string scriptingDefineSymbol = "AVPRO_IMPORTED";
        private const string scriptingDefineSymbolV2 = "AVPRO_V2";
        private const string scriptingDefineSymbolV3 = "AVPRO_V3";
        private const string doAVProImportPromptKey = "VideoPlayerShim-DoAVProImport";
        private const string skipAVProDefineChecks = "VideoPlayerShim-SkipAVProDefineChecks";
        private const string importAVProMenu = "Tools/VideoPlayerShim/Import AVPro";

        private static bool _hasCheckedDefines = false;
        private static readonly Regex versionPattern = new Regex("public +const +string +AVProVideoVersion *= *\"([a-zA-Z0-9_.]+)\";");

        private static string avproVersionCache = null;

        public static bool IsAVProPresent
        {
            get
            {
                // unmodified path check
                if (File.Exists(filePathCheck)) return true;
                // GUID check for when people move assets into other folders, file check to ensure the guid is a valid file as well.
                var guidPath = AssetDatabase.GUIDToAssetPath(guidCheck);
                return guidPath.Contains("AVProVideo") && File.Exists(guidPath);
            }
        }

        public static bool IsAVProVersion2 => MatchesAVProVersion("2");
        public static bool IsAVProVersion3 => MatchesAVProVersion("3");

        private static string AVProVersion
        {
            get
            {
                if (avproVersionCache != null) return avproVersionCache;
                if (!File.Exists(filePathCheck)) return null;
                string helperInfo = File.ReadAllText(filePathCheck);
                var match = versionPattern.Match(helperInfo);
                var versionCapture = match.Groups[1];
                avproVersionCache = versionCapture?.Value ?? "";
                return avproVersionCache;
            }
        }

        public static bool MatchesAVProVersion(string targetVersion)
        {
            var version = AVProVersion;
#if VPM_RESOLVER
            return SemanticVersioning.Range.IsSatisfied(">=" + targetVersion, version, true);
#else
            return version.StartsWith(targetVersion);
#endif
        }

        [InitializeOnLoadMethod]
        private static void HandleAVProImportPrep()
        {
            if (!IsAVProPresent)
            {
                if (AVProImportPrefs.GetInt(doAVProImportPromptKey) == 0)
                {
                    var dialogChoice = EditorUtility.DisplayDialog(
                        "AVPro Trial Importer",
                        "AVPro is currently not detected in the project. Would you like to download and import the trial package to enable testing in playmode?",
                        "Yes, Import",
                        "No, Skip"
                    );
                    AVProImportPrefs.SetInt(doAVProImportPromptKey, dialogChoice ? 1 : 2);
                }
            }
            else AVProImportPrefs.SetInt(doAVProImportPromptKey, 3);

            EditorApplication.projectChanged -= OnProjectChange;
            EditorApplication.projectChanged += OnProjectChange;
            AssetDatabase.importPackageStarted -= OnPackageImportStart;
            AssetDatabase.importPackageStarted += OnPackageImportStart;
            AssetDatabase.importPackageCompleted += OnPackageImportComplete;
            AssetDatabase.importPackageCompleted += OnPackageImportComplete;
            AssetDatabase.importPackageCancelled -= OnPackageImportCancel;
            AssetDatabase.importPackageCancelled += OnPackageImportCancel;
            AssetDatabase.importPackageFailed -= OnPackageImportFail;
            AssetDatabase.importPackageFailed += OnPackageImportFail;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnProjectChange()
        {
            if (!IsAVProPresent && AVProImportPrefs.GetInt(doAVProImportPromptKey, 0) == 3)
                _hasCheckedDefines = false;
        }

        private static void OnPackageImportStart(string packageName)
        {
            if (packageName.Contains("AVPro")) AVProImportPrefs.SetBool(skipAVProDefineChecks, true);
        }

        private static void OnPackageImportComplete(string packageName)
        {
            if (packageName.Contains("AVPro"))
            {
                UnityEngine.Debug.Log($"{packageName} imported successfully.");
                PurgeAVProDefines();
                RefreshAVProDefines();
            }
        }

        private static void OnPackageImportCancel(string packageName)
        {
            if (packageName.Contains("AVPro")) RefreshAVProDefines();
        }

        private static void OnPackageImportFail(string packageName, string err)
        {
            if (packageName.Contains("AVPro"))
            {
                UnityEngine.Debug.Log($"{packageName} failed to import: {err}");
                RefreshAVProDefines();
            }
        }

        private static void PurgeAVProDefines()
        {
            removeScriptingDefine(scriptingDefineSymbol);
            removeScriptingDefine(scriptingDefineSymbolV2);
            removeScriptingDefine(scriptingDefineSymbolV3);
        }

        private static void RefreshAVProDefines()
        {
            AVProImportPrefs.DeleteKey(skipAVProDefineChecks);
            _hasCheckedDefines = false;
            HandleAVProImportPrep();
        }

        private static void OnEditorUpdate()
        {
            if (AVProImportPrefs.GetBool(skipAVProDefineChecks)) return;

            if (_hasCheckedDefines || EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            if (AVProImportPrefs.GetInt(doAVProImportPromptKey, 0) == 1)
            {
                HandleAVProImport();
                return;
            }

            avproVersionCache = null;
            if (IsAVProPresent) addScriptingDefine(scriptingDefineSymbol);
            else removeScriptingDefine(scriptingDefineSymbol);
            if (IsAVProVersion2) addScriptingDefine(scriptingDefineSymbolV2);
            else removeScriptingDefine(scriptingDefineSymbolV2);
            if (IsAVProVersion3) addScriptingDefine(scriptingDefineSymbolV3);
            else removeScriptingDefine(scriptingDefineSymbolV3);
            _hasCheckedDefines = true;
        }

        private static void HandleAVProImport()
        {
            AVProImportPrefs.GetInt(doAVProImportPromptKey, 3);
            AVProImportPrefs.DeleteKey(skipAVProDefineChecks);
            var pkgUrl = string.Format(avproPackageSrc, vrchatAVProVersion);
            UnityEngine.Networking.UnityWebRequest www = new UnityEngine.Networking.UnityWebRequest(pkgUrl);
            var cacheFile = Application.temporaryCachePath + $"/UnityPlugin-AVProVideo-v{vrchatAVProVersion}-Trial.unitypackage";
            www.downloadHandler = new DownloadHandlerFile(cacheFile);
            UnityEngine.Debug.Log($"Downloading AVPro Trial {vrchatAVProVersion}...\n{pkgUrl}");
            var req = www.SendWebRequest();
            req.completed += op =>
            {
                if (!File.Exists(cacheFile))
                {
                    UnityEngine.Debug.LogError("AVPro Trial Package download failed.");
                    return;
                }

                UnityEngine.Debug.Log($"AVPro Trial {vrchatAVProVersion} downloaded. Importing...");
                AssetDatabase.ImportPackage(cacheFile, false);
                AssetDatabase.Refresh();
            };
        }

        private static bool hasScriptingDefine(string name)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
            return defines.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private static void addScriptingDefine(string name)
        {
            if (!hasScriptingDefine(name))
            {
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
                defines = defines.Append(name).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
            }
        }

        private static void removeScriptingDefine(string name)
        {
            if (hasScriptingDefine(name))
            {
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
                defines = defines.Where(s => s != name).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
            }
        }

        [MenuItem(importAVProMenu, false, 0)]
        private static void DoImport() => HandleAVProImport();

        [MenuItem(importAVProMenu, true, 0)]
        private static bool verifyDoImport()
        {
            Menu.SetChecked(importAVProMenu, MatchesAVProVersion(vrchatAVProVersion));
            return true;
        }
    }

    #endregion
}