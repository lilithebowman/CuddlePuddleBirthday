#if VPM_RESOLVER
using System;
using System.IO;
using System.Linq;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEngine;
using VRC.PackageManagement.Core.Types;
using VRC.PackageManagement.Resolver;

namespace ArchiTech.ProTV.Editor
{
    public class ShimImportWindow : ATEditorWindow
    {
        private bool hasAVPro = false;
        private bool hasShim = false;
        private bool refresh = false;

        public static void Open()
        {
            ShimImportWindow window = (ShimImportWindow)GetWindow(typeof(ShimImportWindow));
            window.minSize = new Vector2(800, 200);
            window.maxSize = new Vector2(800, 200);
            window.titleContent = new GUIContent(I18n.Tr("Import Packages"));
            window.Show();
        }

        private void OnEnable()
        {
            refresh = false;
            UnityEditor.PackageManager.PackageInfo shimPackage = AssetDatabase
                .FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                .FirstOrDefault(x => x != null && x.name == ProTVEditorUtility.videoPlayerShimPackage);
            hasShim = File.Exists(ProTVEditorUtility.videoPlayerShimFileCheck) || shimPackage != null;
            hasAVPro = File.Exists(ProTVEditorUtility.avproFileCheck);
        }

        private void OnGUI()
        {
            if (refresh) OnEnable();
            Spacer(25f);
            EditorGUILayout.LabelField(I18n.Tr("To enable Media Playback in editor Playmode, you will want to import both of the below packages."));
            Spacer(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                Spacer(10f);
                EditorGUILayout.PrefixLabel(I18n.Tr("Video Player Shim"));
                if (hasShim) EditorGUILayout.LabelField(I18n.Tr("Already Imported"));
                else
                {
                    if (GUILayout.Button(I18n.Tr("Import Package")))
                    {
                        refresh = true;
                        var project = new UnityProject(Resolver.ProjectDir);
                        project.AddVPMPackage(ProTVEditorUtility.videoPlayerShimPackage, "^1.1.0");
                        Resolver.ForceRefresh();
                    }
                }
            }

            Spacer(25f);

            using (new EditorGUILayout.HorizontalScope())
            {
                Spacer(10f);
                EditorGUILayout.PrefixLabel(I18n.Tr("AVPro"));
                if (hasAVPro) EditorGUILayout.LabelField(I18n.Tr("Already Imported"));
                else
                {
                    if (GUILayout.Button(I18n.Tr("Open Download URL")))
                        Application.OpenURL(ProTVEditorUtility.avproPackageSrc);

                    if (GUILayout.Button(I18n.Tr("Import Package")))
                        importAssetPrompt();
                }
            }

            EditorGUILayout.LabelField(ProTVEditorUtility.avproPackageSrc);
        }

        /// <summary>
        /// Open desired URL in browser to prompt to user to download the file, then spawn the open file panel for the user to select the downloaded file.
        /// </summary>
        /// <param name="src"></param>
        private static void importAssetPrompt()
        {
            string selected = EditorUtility.OpenFilePanel(
                I18n.Tr("Import the Downloaded Package"),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "unitypackage");
            if (!string.IsNullOrWhiteSpace(selected)) AssetDatabase.ImportPackage(selected, false);
        }
    }
}
#endif