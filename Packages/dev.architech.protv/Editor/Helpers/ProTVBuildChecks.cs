using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using VRC.Core;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.Udon;

#pragma warning disable CS0618
#pragma warning disable CS0612

#if AUDIOLINK_0 && !AUDIOLINK_1
using AudioLink = VRCAudioLink;
#endif

// ReSharper disable MemberCanBeMadeStatic.Local

namespace ArchiTech.ProTV.Editor
{
    internal class ProTVBuildProcess : IVRCSDKBuildRequestedCallback
    {
        private static ProTVBuildChecks checks;
        private static bool checksPassed;

        public int callbackOrder => -100;

        // Triggers checks for Playmode
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnPreprocessBuild()
        {
            if (!BuildPipeline.isBuildingPlayer) Build();
        }

        // Triggers checks for VRC Builds
        // Happens immediately on Build button click before any scene building happens.
        // Changes will persist.
        public bool OnBuildRequested(VRCSDKRequestedBuildType type)
        {
            // ignore helpers when building avatars, though I have no idea why you'd import ProTV into an avatar project...
            return type == VRCSDKRequestedBuildType.Avatar || Build();
        }

        // Triggers cleanup for both VRC Builds and Playmode
        [PostProcessScene(-100)]
        private static void ProcessScene()
        {
            if (!BuildPipeline.isBuildingPlayer) PostBuild();
        }

        private static bool Build()
        {
            checks = new ProTVBuildChecks();
            try
            {
                checksPassed = checks.RunChecks();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                checksPassed = UnityEditor.EditorUtility.DisplayDialog(
                    "Unexpected Error",
                    $"An unexpected error occurred during the ProTV Build Checks. Check the log for the exception and report to the ProTV dev team if needed.\n \n{e}",
                    "Continue Uploading", "Stop Build"
                );
            }

            return checksPassed;
        }

        private static void PostBuild()
        {
            try
            {
                checks?.Cleanup();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                UnityEditor.EditorUtility.DisplayDialog(
                    "Unexpected Error",
                    $"An unexpected error occurred during the ProTV Post Build Checks. Check the log for the exception and report to the ProTV dev team if needed.\n \n{e}",
                    "Continue"
                );
            }
        }
    }

    public class ProTVBuildChecks
    {
        private static readonly string[] knownMainInputNames = { "MainUrl", "PcUrl", "MediaInput" };
        private static readonly string[] knownAltInputNames = { "AltUrl", "QuestUrl", "AltInput" };

        private GameObject[] roots;
        private TVManager[] tvs;
        private VPManager[] videoManagers;
        private TVPlugin[] plugins;
        private TVPluginUI[] uiPlugins;
        private TVAuthPlugin[] authPlugins;
        private TVManagedWhitelist[] managedWhitelists;
        private TVManagedWhitelistUI[] managedWhitelistUIs;
        private MediaControls[] controls;
        private QuickPlay[] quickPlays;
        private Playlist[] playlists;
        private PlaylistUI[] playlistUIs;
        private Queue[] queues;
        private QueueUI[] queueUIs;
        private AudioAdapter[] audioAdapters;
        private SkyboxSwapper[] skyboxSwappers;
        private History[] historys;
        private HistoryUI[] historyUIs;
        private VRCUiShape[] uiShapes;
        private ProTVBuildLog log;
        private readonly HashSet<UnityEngine.Object> modifiedObjects = new HashSet<UnityEngine.Object>();

        internal void InitData()
        {
            roots = SceneManager.GetActiveScene().GetRootGameObjects();
            List<TVManager> tvsList = new List<TVManager>();
            List<VPManager> vpsList = new List<VPManager>();
            List<TVPlugin> pluginsList = new List<TVPlugin>();
            List<TVPluginUI> uiPluginsList = new List<TVPluginUI>();
            List<TVAuthPlugin> authPluginsList = new List<TVAuthPlugin>();
            List<VRCUiShape> uiShapesList = new List<VRCUiShape>();
            foreach (GameObject root in roots)
            {
                // grab all necessary component references
                tvsList.AddRange(root.GetComponentsInChildren<TVManager>(true));
                vpsList.AddRange(root.GetComponentsInChildren<VPManager>(true));
                pluginsList.AddRange(root.GetComponentsInChildren<TVPlugin>(true));
                uiPluginsList.AddRange(root.GetComponentsInChildren<TVPluginUI>(true));
                authPluginsList.AddRange(root.GetComponentsInChildren<TVAuthPlugin>(true));
                uiShapesList.AddRange(root.GetComponentsInChildren<VRCUiShape>(true));
            }

            tvs = tvsList.ToArray();
            videoManagers = vpsList.ToArray();
            plugins = pluginsList.ToArray();
            uiPlugins = uiPluginsList.ToArray();
            authPlugins = authPluginsList.ToArray();
            managedWhitelists = authPluginsList.OfType<TVManagedWhitelist>().ToArray();
            managedWhitelistUIs = uiPluginsList.OfType<TVManagedWhitelistUI>().ToArray();
            controls = pluginsList.OfType<MediaControls>().ToArray();
            quickPlays = pluginsList.OfType<QuickPlay>().ToArray();
            playlists = pluginsList.OfType<Playlist>().ToArray();
            playlistUIs = uiPluginsList.OfType<PlaylistUI>().ToArray();
            queues = pluginsList.OfType<Queue>().ToArray();
            queueUIs = uiPluginsList.OfType<QueueUI>().ToArray();
            audioAdapters = pluginsList.OfType<AudioAdapter>().ToArray();
            skyboxSwappers = pluginsList.OfType<SkyboxSwapper>().ToArray();
            historys = pluginsList.OfType<History>().ToArray();
            historyUIs = uiPluginsList.OfType<HistoryUI>().ToArray();
            uiShapes = uiShapesList.Where(uiShape => uiShape.GetComponentInParent<Canvas>(true)?.renderMode == RenderMode.WorldSpace).ToArray();
        }

        public bool RunChecks(bool isDry = false)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            log = ProTVBuildWindow.BuildLog;
            log.dry = isDry;
            log.Clear();
            InitData();

            SetupTVManagerRequirements();
            ConnectMissingTVReferences();
            CheckForMaterialAndTextureContamination();
            CheckAndFixBrokenVRCUrlInputFields();
            SetupVPManagerRequirements();
            UpdateVPSwitcherDropdowns();
            UpdateVersions();
            ConnectAudioLinkReferences();
            // plugins requirements
            ValidateMediaControlsSetupRequirements();
            ValidatePlaylistSetupRequirements();
            ValidateQueueSetupRequirements();
            ValidateSkyboxSwapperSetupRequirements();
            ValidateHistorySetupRequirements();
            ValidateAuthPluginSetupRequirements();

            ValidateDomainWhitelist();
            UpdateAutoplaySettings();
            FixUINavigations();
            FixStartPositionOfScrollbars();
            FixUiShapes();
            FixUiSliderFillImages();
            FixMissingTMPFonts();
            FixUSharpAssetReferences();
            FixTVGSVUnsetBugFromPreviousVersions();

            sw.Stop();
            log.lastExecutionTime = sw.ElapsedMilliseconds;
            UnityEngine.Debug.Log(I18n.Tr("ProTV Build Checks execution time") + $": {sw.ElapsedMilliseconds}ms");
            return CheckErrorsAndSave();
        }

        public bool Cleanup(bool isDry = false)
        {
            if (roots == null) InitData();
            // CleanPlaylistData();
            return true;
        }

        private void Error(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) => log.Error(scope, message, relatedScopes);
        private void Error(UnityEngine.Object scope, string message, string relatedData) => log.Error(scope, message, relatedData);
        private void Warning(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) => log.Warn(scope, message, relatedScopes);
        private void Warning(UnityEngine.Object scope, string message, string relatedData) => log.Warn(scope, message, relatedData);
        private void Info(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) => log.Info(scope, message, relatedScopes);
        private void Info(UnityEngine.Object scope, string message, string relatedData) => log.Info(scope, message, relatedData);

        private bool CheckErrorsAndSave()
        {
            if (log.Count(ATLogLevel.ERROR) > 0 && !ProTVBuildWindow.IsOpen())
            {
                // window is already open, don't need to prompt again
                if (EditorUtility.DisplayDialog(
                        I18n.Tr("ProTV Build Failures"),
                        I18n.Tr("Errors have occured during validation. Check log?"),
                        I18n.Tr("Open"),
                        I18n.Tr("Cancel")))
                    ProTVBuildWindow.Open();
                EditorApplication.ExitPlaymode();
                modifiedObjects.Clear();
                return false;
            }

            if (!log.dry)
            {
                foreach (var target in modifiedObjects.Where(target => target != null))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    EditorUtility.SetDirty(target);
                }
            }

