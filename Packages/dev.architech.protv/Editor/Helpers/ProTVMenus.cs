using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if VPM_RESOLVER
using VRC.PackageManagement.Core.Types;
using VRC.PackageManagement.Resolver;
#endif

#pragma warning disable CS0618

namespace ArchiTech.ProTV.Editor
{
    internal static class ProTVMenus
    {
        [MenuItem("CONTEXT/TVManagerV2/Upgrade Component to TVManager")]
        public static void MigrateTVManager(MenuCommand menuCommand)
        {
            ATEditorUtility.SwapUdonSharpComponentTypeTo<TVManager>((UdonSharpBehaviour)menuCommand.context);
        }

        [MenuItem("CONTEXT/AudioLinkAdapter/Upgrade Component to AudioAdapter")]
        public static void MigrateAudioLinkAdapter(MenuCommand menuCommand)
        {
            ATEditorUtility.SwapUdonSharpComponentTypeTo<AudioAdapter>((UdonSharpBehaviour)menuCommand.context);
        }

        [MenuItem("CONTEXT/TVUsernameWhitelist/Upgrade Component to TVManagedWhitelist")]
        public static void MigrateTVUsernameWhitelist(MenuCommand menuCommand)
        {
            ATEditorUtility.SwapUdonSharpComponentTypeTo<TVManagedWhitelist>((UdonSharpBehaviour)menuCommand.context);
        }

        [MenuItem("Tools/ProTV/Update Scene", false, 0)]
        public static void RunBuildChecks() => new ProTVBuildChecks().RunChecks();

        [MenuItem("Tools/ProTV/Open Build Checks Window")]
        public static void OpenBuildLog() => ProTVBuildWindow.Open();

#if VPM_RESOLVER
        [MenuItem("Tools/ProTV/Enable Media Playback In Unity")]
        public static void ImportVideoPlayerShim()
        {
            var project = new UnityProject(Resolver.ProjectDir);
            bool inProject = project.HasPackage(ProTVEditorUtility.videoPlayerShimPackage);
            var vpm = project.VPMProvider.GetPackage(ProTVEditorUtility.videoPlayerShimPackage);
            if (inProject && vpm == null)
            {
                EditorUtility.DisplayDialog(
                    I18n.Tr("Enable Media Playback in Editor"),
                    I18n.Tr("ArchiTech.VideoPlayerShim was not imported as a VPM package. You will need to update the version manually."),
                    I18n.Tr("Continue")
                );
            }
            else if (EditorUtility.DisplayDialog(
                         I18n.Tr("Enable Media Playback in Editor"),
                         I18n.Tr("This will import the latest ArchiTech.VideoPlayerShim package. This can take up to a few minutes to resolve."),
                         I18n.Tr("Continue"),
                         I18n.Tr("Cancel")
                     ))
            {
                project.AddVPMPackage(ProTVEditorUtility.videoPlayerShimPackage, ">=1.1.0");
                Resolver.ForceRefresh();
            }
        }
#endif

        [MenuItem("GameObject/ProTV/AVPro Video Manager", true)]
        [MenuItem("GameObject/ProTV/Unity Video Manager", true)]
        private static bool validateParentTVExists()
        {
            return ProTVEditorUtility.FindParentTVManager(Selection.activeTransform) != null;
        }

        [MenuItem("Tools/ProTV/Add To Scene/Generic", false, 5)]
        private static void CreateNewTVAsRoot() => CreateNewTV(null);

        [MenuItem("GameObject/ProTV/Add To Scene/Generic", false, 5)]
        private static void CreateNewTVAsChild()
        {
            var selected = ProTVEditorUtility.FindParentTVManager(Selection.activeTransform);
            CreateNewTV(selected ? selected.transform.parent : Selection.activeTransform);
        }

        private static void CreateNewTV(Transform parent)
        {
            var go = new GameObject("ProTV Custom");
            go.transform.SetParent(parent);
            var tv = UdonSharpUndo.AddComponent<TVManager>(go);
            var internalGo = new GameObject("Internal");
            internalGo.transform.SetParent(go.transform);
            var syncGo = new GameObject("TVData");
            syncGo.transform.SetParent(internalGo.transform);
            UdonSharpUndo.AddComponent<TVManagerData>(syncGo);
            var authGo = new GameObject("TVAuth");
            authGo.transform.SetParent(internalGo.transform);
            var auth = UdonSharpUndo.AddComponent<TVManagedWhitelist>(authGo);
            tv.authPlugin = auth;
            Undo.RegisterCreatedObjectUndo(go, "Remove New TV");
            ProTVEditorUtility.AddAVProVPManager(internalGo);
            ProTVEditorUtility.AddUnityVPManager(internalGo);
            Selection.activeTransform = go.transform;
        }

        [MenuItem("Tools/ProTV/Add To Scene/Prefab", false, 5)]
        private static void CreatePrefabTVAsRoot() => CreatePrefabTV(null);

