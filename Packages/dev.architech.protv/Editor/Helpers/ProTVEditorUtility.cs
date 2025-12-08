using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;

namespace ArchiTech.ProTV.Editor
{
    public static class ProTVEditorUtility
    {
        public const string packageName = "dev.architech.protv";
        public const string defaultTvShader = "ProTV/VideoScreen";
        public const string simpleTVPrefab = "Packages/dev.architech.protv/Simple (ProTV).prefab";
        public const string blitMaterialPath = "Packages/dev.architech.protv/Resources/Materials/TVBlit.mat";
        public const string mediaController = "Packages/dev.architech.protv/Resources/Animations/MediaController.controller";
        public const string defaultStaticImage = "Packages/dev.architech.protv/Resources/Images/ProTV_Logo_16x9.png";
        public const string defaultSoundImage = "Packages/dev.architech.protv/Resources/Images/ProTV_Logo_16x9_SoundOnly.png";
        public const string linkIconPath = "Packages/dev.architech.protv/Resources/UI/Icons/plain_plus.png";
        public const string checkmarkIconPath = "Packages/dev.architech.protv/Resources/UI/Icons/plain_checkmark.png";
        public const string defaultTMPFontAsset = "Packages/dev.architech.protv/Resources/UI/LiberationSans SDF.asset";
        public const string runtimeFolder = "Packages/dev.architech.protv/Runtime";
        public const string audioLinkPrefab = "Packages/com.llealloo.audiolink/Runtime/AudioLink.prefab";
        public const string avproPackageSrc = "https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases/download/2.8.5/UnityPlugin-AVProVideo-v2.8.5-Trial.unitypackage";
        public const string avproFileCheck = "Assets/AVProVideo/Runtime/Scripts/Components/MediaPlayer.cs";
        public const string videoPlayerShimFileCheck = "Assets/ArchiTech/VideoPlayerShim/Editor/PlayModeUrlResolverShim.cs";
        public const string videoPlayerShimPackage = "dev.architech.videoplayershim";
        private static bool openShimWindow;

        public static string Version => AssetDatabase
            .FindAssets("package")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
            .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
            .FirstOrDefault(x => x != null && x.name == packageName)
            ?.version;

        public static void PopulateDropdownForTV(Dropdown dropdown, TVManager tv, bool useTMP = false)
        {
            if (tv == null) return;
            if (dropdown == null) return;
            if (tv.videoManagers == null)
            {
                UnityEngine.Debug.Log($"TV {tv.name} missing VPManagers. Please build or playmode at least once.");
                return;
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
                dropdown.ClearOptions();
                dropdown.AddOptions(options);
            }

            dropdown.value = tv.defaultVideoManager;
            if (useTMP)
            {
                string defaultLabel = dropdown.options[dropdown.value].text;
                InjectTMPIntoDropdown(dropdown, defaultLabel, "Option");
            }
        }

        public static void InjectTMPIntoDropdown(Dropdown dropdown, string placeholderLabel, string placeholderItemLabel)
        {
            var label = dropdown.captionText;
            var template = dropdown.template;
            var itemLabel = dropdown.itemText;
            var dgo = dropdown.gameObject;
            var tgo = template.gameObject;

            // add the main label tmp if not detected
            if (label != null)
            {
                var tmp = dropdown.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp == null) AddTMPTextSibling(label, placeholderLabel);
                else
                {
                    Undo.RecordObject(tmp, "Undo placeholder change name");
                    tmp.text = placeholderLabel;
                }
            }

            if (itemLabel != null)
            {
                // Add the item label tmp object if not detected
                var tmp = tgo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp == null) AddTMPTextSibling(itemLabel, placeholderItemLabel);
                else
                {
                    Undo.RecordObject(tmp, "Undo placeholder change name");
                    tmp.text = placeholderItemLabel;
                }
            }

            // add UI shape if missing
            if (!tgo.TryGetComponent(out VRCUiShape _))
                Undo.AddComponent<VRCUiShape>(tgo);

