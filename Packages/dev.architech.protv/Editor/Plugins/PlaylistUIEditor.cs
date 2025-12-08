using ArchiTech.SDK.Editor;
using TMPro;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PlaylistUI))]
    public class PlaylistUIEditor : TVPluginUIEditor
    {
        internal const int latestTemplateVersion = 1;

        private PlaylistUI script;

        private PlaylistEditor.ChangeAction updateMode = PlaylistEditor.ChangeAction.NOOP;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (PlaylistUI)target;
        }

        protected override void RenderChangeCheck()
        {
            bool isChanged = false;
            isChanged |= DrawVariableWithDropdown(nameof(script.playlist));
            DrawCustomHeaderLarge("General Settings");
            using (VBox)
            {
                isChanged |= DrawVariablesByName(nameof(script.defaultImage));
            }

            DrawCustomHeaderLarge("UI References");
            using (VBox) drawUIComponents(isChanged);
        }

        protected override void SaveData()
        {
            if (updateMode == PlaylistEditor.ChangeAction.NOOP) return; // don't save the data if current op was none
            if (updateMode != PlaylistEditor.ChangeAction.OTHER)
            {
                UpdateScene();
                init = false;
            }

            updateMode = PlaylistEditor.ChangeAction.NOOP;
        }

        public void UpdateScene()
        {
            if (script.scrollView == null || script.scrollView.viewport == null)
            {
                Debug.LogError("ScrollRect or associated viewport is null. Ensure they are connected in the inspector.");
                return;
            }

            switch (updateMode)
            {
                case PlaylistEditor.ChangeAction.UPDATEVIEW:
                case PlaylistEditor.ChangeAction.UPDATESELF:
                    UpdateContents(script, 0);
                    break;
                case PlaylistEditor.ChangeAction.UPDATEALL:
                    RebuildScene(script, 0);
                    break;
                default:
                    RebuildScene(script, 0);
                    break;
            }
        }

        public static void RebuildScene(PlaylistUI playlistUI, int offset = 0)
        {
            if (playlistUI == null) return;
            if (playlistUI.scrollView == null || playlistUI.listContainer == null || playlistUI.template == null)
            {
#pragma warning disable CS0618
                UnityEngine.Debug.LogError($"Playlist {playlistUI.transform.GetHierarchyPath()} is missing required UI components. Cannot rebuild playlist in scene.");
#pragma warning restore CS0618
                return;
            }

            // determine how many entries can be shown within the physical space of the viewport
            var visible = calculateVisibleEntries(playlistUI);
            // destroy and rebuild the list of entries for the visibleCount
            rebuildEntries(playlistUI, visible);
            // re-organize the layout to the viewport's size
            recalculateLayout(playlistUI);
            // update the internal content of each entry with in the range of visibleOffset -> visibleOffset + visibleCount and certain constraints
            UpdateContents(playlistUI, offset);
            // ensure the attached scrollbar has the necessary event listener attached
            attachScrollbarEvent(playlistUI);
        }

        private static int calculateVisibleOffset(PlaylistUI playlistUI, int rawOffset)
        {
            var mainUrls = playlistUI.playlist.storage == null ? playlistUI.playlist.mainUrls : playlistUI.playlist.storage.mainUrls;
            Rect max = playlistUI.scrollView.viewport.rect;
            Rect item = ((RectTransform)playlistUI.template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "page", account for the "extra" overflow row as well.
            var maxRow = (mainUrls.Length - 1) / horizontalCount + 1;
            var contentHeight = maxRow * item.height;
            // clamp the min/max row to the view area boundries
            maxRow = Mathf.Min(maxRow, maxRow - verticalCount);
            if (maxRow == 0) maxRow = 1;

            var maxOffset = maxRow * horizontalCount;
            var currentRow = rawOffset / horizontalCount; // int DIV causes stepped values
            var steppedOffset = currentRow * horizontalCount;
            // currentOffset will be smaller than maxOffset when the scroll limit has not yet been reached
            var targetOffset = Mathf.Min(steppedOffset, maxOffset);

            // update the scrollview content proxy's height
            float scrollHeight = Mathf.Max(contentHeight, max.height + item.height / 2);
            playlistUI.scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scrollHeight);
            if (playlistUI.scrollView.verticalScrollbar != null)
                playlistUI.scrollView.verticalScrollbar.value = 1f - (float)rawOffset / (maxOffset);

            return Mathf.Max(0, targetOffset);
        }

        private static int calculateVisibleEntries(PlaylistUI playlistUI)
        {
            // calculate the x/y entry counts
            Rect max = playlistUI.scrollView.viewport.rect;
            Rect item = ((RectTransform)playlistUI.template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            var verticalCount = Mathf.FloorToInt(max.height / item.height) + 1; // allows Y overflow for better visual flow
            var mainUrls = playlistUI.playlist.storage == null ? playlistUI.playlist.mainUrls : playlistUI.playlist.storage.mainUrls;
            return Mathf.Min(mainUrls.Length, horizontalCount * verticalCount);
        }

        private static void rebuildEntries(PlaylistUI playlistUI, int visible)
        {
            // clear existing entries
            while (playlistUI.listContainer.childCount > 0) DestroyImmediate(playlistUI.listContainer.GetChild(0).gameObject);
            // rebuild entries list
            for (int i = 0; i < visible; i++) createEntry(playlistUI);
        }

        private static void createEntry(PlaylistUI playlistUI)
        {
            // create scene entry
            GameObject entry = Instantiate(playlistUI.template, playlistUI.listContainer, false);
            entry.name = $"Entry ({playlistUI.listContainer.childCount})";
            entry.transform.SetAsLastSibling();

            var behavior = UdonSharpEditorUtility.GetBackingUdonBehaviour(playlistUI);
            var button = entry.GetComponentInChildren<Button>();

            if (playlistUI.selectAction == null)
            {
                // trigger isn't present, put one on the template root
                button = entry.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }

            entry.SetActive(true);
        }

        private static void recalculateLayout(PlaylistUI playlistUI)
        {
            // ensure the content box fills exactly 100% of the viewport.
            playlistUI.listContainer.SetParent(playlistUI.scrollView.viewport);
            playlistUI.listContainer.anchorMin = new Vector2(0, 0);
            playlistUI.listContainer.anchorMax = new Vector2(1, 1);
            playlistUI.listContainer.sizeDelta = new Vector2(0, 0);
            var max = playlistUI.listContainer.rect;
            float maxWidth = max.width;
            float maxHeight = max.height;
            int col = 0;
            int row = 0;
            // template always assumes the anchor PIVOT is located at X=0.0 and Y=1.0 (aka upper left corner)
            // TODO enforce this assumption
            // TODO Take the left-right margins into account for spacing
            // should be able to make the assumption that all entries are the same structure (thus width/height) as template
            Rect tmpl = ((RectTransform)playlistUI.template.transform).rect;
            float entryHeight = tmpl.height;
            float entryWidth = tmpl.width;
            float listHeight = entryHeight;
            bool firstEntry = true;
            for (int i = 0; i < playlistUI.listContainer.childCount; i++)
            {
                RectTransform entry = (RectTransform)playlistUI.listContainer.GetChild(i);
                // expect fill in left to right.
                var X = entryWidth * col;
                // detect if a new row is needed, first row will be row 0 implicitly
                if (firstEntry) firstEntry = false;
                else if (X + entryWidth > maxWidth)
                {
                    // reset the horizontal data
                    col = 0;
                    X = 0f;
                    // horizontal exceeds the shape of the container, shift to the next row
                    row++;
                }

                // calculate the target row
                var Y = entryHeight * row;
                entry.anchoredPosition = new Vector2(X, -Y);
                col++; // target next column
            }

            playlistUI.scrollView.CalculateLayoutInputVertical();
        }

        internal static void UpdateHeader(PlaylistUI ui)
        {
            var script = ui.playlist;
            if (script != null && !string.IsNullOrEmpty(script.header))
            {
                if (ui.headerDisplay != null) ui.headerDisplay.text = script.header;
                if (ui.headerDisplayTMP != null) ui.headerDisplayTMP.text = script.header;
            }
        }

        public static void UpdateContents(PlaylistUI playlistUI, int focus)
        {
            int playlistIndex = calculateVisibleOffset(playlistUI, focus);
            var playlist = playlistUI.playlist;
            var hasStorage = playlist.storage != null;
            var mainUrls = hasStorage ? playlist.storage.mainUrls : playlist.mainUrls;
            int numOfUrls = mainUrls.Length;

            for (int i = 0; i < playlistUI.listContainer.childCount; i++)
            {
                if (playlistIndex >= numOfUrls)
                {
                    // urls have exceeded count, hide the remaining entries
                    playlistUI.listContainer.GetChild(i).gameObject.SetActive(false);
                    continue;
                }

                var entry = playlistUI.listContainer.GetChild(i);
                entry.gameObject.SetActive(true);
                var titles = hasStorage ? playlist.storage.titles : playlist.titles;
                var descriptions = hasStorage ? playlist.storage.descriptions : playlist.descriptions;
                var images = hasStorage ? playlist.storage.images : playlist.images;

                var currentUrl = playlist.showUrls ? mainUrls[playlistIndex].Get() : string.Empty;
                var currentTitle = titles[playlistIndex];
                var currentDescription = descriptions[playlistIndex];
                var currentImage = images[playlistIndex];

                if (currentTitle != null && currentTitle.StartsWith("~")) currentTitle = currentTitle.Substring(1);

                Transform t;

                if (playlistUI.urlDisplay != null)
                {
                    t = entry;
                    if (playlistUI.urlDisplayTmplPath != "") t = entry.Find(playlistUI.urlDisplayTmplPath);
                    var component = t.GetComponent<Text>();
                    component.text = currentUrl;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.urlDisplayTMP != null)
                {
                    t = entry;
                    if (playlistUI.urlDisplayTMPTmplPath != "") t = entry.Find(playlistUI.urlDisplayTMPTmplPath);
                    var component = t.GetComponent<TextMeshProUGUI>();
                    component.text = currentUrl;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.titleDisplay != null)
                {
                    t = entry;
                    if (playlistUI.titleDisplayTmplPath != "") t = entry.Find(playlistUI.titleDisplayTmplPath);
                    var component = t.GetComponent<Text>();
                    component.text = currentTitle;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.titleDisplayTMP != null)
                {
                    t = entry;
                    if (playlistUI.titleDisplayTMPTmplPath != "") t = entry.Find(playlistUI.titleDisplayTMPTmplPath);
                    var component = t.GetComponent<TextMeshProUGUI>();
                    component.text = currentTitle;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.descriptionDisplay != null)
                {
                    t = entry;
                    if (playlistUI.descriptionDisplayTmplPath != "") t = entry.Find(playlistUI.descriptionDisplayTmplPath);
                    var component = t.GetComponent<Text>();
                    component.text = currentDescription;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.descriptionDisplayTMP != null)
                {
                    t = entry;
                    if (playlistUI.descriptionDisplayTMPTmplPath != "") t = entry.Find(playlistUI.descriptionDisplayTMPTmplPath);
                    var component = t.GetComponent<TextMeshProUGUI>();
                    component.text = currentDescription;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.imageDisplay != null)
                {
                    t = entry;
                    if (playlistUI.imageDisplayTmplPath != "") t = entry.Find(playlistUI.imageDisplayTmplPath);
                    var component = t.GetComponent<Image>();
                    component.sprite = currentImage != null ? currentImage : playlist.placeholderImage;
                    EditorUtility.SetDirty(component);
                }

                if (playlistUI.loadingBar != null)
                {
                    t = entry;
                    if (playlistUI.loadingBarTmplPath != "") t = entry.Find(playlistUI.loadingBarTmplPath);
                    var component = t.GetComponent<Slider>();
                    component.SetValueWithoutNotify(0f);
                    EditorUtility.SetDirty(component);
                }

                playlistIndex++;
            }
        }

        private static void attachScrollbarEvent(PlaylistUI playlistUI)
        {
            ATEditorUtility.EnsureSelectableActionEvent(playlistUI.scrollView.verticalScrollbar, playlistUI.scrollView.verticalScrollbar.onValueChanged, playlistUI.UpdateView);
        }


        private void drawUIComponents(bool isChanged)
        {
            isChanged |= DrawVariablesByName(nameof(script.scrollView));

            if (isChanged) updateMode = PlaylistEditor.ChangeAction.OTHER;

            EditorGUI.BeginDisabledGroup(targets.Length > 1);
            DrawVariablesByName(nameof(script.listContainer));

            GUIContent label = GetPropertyLabel(nameof(script.headerDisplay), showHints);
            using (HArea)
            {
                EditorGUILayout.PrefixLabel(label);
                DrawVariablesByNameWithoutLabels(nameof(script.headerDisplay), nameof(script.headerDisplayTMP));
            }

            bool templateChanged = DrawVariablesByName(nameof(script.template)) || script._EDITOR_templateUpgrade < latestTemplateVersion;

            if (script.template != null)
            {
                EditorGUI.BeginChangeCheck();
                if (templateChanged) GUI.changed = true; // enable the lack of template upgrade to trigger the change check below
                EditorGUI.indentLevel++;
                var template = script.template;

                label = GetPropertyLabel(nameof(script.urlDisplay), showHints);
                label.text = "└ " + label.text;
                using (HArea)
                {
                    EditorGUILayout.PrefixLabel(label);
                    DrawVariablesByNameWithoutLabels(nameof(script.urlDisplay), nameof(script.urlDisplayTMP));
                }

                if (!template.IsComponentsInChildren(script.urlDisplay, script.urlDisplayTMP))
                    DisplayTemplateError();

                label = GetPropertyLabel(nameof(script.titleDisplay), showHints);
                label.text = "└ " + label.text;
                using (HArea)
                {
                    EditorGUILayout.PrefixLabel(label);
                    DrawVariablesByNameWithoutLabels(nameof(script.titleDisplay), nameof(script.titleDisplayTMP));
                }

                if (!template.IsComponentsInChildren(script.titleDisplay, script.titleDisplayTMP))
                    DisplayTemplateError();

                label = GetPropertyLabel(nameof(script.descriptionDisplay), showHints);
                label.text = "└ " + label.text;
                using (HArea)
                {
                    EditorGUILayout.PrefixLabel(label);
                    DrawVariablesByNameWithoutLabels(nameof(script.descriptionDisplay), nameof(script.descriptionDisplayTMP));
                }

                if (!template.IsComponentsInChildren(script.descriptionDisplay, script.descriptionDisplayTMP))
                    DisplayTemplateError();

                label = GetPropertyLabel(nameof(script.selectAction), showHints);
                label.text = "└ " + label.text;
                DrawVariablesByNameWithLabel(label, nameof(script.selectAction));
                if (!template.IsComponentsInChildren(script.selectAction)) DisplayTemplateError();

                label = GetPropertyLabel(nameof(script.loadingBar), showHints);
                label.text = "└ " + label.text;
                DrawVariablesByNameWithLabel(label, nameof(script.loadingBar));
                if (!template.IsComponentsInChildren(script.loadingBar)) DisplayTemplateError();

                label = GetPropertyLabel(nameof(script.imageDisplay), showHints);
                label.text = "└ " + label.text;
                DrawVariablesByNameWithLabel(label, nameof(script.imageDisplay));
                if (!template.IsComponentsInChildren(script.imageDisplay)) DisplayTemplateError();

                Spacer(2f);
                EditorGUI.indentLevel--;

                DrawToggleIconsControls(
                    "Autoplay Toggle",
                    nameof(script.autoplay),
                    nameof(script.autoplayIndicator),
                    nameof(script.autoplayOn),
                    nameof(script.autoplayOff),
                    nameof(script.autoplayOnColor),
                    nameof(script.autoplayOffColor)
                );

                if (EditorGUI.EndChangeCheck())
                {
                    var msg = templateChanged ? "Auto-populating the template child references" : "Update template reference paths";
                    using (new SaveObjectScope(script, msg))
                    {
                        if (templateChanged) AutopopulateTemplateFields(script);
                        UpdateTmplPaths(script);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        internal static void AutopopulateTemplateFields(PlaylistUI script)
        {
            var template = script.template;
            if (template == null) return; // no template, no autofill
            // clear old template references that don't match the template
            if (!template.IsComponentsInChildren(script.urlDisplay)) script.urlDisplay = null;
            if (!template.IsComponentsInChildren(script.urlDisplayTMP)) script.urlDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.titleDisplay)) script.titleDisplay = null;
            if (!template.IsComponentsInChildren(script.titleDisplayTMP)) script.titleDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.descriptionDisplay)) script.descriptionDisplay = null;
            if (!template.IsComponentsInChildren(script.descriptionDisplayTMP)) script.descriptionDisplayTMP = null;
            if (!template.IsComponentsInChildren(script.selectAction)) script.selectAction = null;
            if (!template.IsComponentsInChildren(script.loadingBar)) script.loadingBar = null;
            if (!template.IsComponentsInChildren(script.imageDisplay)) script.imageDisplay = null;
            var t_texts = template.GetComponentsInChildren<Text>(true);
            var t_tmpTexts = template.GetComponentsInChildren<TextMeshProUGUI>(true);
            var t_buttons = template.GetComponentsInChildren<Button>(true);
            var t_images = template.GetComponentsInChildren<Image>(true);
            var t_sliders = template.GetComponentsInChildren<Slider>(true);


            foreach (var text in t_texts)
            {
                var textName = text.name.ToLower();
                if (script.urlDisplay == null && textName.Contains("url")) script.urlDisplay = text;
                if (script.titleDisplay == null && textName.Contains("title")) script.titleDisplay = text;
                if (script.descriptionDisplay == null && textName.Contains("desc")) script.descriptionDisplay = text;
            }

            foreach (var tmpText in t_tmpTexts)
            {
                var textName = tmpText.name.ToLower();
                if (script.urlDisplayTMP == null && textName.Contains("url")) script.urlDisplayTMP = tmpText;
                if (script.titleDisplayTMP == null && textName.Contains("title")) script.titleDisplayTMP = tmpText;
                if (script.descriptionDisplayTMP == null && textName.Contains("desc")) script.descriptionDisplayTMP = tmpText;
            }

            foreach (var button in t_buttons)
            {
                if (script.selectAction == null) script.selectAction = button;
            }

            foreach (var slider in t_sliders)
            {
                var sliderName = slider.name.ToLower();
                if (script.loadingBar == null && sliderName.Contains("load")) script.loadingBar = slider;
            }

            foreach (var image in t_images)
            {
                var toggleName = image.name.ToLower();
                if (script.imageDisplay == null && (toggleName.Contains("image") || toggleName.Contains("poster"))) script.imageDisplay = image;
            }

            script._EDITOR_templateUpgrade = latestTemplateVersion;
        }

        internal static void UpdateTmplPaths(PlaylistUI script)
        {
            script.urlDisplayTmplPath = null;
            script.titleDisplayTmplPath = null;
            script.descriptionDisplayTmplPath = null;
            script.urlDisplayTMPTmplPath = null;
            script.titleDisplayTMPTmplPath = null;
            script.descriptionDisplayTMPTmplPath = null;
            script.selectActionTmplPath = null;
            script.loadingBarTmplPath = null;
            script.imageDisplayTmplPath = null;

            if (script.template == null) return; // no template, no paths

#pragma warning disable CS0618
            Transform t = script.template.transform;
            Transform st;
            if (script.urlDisplay != null)
            {
                st = script.urlDisplay.transform;
                script.urlDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.titleDisplay != null)
            {
                st = script.titleDisplay.transform;
                script.titleDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.descriptionDisplay != null)
            {
                st = script.descriptionDisplay.transform;
                script.descriptionDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.urlDisplayTMP != null)
            {
                st = script.urlDisplayTMP.transform;
                script.urlDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.titleDisplayTMP != null)
            {
                st = script.titleDisplayTMP.transform;
                script.titleDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.descriptionDisplayTMP != null)
            {
                st = script.descriptionDisplayTMP.transform;
                script.descriptionDisplayTMPTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.selectAction != null)
            {
                st = script.selectAction.transform;
                script.selectActionTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.loadingBar != null)
            {
                st = script.loadingBar.transform;
                script.loadingBarTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }

            if (script.imageDisplay != null)
            {
                st = script.imageDisplay.transform;
                script.imageDisplayTmplPath = st == t ? "" : st.GetHierarchyPath(t);
            }
#pragma warning restore CS0618
        }

        internal static PlaylistUI MigrateUI(Playlist playlist, PlaylistUI playlistUI = null)
        {
#pragma warning disable CS0612
            if (playlistUI == null) playlistUI = ATEditorUtility.GetOrAddComponent<PlaylistUI>(playlist);
            // references
            playlistUI.scrollView = playlist.scrollView;
            playlistUI.listContainer = playlist.listContainer;
            playlistUI.template = playlist.template;
            playlistUI.urlDisplay = playlist.urlDisplay;
            playlistUI.urlDisplayTMP = playlist.urlDisplayTMP;
            playlistUI.titleDisplay = playlist.titleDisplay;
            playlistUI.titleDisplayTMP = playlist.titleDisplayTMP;
            playlistUI.descriptionDisplay = playlist.descriptionDisplay;
            playlistUI.descriptionDisplayTMP = playlist.descriptionDisplayTMP;
            playlistUI.selectAction = playlist.selectAction;
            playlistUI.imageDisplay = playlist.imageDisplay;
            playlistUI.loadingBar = playlist.loadingBar;
            playlist.scrollView = null;
            playlist.listContainer = null;
            playlist.template = null;
            playlist.urlDisplay = null;
            playlist.urlDisplayTMP = null;
            playlist.titleDisplay = null;
            playlist.titleDisplayTMP = null;
            playlist.descriptionDisplay = null;
            playlist.descriptionDisplayTMP = null;
            playlist.selectAction = null;
            playlist.imageDisplay = null;
            playlist.loadingBar = null;
            // paths
            playlistUI.urlDisplayTmplPath = playlist.urlDisplayTmplPath;
            playlistUI.urlDisplayTMPTmplPath = playlist.urlDisplayTMPTmplPath;
            playlistUI.titleDisplayTmplPath = playlist.titleDisplayTmplPath;
            playlistUI.titleDisplayTMPTmplPath = playlist.titleDisplayTMPTmplPath;
            playlistUI.descriptionDisplayTmplPath = playlist.descriptionDisplayTmplPath;
            playlistUI.descriptionDisplayTMPTmplPath = playlist.descriptionDisplayTMPTmplPath;
            playlistUI.selectActionTmplPath = playlist.selectActionTmplPath;
            playlistUI.loadingBarTmplPath = playlist.loadingBarTmplPath;
            playlistUI.imageDisplayTmplPath = playlist.imageDisplayTmplPath;
            playlist.urlDisplayTmplPath = null;
            playlist.urlDisplayTMPTmplPath = null;
            playlist.titleDisplayTmplPath = null;
            playlist.titleDisplayTMPTmplPath = null;
            playlist.descriptionDisplayTmplPath = null;
            playlist.descriptionDisplayTMPTmplPath = null;
            playlist.selectActionTmplPath = null;
            playlist.loadingBarTmplPath = null;
            playlist.imageDisplayTmplPath = null;
            // save
            playlistUI.playlist = playlist;
            return playlistUI;
#pragma warning restore CS0612
        }
    }
}