        [MenuItem("GameObject/ProTV/Add To Scene/Prefab", false, 5)]
        private static void CreatePrefabTVAsChild()
        {
            var selected = ProTVEditorUtility.FindParentTVManager(Selection.activeTransform);
            CreatePrefabTV(selected ? selected.transform.parent : Selection.activeTransform);
        }

        private static void CreatePrefabTV(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProTVEditorUtility.simpleTVPrefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(go, "Remove created prefab");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ProTV/AVPro Video Manager", false, 5)]
        private static void AddAVProVPManager()
        {
            Selection.activeObject = ProTVEditorUtility.AddAVProVPManager(Selection.activeGameObject);
        }

        [MenuItem("GameObject/ProTV/Unity Video Manager", false, 5)]
        private static void AddUnityVPManager()
        {
            Selection.activeObject = ProTVEditorUtility.AddUnityVPManager(Selection.activeGameObject);
        }

        [MenuItem("GameObject/ProTV/Enable RTGI for MeshRenderer", false, 8)]
        private static void EnableRTGI()
        {
            var go = Selection.activeGameObject;
            if (!go.TryGetComponent(out MeshRenderer renderer)) return;
            Undo.RecordObjects(new Object[] { renderer, go }, "Enabling RGTI");
            if (go.GetComponent<RTGIUpdater>() == null) UdonSharpUndo.AddComponent<RTGIUpdater>(go);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
            if (!Lightmapping.realtimeGI)
                UnityEngine.Debug.LogWarning(I18n.Tr("RTGI applied without Global Illumination enabled. Make sure to enable GI in the Lighting panel for RTGI to work."));
        }

        [MenuItem("GameObject/ProTV/Enable RTGI for MeshRenderer", true, 8)]
        private static bool validateEnableRTGI()
        {
            var go = Selection.activeGameObject;
            return go != null && go.TryGetComponent(out MeshRenderer _) && !go.TryGetComponent(out RTGIUpdater _);
        }


        [MenuItem("GameObject/ProTV/Utility/Add TMP to Dropdown", false, 6)]
        private static void InjectTMPIntoDropdown()
        {
            if (!Selection.activeGameObject.TryGetComponent(out Dropdown dropdown)) return;
            ProTVEditorUtility.InjectTMPIntoDropdown(dropdown, "Select", "Option A");
        }


        [MenuItem("GameObject/ProTV/Utility/Add TMP to Dropdown", true, 6)]
        private static bool validateInjectTMPIntoDropdown()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out Dropdown _);
        }

        [MenuItem("GameObject/ProTV/Utility/Reset Children Z Scale")]
        private static void resetZScales()
        {
            var children = Selection.activeTransform.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                var localScale = child.localScale;
                if (localScale.z != 0) continue;
                Undo.RecordObject(child, "Resetting Z scale");
                var zscale = Mathf.Max(localScale.x, localScale.y);
                localScale = new Vector3(localScale.x, localScale.y, zscale);
                child.localScale = localScale;
            }
        }

        [MenuItem("Tools/ProTV/Generate/Playlist from Youtube")]
        public static void GeneratePlaylist()
        {
            ProTVGeneratorsWindow window = (ProTVGeneratorsWindow)EditorWindow.GetWindow(typeof(ProTVGeneratorsWindow));
            window.minSize = new Vector2(200, 200);
            window.titleContent = new GUIContent(I18n.Tr("Generate a ProTV Playlist from..."));
            window.Show();
        }

        private const string previewCustomTexturesPath = "Tools/ProTV/Preview Custom Textures";

        [MenuItem(previewCustomTexturesPath)]
        private static void PreviewCustomTextures()
        {
            var previewActive = !ProTVEditorPrefs.GetBool(ProTVEditorPrefs.PreviewCustomTextures, false);
            ProTVEditorPrefs.SetBool(ProTVEditorPrefs.PreviewCustomTextures, previewActive);
            ProTVEditorUtility.UpdateAllCustomTexturesForEditorPreview();
        }

        [MenuItem(previewCustomTexturesPath, true)]
        private static bool validatePreviewCustomTextures()
        {
            var previewActive = ProTVEditorPrefs.GetBool(ProTVEditorPrefs.PreviewCustomTextures, false);
            Menu.SetChecked(previewCustomTexturesPath, previewActive);
            return true;
        }

        // [MenuItem("CONTEXT/AudioSource/Log Rolloff Keyframes")]
        // private static void logRolloffKeyframes(MenuCommand menuCommand)
        // {
        //     var audioSource = (AudioSource)menuCommand.context;
        //     var curve = audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
        //     UnityEngine.Debug.Log(string.Join("\n", curve.keys.Select(k=>$"new Keyframe({k.time}f, {k.value}f, {k.inTangent}f, {k.outTangent}f, {k.inWeight}f, {k.outWeight}f),").ToArray()));
        // }
    }
}