            modifiedObjects.Clear();
            return true;
        }

        private void Save(UnityEngine.Object target) => modifiedObjects.Add(target);

        public void UpdateVersions()
        {
            UnityEditor.PackageManager.PackageInfo pkg = AssetDatabase
                .FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                .FirstOrDefault(x => x != null && x.name == ProTVEditorUtility.packageName);

            if (pkg == null)
            {
                Warning(null, I18n.Tr("The ProTV package.json file could not be found in the project. Unable to inject the version number into the scene."));
                return;
            }

            string versionNumber = pkg.version;

            foreach (GameObject root in roots)
            {
                Text[] possibles = root.GetComponentsInChildren<Text>(true);
                foreach (var possible in possibles)
                {
                    string possibleName = possible.gameObject.name.ToLower();
                    // loosely match the game object name to include 'protv' and 'version' anywhere in the name
                    if (possibleName.Contains("protv") && possibleName.Contains("version"))
                    {
                        Info(null, I18n.Tr("Updating version number in scene"));
                        if (!log.dry)
                        {
                            var vn = versionNumber;
                            if (possibleName.Contains("prefix")) vn = "ProTV " + vn;
                            if (possible.text != vn)
                            {
                                possible.text = vn;
                                Save(possible);
                            }
                        }
                    }
                }

                TMP_Text[] possiblesTMPUGUI = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var possible in possiblesTMPUGUI)
                {
                    string possibleName = possible.gameObject.name.ToLower();
                    // loosely match the game object name to include 'protv' and 'version' anywhere in the name
                    if (possibleName.Contains("protv") && possibleName.Contains("version"))
                    {
                        Info(null, I18n.Tr("Updating version number in scene"));
                        if (!log.dry)
                        {
                            var vn = versionNumber;
                            if (possibleName.Contains("prefix")) vn = "ProTV " + vn;
                            if (possible.text != vn)
                            {
                                possible.text = vn;
                                Save(possible);
                            }
                        }
                    }
                }
            }

            foreach (var tv in tvs)
            {
                if (versionNumber != tv.versionNumber)
                {
                    tv.versionNumber = versionNumber;
                    Save(tv);
                }
            }
        }

        public void SetupTVManagerRequirements()
        {
            TVManager gsvEnabled = null;
            var blitMat = AssetDatabase.LoadAssetAtPath<Material>(ProTVEditorUtility.blitMaterialPath);
            foreach (TVManager tv in tvs)
            {
                // Ensure the VideoManager list is populated correctly.
                var managers = tv.GetComponentsInChildren<VPManager>(true);
                var sizeCheckFailure = tv.videoManagers == null || tv.videoManagers.Length == 0 || managers.Length != tv.videoManagers.Length;
                if (sizeCheckFailure || !managers.SequenceEqual(tv.videoManagers))
                {
                    Info(tv, I18n.Tr("Updating the VPManagers list..."));
                    if (!log.dry)
                    {
                        int defaultManager = tv.defaultVideoManager;
                        if (defaultManager >= managers.Length) defaultManager = 0;
                        if (tv.videoManagers != null && tv.videoManagers.Length > 0)
                            defaultManager = System.Array.IndexOf(managers, tv.videoManagers[defaultManager]);
                        if (tv.defaultVideoManager == -1) defaultManager = 0;
                        tv.videoManagers = managers;
                        tv.defaultVideoManager = defaultManager;
                    }
                }

                // Ensure there is always a TVManagerData component as a direct child of the TVManager
                tv.syncData = tv.GetComponentInChildren<TVManagerData>(true);
                if (tv.syncData == null)
                {
                    Info(tv, I18n.Tr("TVManagerData missing. Adding..."));
                    if (!log.dry)
                    {
                        var go = new GameObject { name = "TVData" };
                        var t = tv.transform.Find("Internal");
                        if (t == null) t = tv.transform;
                        go.transform.SetParent(t, false);
                        tv.syncData = UdonSharpUndo.AddComponent<TVManagerData>(go);
                        Save(go);
                    }
                }

                // Ensure the blit material for the custom render texture is assigned
                if (tv.blitMaterial == null) tv.blitMaterial = blitMat;
                if (tv.enableGSV)
                {
                    if (gsvEnabled == null) gsvEnabled = tv;
                    else Error(tv, I18n.Tr("Global Shader Variables are enabled for multiple TVs. Ensure only one TV has them active."), gsvEnabled);
                }

                if (tv.customMaterials == null) tv.customMaterials = new Material[0];
                if (tv.customMaterialProperties == null) tv.customMaterialProperties = new string[0];
                if (tv.customMaterial != null)
                {
                    var validMats = tv.customMaterials.Count(material => material != null);
                    if (validMats == 0)
                    {
                        tv.customMaterials = new[] { tv.customMaterial };
                        var prop = string.IsNullOrWhiteSpace(tv.customMaterialProperty) ? "_VideoTex" : tv.customMaterialProperty;
                        tv.customMaterialProperties = new[] { prop };
                    }

                    tv.customMaterial = null;
                    tv.customMaterialProperty = null;
                }

                if (tv.authPlugin == null)
                {
                    var auth = tv.GetComponentInChildren<TVAuthPlugin>(true);
                    if (auth != null)
                    {
                        Info(tv, I18n.Tr("Assigning detected Auth Plugin"), auth);
                        if (!log.dry)
                        {
                            auth.tv = tv;
                            tv.authPlugin = auth;
                            Save(auth);
                        }
                    }
                }
                else if (tv.authPlugin.tv != tv)
                {
                    Info(tv, I18n.Tr("Fixing TV reference for Auth Plugin"), tv.authPlugin);
                    if (!log.dry)
                    {
                        tv.authPlugin.tv = tv;
                        Save(tv.authPlugin);
                    }
                }

                Save(tv);
            }
        }

        public void SetupVPManagerRequirements()
        {
            // use reflection to extract the reference to the audio source array because it's normally private
            var targetAudioSourcesInfo = typeof(VRCUnityVideoPlayer).GetField("targetAudioSources", BindingFlags.Instance | BindingFlags.NonPublic);
            List<VRCAVProVideoSpeaker> avproSpeakers = new List<VRCAVProVideoSpeaker>();
            foreach (GameObject root in roots)
            {
                var rawSpeakers = root.GetComponentsInChildren<VRCAVProVideoSpeaker>(true);
                foreach (var s in rawSpeakers)
                {
                    if (s.VideoPlayer == null)
                        Warning(s, "Speaker is missing the AVPro Video Player reference. This is generally undesired and should be fixed if unintentional.");
                    if (
                        s.GetComponent<AudioLowPassFilter>()
                        || s.GetComponent<AudioHighPassFilter>()
                        || s.GetComponent<AudioReverbFilter>()
                        || s.GetComponent<AudioEchoFilter>()
                        || s.GetComponent<AudioDistortionFilter>()
                        || s.GetComponent<AudioChorusFilter>()
                    ) Error(s, "AVPro speakers currently do NOT support audio filters with VRChat. Remove the component.");
                }

                avproSpeakers.AddRange(rawSpeakers);
            }

            foreach (VPManager manager in videoManagers)
            {
                var vp = manager.GetComponent<BaseVRCVideoPlayer>();
                // if speakers are missing entirely, populate with all possible audio sources
                if (manager.spatialSpeakers == null)
                {
                    var speakers = new AudioSource[0];
                    if (vp is VRCAVProVideoPlayer)
                    {
                        speakers = avproSpeakers.Where(s => s.VideoPlayer == vp).Select(s => s.GetComponent<AudioSource>()).ToArray();
                    }
                    else if (vp is VRCUnityVideoPlayer)
                    {
                        var sources = (AudioSource[])targetAudioSourcesInfo?.GetValue(vp);
                        sources = sources?.Where(s => s != null).ToArray();
                        speakers = sources ?? new AudioSource[0];
                    }

                    if (!log.dry)
                    {
                        manager.spatialSpeakers = speakers;
                        Save(manager);
                    }

                    Info(manager, I18n.Tr("Populating missing speakers..."));
                }
                else
                {
                    if (vp is VRCUnityVideoPlayer)
                    {
                        var speakers = (AudioSource[])targetAudioSourcesInfo?.GetValue(vp);
                        speakers = speakers?.Where(s => s != null).ToArray();
                        if (speakers == null || speakers.Length == 0)
                        {
                            if (manager.spatialSpeakers.Length > 0) speakers = new[] { manager.spatialSpeakers[0] };
                            targetAudioSourcesInfo?.SetValue(vp, speakers);
                            Save(vp);
                        }
                    }
                }

                if (!log.dry)
                {
                    // if screens are missing entirely, populate with an empty array
                    var tv = ProTVEditorUtility.FindParentTVManager(manager);

                    if (tv == null)
                    {
                        Error(manager, I18n.Tr("This VPManager object is not part of a TVManager. Remove the component, delete the object, or move the object to be a child of a TVManager."));
                        continue;
                    }

                    if (manager.screens == null) manager.screens = new GameObject[0];
                    if (string.IsNullOrWhiteSpace(manager.customLabel)) manager.customLabel = manager.gameObject.name;

                    var implicitRenderer = manager.GetComponent<MeshRenderer>();
                    if (implicitRenderer == null) implicitRenderer = Undo.AddComponent<MeshRenderer>(manager.gameObject);
                    manager.matRenderer = implicitRenderer;
                    bool isImplicitTestMaterial = implicitRenderer.sharedMaterial != null && implicitRenderer.sharedMaterial.name.StartsWith("_Test");

                    if (vp is VRCAVProVideoPlayer avpro)
                    {
                        manager.IsAVPro = true;
                        var implicitScreen = manager.GetComponent<VRCAVProVideoScreen>();

                        if (implicitScreen == null) implicitScreen = ProTVEditorUtility.AddAVProVideoScreen(manager.gameObject, avpro);

                        var videoPlayerInfo = implicitScreen.GetType().GetField("videoPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (implicitScreen.VideoPlayer != avpro) videoPlayerInfo.SetValue(implicitScreen, avpro);

                        if (!isImplicitTestMaterial)
                        {
                            var matIndexInfo = implicitScreen.GetType().GetField("materialIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                            var sharedMatInfo = implicitScreen.GetType().GetField("useSharedMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
                            var matTexInfo = implicitScreen.GetType().GetField("textureProperty", BindingFlags.Instance | BindingFlags.NonPublic);

                            if (matIndexInfo != null) matIndexInfo.SetValue(implicitScreen, 0);
                            if (sharedMatInfo != null) sharedMatInfo.SetValue(implicitScreen, false);
                            if (matTexInfo != null) matTexInfo.SetValue(implicitScreen, "_MainTex");
                        }

                        Save(implicitScreen);

                        var _screens = ProTVEditorUtility.GetScreensForManager(avpro);
                        foreach (var screen in _screens)
                        {
                            var renderer = screen.GetComponent<MeshRenderer>();
                            var mat = renderer.sharedMaterials[screen.MaterialIndex];
                            // explicitly disallow the internal protv shaders from the materials list.
                            if (mat != null && !mat.name.StartsWith("_Test") && !mat.shader.name.StartsWith("Hidden/ProTV"))
                            {
                                var tex = mat.shader.name.StartsWith("ProTV") ? "_VideoTex" : screen.TextureProperty;
                                if (Array.IndexOf(tv.customMaterials, mat) == -1)
                                {
                                    tv.customMaterials = tv.customMaterials.Append(mat).ToArray();
                                    tv.customMaterialProperties = tv.customMaterialProperties.Append(tex).ToArray();
                                    Save(tv);
                                }
                            }

                            if (screen != implicitScreen) UnityEngine.Object.DestroyImmediate(screen);
                        }
                    }
                    else if (vp is VRCUnityVideoPlayer unityPlayer)
                    {
                        manager.IsAVPro = false;

                        // in order to correctly access the fields on the unityPlayer proxy, we need to extract the info via reflection
                        // if we used unityPlayer.VideoPlayer, it would instantiate a VideoPlayer component, which is an undesired side-effect
                        var renderModeInfo = unityPlayer.GetType().GetField("renderMode", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (renderModeInfo == null) return;
                        var targetTextureInfo = unityPlayer.GetType().GetField("targetTexture", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (targetTextureInfo == null) return;
                        var targetMatRendererInfo = unityPlayer.GetType().GetField("targetMaterialRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (targetMatRendererInfo == null) return;
                        var targetMatPropertyInfo = unityPlayer.GetType().GetField("targetMaterialProperty", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (targetMatPropertyInfo == null) return;
                        var aspectRatioInfo = unityPlayer.GetType().GetField("aspectRatio", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (aspectRatioInfo == null) return;


                        int renderMode = (int)renderModeInfo.GetValue(unityPlayer);
                        Renderer renderer = (Renderer)targetMatRendererInfo.GetValue(unityPlayer);
                        if (renderMode == 1 && renderer != implicitRenderer)
                        {
                            var mat = renderer.sharedMaterial;
                            if (mat != null && !mat.name.StartsWith("_Test") && !mat.shader.name.StartsWith("Hidden/ProTV"))
                            {
                                string tex = (string)targetMatPropertyInfo.GetValue(unityPlayer);
                                if (mat.shader.name.StartsWith("ProTV")) tex = "_VideoTex";
                                if (Array.IndexOf(tv.customMaterials, mat) == -1)
                                {
                                    tv.customMaterials = tv.customMaterials.Append(mat).ToArray();
                                    tv.customMaterialProperties = tv.customMaterialProperties.Append(tex).ToArray();
                                    Save(tv);
                                }
                            }
                        }

                        renderModeInfo.SetValue(unityPlayer, 1);
                        targetMatRendererInfo.SetValue(unityPlayer, implicitRenderer);
                        targetMatPropertyInfo.SetValue(unityPlayer, "_MainTex");
                        aspectRatioInfo.SetValue(unityPlayer, VideoAspectRatio.NoScaling);
                        Save(unityPlayer);

                        manager.matRenderer = implicitRenderer;
                    }

                    if (!isImplicitTestMaterial)
                    {
                        implicitRenderer.sharedMaterials = new Material[1];
                        implicitRenderer.enabled = false;
                    }

                    Save(implicitRenderer);

                    if (tv && tv.GetComponentsInChildren<VPManager>(true).Length > 1)
                        manager.gameObject.SetActive(false); // hide only if multiple managers found

                    // ensure the mute and volume flag arrays match the speaker list lengths
                    ensureSpeakerSettingLength(manager.spatialSpeakers, ref manager.managedSpatialMute, true);
                    ensureSpeakerSettingLength(manager.spatialSpeakers, ref manager.managedSpatialVolume, true);
                    ensureSpeakerSettingLength(manager.stereoSpeakers, ref manager.managedStereoMute, true);
                    ensureSpeakerSettingLength(manager.stereoSpeakers, ref manager.managedStereoVolume, true);

                    Save(manager);
                }

                if (manager.TryGetComponent(out MeshFilter mf))
                    Error(manager, I18n.Tr("Cannot have a MeshFilter on the VideoManager object. Move your MeshFilter to a separate game object with a different MeshRenderer on it."), mf);
            }
        }

        private void ensureSpeakerSettingLength(AudioSource[] speakers, ref bool[] setting, bool fill)
        {
            var stale = setting;
            int count = speakers.Length;
            if (setting == null) setting = new bool[0];
            if (setting.Length != count)
            {
                setting = new bool[count];
                var copySize = Math.Min(stale.Length, count);
                Array.Copy(stale, setting, copySize);
                for (int i = copySize; i < count; i++) setting[i] = fill;
            }
        }

        public void ConnectMissingTVReferences()
        {
            // Ensure any plugins that are a child of a given TV have a TV reference.
            foreach (TVPlugin plugin in plugins)
            {
                // If no reference and is a child, auto-assign the reference.
                // if no reference and not a child, dump a console warning about the reference being absent.
                if (plugin.tv == null)
                {
                    var pluginTV = plugin.GetComponentInParent<TVManager>();
                    if (pluginTV == null)
                    {
                        Warning(plugin, I18n.Tr("Could not find TV reference. Disregard if this is intentional."));
                        continue;
                    }

                    if (!log.dry)
                    {
                        plugin.tv = pluginTV;
                        Save(plugin);
                    }

                    Info(plugin, I18n.Tr("Connecting parent TV reference..."));
                }
            }
        }

        public void CheckForMaterialAndTextureContamination()
        {
            var materials = new Dictionary<(Material, string), TVManager>();
            var textures = new Dictionary<RenderTexture, TVManager>();
            bool textureDialogDisplayed = false;
            bool textureDialogValue = false;
            bool materialDialogDisplayed = false;
            bool materialDialogValue = false;
            foreach (TVManager tv in tvs)
            {
                var count = tv.customMaterials.Length;
                for (var index = 0; index < count; index++)
                {
                    var material = tv.customMaterials[index];
                    if (material == null) continue;
                    var prop = tv.customMaterialProperties[index];
                    var key = (material, prop);
                    if (materials.ContainsKey(key))
                    {
                        if (!materialDialogDisplayed)
                        {
                            materialDialogDisplayed = true;
                            materialDialogValue = EditorUtility.DisplayDialog(
                                I18n.Tr("TV Material Contamination"),
                                string.Format(I18n.Tr("Material on TV {0} with property '{1}' is being drawn to by other TVs. It needs separate materials (or change the property target) for each TV."), tv.gameObject.name, prop)
                                + "\n" + I18n.Tr("Automatically create necessary materials?"),
                                I18n.Tr("Yes"),
                                I18n.Tr("No")
                            );
                        }

                        if (materialDialogValue)
                        {
                            material = TVManagerEditor.ReplaceWithNewMaterial(tv, material);
                            key = (material, prop);
                            materials.Add(key, tv);
                        }
                        else
                        {
                            Error(material,
                                string.Format(I18n.Tr("Material on TV {0} with property '{1}' is being drawn to by other TVs. It needs separate materials (or change the property target) for each TV."), tv.gameObject.name, prop),
                                tv, materials[key]
                            );
                        }
                    }
                    else materials.Add(key, tv);
                }

                if (tv.customTexture == null) continue;
                if (textures.ContainsKey(tv.customTexture))
                {
                    if (!textureDialogDisplayed && !log.dry)
                    {
                        textureDialogDisplayed = true;
                        textureDialogValue = EditorUtility.DisplayDialog(
                            I18n.Tr("TV RenderTexture Contamination"),
                            string.Format(I18n.Tr("Custom Texture on TV {0} is being drawn to by multiple TVs. It's recommended to create separate RenderTextures for each TV unless you know what you are doing."), tv.gameObject.name)
                            + "\n" + I18n.Tr("If this is intentional, you can click continue."),
                            I18n.Tr("Continue"),
                            I18n.Tr("Go Back"),
                            DialogOptOutDecisionType.ForThisMachine,
                            ProTVEditorPrefs.GetKey(ProTVEditorPrefs.SkipTextureContaminationPrompt)
                        );
                    }

                    if (textureDialogValue || log.dry && ProTVEditorPrefs.GetBool(ProTVEditorPrefs.SkipTextureContaminationPrompt, false))
                    {
                        Warning(tv.customTexture,
                            I18n.Tr("Custom Texture is being drawn to by multiple TVs. It's recommended to create separate RenderTextures for each TV unless you know what you are doing."),
                            tv, textures[tv.customTexture]
                        );
                    }
                    else
                    {
                        Error(tv.customTexture,
                            I18n.Tr("Custom Texture is being drawn to by multiple TVs. It's recommended to create separate RenderTextures for each TV unless you know what you are doing."),
                            tv, textures[tv.customTexture]
                        );
                    }
                }
                else textures.Add(tv.customTexture, tv);
            }
        }

        public void ConnectAudioLinkReferences()
        {
#if AUDIOLINK_0 || AUDIOLINK_1
            if (audioAdapters.Length == 0) return;
            AudioLink.AudioLink audioLink = null;
            foreach (var root in roots)
            {
                audioLink = root.GetComponentInChildren<AudioLink.AudioLink>(true);
                if (audioLink != null) break;
            }

            if (!audioAdapters.Any(adapter => adapter.enableAudioLink)) return; // no adapters are looking for an audiolink instance so skip the rest.
            if (audioLink == null) Error(null, I18n.Tr("No AudioLink instance could be found in the scene."));
#if AUDIOLINK_1
            else
            {
                // ProTV expects to be setting the media state, so disable the auto assignment of the values.
                audioLink.autoSetMediaState = false;
                Save(audioLink);
            }
#endif
            foreach (AudioAdapter adapter in audioAdapters)
            {
                if (adapter.audioLinkInstance != audioLink)
                {
                    if (!log.dry)
                    {
                        adapter.audioLinkInstance = audioLink;
                        Save(adapter);
                    }

                    Info(audioLink, I18n.Tr("Connecting AudioLink instance to adapters..."), adapter);
                }
            }
#endif
        }

        public void ValidateMediaControlsSetupRequirements()
        {
            foreach (MediaControls control in controls)
            {
                if (control.mainUrlInput != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.mainUrlInput, control.mainUrlInput.onValueChanged, control.ChangeMedia);
                    ATEditorUtility.RemoveSelectableActionEvent(control.mainUrlInput, control.mainUrlInput.onEndEdit, control.ChangeMedia);
                    ATEditorUtility.RemoveSelectableActionEvent(control.mainUrlInput, control.mainUrlInput.onValueChanged, control._UpdateUrlInput);
                    ATEditorUtility.RemoveSelectableActionEvent(control.mainUrlInput, control.mainUrlInput.onEndEdit, control._EndEditUrlInput);
                    handleSelectableActionEvent(control.mainUrlInput, control.mainUrlInput.onValueChanged, control.UpdateUrlInput);
                    handleSelectableActionEvent(control.mainUrlInput, control.mainUrlInput.onEndEdit, control.EndEditUrlInput);
                }

                if (control.alternateUrlInput != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.alternateUrlInput, control.alternateUrlInput.onValueChanged, control.ChangeMedia);
                    ATEditorUtility.RemoveSelectableActionEvent(control.alternateUrlInput, control.alternateUrlInput.onEndEdit, control.ChangeMedia);
                    ATEditorUtility.RemoveSelectableActionEvent(control.alternateUrlInput, control.alternateUrlInput.onValueChanged, control._UpdateUrlInput);
                    ATEditorUtility.RemoveSelectableActionEvent(control.alternateUrlInput, control.alternateUrlInput.onEndEdit, control._EndEditUrlInput);
                    handleSelectableActionEvent(control.alternateUrlInput, control.alternateUrlInput.onValueChanged, control.UpdateUrlInput);
                    handleSelectableActionEvent(control.alternateUrlInput, control.alternateUrlInput.onEndEdit, control.EndEditUrlInput);
                }

                if (control.titleInput != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.titleInput, control.titleInput.onValueChanged, control._UpdateUrlInput);
                    handleSelectableActionEvent(control.titleInput, control.titleInput.onValueChanged, control.UpdateUrlInput);
                }

                if (control.sendInputs != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.sendInputs, control.sendInputs.onClick, control._ChangeMedia);
                    handleSelectableActionEvent(control.sendInputs, control.sendInputs.onClick, control.ChangeMedia);
                }

                if (control.urlSwitch != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.urlSwitch, control.urlSwitch.onValueChanged, control._ToggleUrlMode);
                    handleSelectableActionEvent(control.urlSwitch, control.urlSwitch.onValueChanged, control.ToggleUrlMode);
                }

                if (control.play != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.play, control.play.onClick, control._Play);
                    handleSelectableActionEvent(control.play, control.play.onClick, control.Play);
                }

                if (control.pause != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.pause, control.pause.onClick, control._Pause);
                    handleSelectableActionEvent(control.pause, control.pause.onClick, control.Pause);
                }

                if (control.stop != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.stop, control.stop.onClick, control._Stop);
                    handleSelectableActionEvent(control.stop, control.stop.onClick, control.Stop);
                }

                if (control.skip != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.skip, control.skip.onClick, control._Skip);
                    handleSelectableActionEvent(control.skip, control.skip.onClick, control.Skip);
                }

                if (control.reload != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.reload, control.reload.onClick, control._RefreshMedia);
                    handleSelectableActionEvent(control.reload, control.reload.onClick, control.RefreshMedia);
                }

                if (control.resync != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.resync, control.resync.onClick, control._ReSync);
                    handleSelectableActionEvent(control.resync, control.resync.onClick, control.ReSync);
                }

                if (control.seek != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.seek, control.seek.onValueChanged, control._Seek);
                    ATEditorUtility.RemoveSelectableActionEvent(control.seek, control.seek.onValueChanged, control.Seek);
                    handleSelectableActionEvent(control.seek, control.seek.onValueChanged, control.ChangeSeek);
                }

                if (control.playbackSpeed != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.playbackSpeed, control.playbackSpeed.onValueChanged, control._ChangePlaybackSpeed);
                    handleSelectableActionEvent(control.playbackSpeed, control.playbackSpeed.onValueChanged, control.ChangePlaybackSpeed);
                }

                if (control.seekOffset != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.seekOffset, control.seekOffset.onValueChanged, control._ChangeSeekOffset);
                    handleSelectableActionEvent(control.seekOffset, control.seekOffset.onValueChanged, control.ChangeSeekOffset);
                }

                if (control.volume != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.volume, control.volume.onValueChanged, control._ChangeVolume);
                    handleSelectableActionEvent(control.volume, control.volume.onValueChanged, control.ChangeVolume);
                }

                if (control.audioMode != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.audioMode, control.audioMode.onClick, control._ToggleAudioMode);
                    handleSelectableActionEvent(control.audioMode, control.audioMode.onClick, control.ToggleAudioMode);
                }

                if (control.colorSpaceCorrection != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.colorSpaceCorrection, control.colorSpaceCorrection.onClick, control._ToggleColorCorrection);
                    handleSelectableActionEvent(control.colorSpaceCorrection, control.colorSpaceCorrection.onClick, control.ToggleColorCorrection);
                }

                if (control.mode3dSwap != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.mode3dSwap, control.mode3dSwap.onValueChanged, control._Change3DMode);
                    handleSelectableActionEvent(control.mode3dSwap, control.mode3dSwap.onValueChanged, control.Change3DMode);
                }

                if (control.width3dMode != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.width3dMode, control.width3dMode.onClick, control._Toggle3DWidth);
                    handleSelectableActionEvent(control.width3dMode, control.width3dMode.onClick, control.Toggle3DWidth);
                }

                if (control.mute != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.mute, control.mute.onClick, control._ToggleMute);
                    handleSelectableActionEvent(control.mute, control.mute.onClick, control.ToggleMute);
                }

                if (control.tvLock != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.tvLock, control.tvLock.onClick, control._ToggleLock);
                    handleSelectableActionEvent(control.tvLock, control.tvLock.onClick, control.ToggleLock);
                }

                if (control.syncMode != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.syncMode, control.syncMode.onClick, control._ToggleSync);
                    handleSelectableActionEvent(control.syncMode, control.syncMode.onClick, control.ToggleSync);
                }

                if (control.loopMode != null)
                {
                    // old event(s)
                    ATEditorUtility.RemoveSelectableActionEvent(control.loopMode, control.loopMode.onClick, control._ToggleLoop);
                    handleSelectableActionEvent(control.loopMode, control.loopMode.onClick, control.ToggleLoop);
                }

                if (control.videoPlayerSwap != null)
                {
                    var swap = control.videoPlayerSwap;
                    ATEditorUtility.RemoveSelectableActionEvent(swap, swap.onValueChanged, control._ChangeVideoPlayer);
                    handleSelectableActionEvent(swap, swap.onValueChanged, control.ChangeVideoPlayer);
                    var tmpl = swap.template;
                    if (!log.dry)
                    {
                        if (!tmpl.TryGetComponent(out VRCUiShape _)) tmpl.gameObject.AddComponent<VRCUiShape>();
                        if (!swap.TryGetComponent(out TVDropdownFix _)) UdonSharpUndo.AddComponent<TVDropdownFix>(swap.gameObject);
                        Save(tmpl.gameObject);
                        Save(swap.gameObject);
                    }
                }

                if (control.currentTimeDisplay != null)
                {
                    var btn = control.currentTimeDisplay.GetComponent<Button>();
                    if (btn != null)
                    {
                        // old event(s)
                        ATEditorUtility.RemoveSelectableActionEvent(btn, btn.onClick, control._ToggleCurrentRemainingTime);
                        handleSelectableActionEvent(btn, btn.onClick, control.ToggleCurrentRemainingTime);
                    }
                }

                if (control.currentTimeDisplayTMP != null)
                {
                    var btn = control.currentTimeDisplayTMP.GetComponent<Button>();
                    if (btn != null)
                    {
                        // old event(s)
                        ATEditorUtility.RemoveSelectableActionEvent(btn, btn.onClick, control._ToggleCurrentRemainingTime);
                        handleSelectableActionEvent(btn, btn.onClick, control.ToggleCurrentRemainingTime);
                    }
                }
            }
        }

        public void ValidatePlaylistSetupRequirements()
        {
            foreach (var playlist in playlists)
            {
                if (!log.dry)
                {
                    if (playlist.GetComponentInChildren<PlaylistRPC>(true) == null)
                    {
                        var obj = new GameObject("PlaylistRPC");
                        obj.transform.SetParent(playlist.transform, false);
                        Undo.RegisterCreatedObjectUndo(obj, "PlaylistRPC add");
                        UdonSharpUndo.AddComponent<PlaylistRPC>(obj);
                    }

                    PlaylistEditor.EnforcePlaylistData(playlist, false);
                }

                // check for old UI and convert to new playlist UI component.
                if (playlist.scrollView || playlist.listContainer || playlist.template)
                {
                    if (!log.dry)
                    {
                        var playlistUI = UdonSharpUndo.AddComponent<PlaylistUI>(playlist.gameObject);
                        PlaylistUIEditor.MigrateUI(playlist, playlistUI);
                        Save(playlist);
                        Save(playlistUI);
                    }

                    Info(playlist, "Moving UI references to new PlaylistUI component.");
                }
            }

            foreach (PlaylistUI playlistUI in playlistUIs)
            {
                if (playlistUI.playlist == null)
                {
                    var playlist = playlistUI.GetComponentInParent<Playlist>(true);
                    if (playlist == null)
                    {
                        Error(playlistUI, I18n.Tr("A Playlist MUST be provided for the playlist UI to operate correctly."));
                        continue;
                    }

                    Info(playlistUI, I18n.Tr("Updating missing Playlist reference"));
                    if (!log.dry)
                    {
                        playlistUI.playlist = playlist;
                        Save(playlistUI);
                    }
                }

                if (playlistUI.scrollView == null)
                {
                    var scrollView = playlistUI.GetComponentInChildren<ScrollRect>(true);
                    if (scrollView == null && playlistUI.template != null) scrollView = playlistUI.template.GetComponentInParent<ScrollRect>();
                    if (scrollView == null || scrollView.content == null)
                    {
                        Error(playlistUI, I18n.Tr("A ScrollView MUST be provided for the playlist UI to operate correctly."));
                        continue;
                    }

                    Info(playlistUI, I18n.Tr("Updating missing ScrollView reference"));
                    if (!log.dry)
                    {
                        playlistUI.scrollView = scrollView;
                        Save(playlistUI);
                    }
                }

                if (playlistUI.listContainer == null)
                {
                    var container = playlistUI.scrollView.GetComponentsInChildren<RectTransform>(true)
                        .FirstOrDefault(t => t.name == "Content" && t.IsChildOf(playlistUI.scrollView.viewport));
                    if (container == null)
                    {
                        Error(playlistUI, I18n.Tr("A list container MUST be provided for the playlist UI to operate correctly."));
                        continue;
                    }

                    Info(playlistUI, I18n.Tr("Updating missing list container reference"));
                    if (!log.dry)
                    {
                        playlistUI.listContainer = container;
                        Save(playlistUI);
                    }
                }

                if (playlistUI.template == null)
                {
                    var tmpl = playlistUI.scrollView.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(t => t.name == "Template" && !t.IsChildOf(playlistUI.listContainer));
                    if (tmpl == null)
                    {
                        Error(playlistUI, I18n.Tr("A template object MUST be provided for the playlist to operate correctly."));
                        continue;
                    }

                    Info(playlistUI, I18n.Tr("Updating missing template reference"));
                    if (!log.dry)
                    {
                        playlistUI.template = tmpl.gameObject;
                        PlaylistUIEditor.AutopopulateTemplateFields(playlistUI);
                        PlaylistUIEditor.UpdateTmplPaths(playlistUI);
                        Save(playlistUI);
                    }
                }

                if (!log.dry && playlistUI.scrollView != null)
                {
                    var selectable = playlistUI.scrollView.verticalScrollbar;
                    if (selectable != null)
                    {
                        var action = selectable.onValueChanged;
                        handleSelectableActionEvent(selectable, action, playlistUI.UpdateView);
                    }
                }

                if (playlistUI._EDITOR_templateUpgrade < PlaylistUIEditor.latestTemplateVersion)
                {
                    PlaylistUIEditor.AutopopulateTemplateFields(playlistUI);
                    PlaylistUIEditor.UpdateTmplPaths(playlistUI);
                    Save(playlistUI);
                }

                if (!log.dry && playlistUI.template != null)
                {
                    var selectable = playlistUI.selectAction;
                    var action = selectable.onClick;

                    List<UnityAction> actions = new List<UnityAction> { playlistUI.SwitchEntry };
                    if (playlistUI.playlist.disableAutoplayOnInteract) actions.Add(playlistUI.ManualPlay);
                    else ATEditorUtility.RemoveSelectableActionEvent(selectable, action, playlistUI.ManualPlay);
                    if (playlistUI.playlist.enableAutoplayOnInteract) actions.Add(playlistUI.AutoPlay);
                    else ATEditorUtility.RemoveSelectableActionEvent(selectable, action, playlistUI.AutoPlay);

                    handleSelectableAutoDetectionEvents(selectable, action, actions.ToArray());

                    var entries = playlistUI.listContainer.childCount;
                    for (int i = 0; i < entries; i++)
                    {
                        var entry = playlistUI.listContainer.GetChild(i);
                        if (playlistUI.selectActionTmplPath != string.Empty)
                            entry = entry.Find(playlistUI.selectActionTmplPath);
                        selectable = entry.GetComponent<Button>();
                        action = selectable.onClick;

                        if (selectable != null)
                        {
                            if (!playlistUI.playlist.disableAutoplayOnInteract)
                                ATEditorUtility.RemoveSelectableActionEvent(selectable, action, playlistUI.ManualPlay);
                            handleSelectableAutoDetectionEvents(selectable, action, actions.ToArray());
                            var idx = ATEditorUtility.GetPersistentListenerIndex(action, selectable, "set_interactable", false);
                            if (idx > -1) UnityEventTools.RemovePersistentListener(action, idx);
                            idx = ATEditorUtility.GetPersistentListenerIndex(action, selectable, "set_interactable", true);
                            if (idx > -1) UnityEventTools.RemovePersistentListener(action, idx);
                        }
                    }
                }

                if (playlistUI.autoplay != null)
                {
                    handleSelectableActionEvent(playlistUI.autoplay, playlistUI.autoplay.onClick, playlistUI.ToggleAutoPlay);
                }
            }
        }

        public void CleanPlaylistData()
        {
            foreach (var playlist in playlists)
            {
                if (playlist.storage)
                {
                    playlist.mainUrls = null;
                    playlist.alternateUrls = null;
                    playlist.titles = null;
                    playlist.descriptions = null;
                    playlist.tags = null;
                    playlist.images = null;
                }

                playlist._EDITOR_autofillEscape = false;
                playlist._EDITOR_entriesCount = 0;
                playlist._EDITOR_autofillFormat = null;
                playlist._EDITOR_imagesCount = 0;
                playlist._EDITOR_importUrl = null;
                playlist._EDITOR_importPath = null;
                playlist._EDITOR_importFromFile = false;
                playlist._EDITOR_autofillAltURL = false;
            }
        }

        public void ValidateQueueSetupRequirements()
        {
            foreach (QueueUI queueUI in queueUIs)
            {
                // Validate missing core references
                if (queueUI.listContainer == null)
                {
                    var scrollView = queueUI.GetComponentInChildren<ScrollRect>(true);
                    if (scrollView == null && queueUI.template != null) scrollView = queueUI.template.GetComponentInParent<ScrollRect>();
                    if (scrollView == null || scrollView.content == null)
                    {
                        Error(queueUI, I18n.Tr("A list container MUST be provided for the queue to operate correctly."));
                        continue;
                    }

                    Info(queueUI, I18n.Tr("Updating missing list container reference"));
                    if (!log.dry)
                    {
                        queueUI.listContainer = scrollView.content;
                        Save(queueUI);
                    }
                }

                Transform tmpl = null;
                if (queueUI.template == null)
                {
                    var scrollView = queueUI.GetComponentInChildren<ScrollRect>(true);
                    if (scrollView == null && queueUI.listContainer != null) scrollView = queueUI.listContainer.GetComponentInParent<ScrollRect>();
                    tmpl = scrollView == null
                        ? null
                        : scrollView.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(t => t.name == "Template" && !t.IsChildOf(queueUI.listContainer));
                    if (tmpl == null)
                    {
                        Error(queueUI, I18n.Tr("A template object MUST be provided for the queue to operate correctly."));
                        continue;
                    }

                    Info(queueUI, I18n.Tr("Updating missing template reference"));
                    if (!log.dry)
                    {
                        queueUI.template = tmpl.gameObject;
                        QueueUIEditor.AutopopulateTemplateFields(queueUI);
                        QueueUIEditor.UpdateTmplPaths(queueUI);
                        Save(queueUI);
                    }
                }
                else tmpl = queueUI.template.transform;

                if (tmpl == null) return;

                // validate template child objects are in fact child objects of the template. ERROR IF NOT VALID CHILD.
                var tmplT = tmpl.transform;

                Component component = queueUI.urlDisplay;
                Transform ct;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.urlDisplayTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Url Display is not a child of the Template object."));
                }

                component = queueUI.titleDisplay;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.titleDisplayTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Title Display is not a child of the Template object."));
                }

                component = queueUI.ownerDisplay;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.ownerDisplayTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Owner Display is not a child of the Template object."));
                }

                component = queueUI.urlDisplayTMP;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.urlDisplayTMPTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Url Display (TMP) is not a child of the Template object."));
                }

                component = queueUI.titleDisplayTMP;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.titleDisplayTMPTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Title Display (TMP) is not a child of the Template object."));
                }

                component = queueUI.ownerDisplayTMP;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.ownerDisplayTMPTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Owner Display (TMP) is not a child of the Template object."));
                }

                component = queueUI.selectAction;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.selectActionTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Remove Action is not a child of the Template object."));
                }

                component = queueUI.removeAction;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.removeActionTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Remove Action is not a child of the Template object."));
                }

                component = queueUI.persistenceAction;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.persistenceToggleTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Persistence Toggle is not a child of the Template object."));
                }

                component = queueUI.loadingBar;
                if (component != null)
                {
                    ct = component.transform;
                    if (ct.IsChildOf(tmplT))
                    {
                        if (!log.dry)
                        {
                            queueUI.loadingBarTmplPath = tmplT == ct ? "" : ct.GetHierarchyPath(tmplT);
                            Save(queueUI);
                        }
                    }
                    else Error(queueUI, I18n.Tr("Loading Bar is not a child of the Template object."));
                }

                if (queueUI.selectAction != null)
                {
                    handleSelectableAutoDetectionEvents(queueUI.selectAction, queueUI.selectAction.onClick, queueUI.SwitchEntry);
                    removeSelectableActionEventsOfType<Queue>(queueUI.selectAction, queueUI.selectAction.onClick);
                }

                if (queueUI.removeAction != null)
                {
                    handleSelectableAutoDetectionEvents(queueUI.removeAction, queueUI.removeAction.onClick, queueUI.RemoveEntry);
                    removeSelectableActionEventsOfType<Queue>(queueUI.removeAction, queueUI.removeAction.onClick);
                }

                if (queueUI.persistenceAction != null)
                {
                    handleSelectableAutoDetectionEvents(queueUI.persistenceAction, queueUI.persistenceAction.onValueChanged, queueUI.PersistEntry);
                    removeSelectableActionEventsOfType<Queue>(queueUI.persistenceAction, queueUI.persistenceAction.onValueChanged);
                }

                if (queueUI._EDITOR_templateUpgrade < QueueUIEditor.latestTemplateVersion)
                {
                    QueueUIEditor.AutopopulateTemplateFields(queueUI);
                    QueueUIEditor.UpdateTmplPaths(queueUI);
                    Save(queueUI);
                }
            }
        }

        private void handleSelectableActionEvent(Selectable component, UnityEventBase evnt, UnityAction action)
        {
            if (component == null) return;
            string udonEventName = action.Method.Name;
            UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)action.Target);
            if (behaviour == null)
            {
                Error((UdonSharpBehaviour)action.Target, $"Behaviour unexpectedly null");
            }

            for (int i = 0; i < evnt.GetPersistentEventCount(); i++)
            {
                if (evnt.GetPersistentTarget(i) == null)
                {
                    // clean up noop events
                    if (!log.dry) UnityEventTools.RemovePersistentListener(evnt, i--);
                }
            }

            var stage = ATEditorUtility.GetPersistentListenerIndex(evnt, behaviour, nameof(UdonBehaviour.SendCustomEvent), udonEventName);

            if (stage == -1)
            {
                Info(component, I18n.Tr("Updating events to have the correct list"));
                if (!log.dry)
                {
                    UnityEventTools.AddStringPersistentListener(evnt, behaviour.SendCustomEvent, udonEventName);
                    Save(component);
                }
            }
        }

        private void handleSelectableAutoDetectionEvents(Selectable component, UnityEventBase evnt, params UnityAction[] actions)
        {
            if (component == null) return;
            for (int i = 0; i < evnt.GetPersistentEventCount(); i++)
            {
                if (evnt.GetPersistentTarget(i) == null)
                {
                    // clean up noop events
                    if (!log.dry) UnityEventTools.RemovePersistentListener(evnt, i--);
                }
            }

            // grab what index each expected actions might be at
            int preStage = ATEditorUtility.GetPersistentListenerIndex(evnt, component, "set_enabled", false);

            int[] stages = new int[actions.Length];
            for (var index = 0; index < actions.Length; index++)
            {
                var action = actions[index];
                string udonEventName = action.Method.Name;
                UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)action.Target);
                stages[index] = ATEditorUtility.GetPersistentListenerIndex(evnt, behaviour, nameof(UdonBehaviour.SendCustomEvent), udonEventName);
            }

            int postStage = ATEditorUtility.GetPersistentListenerIndex(evnt, component, "set_enabled", true);

            // remove stages that are out of order
            if (!log.dry)
            {
                List<int> removals = new List<int>();
                int stageMin = preStage;
                for (int i = 0; i < stages.Length; i++)
                {
                    var stage = stages[i];
                    if (stage != -1 && stageMin > stage || stageMin == -1 && stage > -1)
                    {
                        // if the listener exists earlier than the prior stage or prior stage doesn't exist, remove
                        removals.Add(stage);
                        stages[i] = -1;
                        stageMin = -1;
                    }
                    else stageMin = stage;
                }

                if (postStage != -1 && stageMin > postStage || stageMin == -1 && postStage > -1)
                {
                    // if the listener exists earlier than the prior stage or prior stage doesn't exist, remove
                    removals.Add(postStage);
                    postStage = -1;
                }

                // remove the stages in reverse to avoid index issues.
                removals.Reverse();
                foreach (var stage in removals) UnityEventTools.RemovePersistentListener(evnt, stage);
            }

            // readd stages as needed
            if (preStage == -1)
            {
                Info(component, I18n.Tr("Updating events to have the correct list"));
                if (!log.dry)
                {
                    UnityAction<bool> enabledAction = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), component, "set_enabled") as UnityAction<bool>;
                    UnityEventTools.AddBoolPersistentListener(evnt, enabledAction, false);
                    Save(component);
                }
            }

            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] == -1)
                {
                    Info(component, I18n.Tr("Updating events to have the correct list"));
                    if (!log.dry)
                    {
                        var action = actions[i];
                        string udonEventName = action.Method.Name;
                        UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)action.Target);
                        UnityEventTools.AddStringPersistentListener(evnt, behaviour.SendCustomEvent, udonEventName);
                        Save(component);
                    }
                }
            }

            if (postStage == -1)
            {
                Info(component, I18n.Tr("Updating events to have the correct list"));
                if (!log.dry)
                {
                    UnityAction<bool> enabledAction = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), component, "set_enabled") as UnityAction<bool>;
                    UnityEventTools.AddBoolPersistentListener(evnt, enabledAction, true);
                    Save(component);
                }
            }
        }

        private void removeSelectableActionEventsOfType<T>(Selectable component, UnityEventBase evnt)
        {
            if (component == null) return;
            for (int i = 0; i < evnt.GetPersistentEventCount(); i++)
            {
                if (evnt.GetPersistentTarget(i) == null || evnt.GetPersistentTarget(i) is T)
                {
                    // clean up noop events
                    if (!log.dry) UnityEventTools.RemovePersistentListener(evnt, i--);
                }
            }
        }

        public void ValidateSkyboxSwapperSetupRequirements()
        {
            foreach (SkyboxSwapper skyboxSwapper in skyboxSwappers)
            {
                var selectables = skyboxSwapper.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    if (selectable.colors != skyboxSwapper.uiColors)
                    {
                        Info(skyboxSwapper, I18n.Tr("Updating UI element colors."));
                        if (!log.dry)
                        {
                            selectable.colors = skyboxSwapper.uiColors;
                            Save(selectable);
                        }
                    }
                }
            }
        }

        public void ValidateHistorySetupRequirements()
        {
            foreach (HistoryUI historyUI in historyUIs)
                if (historyUI.restoreAction != null)
                {
                    if (historyUI.history != null)
                        ATEditorUtility.RemoveSelectableActionEvent(historyUI.restoreAction, historyUI.restoreAction.onClick, historyUI.history._SelectEntry);
                    handleSelectableAutoDetectionEvents(historyUI.restoreAction, historyUI.restoreAction.onClick, historyUI.SelectEntry);
                }
        }

        public void ValidateAuthPluginSetupRequirements()
        {
            foreach (TVManagedWhitelistUI whitelist in managedWhitelistUIs)
            {
                if (whitelist.authAction != null)
                {
                    handleSelectableAutoDetectionEvents(whitelist.authAction, whitelist.authAction.onValueChanged, whitelist.AuthorizeEntry);
                    removeSelectableActionEventsOfType<TVManagedWhitelist>(whitelist.authAction, whitelist.authAction.onValueChanged);
                }
            }
        }


        public void CheckAndFixBrokenVRCUrlInputFields()
        {
            foreach (MediaControls control in controls)
            {
                // inspect for missing URLInput components.
                // mitigations against VRCSDK not correctly importing.
                var texts = control.GetComponentsInChildren<Text>(true).Where(text => text != null && text.gameObject.name == "Placeholder");
                foreach (Text text in texts)
                {
                    var parent = text.transform.parent;
                    if (parent != null)
                    {
                        // if there is a component whos reference is null as a parent of a "Placeholder" object, this is typically a missing VRCUrlInputField.
                        if (parent.GetComponents<Component>().Where(x => x == null).ToArray().Length > 0)
                        {
                            Error(parent.gameObject, I18n.Tr("Possible missing VRCUrlInputField. Try fixing by going to the Unity menu -> VRChat SDK -> Reload SDK"));
                            return; // if the VRCUrlInputField is missing, none of the subsequent fixes will work so just skip.
                        }
                    }
                }

                // inspect for missing URLInput properties
                // mitigations against VRCSDK not correctly importing.
                var inputs = control.GetComponentsInChildren<VRCUrlInputField>(true);
                foreach (VRCUrlInputField input in inputs)
                {
                    // if missing textComponent, assume VRCUrlInputField import was bad, try fixing the references
                    if (input.textComponent == null)
                    {
                        // try to find the default textComponent that unity creates for InputFields
                        texts = input.GetComponentsInChildren<Text>(true);
                        foreach (Text text in texts)
                        {
                            if (text.transform.parent == input.transform)
                            {
                                if (text.gameObject.name != "Placeholder")
                                {
                                    if (!log.dry) input.textComponent = text;
                                    break;
                                }
                            }
                        }

                        // try to find the default placeholder that unity creates for InputFields
                        var graphics = input.GetComponentsInChildren<Text>(true);
                        foreach (Text graphic in graphics)
                        {
                            if (graphic.transform.parent == input.transform)
                            {
                                if (graphic.gameObject.name == "Placeholder")
                                {
                                    if (!log.dry) input.placeholder = graphic;
                                    break;
                                }
                            }
                        }

                        if (!log.dry)
                        {
                            ATEditorUtility.RemoveSelectableActionEvent(input, input.onValueChanged, control._UpdateUrlInput);
                            ATEditorUtility.RemoveSelectableActionEvent(input, input.onEndEdit, control._EndEditUrlInput);
                            handleSelectableActionEvent(input, input.onValueChanged, control.UpdateUrlInput);
                            handleSelectableActionEvent(input, input.onEndEdit, control.EndEditUrlInput);

                            // add the reference into the control
                            if (knownMainInputNames.Contains(input.gameObject.name)) control.mainUrlInput = input;
                            else if (knownAltInputNames.Contains(input.gameObject.name)) control.alternateUrlInput = input;

                            Save(input);
                            Save(control);
                        }

                        Info(input, I18n.Tr("Reconnecting missing VRCUrlInputField references..."));
                    }
                }
            }
        }


        // TODO finish whitelist build check
        public void ValidateDomainWhitelist()
        {
            foreach (TVManager tv in tvs)
            {
                if (!tv.enforceDomainWhitelist) continue;
                string[] whitelist = tv.domainWhitelist;
                string warning = I18n.Tr("Domain is not on the whitelist. Unauthorized users won't be able to play this link.");

                {
                    var mainDomain = tv._GetUrlDomain(tv.autoplayMainUrl.Get());
                    var altDomain = tv._GetUrlDomain(tv.autoplayAlternateUrl.Get());
                    if (!string.IsNullOrWhiteSpace(mainDomain) && !whitelist.Any(w => mainDomain.EndsWith(w)))
                        Warning(tv, warning, $"Main: {mainDomain}");
                    if (!string.IsNullOrWhiteSpace(altDomain) && !whitelist.Any(w => altDomain.EndsWith(w)))
                        Warning(tv, warning, $"Alternate: {altDomain}");
                }

                foreach (QuickPlay quickPlay in quickPlays)
                {
                    if (quickPlay.tv != tv) continue;
                    var mainDomain = tv._GetUrlDomain(quickPlay.mainUrl.Get());
                    var altDomain = tv._GetUrlDomain(quickPlay.alternateUrl.Get());
                    if (!string.IsNullOrWhiteSpace(mainDomain) && !whitelist.Any(w => mainDomain.EndsWith(w)))
                        Warning(quickPlay, warning, $"Main: {mainDomain}");
                    if (!string.IsNullOrWhiteSpace(altDomain) && !whitelist.Any(w => altDomain.EndsWith(w)))
                        Warning(quickPlay, warning, $"Alternate: {altDomain}");
                }

                foreach (Playlist playlist in playlists)
                {
                    if (playlist.tv != tv) continue;
                    var main = playlist.storage != null ? playlist.storage.mainUrls : playlist.mainUrls;
                    var alts = playlist.storage != null ? playlist.storage.alternateUrls : playlist.alternateUrls;
                    var badDomains = new Dictionary<string, int>();
                    for (var i = 0; i < main.Length; i++)
                    {
                        var mainUrl = main[i];
                        var altUrl = alts[i];
                        var mainDomain = tv._GetUrlDomain(mainUrl.Get());
                        var altDomain = tv._GetUrlDomain(altUrl.Get());
                        if (!string.IsNullOrWhiteSpace(mainDomain) && !whitelist.Any(w => mainDomain.EndsWith(w)))
                        {
                            if (!badDomains.ContainsKey(mainDomain)) badDomains.Add(mainDomain, 0);
                            badDomains[mainDomain]++;
                        }

                        if (!string.IsNullOrWhiteSpace(altDomain) && !whitelist.Any(w => altDomain.EndsWith(w)))
                        {
                            if (!badDomains.ContainsKey(altDomain)) badDomains.Add(altDomain, 0);
                            badDomains[altDomain]++;
                        }
                    }

                    foreach (var bad in badDomains)
                        Warning(playlist, warning, $"{bad.Key}: {bad.Value}");
                }
            }
        }


        public void UpdateVPSwitcherDropdowns()
        {
            foreach (MediaControls control in controls)
            {
                TVManager tv = control.tv;
                if (tv == null) continue;
                Dropdown dropdown = control.videoPlayerSwap;
                if (dropdown == null) continue;
                if (tv.videoManagers == null)
                {
                    Warning(control, I18n.Tr("Something went wrong with the TV. No video managers are available."), tv);
                    continue;
                }

                var oldSwap = dropdown.options.Select(t => t.text).ToList();
                var newSwap = tv.videoManagers.Select(t => t.customLabel).ToList();
                bool allManagersMatch = oldSwap.SequenceEqual(newSwap);
                List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
                foreach (VPManager manager in tv.videoManagers)
                {
                    if (manager == null)
                    {
                        options.Add(new Dropdown.OptionData("<Missing Ref>"));
                        continue;
                    }

                    string label = !string.IsNullOrEmpty(manager.customLabel) ? manager.customLabel : manager.gameObject.name;
                    // check for mismatched entries
                    if (!oldSwap.Contains(label)) allManagersMatch = false;
                    options.Add(new Dropdown.OptionData(label));
                }

                // if any entries are missing, rebuild the list.
                if (!allManagersMatch)
                {
                    Info(control, I18n.Tr("Updating the VPManager swap dropdown..."));
                    if (!log.dry)
                    {
                        dropdown.ClearOptions();
                        dropdown.AddOptions(options);
                    }
                }

                if (!log.dry)
                {
                    dropdown.value = control.tv.defaultVideoManager;
                    if (control.videoPlayerSwapUseTMP)
                    {
                        string defaultLabel = dropdown.options[dropdown.value].text;
                        ProTVEditorUtility.InjectTMPIntoDropdown(dropdown, defaultLabel, "Option");
                    }

                    Save(dropdown);
                }
            }
        }

        private void UpdateAutoplaySettings()
        {
            var count = 0;
            foreach (var tv in tvs)
            {
                bool hasMainAutoplay = !string.IsNullOrWhiteSpace(tv.autoplayMainUrl.Get());
                bool hasAltAutoplay = !string.IsNullOrWhiteSpace(tv.autoplayAlternateUrl.Get());
                bool hasAutoplay = hasMainAutoplay || hasAltAutoplay;

                // pre-playlist autoplay check
                // I forgot what I was doing here?
                // todo try to remember what this check was for. I think it was kind of important...
                // if (hasAutoplay)
                // {
                //     if (hasMainAutoplay)
                //     {
                //         
                //     }
                // }


                // check for playlist autoplay
                if (!hasAutoplay)
                {
                    foreach (Playlist _playlist in playlists)
                    {
                        if (_playlist.tv == tv && _playlist.autoplayList && _playlist.autoplayOnLoad)
                        {
                            hasAutoplay = true;
                            break;
                        }
                    }
                }

                if (!log.dry)
                {
                    if (hasAutoplay && tv.gameObject.activeInHierarchy)
                    {
                        tv.autoplayStartOffset = 5f * count;
                        count++;
                    }
                    else tv.autoplayStartOffset = 0f;

                    Save(tv);
                }

                if (hasAutoplay) Info(tv, I18n.Tr("Updating autoplay start offset"));
            }
        }

        public void FixUINavigations()
        {
            var navigationInfo = typeof(Selectable).GetField("m_Navigation", BindingFlags.Instance | BindingFlags.NonPublic);
            if (navigationInfo == null) return;
            foreach (var plugin in plugins)
            {
                var selectables = plugin.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    if (selectable.navigation.mode != Navigation.Mode.None)
                    {
                        selectable.navigation = new Navigation { mode = Navigation.Mode.None };
                        Save(selectable);
                    }
                }
            }

            foreach (var plugin in authPlugins)
            {
                var selectables = plugin.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    if (selectable.navigation.mode != Navigation.Mode.None)
                    {
                        selectable.navigation = new Navigation { mode = Navigation.Mode.None };
                        Save(selectable);
                    }
                }
            }
        }

        private void FixStartPositionOfScrollbars()
        {
            foreach (PlaylistUI playlistUI in playlistUIs)
            {
                var scrollView = playlistUI.scrollView;
                if (scrollView == null) continue;
                var hScrollbar = scrollView.horizontalScrollbar;
                var vScrollbar = scrollView.verticalScrollbar;
                if (hScrollbar != null)
                {
                    hScrollbar.value = 0;
                    Save(hScrollbar);
                }

                if (vScrollbar != null)
                {
                    vScrollbar.value = 1;
                    Save(vScrollbar);
                }
            }

            foreach (QueueUI queueUI in queueUIs)
            {
                var scrollView = queueUI.GetComponentInChildren<ScrollRect>(true);
                if (scrollView == null) continue;
                var hScrollbar = scrollView.horizontalScrollbar;
                var vScrollbar = scrollView.verticalScrollbar;
                if (hScrollbar != null)
                {
                    hScrollbar.value = 0;
                    Save(hScrollbar);
                }

                if (vScrollbar != null)
                {
                    vScrollbar.value = 1;
                    Save(vScrollbar);
                }
            }
        }

        public void FixUiShapes()
        {
            foreach (VRCUiShape uiShape in uiShapes)
            {
                // purge old UIShapeFixes scripts
                var oldFixes = uiShape.GetComponentsInChildren<UiShapeFixes>(true);
                foreach (var fix in oldFixes) UdonSharpEditorUtility.DestroyImmediate(fix);

                GameObject obj = uiShape.gameObject;
                // if the uishape is on the UI layer and in world-space
                // change the uishape's layer to Interactive to allow the Raycast pointer to interact with it.
                if (obj.layer == LayerMask.NameToLayer("UI"))
                {
                    obj.layer = LayerMask.NameToLayer("Interactive");
                    Save(obj);
                }

                BoxCollider box = uiShape.GetComponent<BoxCollider>();
                if (!box)
                {
                    // if a collider doesn't exist, add one and implictly make it a trigger
                    // This allows a creator to specify a box being a collidable one by having one exist on the object AoT
                    if (!log.dry)
                    {
                        box = obj.AddComponent<BoxCollider>();
                        box.isTrigger = true;
                        Save(obj);
                    }
                }

                if (!box) return;
                // box center is non-normalized, so convert the normalized pivot value and then scale by size
                // if pivot is 0, a positive conversion offset is needed. if pivot is 1, a negative conversion offset is needed
                // changes range 0 -> 1 into a 0.5 to -0.5 range respectively
                RectTransform rectT = uiShape.transform as RectTransform;
                if (rectT == null) continue;
                var pivot = rectT.pivot;
                var rect = rectT.rect;
                var newCenter = new Vector3((-pivot.x + 0.5f) * rect.width, (-pivot.y + 0.5f) * rect.height, 0);
                var newSize = new Vector3(rect.width, rect.height, box.size.z);
                if (!box.center.Equals(newCenter))
                {
                    // box == null allows passthrough when in dry mode
                    Info(uiShape, I18n.Tr("Updating VRCUiShape collider to the correct placement..."));
                    if (!log.dry)
                    {
                        box.center = newCenter;
                        Save(box);
                    }
                }

                if (!box.size.Equals(newSize))
                {
                    Info(uiShape, I18n.Tr("Updating VRCUiShape collider to the correct placement..."));
                    if (!log.dry)
                    {
                        box.size = newSize;
                        Save(box);
                    }
                }
            }
        }

        public void FixUiSliderFillImages()
        {
            // This handles fixes for how the Retro style sliders setup their fill slider with an unstretched gradient background
            // Tried finding a unity-native way of accomplishing this, but to no avail. If you know of another way to do this, let me know please!
            foreach (TVPlugin plugin in plugins)
            {
                var sliders = plugin.gameObject.GetComponentsInChildren<Slider>(true);
                foreach (var slider in sliders)
                {
                    RectTransform fillRect;
                    try
                    {
                        // catch when fill rect is null because it might throw an exception
                        fillRect = slider.fillRect;
                        if (fillRect == null)
                        {
                            Warning(slider, "Slider is missing its Fill Rect reference. Ensure the object has one connected.");
                            continue;
                        }
                    }
                    catch
                    {
                        Warning(slider, "Slider is missing its Fill Rect reference. Ensure the object has one connected.");
                        continue;
                    }

                    var fillBG = fillRect.GetComponentsInChildren<Image>(true).FirstOrDefault(t => t.gameObject != fillRect.gameObject);
                    if (fillBG == null) continue;
                    var fillBGRect = fillBG.rectTransform;
                    var fillBGTarget = slider.GetComponent<RectTransform>();
                    // horizontally oriented sliders
                    if (fillBGRect.anchorMin.x == 0 && fillBGRect.anchorMax.x == 0 && fillBGRect.pivot.x == 0 && fillBGRect.anchoredPosition.x == 0)
                    {
                        if (fillBGTarget == null || fillBGRect.rect.size.x == fillBGTarget.rect.size.x) continue;
                        if (!log.dry)
                        {
                            fillBGRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillBGTarget.rect.size.x);
                            Save(fillBGRect);
                        }

                        Info(slider, I18n.Tr("Adjusting horizontal slider fill background"));
                    }
                    // vertically oriented sliders
                    else if (fillBGRect.anchorMin.y == 0 && fillBGRect.anchorMax.y == 0 && fillBGRect.pivot.y == 0 && fillBGRect.anchoredPosition.y == 0)
                    {
                        if (fillBGTarget == null || fillBGRect.rect.size.y == fillBGTarget.rect.size.y) continue;
                        if (!log.dry)
                        {
                            fillBGRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fillBGTarget.rect.size.y);
                            Save(fillBGRect);
                        }

                        Info(slider, I18n.Tr("Adjusting vertical slider fill background"));
                    }
                }
            }
        }

        private void FixTVGSVUnsetBugFromPreviousVersions()
        {
            if (tvs.Length == 0) return;
            // if no gsvchecks are found, and no tv have the feature enabled, enable it on the first available TV
            if (tvs.Count(t => t.gsvfixcheck) == 0 && tvs.Count(t => t.enableGSV) == 0)
            {
                var tv = tvs[0];
                tv.enableGSV = true;
                tv.gsvfixcheck = true;
                Save(tv);
            }

            // set the gsv check flag for all TVs in scene
            foreach (var tv in tvs)
            {
                if (!tv.gsvfixcheck)
                {
                    tv.gsvfixcheck = true;
                    Save(tv);
                }
            }
        }

        #region Migration Fixes from 2x to 3x

        private void FixMissingTMPFonts()
        {
            if (!log.dry)
            {
                var tmps = ATEditorUtility.GetComponentsInScene<TextMeshProUGUI>();
                var ssdFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProTVEditorUtility.defaultTMPFontAsset);
                foreach (var tmp in tmps)
                {
                    if (tmp.font == null)
                    {
                        tmp.font = ssdFont;
                        Save(tmp);
                    }
                }
            }
        }

        // explicitly for fixing migration issues from 2.x to 3.x
        private void FixUSharpAssetReferences()
        {
            var files = AssetDatabase.FindAssets("*", new[] { ProTVEditorUtility.runtimeFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(f => f.EndsWith(".cs"))
                .ToArray();
            foreach (var f in files) forceAssetScript(f);

            foreach (var tv in ATEditorUtility.GetComponentsInScene<TVManagerV2>())
            {
                // Due to legacy third-party plugins, do not implicitly update the TVManagerV2 references.
                // These will require manually updating via component context menu by the world creator
                // to avoid compiler issues with custom code.
                // VideoManagerV2 and TVManagerV2ManualSync are internally focused,
                // so we won't worry about retaining those old component types.
                if (tv.syncData is TVManagerV2ManualSync oldData)
                {
                    if (modifiedObjects.Contains(oldData)) modifiedObjects.Remove(oldData);
                    tv.syncData = ATEditorUtility.SwapUdonSharpComponentTypeTo<TVManagerData>(oldData);
                }

                if (tv.videoManagers != null)
                    for (var index = 0; index < tv.videoManagers.Length; index++)
                    {
                        var manager = tv.videoManagers[index];
                        if (manager is VideoManagerV2)
                        {
                            if (modifiedObjects.Contains(manager)) modifiedObjects.Remove(manager);
                            tv.videoManagers[index] = ATEditorUtility.SwapUdonSharpComponentTypeTo<VPManager>(manager);
                        }
                    }
            }

            foreach (var control in ATEditorUtility.GetComponentsInScene<Controls_ActiveState>())
                if (!PrefabUtility.IsPartOfAnyPrefab(control))
                    ATEditorUtility.SwapUdonSharpComponentTypeTo<MediaControls>(control);

            foreach (var control in ATEditorUtility.GetComponentsInScene<AudioLinkAdapter>())
                if (!PrefabUtility.IsPartOfAnyPrefab(control))
                    ATEditorUtility.SwapUdonSharpComponentTypeTo<AudioAdapter>(control);
        }

        private void forceAssetScript(string scriptLoc)
        {
            var str = scriptLoc.Substring(0, scriptLoc.LastIndexOf(".", StringComparison.Ordinal));
            forceAssetScript(str + ".cs", str + ".asset");
        }

        private void forceAssetScript(string scriptLoc, string assetLoc)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptLoc);
            var asset = AssetDatabase.LoadAssetAtPath<UdonSharp.UdonSharpProgramAsset>(assetLoc);
            if (asset != null && asset.sourceCsScript != script)
            {
                asset.sourceCsScript = script;
                asset.UpdateProgram(); // this is superstition. no idea if it's needed.
            }
        }

        #endregion
    }

    #region Build Log Handler

    internal class ProTVBuildLog : IEnumerator<ProTVBuildLog.LogEntry>
    {
        internal class LogEntry
        {
            public readonly ATLogLevel level;
            public readonly UnityEngine.Object scope;
            public readonly string message;
            public readonly HashSet<object> relatedScopes = new HashSet<object>();

            private LogEntry(ATLogLevel level, UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes)
            {
                this.level = level;
                this.scope = scope;
                this.message = message;
                foreach (var s in relatedScopes)
                    this.relatedScopes.Add(s);
            }

            private LogEntry(ATLogLevel level, UnityEngine.Object scope, string message, string relatedData)
            {
                this.level = level;
                this.scope = scope;
                this.message = message;
                relatedScopes.Add(relatedData);
            }

            public static LogEntry Error(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) =>
                new LogEntry(ATLogLevel.ERROR, scope, message, relatedScopes);

            public static LogEntry Error(UnityEngine.Object scope, string message, string relatedData) =>
                new LogEntry(ATLogLevel.ERROR, scope, message, relatedData);

            public static LogEntry Warn(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) =>
                new LogEntry(ATLogLevel.WARN, scope, message, relatedScopes);

            public static LogEntry Warn(UnityEngine.Object scope, string message, string relatedData) =>
                new LogEntry(ATLogLevel.WARN, scope, message, relatedData);

            public static LogEntry Info(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) =>
                new LogEntry(ATLogLevel.INFO, scope, message, relatedScopes);

            public static LogEntry Info(UnityEngine.Object scope, string message, string relatedData) =>
                new LogEntry(ATLogLevel.INFO, scope, message, relatedData);
        }

        public readonly List<LogEntry> history = new List<LogEntry>();
        public readonly Dictionary<string, int> aggregate = new Dictionary<string, int>();
        public bool dry = true;
        private int index = -1;
        public long lastExecutionTime = 0L;

        public void Error(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) =>
            Add(LogEntry.Error(scope, message, relatedScopes));

        public void Error(UnityEngine.Object scope, string message, string relatedData) =>
            Add(LogEntry.Error(scope, message, relatedData));

        public void Warn(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) =>
            Add(LogEntry.Warn(scope, message, relatedScopes));

        public void Warn(UnityEngine.Object scope, string message, string relatedData) =>
            Add(LogEntry.Warn(scope, message, relatedData));

        public void Info(UnityEngine.Object scope, string message, params UnityEngine.Object[] relatedScopes) =>
            Add(LogEntry.Info(scope, message, relatedScopes));

        public void Info(UnityEngine.Object scope, string message, string relatedData) =>
            Add(LogEntry.Info(scope, message, relatedData));

        private void Add(LogEntry newEntry)
        {
            var existing = history.FirstOrDefault(entry => newEntry.scope == entry.scope && newEntry.message == entry.message);
            if (existing == null) history.Add(newEntry);
            else if (newEntry.relatedScopes.Count > 0) existing.relatedScopes.UnionWith(newEntry.relatedScopes);
        }

        public int Count() => history.Count();
        public int Count(ATLogLevel level) => history.Count(x => x.level == level);
        public int Count(ATLogLevel level, string message) => history.Count(x => x.level == level && x.message == message);
        public int Uniques() => history.Select(x => $"{x.level}:{x.message}").Distinct().Count();
        public void Clear() => history.Clear();

        public bool MoveNext()
        {
            var next = index + 1;
            bool check = next < Count();
            if (check) index = next;
            return check;
        }

        public void Reset()
        {
            index = -1;
        }

        public LogEntry Current
        {
            get => index == -1 ? null : history[index];
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            Clear();
        }
    }

    internal class ProTVBuildWindow : UnityEditor.EditorWindow
    {
        private static Texture2D errorIcon = null;
        private static Texture2D warnIcon = null;
        private static Texture2D infoIcon = null;

        internal static readonly ProTVBuildLog BuildLog = new ProTVBuildLog();
        internal static readonly Dictionary<string, bool> skipTruncate = new Dictionary<string, bool>();
        private ProTVBuildLog log = null;
        private Vector2 scrollPos = Vector2.zero;

        public static void Open()
        {
            ProTVBuildWindow window = (ProTVBuildWindow)GetWindow(typeof(ProTVBuildWindow));
            window.minSize = new Vector2(200, 600);
            window.maxSize = new Vector2(600, 900);
            window.titleContent = new GUIContent(I18n.Tr("ProTV Build Logs"));
            window.Show();
        }

        public static bool IsOpen() => HasOpenInstances<ProTVBuildWindow>();

        private void OnEnable()
        {
            if (errorIcon == null) errorIcon = (Texture2D)typeof(EditorGUIUtility).InvokeMember("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { MessageType.Error });
            if (warnIcon == null) warnIcon = (Texture2D)typeof(EditorGUIUtility).InvokeMember("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { MessageType.Warning });
            if (infoIcon == null) infoIcon = (Texture2D)typeof(EditorGUIUtility).InvokeMember("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { MessageType.Info });

            if (log == null) log = BuildLog;
            if (log.Count() == 0) new ProTVBuildChecks().RunChecks(true);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(I18n.Tr("Run Build Checks")))
                    new ProTVBuildChecks().RunChecks(log.dry);
                log.dry = EditorGUILayout.Toggle(I18n.Tr("Dry Run"), log.dry);
            }


            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            int index = 0;
            int consecutive = 0;
            string lastId = null;
            UnityEngine.Object lastScope = null;
            while (index < log.history.Count)
            {
                var check = log.history[index];
                var id = $"{check.level}:{check.message}";
                if (!skipTruncate.ContainsKey(id)) skipTruncate[id] = false;
                if (id != lastId)
                {
                    consecutive = 0;
                    if (index > 0)
                    {
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginHorizontal("Box");
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(32), GUILayout.Height(32));
                    switch (check.level)
                    {
                        case ATLogLevel.ERROR:
                            GUI.DrawTexture(rect, errorIcon, ScaleMode.ScaleToFit);
                            break;

                        case ATLogLevel.WARN:
                            GUI.DrawTexture(rect, warnIcon, ScaleMode.ScaleToFit);
                            break;

                        default:
                            GUI.DrawTexture(rect, infoIcon, ScaleMode.ScaleToFit);
                            break;
                    }

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(new GUIContent(check.message, check.message), EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(12f);
                    EditorGUI.BeginDisabledGroup(true);
                }

                // ignore exact repeating entries
                if (lastScope != check.scope)
                {
                    if (check.scope != null) EditorGUILayout.ObjectField(check.scope, check.scope.GetType(), true);
                    if (check.relatedScopes != null && check.relatedScopes.Count > 0)
                    {
                        foreach (var scope in check.relatedScopes.Where(scope => scope != null))
                        {
                            using (new GUILayout.HorizontalScope(GUIStyle.none))
                            {
                                ATEditorGUILayout.Spacer(25f);
                                if (scope is string s) EditorGUILayout.LabelField(s);
                                else EditorGUILayout.ObjectField((UnityEngine.Object)scope, scope.GetType(), true);
                            }
                        }
                    }

                    consecutive++;
                }

                lastId = id;
                lastScope = check.scope;
                index++;

                if (consecutive == 3 && check.level == ATLogLevel.INFO)
                {
                    if (!skipTruncate[id])
                    {
                        // skip the remainder of the entries till a new level/message is found
                        while (index < log.history.Count && $"{log.history[index].level}:{log.history[index].message}" == id) index++;
                    }

                    skipTruncate[id] = EditorGUILayout.Foldout(skipTruncate[id], "...", true);
                }
            }

            if (index > 0)
            {
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(I18n.Tr("ProTV Build Checks execution time") + $": {log.lastExecutionTime}ms");
        }
    }

    #endregion
}