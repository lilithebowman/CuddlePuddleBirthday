using System;
using System.Linq;
using ArchiTech.SDK.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using VRC.Core;

namespace ArchiTech.Umbrella.Editor
{
    /// <summary>
    /// This tool enables bulk update of Font assets on both Unity UI and TextMeshPro components.
    /// Commissioned by the user DIGITAL.
    /// </summary>
    public class FontEnforcementWindow : ATEditorWindow
    {
        [MenuItem("Tools/Umbrella/Font Enforcement", false, 10)]
        [MenuItem("Window/Text/Font Enforcement", false, 2024)]
        [MenuItem("Window/TextMeshPro/Font Enforcement", false, 2024)]
        public static void Open()
        {
            FontEnforcementWindow window = (FontEnforcementWindow)GetWindow(typeof(FontEnforcementWindow));
            window.minSize = new Vector2(300, 500);
            window.maxSize = new Vector2(900, 900);
            window.titleContent = new GUIContent("Font Enforcement");
            window.Show();
        }

        private Text[] _texts;
        private bool[] _textsSelection;
        private string[] _textsLabels;
        private TMP_Text[] _tmpTexts;
        private bool[] _tmpTextsSelection;
        private string[] _tmpTextsLabels;

        private Vector2 scrollPos;
        private bool cacheOnLoad;

        private Font font = null;
        private TMP_FontAsset tmpFont = null;
        private Material tmpMat = null;

        private GUIStyle _pathStyle = null;

        private enum Tab
        {
            UnityUI,
            TextMeshPro
        }

        private Tab currentTab = Tab.UnityUI;

        private GUIStyle PathStyle
        {
            get
            {
                if (_pathStyle == null)
                {
                    _pathStyle = new GUIStyle(EditorGUIUtility.isProSkin ? EditorStyles.whiteMiniLabel : EditorStyles.miniLabel)
                    {
                        fontSize = 10
                    };
                }

                return _pathStyle;
            }
        }

        private void OnEnable()
        {
            CacheComponents();
            EditorSceneManager.activeSceneChangedInEditMode -= SceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += SceneChanged;
            EditorSceneManager.sceneOpened -= SceneLoaded;
            EditorSceneManager.sceneOpened += SceneLoaded;
        }

        private void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= SceneChanged;
            EditorSceneManager.sceneOpened -= SceneLoaded;
        }

        private void CacheComponents()
        {
            // gather all possible font controls here
            // include TMP font control
            _texts = ATEditorUtility.GetComponentsInScene<Text>();
            _textsLabels = _texts.Select(t => t.transform.GetHierarchyPath()).ToArray();
            _textsSelection = new bool[_texts.Length];
            Array.Fill(_textsSelection, true);
            _tmpTexts = ATEditorUtility.GetComponentsInScene<TMP_Text>();
            _tmpTextsLabels = _tmpTexts.Select(t => t.transform.GetHierarchyPath()).ToArray();
            _tmpTextsSelection = new bool[_tmpTexts.Length];
            Array.Fill(_tmpTextsSelection, true);
            scrollPos = Vector2.zero;
        }

        private void SceneChanged(Scene stale, Scene fresh)
        {
            if (fresh.isLoaded) CacheComponents();
            else cacheOnLoad = true;
        }

        private void SceneLoaded(Scene fresh, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
            {
                CacheComponents();
            }
            else if (cacheOnLoad)
            {
                cacheOnLoad = false;
                CacheComponents();
            }
        }

        private void OnGUI()
        {
            using (HArea)
            {
                if (GUILayout.Button("Unity UI"))
                {
                    currentTab = Tab.UnityUI;
                    scrollPos = Vector2.zero;
                }

                if (GUILayout.Button("Text Mesh Pro"))
                {
                    currentTab = Tab.TextMeshPro;
                    scrollPos = Vector2.zero;
                }
            }

            if (currentTab == Tab.UnityUI) DrawUnityUIOptions();
            else if (currentTab == Tab.TextMeshPro) DrawTMPOptions();
        }

        private void DrawUnityUIOptions()
        {
            using (VBox)
            {
                font = (Font)EditorGUILayout.ObjectField("Font Asset", font, typeof(Font), false);
                Spacer(EditorGUIUtility.singleLineHeight);
            }

            bool hasFont = font != null;

            Spacer();

            using (HArea)
            {
                EditorGUILayout.LabelField("Text Component");
                EditorGUILayout.LabelField("Selected", GUILayout.Width(60));
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);
            for (int i = 0; i < _texts.Length; i++)
            {
                EditorGUILayout.LabelField(_textsLabels[i], PathStyle);
                using (HArea)
                {
                    using (DisabledScope()) EditorGUILayout.ObjectField(_texts[i], typeof(Text), true);
                    bool noChanges = !hasFont || _texts[i].font == font;
                    if (noChanges) EditorGUILayout.LabelField(!hasFont ? "" : "OK", GUILayout.Width(50));
                    else _textsSelection[i] = EditorGUILayout.Toggle(_textsSelection[i], GUILayout.Width(50));
                }

                Spacer(3f);
            }

            EditorGUILayout.EndScrollView();
            using (DisabledScope(!hasFont))
            {
                if (GUILayout.Button("Apply Font Changes"))
                {
                    for (int i = 0; i < _texts.Length; i++)
                    {
                        if (!_textsSelection[i]) continue;
                        var text = _texts[i];
                        Undo.RecordObject(text, "Font change");
                        text.font = font;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    }
                }
            }
        }

        private void DrawTMPOptions()
        {
            using (VBox)
            {
                tmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", tmpFont, typeof(TMP_FontAsset), false);
                tmpMat = (Material)EditorGUILayout.ObjectField("Font Material", tmpMat, typeof(Material), false);
            }

            bool hasFont = tmpFont != null;
            bool hasMat = tmpMat != null;
            bool noSelection = !hasFont && !hasMat;

            Spacer();

            using (HArea)
            {
                EditorGUILayout.LabelField("Text Component");
                EditorGUILayout.LabelField("Selected", GUILayout.Width(60));
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);
            for (int i = 0; i < _tmpTexts.Length; i++)
            {
                EditorGUILayout.LabelField(_tmpTextsLabels[i], PathStyle);
                using (HArea)
                {
                    using (DisabledScope()) EditorGUILayout.ObjectField(_tmpTexts[i], _tmpTexts[i].GetType(), true);
                    bool noChanges = (!hasFont || _tmpTexts[i].font == tmpFont) && (!hasMat || _tmpTexts[i].fontSharedMaterial == tmpMat);
                    if (noChanges) EditorGUILayout.LabelField(noSelection ? "" : "OK", GUILayout.Width(50));
                    else _tmpTextsSelection[i] = EditorGUILayout.Toggle(_tmpTextsSelection[i], GUILayout.Width(50));
                }

                Spacer(3f);
            }

            EditorGUILayout.EndScrollView();
            using (DisabledScope(noSelection))
            {
                if (GUILayout.Button("Apply Font Changes"))
                {
                    for (int i = 0; i < _tmpTexts.Length; i++)
                    {
                        if (!_tmpTextsSelection[i]) continue;
                        var text = _tmpTexts[i];
                        Undo.RecordObject(text, "Font change");
                        if (hasFont) text.font = tmpFont;
                        if (hasMat) text.fontSharedMaterial = tmpMat;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    }
                }
            }
        }
    }
}