            // add dropdown fix if missing
            if (!dgo.TryGetComponent(out TVDropdownFix fix))
                fix = UdonSharpUndo.AddComponent<TVDropdownFix>(dgo);
            // make sure the TMP label update action is registered
            ATEditorUtility.EnsureSelectableActionEvent(dropdown, dropdown.onValueChanged, fix.UpdateTMPLabel);
        }

        public static void AddTMPTextSibling(Text label, string placeholder)
        {
            GameObject tmpGO;
            RectTransform tmpRect;
            TextMeshProUGUI tmpText = label.transform.parent.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpGO = tmpText.gameObject;
                Undo.RegisterCompleteObjectUndo(tmpGO, "Undo " + tmpGO);
                tmpGO.name = label.gameObject.name + " (TMP)";
                tmpRect = (RectTransform)tmpGO.transform;
            }
            else
            {
                tmpGO = new GameObject(label.gameObject.name + " (TMP)");
                Undo.RegisterCreatedObjectUndo(tmpGO, "Undo " + tmpGO.name);
                tmpRect = tmpGO.AddComponent<RectTransform>();
                tmpText = tmpGO.AddComponent<TextMeshProUGUI>();
                tmpText.enabled = label.enabled;
                tmpText.alignment = TextAlignmentOptions.Center;
                bool resize = label.resizeTextForBestFit;
                tmpText.enableAutoSizing = resize;
                if (resize)
                {
                    tmpText.fontSizeMin = label.resizeTextMinSize;
                    tmpText.fontSizeMax = label.resizeTextMaxSize;
                }
                else tmpText.fontSize = label.fontSize;

                tmpText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(defaultTMPFontAsset);
                tmpText.color = label.color;
            }

            tmpGO.transform.SetParent(label.transform.parent, false);
            var rect = (RectTransform)label.transform;
            tmpRect.SetSiblingIndex(label.transform.GetSiblingIndex() + 1);
            tmpRect.SetPositionAndRotation(rect.position, rect.rotation);
            Undo.RecordObject(rect, "Unscale from TMP modification");
            rect.transform.localScale = Vector3.zero;
            tmpRect.localScale = Vector3.one;
            tmpRect.pivot = rect.pivot;
            tmpRect.anchorMin = rect.anchorMin;
            tmpRect.anchorMax = rect.anchorMax;
            tmpRect.anchoredPosition = rect.anchoredPosition;
            tmpRect.sizeDelta = rect.sizeDelta;
            tmpText.text = placeholder;
        }

        public static TVManager FindParentTVManager(GameObject go, bool includeSelf = true) =>
            ATEditorUtility.GetComponentInNearestParent<TVManager>(go, includeSelf);

        public static TVManager FindParentTVManager(Component component, bool includeSelf = true) =>
            ATEditorUtility.GetComponentInNearestParent<TVManager>(component, includeSelf);

        public static TVManager FindParentTVManager(Transform t, bool includeSelf = true) =>
            ATEditorUtility.GetComponentInNearestParent<TVManager>(t, includeSelf);

        internal static VRCAVProVideoScreen[] GetScreensForManager(VRCAVProVideoPlayer vp) =>
            ATEditorUtility.GetComponentsInScene<VRCAVProVideoScreen>().Where(s => s.VideoPlayer == vp).ToArray();

        internal static VPManager AddAVProVPManager(GameObject parent)
        {
            var pt = parent.transform;
            var tv = FindParentTVManager(pt);
            if (tv == null) return null; // only allow on child elements of a TVManager
            var tpt = tv.transform.Find("Internal");
            if (tpt != null) pt = tpt;
            var managers = tv.GetComponentsInChildren<VPManager>(true);
            var count = managers.Count(manager => manager.name.Contains("AVProVideo"));
            var n = "AVProVideo";
            if (count > 0) n += count;
            var go = CreateAVProVideoManager(n);
            var t = go.transform;
            t.SetParent(pt, false);
            var localPosition = t.localPosition;
            localPosition = new Vector3(localPosition.x, localPosition.y + 1f, localPosition.z);
            t.localPosition = localPosition;
            go.SetActive(false);
            return go.GetComponent<VPManager>();
        }

        internal static VRCAVProVideoPlayer AddAVProVideoPlayer(GameObject go, int maxResolution = 4096)
        {
            var vp = Undo.AddComponent<VRCAVProVideoPlayer>(go);
            var maxResInfo = vp.GetType().GetField("maximumResolution", BindingFlags.Instance | BindingFlags.NonPublic);
            var autoplayInfo = vp.GetType().GetField("autoPlay", BindingFlags.Instance | BindingFlags.NonPublic);
            var loopInfo = vp.GetType().GetField("loop", BindingFlags.Instance | BindingFlags.NonPublic);
            if (maxResInfo != null) maxResInfo.SetValue(vp, maxResolution);
            if (autoplayInfo != null) autoplayInfo.SetValue(vp, false);
            if (loopInfo != null) loopInfo.SetValue(vp, false);
            return vp;
        }

        internal static VRCAVProVideoScreen AddAVProVideoScreen(GameObject go, VRCAVProVideoPlayer vp)
        {
            var screen = Undo.AddComponent<VRCAVProVideoScreen>(go);
            // renderer fields
            var vpInfo = screen.GetType().GetField("videoPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            var sharedMatInfo = screen.GetType().GetField("useSharedMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            var texPropInfo = screen.GetType().GetField("textureProperty", BindingFlags.Instance | BindingFlags.NonPublic);
            var matIndexInfo = screen.GetType().GetField("materialIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            if (vpInfo != null) vpInfo.SetValue(screen, vp);
            if (sharedMatInfo != null) sharedMatInfo.SetValue(screen, false);
            if (texPropInfo != null) texPropInfo.SetValue(screen, "_MainTex");
            if (matIndexInfo != null) matIndexInfo.SetValue(screen, 0);
            return screen;
        }

        internal static GameObject CreateAVProVideoManager(string name, int maxResolution = 4096)
        {
            var go = new GameObject { name = name.Replace(" ", "") };
            Undo.RegisterCreatedObjectUndo(go, "Added AVPro VPManager");
            var renderer = Undo.AddComponent<MeshRenderer>(go);
            var vp = AddAVProVideoPlayer(go, maxResolution);
            var screen = AddAVProVideoScreen(go, vp);
            var manager = UdonSharpUndo.AddComponent<VPManager>(go);
            renderer.enabled = false;
            manager.customLabel = name;
            var stereo = CreateAVProVideoSpeaker(vp, VRCAVProVideoSpeaker.ChannelMode.StereoMix, "Stereo");
            var stereoCenter = CreateAVProVideoSpeaker(vp, VRCAVProVideoSpeaker.ChannelMode.Three, "Stereo Center");
            manager.spatialSpeakers = manager.stereoSpeakers = new[] { stereo, stereoCenter };
            ATEditorUtility.MoveComponentToTop(screen);
            ATEditorUtility.MoveComponentToTop(vp);
            ATEditorUtility.MoveComponentToTop(manager);
            return go;
        }

        internal static AudioSource CreateAVProVideoSpeaker(VRCAVProVideoPlayer vp, VRCAVProVideoSpeaker.ChannelMode mode, string label = null)
        {
            var vpname = vp.gameObject.name;
            if (label == null) label = "Stereo";
            var goStereo = new GameObject { name = $"{vpname} {label}" };
            Undo.RegisterCreatedObjectUndo(goStereo, "Added AVProVideo AudioSource");
            // slide the audio objects spatially away from each other by some amount.
            goStereo.transform.SetParent(vp.transform);
            goStereo.transform.localPosition = Vector3.zero;
            var audioStereo = CreateAudioSource(goStereo);
            var speakerStereo = goStereo.AddComponent<VRCAVProVideoSpeaker>();
            ATEditorUtility.MoveComponentToTop(speakerStereo);
            const BindingFlags bind = BindingFlags.Instance | BindingFlags.NonPublic;
            // update the avpro speaker component references.
            var videoPlayerInfo = speakerStereo.GetType().GetField("videoPlayer", bind);
            if (videoPlayerInfo != null) videoPlayerInfo.SetValue(speakerStereo, vp);
            var modeInfo = speakerStereo.GetType().GetField("mode", bind);
            if (modeInfo != null) modeInfo.SetValue(speakerStereo, mode);
            return audioStereo;
        }

        internal static VPManager AddUnityVPManager(GameObject parent)
        {
            var pt = parent.transform;
            var tv = FindParentTVManager(pt);
            if (tv == null) return null; // only allow on child elements of a TVManager
            var tpt = tv.transform.Find("Internal");
            if (tpt != null) pt = tpt;
            var managers = tv.GetComponentsInChildren<VPManager>(true);
            var count = managers.Count(manager => manager.name.Contains("UnityVideo"));
            var n = "UnityVideo";
            if (count > 0) n += count;
            var go = CreateUnityVideoManager(n);
            var t = go.transform;
            t.SetParent(pt, false);
            var localPosition = t.localPosition;
            localPosition = new Vector3(localPosition.x, localPosition.y + 1f, localPosition.z);
            t.localPosition = localPosition;
            go.SetActive(false);
            return go.GetComponent<VPManager>();
        }

        internal static GameObject CreateUnityVideoManager(string name, int maxResolution = 4096)
        {
            var go = new GameObject { name = name.Replace(" ", "") };
            Undo.RegisterCreatedObjectUndo(go, "Added UnityVideo VPManager");
            var renderer = go.AddComponent<MeshRenderer>();
            var vp = go.AddComponent<VRCUnityVideoPlayer>();
            var manager = UdonSharpUndo.AddComponent<VPManager>(go);
            var animator = go.AddComponent<Animator>();

            // videoplayer fields
            const BindingFlags bind = BindingFlags.Instance | BindingFlags.NonPublic;
            var renderModeInfo = vp.GetType().GetField("renderMode", bind);
            var targetMaterialInfo = vp.GetType().GetField("targetMaterialRenderer", bind);
            var targetPropertyInfo = vp.GetType().GetField("targetMaterialProperty", bind);
            var targetAudioSourcesInfo = vp.GetType().GetField("targetAudioSources", bind);
            var aspectRatioInfo = vp.GetType().GetField("aspectRatio", bind);
            var autoplayInfo = vp.GetType().GetField("autoPlay", bind);
            var loopInfo = vp.GetType().GetField("loop", bind);
            var maxResInfo = vp.GetType().GetField("maximumResolution", bind);
            if (renderModeInfo != null) renderModeInfo.SetValue(vp, 1); // VRCUnityVideoPlayer.VideoRenderMode.MaterialOverride = 1
            if (targetMaterialInfo != null) targetMaterialInfo.SetValue(vp, renderer);
            if (targetPropertyInfo != null) targetPropertyInfo.SetValue(vp, "_MainTex");
            var speaker = createUnityVideoSpeaker(vp);
            if (targetAudioSourcesInfo != null) targetAudioSourcesInfo.SetValue(vp, new AudioSource[1] { speaker });
            if (aspectRatioInfo != null) aspectRatioInfo.SetValue(vp, VideoAspectRatio.NoScaling);
            if (maxResInfo != null) maxResInfo.SetValue(vp, maxResolution);
            if (autoplayInfo != null) autoplayInfo.SetValue(vp, false);
            if (loopInfo != null) loopInfo.SetValue(vp, false);
            renderer.enabled = false;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(mediaController);
            manager.customLabel = name;
            manager.spatialSpeakers = manager.stereoSpeakers = new[] { speaker };
            ATEditorUtility.MoveComponentToTop(vp);
            ATEditorUtility.MoveComponentToTop(manager);
            return go;
        }

        private static AudioSource createUnityVideoSpeaker(VRCUnityVideoPlayer vp)
        {
            var go = new GameObject { name = vp.gameObject.name + " Stereo" };
            Undo.RegisterCreatedObjectUndo(go, "Added UnityVideo AudioSource");
            go.transform.SetParent(vp.transform);
            go.transform.localPosition = Vector3.zero;
            return CreateAudioSource(go);
        }

        internal static AudioSource CreateAudioSource(GameObject go, bool stereo = true)
        {
            var audioSource = go.AddComponent<AudioSource>();
            AnimationCurve volumeCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f, 0.5f, 0.5f),
                new Keyframe(0.6964619f, 0.4961839f, -2.199603f, -2.199603f, 0.207232f, 0.4897707f),
                new Keyframe(0.8517385f, 0.1034481f, -1.400948f, -1.400948f, 0.327098f, 0.2328144f),
                new Keyframe(1f, 0f, -0.05001967f, -0.05001967f, 0.5f, 0.5f)
            );

            audioSource.panStereo = 0f;
            audioSource.dopplerLevel = 0f;
            audioSource.reverbZoneMix = 0.5f;
            audioSource.priority = 16;
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = true;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeCurve);
            audioSource.spatialize = !stereo;
            audioSource.maxDistance = stereo ? 20f : 8f;
            audioSource.spread = stereo ? 180f : 30f;
            return audioSource;
        }

        public static void MergeMediaControls(MediaControls source, MediaControls target)
        {
            using (new ATEditorGUIUtility.SaveObjectScope(target))
            {
                if (target.tv == null) target.tv = source.tv;
                if (target.queue == null) target.queue = source.queue;
                if (target.mainUrlInput == null) target.mainUrlInput = source.mainUrlInput;
                if (string.IsNullOrEmpty(target.mainUrlDefault.Get())) target.mainUrlDefault = source.mainUrlDefault;
                if (target.alternateUrlInput == null) target.alternateUrlInput = source.alternateUrlInput;
                if (string.IsNullOrEmpty(target.alternateUrlDefault.Get())) target.alternateUrlDefault = source.alternateUrlDefault;
                if (target.titleInput == null) target.titleInput = source.titleInput;
                if (string.IsNullOrEmpty(target.titleDefault)) target.titleDefault = source.titleDefault;
                if (target.sendInputs == null) target.sendInputs = source.sendInputs;
                if (target.urlSwitch == null) target.urlSwitch = source.urlSwitch;
                if (target.play == null) target.play = source.play;
                if (target.pause == null) target.pause = source.pause;
                if (target.stop == null) target.stop = source.stop;
                if (target.resync == null) target.resync = source.resync;
                if (target.reload == null) target.reload = source.reload;
                if (target.seek == null) target.seek = source.seek;
                if (target.seekOffset == null) target.seekOffset = source.seekOffset;
                if (target.seekOffsetDisplay == null) target.seekOffsetDisplay = source.seekOffsetDisplay;
                if (target.seekOffsetDisplayTMP == null) target.seekOffsetDisplayTMP = source.seekOffsetDisplayTMP;
                if (target.playbackSpeed == null) target.playbackSpeed = source.playbackSpeed;
                if (target.videoPlayerSwap == null)
                {
                    target.videoPlayerSwap = source.videoPlayerSwap;
                    target.videoPlayerSwapUseTMP = source.videoPlayerSwapUseTMP;
                }

                if (target.mode3dSwap == null)
                {
                    target.mode3dSwap = source.mode3dSwap;
                    target.mode3dSwapUseTMP = source.mode3dSwapUseTMP;
                }

                if (target.width3dMode == null) target.width3dMode = source.width3dMode;
                if (target.width3dModeIndicator == null)
                {
                    target.width3dModeIndicator = source.width3dModeIndicator;
                    target.width3dHalf = source.width3dHalf;
                    target.width3dHalfColor = source.width3dHalfColor;
                    target.width3dFull = source.width3dFull;
                    target.width3dFullColor = source.width3dFullColor;
                }

                if (target.colorSpaceCorrection == null) target.colorSpaceCorrection = source.colorSpaceCorrection;
                if (target.colorSpaceCorrectionIndicator == null)
                {
                    target.colorSpaceCorrectionIndicator = source.colorSpaceCorrectionIndicator;
                    target.colorSpaceCorrected = source.colorSpaceCorrected;
                    target.colorSpaceRaw = source.colorSpaceRaw;
                    target.colorSpaceCorrectedColor = source.colorSpaceCorrectedColor;
                    target.colorSpaceRawColor = source.colorSpaceRawColor;
                }

                if (target.volume == null) target.volume = source.volume;
                if (target.volumeIndicator == null)
                {
                    target.volumeIndicator = source.volumeIndicator;
                    target.volumeOff = source.volumeOff;
                    target.volumeLow = source.volumeLow;
                    target.volumeMed = source.volumeMed;
                    target.volumeHigh = source.volumeHigh;
                }

                if (target.audioMode == null) target.audioMode = source.audioMode;
                if (target.audioModeIndicator == null)
                {
                    target.audioModeIndicator = source.audioModeIndicator;
                    target.audio3d = source.audio3d;
                    target.audio2d = source.audio2d;
                    target.audio3dColor = source.audio3dColor;
                    target.audio2dColor = source.audio2dColor;
                }

                if (target.mute == null) target.mute = source.mute;
                if (target.muteIndicator == null)
                {
                    target.muteIndicator = source.muteIndicator;
                    target.muted = source.muted;
                    target.unmuted = source.unmuted;
                    target.mutedColor = source.mutedColor;
                    target.unmutedColor = source.unmutedColor;
                }

                if (target.tvLock == null) target.tvLock = source.tvLock;
                if (target.tvLockIndicator == null)
                {
                    target.tvLockIndicator = source.tvLockIndicator;
                    target.locked = source.locked;
                    target.unlocked = source.unlocked;
                    target.lockedColor = source.lockedColor;
                    target.unlockedColor = source.unlockedColor;
                }

                if (target.syncMode == null) target.syncMode = source.syncMode;
                if (target.syncModeIndicator == null)
                {
                    target.syncModeIndicator = source.syncModeIndicator;
                    target.syncEnabled = source.syncEnabled;
                    target.syncDisabled = source.syncDisabled;
                    target.syncEnabledColor = source.syncEnabledColor;
                    target.syncDisabledColor = source.syncDisabledColor;
                }

                if (target.loopMode == null) target.loopMode = source.loopMode;
                if (target.loopModeIndicator == null)
                {
                    target.loopModeIndicator = source.loopModeIndicator;
                    target.loopEnabled = source.loopEnabled;
                    target.loopDisabled = source.loopDisabled;
                    target.loopEnabledColor = source.loopEnabledColor;
                    target.loopDisabledColor = source.loopDisabledColor;
                }

                if (string.IsNullOrEmpty(target.emptyTitlePlaceholder)) target.emptyTitlePlaceholder = source.emptyTitlePlaceholder;
                if (target.loadingBar == null) target.loadingBar = source.loadingBar;
                if (target.loadingSpinner == null)
                {
                    target.loadingSpinner = source.loadingSpinner;
                    target.loadingSpinnerContainer = source.loadingSpinnerContainer;
                    target.loadingSpinReverse = source.loadingSpinReverse;
                    target.loadingSpinSpeed = source.loadingSpinSpeed;
                }

                if (target.currentTimeDisplay == null) target.currentTimeDisplay = source.currentTimeDisplay;
                if (target.currentTimeDisplayTMP == null) target.currentTimeDisplayTMP = source.currentTimeDisplayTMP;
                if (target.endTimeDisplay == null) target.endTimeDisplay = source.endTimeDisplay;
                if (target.endTimeDisplayTMP == null) target.endTimeDisplayTMP = source.endTimeDisplayTMP;
                if (target.infoDisplay == null) target.infoDisplay = source.infoDisplay;
                if (target.infoDisplayTMP == null) target.infoDisplayTMP = source.infoDisplayTMP;
                if (target.clockTimeDisplay == null) target.clockTimeDisplay = source.clockTimeDisplay;
                if (target.clockTimeDisplayTMP == null) target.clockTimeDisplayTMP = source.clockTimeDisplayTMP;
            }
        }


        #region Internal Scene Management Stuff

        [InitializeOnLoadMethod]
        internal static void SceneInit()
        {
            // ProTV requires the spritepacker to be enabled
            if (UnityEditor.EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
                UnityEditor.EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOnAtlas;

            EditorApplication.playModeStateChanged -= SceneCleanup;
            EditorApplication.playModeStateChanged += SceneCleanup;
        }

        internal static void SceneCleanup(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
#if VPM_RESOLVER
                if (ATEditorUtility.HasComponentInScene<TVManager>())
                {
                    UnityEditor.PackageManager.PackageInfo shimPackage = AssetDatabase
                        .FindAssets("package")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                        .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                        .FirstOrDefault(x => x != null && x.name == videoPlayerShimPackage);
                    if (!File.Exists(videoPlayerShimFileCheck) && shimPackage == null)
                    {
                        bool ask = SessionState.GetBool("VideoPlayerShimRejected", false);
                        if (!ask)
                        {
                            ask = EditorUtility.DisplayDialog(
                                I18n.Tr("VideoPlayerShim Missing"),
                                I18n.Tr("The VideoPlayerShim tool is not present in the project. It is highly recommended to import the tool to enable testing video players in playmode. Do you wish to exit playmode and import?"),
                                I18n.Tr("Yes"), I18n.Tr("No")
                            );
                            if (ask)
                            {
                                openShimWindow = true;
                                EditorApplication.ExitPlaymode();
                            }
                            else SessionState.SetBool("VideoPlayerShimRejected", true);
                        }
                    }
                }
#endif
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                VPManager[] vps = ATEditorUtility.GetComponentsInScene<VPManager>();
                foreach (var vp in vps)
                {
                    vp.gameObject.SetActive(false);
                }
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                TVManager[] tvs = ATEditorUtility.GetComponentsInScene<TVManager>();
                foreach (var tv in tvs)
                {
                    if (tv.customMaterials.Length > 0)
                    {
                        for (var index = 0; index < tv.customMaterials.Length; index++)
                        {
                            var mat = tv.customMaterials[index];
                            if (mat == null) continue;
                            var prop = tv.customMaterialProperties[index];
                            if (tv.customTexture == null) mat.SetTexture(prop, null);
                            mat.SetMatrix("_VideoData", Matrix4x4.zero);
                        }
                    }

                    UpdateCustomTextureForEditorPreview(tv);
                    if (SceneView.lastActiveSceneView != null) EditorUtility.SetDirty(SceneView.lastActiveSceneView);
                    if (SceneView.currentDrawingSceneView != null) EditorUtility.SetDirty(SceneView.currentDrawingSceneView);
                }

                // clear any video texture/data left over from playmode in the global shader space
                VRCShader.SetGlobalTexture(VRCShader.PropertyToID(TVManager.shaderNameGlobal_Udon_VideoTex), null);
                VRCShader.SetGlobalMatrix(VRCShader.PropertyToID(TVManager.shaderNameGlobal_Udon_VideoData), Matrix4x4.zero);

#if VPM_RESOLVER
                if (openShimWindow)
                {
                    openShimWindow = false;
                    ShimImportWindow.Open();
                }
#endif
            }
        }

        internal static void UpdateAllCustomTexturesForEditorPreview()
        {
            if (!ProTVEditorPrefs.GetBool(ProTVEditorPrefs.PreviewCustomTextures, false)) return;
            TVManager[] tvs = ATEditorUtility.GetComponentsInScene<TVManager>();
            foreach (var tv in tvs) UpdateCustomTextureForEditorPreview(tv);
        }

        internal static void UpdateCustomTextureForEditorPreview(TVManager tv)
        {
            Shader.SetGlobalTexture("_Udon_VideoTex", null);
            Shader.SetGlobalVector("_Udon_VideoTex_ST", new Vector4(1, 1, 0, 0));
            Shader.SetGlobalMatrix("_Udon_VideoData", Matrix4x4.identity);
            if (tv.customTexture == null) return;
            if (tv.customTexture == RenderTexture.active) return; // don't update when it's active, prevents certain edge cases
            // reset the contents of the blit texture
            if (tv.customTexture.IsCreated()) tv.customTexture.Release();
            if (!ProTVEditorPrefs.GetBool(ProTVEditorPrefs.PreviewCustomTextures, false)) return;
            if (tv.defaultStandbyTexture != null)
            {
                if (tv.autoResizeTexture)
                {
                    tv.customTexture.width = tv.defaultStandbyTexture.width;
                    tv.customTexture.height = tv.defaultStandbyTexture.height;
                }

                if (!tv.customTexture.IsCreated()) tv.customTexture.Create();
                // reset blit material data
                var blitMat = tv.blitMaterial != null ? tv.blitMaterial : AssetDatabase.LoadAssetAtPath<Material>(blitMaterialPath);
                blitMat.SetFloat("_SkipGamma", 0);
                blitMat.SetFloat("_AVPro", 0);
                // cannot use _MainTex_ST due to unity shenanigans overriding the value during blit
                blitMat.SetVector("_MainTex_ST", new Vector4(1, 1, 0, 0));
                blitMat.SetVector("_MainTex_ST_Override", new Vector4(1, 1, 0, 0));
                // do not aspect the render if 3D mode is enabled, leave that up to a 3D shader downstream
                blitMat.SetFloat("_ForceAspect", 0);
                blitMat.SetFloat("_3D", 0);
                // make temp material to run the injection blit operation
                var mat = new Material(blitMat);
                // do not aspect the render if 3D mode is enabled, leave that up to a 3D shader downstream
                mat.SetFloat("_ForceAspect", tv.applyAspectToBlit ? tv.targetAspectRatio : 0);
                mat.SetFloat("_3D", (float)tv.standby3dMode);
                Graphics.Blit(tv.defaultStandbyTexture, tv.customTexture, blitMat, 1);
            }
            else if (tv.autoResizeTexture)
            {
                tv.customTexture.width = 16;
                tv.customTexture.height = 16;
            }

            if (tv.enableGSV) Shader.SetGlobalTexture("_Udon_VideoTex", tv.defaultStandbyTexture);

            foreach (var rtgi in ATEditorUtility.GetComponentsInScene<RTGIUpdater>())
            {
                rtgi.GetComponent<Renderer>().UpdateGIMaterials();
            }
        }

        private static void sceneOpenSetup(Scene scene, OpenSceneMode mode) => UpdateAllCustomTexturesForEditorPreview();
        private static void sceneSaveSetup(Scene scene) => UpdateAllCustomTexturesForEditorPreview();

        [InitializeOnLoadMethod]
        private static void setupPreviewTextureBlitRestore()
        {
            EditorSceneManager.sceneOpened -= sceneOpenSetup;
            EditorSceneManager.sceneOpened += sceneOpenSetup;
            EditorSceneManager.sceneSaved -= sceneSaveSetup;
            EditorSceneManager.sceneSaved += sceneSaveSetup;
        }

        #endregion
    }

    #region AudioLink Scripting Define Fixes

    // TODO: Remove this logic once UdonSharp compiler has proper support for assembly definition version defines (or udon2 comes out)

    [InitializeOnLoad]
    internal static class AudioLinkScriptingDefineHandler
    {
        private const string audioLinkPackageName = "com.llealloo.audiolink";
        private static readonly List<(string[], string)> releases = new List<(string[], string)>();

        static AudioLinkScriptingDefineHandler()
        {
            releases.Clear();
            #if VPM_RESOLVER // if resolver is present, semantic version ranges can be used for more accurate matching
            releases.Add((new[]{">=1.0.0"}, "AUDIOLINK_V1"));
            #else
            releases.Add((new[]{"1"}, "AUDIOLINK_V1"));
            #endif
            AssetDatabase.onImportPackageItemsCompleted -= UpdateDefines;
            AssetDatabase.onImportPackageItemsCompleted += UpdateDefines;
            AssemblyReloadEvents.beforeAssemblyReload -= ReloadDefines;
            AssemblyReloadEvents.beforeAssemblyReload += ReloadDefines;
        }

        private static void UpdateDefines(string[] packagesImported)
        {
            ReloadDefines();
        }

        private static void ReloadDefines()
        {
            var alv = ATEditorUtility.GetPackageInfo(audioLinkPackageName);
            if (alv != null)
            {
                foreach (var (checks, vdefine) in releases)
                {
#if VPM_RESOLVER
                    if (checks.Any(c => SemanticVersioning.Range.IsSatisfied(c, alv.version, true))) 
                        ATEditorUtility.AddScriptingDefine(vdefine);
#else
                    if (checks.Any(c => int.Parse(alv.version.Split('.')[0]) >= int.Parse(c))) 
                        ATEditorUtility.AddScriptingDefine(vdefine);
#endif
                    else
                        ATEditorUtility.RemoveScriptingDefine(vdefine);
                }
            }
            else
            {
                foreach (var (_, vdefine) in releases)
                    ATEditorUtility.RemoveScriptingDefine(vdefine);
            }

            if (!Directory.Exists("Packages/" + audioLinkPackageName))
            {
                // remove the canonical defines explicitly if the package is no longer present in the project.
                if (ATEditorUtility.HasScriptingDefine("AUDIOLINK")) ATEditorUtility.RemoveScriptingDefine("AUDIOLINK");
                if (ATEditorUtility.HasScriptingDefine("AUDIOLINK_V1")) ATEditorUtility.RemoveScriptingDefine("AUDIOLINK_V1");
            }

            // old scripting defines from protv 3 alpha. Needs to be removed to avoid certain compiler error scenarios.
            // these defines are only used for version defines.
            if (ATEditorUtility.HasScriptingDefine("AUDIOLINK_0")) ATEditorUtility.RemoveScriptingDefine("AUDIOLINK_0");
            if (ATEditorUtility.HasScriptingDefine("AUDIOLINK_1")) ATEditorUtility.RemoveScriptingDefine("AUDIOLINK_1");
        }
    }

    #endregion
}