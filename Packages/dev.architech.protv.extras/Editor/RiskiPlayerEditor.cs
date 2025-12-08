using System.Linq;
using System.Reflection;
using ArchiTech.ProTV.Editor;
using UnityEditor;
using UnityEngine;

namespace RiskiVR
{
    [CustomEditor(typeof(RiskiPlayer))]
    internal class RiskiPlayerEditor : UnityEditor.Editor
    {
        private int toolbarInt = 0;
        private readonly string[] toolbarStrings = { "Credits", "TVManager", "RiskiPlayer UI" };
        private const string themeRootFolder = "Packages/dev.architech.protv.extras/Resources/Themes/RiskiPlayer";
        private const string txtLogoFile = "/Textures/RiskiPlayer LogoTXT.png";

        private RiskiPlayer riskiPlayer;
        private UnityEditor.Editor tvEditor;
        private string protvVersion;
        private Texture2D riskiTextLogo;

        private GUIStyle headerText;
        private GUIStyle subText;

        private void OnEnable()
        {
            riskiPlayer = (RiskiPlayer)target;
            protvVersion = AssetDatabase
                .FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                .FirstOrDefault(x => x != null && x.name == ProTVEditorUtility.packageName)
                ?.version;

            riskiTextLogo = (Texture2D)AssetDatabase.LoadAssetAtPath(themeRootFolder + txtLogoFile, typeof(Texture2D));
        }

        public override void OnInspectorGUI()
        {
            if (tvEditor == null)
            {
                tvEditor = CreateEditor(riskiPlayer.tv, typeof(ProTVEditorUtility).Assembly.GetType("ArchiTech.ProTV.Editor.TVManagerEditor"));
                var onEnableInfo = typeof(UnityEditor.Editor).GetMethod("OnEnable", (BindingFlags)~0);
                if (onEnableInfo != null) onEnableInfo.Invoke(tvEditor, null);
                tvEditor.serializedObject.UpdateIfRequiredOrScript();
                tvEditor.OnInspectorGUI();
            }

            if (riskiTextLogo != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                GUILayout.Label(riskiTextLogo, GUILayout.Width(400f), GUILayout.Height(100f));
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            }

            #region TextStyles

            headerText = headerText ?? new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            subText = subText ?? new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };

            #endregion

            GUILayout.Label($"Made by RiskiVR ({riskiPlayer.version})", headerText);
            GUILayout.Label($"Powered by ProTV (v{protvVersion})", subText);


            var newToolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
            var changed = newToolbarInt != toolbarInt;
            toolbarInt = newToolbarInt;
            EditorGUILayout.Space(5f);
            switch (toolbarInt)
            {
                case 0:
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("RiskiVR", GUILayout.Width(100f))) Application.OpenURL("https://riskivr.com");
                    GUILayout.Label("- RiskiPlayer");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("ArchiTechVR", GUILayout.Width(100f))) Application.OpenURL("https://protv.dev/");

                    GUILayout.Label("- ProTV");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("VRChat", GUILayout.Width(100f))) Application.OpenURL("https://udonsharp.docs.vrchat.com");

                    GUILayout.Label("- UdonSharp");
                    GUILayout.EndHorizontal();
                    break;

                case 1:
                    if (tvEditor == null)
                    {
                        tvEditor = CreateEditor(riskiPlayer.tv, typeof(ProTVEditorUtility).Assembly.GetType("ArchiTech.ProTV.Editor.TVManagerEditor"));
                        var onEnableInfo = typeof(UnityEditor.Editor).GetMethod("OnEnable", (BindingFlags)~0);
                        if (onEnableInfo != null) onEnableInfo.Invoke(tvEditor, null);
                    }

                    if (changed) tvEditor.serializedObject.UpdateIfRequiredOrScript();
                    tvEditor.OnInspectorGUI();
                    break;

                case 2:
                    DrawDefaultInspector();
                    break;
            }
        }
    }
}