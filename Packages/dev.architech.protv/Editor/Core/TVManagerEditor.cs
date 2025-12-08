using System.IO;
using System.Linq;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using HarmonyLib;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Video.Components.AVPro;

#if LTCGI_1
using pi.LTCGI;
#endif

#if AUDIOLINK_0 && !AUDIOLINK_1
using AudioLink = VRCAudioLink;
#endif

namespace ArchiTech.ProTV.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TVManager), true)]
    public class TVManagerEditor : SDK.Editor.ATEventHandlerEditor
    {
        protected override bool autoRenderVariables => false;

        private TVManager script;
        private int initialVideoManager;
        private bool showTexturePreviews;

        private TVPlugin[] detectedPlugins;
        private bool isPrefab = false;
        private bool nestedTVDetected = false;
        private ATReorderableList _customMaterialsList;

        private SerializedProperty _materialProperty;
        private SerializedProperty _textureProperty;
        private SerializedProperty _customTexture;
        private SerializedProperty _gammaZone;
        private SerializedProperty _standbyTexture;
        private SerializedProperty _defaultVideoManager;

        private ATReorderableList _whitelistDomains;

#if AUDIOLINK_0 || AUDIOLINK_1
        private AudioLink.AudioLink audioLinkInScene;
        private AudioAdapter audioAdapterInScene;
        private bool canConnectAudioAdapter = false;
#endif

#if LTCGI_1
        private LTCGI_Controller ltcgiInScene;
        private bool canConnectToLtcgi = false;
#endif

        private Texture linkIcon;
        private Texture checkmarkIcon;

        private bool isAreaLitInScene;

        // private string arealitFolder;
        private GUIStyle noticeStyle;

        private void OnEnable()
        {
            script = target as TVManager;
            if (script == null) return;
            detectedPlugins = ATEditorUtility.GetComponentsInScene<TVPlugin>();
            if (detectedPlugins.Length > 0)
                detectedPlugins = detectedPlugins
                    .OrderBy(plugin => plugin.tv != script)
                    .ThenBy(plugin => plugin.Priority)
                    .ToArray();

            // disallow creating a TV nested within a TV.
            var tv = ProTVEditorUtility.FindParentTVManager(script, false);
            nestedTVDetected = tv != null || script.GetComponentsInChildren<TVManager>(true).Length > 1;
            _materialProperty = serializedObject.FindProperty(nameof(script.customMaterials));
            _textureProperty = serializedObject.FindProperty(nameof(script.customMaterialProperties));
            _customTexture = serializedObject.FindProperty(nameof(script.customTexture));
            _gammaZone = serializedObject.FindProperty(nameof(script.gammaZoneTransformMode));
            _standbyTexture = serializedObject.FindProperty(nameof(script.defaultStandbyTexture));
            _defaultVideoManager = serializedObject.FindProperty(nameof(script.defaultVideoManager));

            _customMaterialsList = new ATReorderableList("Custom Materials") { onDropObject = handleMaterialDrop, onPropertyChange = handleMaterialSwap }
                .AddArrayProperty(_materialProperty, new GUIContent(I18n.Tr("Material")))
                .AddArrayProperty(_textureProperty, new GUIContent(I18n.Tr("Texture Property")));

            _whitelistDomains = new ATReorderableList("Whitelisted Domains") { onContextMenuBuild = handleWhitelistContextMenu }
                .AddArrayProperty(serializedObject.FindProperty(nameof(script.domainWhitelist)), GUIContent.none);

            linkIcon = AssetDatabase.LoadAssetAtPath<Texture>(ProTVEditorUtility.linkIconPath);
            checkmarkIcon = AssetDatabase.LoadAssetAtPath<Texture>(ProTVEditorUtility.checkmarkIconPath);

            UnityEditor.PackageManager.PackageInfo pkg = AssetDatabase
                .FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                .FirstOrDefault(x => x != null && x.name == ProTVEditorUtility.packageName);

            if (pkg != null) SetVariableByName(nameof(script.versionNumber), pkg.version);

            if (script.customTexture != null && script.defaultStandbyTexture != null && !script.customTexture.IsCreated())
                ProTVEditorUtility.UpdateCustomTextureForEditorPreview(script);

            if (!script.gsvfixcheck)
            {
                // if any TVs in the scene have the GSV check flagged, scene has been upgraded, set flag
                var tvs = ATEditorUtility.GetComponentsInScene<TVManager>();
                if (tvs.Count(t => !t.gsvfixcheck) != tvs.Length)
                    SetVariableByName(nameof(script.gsvfixcheck), true);
            }
        }

        protected override void InitData()
        {
            if (noticeStyle == null) noticeStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, alignment = TextAnchor.UpperCenter };
            var videoManagers = NormalizeArray(script.videoManagers, 0, typeof(VPManager));
            var managers = script.GetComponentsInChildren<VPManager>(true);
            var sizeCheckFailure = script.videoManagers == null || videoManagers.Length == 0 || managers.Length != videoManagers.Length;
            if (sizeCheckFailure || System.Array.IndexOf(videoManagers, null) > -1 || managers.Any(m => System.Array.IndexOf(videoManagers, m) == -1))
            {
                // if any managers are missing, or there aren't any, use the current found children
                if (initialVideoManager >= managers.Length) initialVideoManager = 0;
                if (videoManagers.Length > 0)
                    initialVideoManager = System.Array.IndexOf(managers, videoManagers[initialVideoManager]);
                if (initialVideoManager == -1) initialVideoManager = 0;
                SetVariableByName(nameof(script.videoManagers), managers);
                SetVariableByName(nameof(script.defaultVideoManager), initialVideoManager);
            }

            isPrefab = PrefabStageUtility.GetCurrentPrefabStage() != null;
            if (!isPrefab)
            {
#if AUDIOLINK_0 || AUDIOLINK_1
                // only enable audiolink connector when in scene, not prefab stage
                var hasAudioLink = ATEditorUtility.TryGetComponentInScene(out audioLinkInScene);
                var hasAudioAdapter = ATEditorUtility.TryGetComponentInScene(out audioAdapterInScene);
                canConnectAudioAdapter = hasAudioLink && (!hasAudioAdapter || audioAdapterInScene.tv != script);
#endif
#if LTCGI_1
                var hasLtcgi = ATEditorUtility.TryGetComponentInScene(out ltcgiInScene);
                canConnectToLtcgi = hasLtcgi && ATEditorUtility.GetComponentsInScene<TVManager>()
                    .All(tv => tv.customTexture == null || tv.customTexture != ltcgiInScene.VideoTexture);
#endif
                isAreaLitInScene = ATEditorUtility.TryGetComponentsInScene(out Camera[] cams) && cams
                    .Any(c => AssetDatabase.GetAssetPath(c.targetTexture)?.ToLower().Contains("lightmesh") ?? false);
            }

            if (script.blitMaterial == null)
                SetVariableByName(nameof(script.blitMaterial), AssetDatabase.LoadAssetAtPath<Material>(ProTVEditorUtility.blitMaterialPath));
        }

        protected override void Header()
        {
            if (nestedTVDetected)
                EditorGUILayout.HelpBox(
                    "Nested TVManagers detected. This WILL cause issues and is highly advised to unnest any TVs.",
                    MessageType.Error);
        }

        protected override void Footer()
        {
            if (!string.IsNullOrWhiteSpace(script.versionNumber))
                EditorGUILayout.LabelField($"ProTV v{script.versionNumber}");
        }

        protected override void RenderChangeCheck()
        {
            if (!isPrefab && !serializedObject.isEditingMultipleObjects)
            {
#if AUDIOLINK_0 || AUDIOLINK_1
                if (audioLinkInScene == null)
                {
                    using (VBox)
                    {
                        EditorGUILayout.LabelField(I18n.Tr("AudioLink detected but not present in scene."));
                        if (GUILayout.Button(I18n.Tr("Add AudioLink into Scene and Connect to this TV"))) addAudioLinkToScene();
                    }
                }

                if (canConnectAudioAdapter)
                {
                    using (VBox)
                    {
                        EditorGUILayout.LabelField(I18n.Tr("AudioLink is present in the scene but no AudioAdapter is connected to this TV."));
                        if (GUILayout.Button(I18n.Tr("Connect AudioAdapter to this TV"))) upsertAudioAdapter();
                    }
                }

#else
                using (VBox)
                {
                    string msg = "\n" + I18n.Tr("AudioLink is not detected. ProTV highly recommends including AudioLink in your project for fun visuals!");
                    msg += "\n\n" + I18n.Tr("If you know you have AudioLink already installed, you may need to upgrade to the latest version. ProTV expects 0.3.2 or later.") + "\n";
                    EditorGUILayout.HelpBox(msg, MessageType.Info);
                }
#endif
            }

            DrawCustomHeader("Autoplay Settings");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.autoplayMainUrl),
                    nameof(script.autoplayAlternateUrl),
                    nameof(script.autoplayTitle),
                    nameof(script.autoplayLoop)
                );
            }

            DrawCustomHeader("Default TV Settings");
            using (VBox)
            {
                if (script.videoManagers == null || script.videoManagers.Length == 0)
                    EditorGUILayout.HelpBox(I18n.Tr("Ensure any related VPManagers are children of this GameObject. The TV will not work without any VPManagers."), MessageType.Error);
                else DrawCustomInitialPlayer();

                DrawVariablesByName(
                    nameof(script.defaultVolume),
                    nameof(script.startWith2DAudio),
                    nameof(script.startWithVideoDisabled),
                    nameof(script.startWithAudioMuted)
                );
            }

            DrawCustomHeader("Sync Options");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.syncToOwner),
                    nameof(script.automaticResyncInterval),
                    nameof(script.playDriftThreshold),
                    nameof(script.pauseDriftThreshold)
                );

                if (DrawCustomFoldout(nameof(script.syncToOwner), I18n.TrContent("Sync Tweaks")))
                {
                    EditorGUI.indentLevel++;
                    DrawVariablesByName(
                        nameof(script.syncVideoManagerSelection),
                        nameof(script.syncVolumeControl),
                        nameof(script.syncAudioMode),
                        nameof(script.syncVideoMode),
                        nameof(script.allowLocalTweaks)
                    );
                    EditorGUI.indentLevel--;
                }
            }

            DrawCustomHeader("Media Load Options");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.playVideoAfterLoad),
                    nameof(script.bufferDelayAfterLoad),
                    nameof(script.implicitReplayThreshold),
                    nameof(script.maxAllowedLoadingTime),
                    nameof(script.liveMediaAutoReloadInterval),
                    nameof(script.preferAlternateUrlForQuest)
                );

                using (HArea)
                {
                    DrawVariablesByName(nameof(script.enableReloadKeybind));
                    if (script.enableReloadKeybind)
                    {
                        var label = GetPropertyLabel(nameof(script.reloadKey));
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                            DrawVariablesByName(new[] { nameof(script.reloadKey) }, GUILayout.Width(105));
                    }
                }
            }

            DrawCustomHeader("Security Options");
            using (VBox)
            {
                using (DisabledScope(Application.isPlaying))
                using (ATEditorGUI.PropertyDropdown)
                    DrawVariablesByName(nameof(script.authPlugin));
                DrawVariablesByName(nameof(script.allowMasterControl));
                if (script.authPlugin == null && !script.allowMasterControl && (!script.allowFirstMasterControl || !script.firstMasterIsSuper))
                {
                    var auth = script.GetComponentInChildren<TVAuthPlugin>(true);
                    EditorGUILayout.HelpBox(
                        I18n.Tr("No auth plugin connected. Without master control nor an auth plugin, the TV may get soft-locked into an un-usable state in public or group instances."),
                        script.lockedByDefault ? MessageType.Error : MessageType.Warning);
                    var btnLabel = auth != null ? I18n.Tr("Reconnect Auth Plugin: ") + auth.gameObject.name : I18n.Tr("Connect Basic Whitelist");
                    if (GUILayout.Button(btnLabel))
                    {
                        if (auth == null)
                        {
                            var whitelist = new GameObject("TVAuth");
                            Undo.RegisterCreatedObjectUndo(whitelist, "Remove added whitelist.");
                            auth = UdonSharpUndo.AddComponent<TVManagedWhitelist>(whitelist);
                            whitelist.transform.SetParent(script.transform);
                        }

                        using (new SaveObjectScope(script))
                            script.authPlugin = auth;
                    }
                }

                DrawVariablesByName(nameof(script.lockedByDefault));

                if (DrawCustomFoldout(nameof(script.superUserLockOverride), I18n.TrContent("Security Tweaks")))
                {
                    EditorGUI.indentLevel++;
                    DrawVariablesByName(nameof(script.allowFirstMasterControl));
                    using (DisabledScope(!script.allowFirstMasterControl))
                        DrawVariablesByName(nameof(script.firstMasterIsSuper));

                    DrawVariablesByName(
                        nameof(script.instanceOwnerIsSuper),
                        nameof(script.superUserLockOverride),
                        nameof(script.disallowUnauthorizedUsers),
                        nameof(script.playStateTakesOwnership),
                        nameof(script.enableAutoOwnership),
                        nameof(script.authorizedUsersAlwaysLogTrace),
                        nameof(script.superUsersAlwaysLogTrace)
                    );
                    EditorGUI.indentLevel--;
                }

                DrawVariablesByName(nameof(script.enforceDomainWhitelist));
                if (script.enforceDomainWhitelist)
                {
                    DrawVariablesByName(nameof(script.enableAuthUserDomainBypass));
                    if (script.domainWhitelist == null)
                        using (new SaveObjectScope(script))
                            script.domainWhitelist = script.defaultDomains;
                    _whitelistDomains.DrawLayout(showHints);
                }
            }

            DrawCustomHeader("Error/Retry Options");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.defaultRetryCount),
                    nameof(script.repeatingRetryDelay),
                    nameof(script.retryUsingAlternateUrl));
            }

            DrawCustomHeader("Rendering Options");
            using (VBox)
            {
                DrawCustomHeaderLarge("Internal Texture Settings");
                EditorGUI.indentLevel++;

                #region Material Targets

                _customMaterialsList.DrawLayout(showHints);
                if (!_materialProperty.isExpanded)
                {
                    if (GUILayout.Button(I18n.Tr("Create Material")))
                    {
                        var path = EditorUtility.SaveFilePanelInProject(
                            I18n.Tr("Save new material"),
                            script.gameObject.name,
                            "mat",
                            I18n.Tr("Pick a location to store the custom material")
                        );

                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            var mat = new Material(Shader.Find(ProTVEditorUtility.defaultTvShader));
                            AssetDatabase.CreateAsset(mat, path);
                            mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ProTVEditorUtility.defaultStaticImage));
                            mat.SetTexture("_SoundTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ProTVEditorUtility.defaultSoundImage));
                            addMaterial(_materialProperty, _textureProperty, mat, "_VideoTex");
                        }
                    }

                    Spacer(5f);
                }

                #endregion

                // if (DrawAndGetVariableByName(nameof(script.enableHDR), out bool isHDR) && script.customTexture != null)
                // {
                //     bool exists = script.customTexture.IsCreated();
                //     if (exists) script.customTexture.Release();
                //     script.customTexture.format = isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                //     if (exists) script.customTexture.Create();
                // }

                #region Gamma Zone Settings

                if (DrawCustomFoldout(_gammaZone, new GUIContent(I18n.Tr("Custom Gamma Zone Settings"))))
                {
                    EditorGUI.indentLevel++;

                    textureTransformMode(
                        nameof(script.gammaZoneTransformMode),
                        nameof(script.gammaZonePixelOrigin),
                        nameof(script.gammaZonePixelSize),
                        nameof(script.gammaZoneTiling),
                        nameof(script.gammaZoneOffset)
                    );

                    if (script.gammaZoneTransformMode != TVTextureTransformMode.ASIS)
                    {
                        if (EditorApplication.isPlaying && script.ActiveManager != null)
                            EditorGUILayout.LabelField($"Normalized: {script.lastGammaZoneST}");
                    }

                    EditorGUI.indentLevel--;
                }

                #endregion

                #region Standby Texture Settings

                var fallbackTexturesLabel = GetPropertyLabel(_standbyTexture);
                fallbackTexturesLabel.text = I18n.Tr("Standby Texture Settings");

                if (DrawCustomFoldout(_standbyTexture, fallbackTexturesLabel))
                {
                    EditorGUI.indentLevel++;
                    DrawVariablesByName(
                        nameof(script.standbyOnMediaEnd),
                        nameof(script.standbyOnMediaPause),
                        nameof(script.standby3dMode)
                    );

                    if (script.standby3dMode != TV3DMode.NONE) DrawVariablesByName(nameof(script.standby3dModeSize));

                    EditorGUI.indentLevel--;
                    using (HArea)
                    {
                        var sh = showHints;
                        showHints = false;
                        Spacer(0, true);
                        var label = GetPropertyLabel(nameof(script.defaultStandbyTexture));
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                            if (DrawVariablesByNameAsTextures(100, nameof(script.defaultStandbyTexture)))
                                ProTVEditorUtility.UpdateCustomTextureForEditorPreview(script);
                        Spacer(0, true);
                        label = GetPropertyLabel(nameof(script.soundOnlyTexture));
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                            DrawVariablesByNameAsTextures(100, nameof(script.soundOnlyTexture));
                        Spacer(0, true);
                        label = GetPropertyLabel(nameof(script.errorTexture));
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                            DrawVariablesByNameAsTextures(100, nameof(script.errorTexture));
                        Spacer(0, true);
                        label = GetPropertyLabel(nameof(script.loadingTexture));
                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                            DrawVariablesByNameAsTextures(100, nameof(script.loadingTexture));
                        showHints = sh;
                    }

                    Spacer(5f);
                }

                #endregion

                EditorGUI.indentLevel--;

                DrawCustomHeaderLarge("Custom Texture Settings");
                EditorGUI.indentLevel++;

                if (isAreaLitInScene)
                {
                    using (VBox)
                    {
                        if (script.customTexture == null)
                        {
                            EditorGUILayout.LabelField(
                                I18n.Tr("AreaLit system is present in the scene but no RenderTexture is defined for the TV."),
                                noticeStyle
                            );
                            if (GUILayout.Button(I18n.Tr("Create RenderTexture to use with AreaLit")))
                            {
                                var rt = createRenderTexture();
                                if (rt != null)
                                {
                                    using (new SaveObjectScope(script, "Undo video texture update."))
                                    {
                                        script.applyAspectToResize = true;
                                        script.applyAspectToBlit = true;
                                    }
                                }
                            }
                        }
                    }
                }
#if LTCGI_1
                if (canConnectToLtcgi)
                {
                    using (VBox)
                    {
                        if (script.customTexture == null)
                        {
                            EditorGUILayout.LabelField(
                                I18n.Tr("LTCGI Controller is present in the scene but no RenderTexture is defined for the TV."),
                                noticeStyle
                            );
                            if (GUILayout.Button(I18n.Tr("Create RenderTexture and connect TV to LTCGI")))
                            {
                                var rt = createRenderTexture();
                                if (rt != null)
                                {
                                    using (new SaveObjectScope(ltcgiInScene, "Undo video texture update"))
                                        ltcgiInScene.VideoTexture = rt;
                                    using (new SaveObjectScope(script, "Undo video texture update."))
                                    {
                                        script.applyAspectToResize = true;
                                        script.applyAspectToBlit = true;
                                    }
                                }
                            }
                        }
                        else if (script.customTexture != ltcgiInScene.VideoTexture)
                        {
                            EditorGUILayout.LabelField(I18n.Tr("LTCGI Controller is present in the scene."));
                            if (GUILayout.Button(I18n.Tr("Connect TV to LTCGI")))
                            {
                                using (new SaveObjectScope(ltcgiInScene, "Undo video texture update."))
                                    ltcgiInScene.VideoTexture = script.customTexture;
                                using (new SaveObjectScope(script, "Undo video texture update."))
                                {
                                    script.applyAspectToResize = true;
                                    script.applyAspectToBlit = true;
                                }
                            }
                        }
                    }
                }
#endif
                var customTextureLabel = GetPropertyLabel(_customTexture);
                using (HArea)
                {
                    var oldTex = (RenderTexture)_customTexture.GetValue();
                    if (DrawVariablesWithLabel(customTextureLabel, _customTexture))
                    {
                        if (_customTexture.GetValue() == null) oldTex.Release();
                        else ProTVEditorUtility.UpdateCustomTextureForEditorPreview(script);
                    }

                    if (script.customTexture == null)
                    {
                        if (GUILayout.Button(I18n.Tr("Create"), GUILayout.ExpandWidth(false)))
                        {
                            createRenderTexture();
                            ProTVEditorUtility.UpdateCustomTextureForEditorPreview(script);
                        }
                    }
                    else if (script.defaultStandbyTexture != null && GUILayout.Button(I18n.Tr("Preview"), GUILayout.ExpandWidth(false)))
                        ProTVEditorUtility.UpdateCustomTextureForEditorPreview(script);
                }

                #region RenderTexture Update Settings

                if (script.customTexture != null && DrawCustomFoldout(_customTexture, new GUIContent(I18n.Tr("RenderTexture Update Settings"))))
                {
                    EditorGUI.indentLevel++;
                    DrawVariablesByName(nameof(script.autoResizeTexture));
                    if (DrawVariablesByName(nameof(script.targetAspectRatio)) && EditorApplication.isPlaying)
                        script.aspectRatio = script.targetAspectRatio;

                    if (script.targetAspectRatio != 0)
                        DrawVariablesByName(nameof(script.aspectFitMode));

                    if (script.autoResizeTexture)
                        DrawVariablesByName(nameof(script.applyAspectToResize));
                    else
                        using (DisabledScope())
                            EditorGUILayout.Toggle(GetPropertyLabel(nameof(script.applyAspectToResize)), false);

                    DrawVariablesByName(
                        nameof(script.applyAspectToBlit),
                        nameof(script.trimToGammaZone),
                        nameof(script.customTextureBrightness),
                        nameof(script.fadeEdges)
                    );
                    EditorGUI.indentLevel--;
                }

                #endregion

                EditorGUI.indentLevel--;

                DrawCustomHeaderLarge("Global Texture Settings");
                EditorGUI.indentLevel++;

                #region Global Video Texture

                using (DisabledScope(serializedObject.isEditingMultipleObjects))
                {
                    if (DrawVariablesByName(nameof(script.enableGSV)))
                    {
                        var tvs = ATEditorUtility.GetComponentsInScene<TVManager>();
                        foreach (var tv in tvs)
                        {
                            if (tv == script) continue;
                            Undo.RecordObject(tv, "Unset global texture assignment");
                            tv.enableGSV = false;
                        }
                    }
                    else if (script.enableGSV && PrefabStageUtility.GetCurrentPrefabStage() == null)
                    {
                        if (ATEditorUtility.GetComponentsInScene<TVManager>().Any(t => t != script && t.enableGSV))
                        {
                            Undo.RecordObject(script, "Unset global texture assignment");
                            script.enableGSV = false;
                        }
                    }
                }

                if (script.enableGSV)
                {
                    EditorGUI.indentLevel++;
                    textureTransformMode(
                        nameof(script.globalTextureTransformMode),
                        nameof(script.globalTexturePixelOrigin),
                        nameof(script.globalTexturePixelSize),
                        nameof(script.globalTextureTiling),
                        nameof(script.globalTextureOffset)
                    );

                    if (script.globalTextureTransformMode != TVTextureTransformMode.ASIS)
                    {
                        if (EditorApplication.isPlaying && script.ActiveManager != null)
                            EditorGUILayout.LabelField($"Normalized: {script.lastGlobalST}");

                        DrawVariablesByName(nameof(script.bakeGlobalVideoTexture));
                    }

                    EditorGUI.indentLevel--;
                }

                #endregion

                EditorGUI.indentLevel--;

                Spacer(10f);

                #region Video Texture Previews

                if (EditorApplication.isPlaying && script.ActiveManager != null)
                {
                    showTexturePreviews = EditorGUILayout.Toggle(I18n.TrContent("Show Texture Previews"), showTexturePreviews);
                    if (showTexturePreviews)
                    {
                        EditorGUILayout.BeginHorizontal();
                        float size = 400;
                        var iTex = script.InternalTexture;
                        var cTex = script.CustomTexture;
                        var gTex = script.GlobalTexture;
                        var iTexValid = iTex != null && iTex.IsCreated();
                        bool bTexValid = cTex != null && cTex.IsCreated();
                        bool gTexValid = gTex != null && gTex.IsCreated();
                        if (iTexValid) size -= 100;
                        if (bTexValid) size -= 100;
                        if (gTexValid) size -= 100;
                        Spacer(10f);
                        if (iTexValid)
                        {
                            EditorGUILayout.BeginVertical();
                            var label = I18n.TrContent("Internal Texture");
                            label.text += $": {iTex.width}x{iTex.height}";
                            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label)) EditorGUILayout.LabelField(label);
                            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(size, size), iTex, null, ScaleMode.ScaleToFit);
                            EditorGUILayout.EndVertical();
                            Spacer(10f);
                        }

                        if (bTexValid)
                        {
                            EditorGUILayout.BeginVertical();
                            var label = I18n.TrContent("RenderTexture Target");
                            label.text += $": {cTex.width}x{cTex.height}";
                            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label)) EditorGUILayout.LabelField(label);
                            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(size, size), cTex, null, ScaleMode.ScaleToFit);
                            EditorGUILayout.EndVertical();
                            Spacer(10f);
                        }

                        if (gTexValid)
                        {
                            EditorGUILayout.BeginVertical();
                            var label = I18n.TrContent("Global Texture (Baked)");
                            label.text += $": {gTex.width}x{gTex.height}";
                            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label)) EditorGUILayout.LabelField(label);
                            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(size, size), gTex, null, ScaleMode.ScaleToFit);
                            EditorGUILayout.EndVertical();
                            Spacer(10f);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                #endregion
            }

            #region Misc Options

            DrawCustomHeader("Misc Options");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.startDisabled),
                    nameof(script.stopMediaWhenDisabled),
                    nameof(script.enableLocalPause)
                );
            }

            #endregion

            #region Detected Plugins

            EditorGUILayout.Space(5f);
            DrawCustomHeader("Detected Plugins");

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField(I18n.Tr("Detected Plugins are not displayed during playmode."));
            }
            else
            {
                var link = new GUIContent(linkIcon);
                var checkmark = new GUIContent(checkmarkIcon);
                using (VBox)
                {
                    foreach (TVPlugin plugin in detectedPlugins.Where(plugin => plugin.tv == script || plugin.tv == null))
                        using (HArea)
                        {
                            var connected = plugin.tv == script;
                            var label = connected ? checkmark : link;
                            using (DisabledScope()) EditorGUILayout.ObjectField(plugin, typeof(TVPlugin), true);
                            using (DisabledScope(connected))
                                if (GUILayout.Button(label, GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                                    using (new SaveObjectScope(plugin))
                                        plugin.tv = script;
                        }

                    var others = detectedPlugins.Where(plugin => plugin.tv != script && plugin.tv != null).ToArray();
                    if (others.Length > 0 && DrawCustomFoldout(nameof(script.defaultVideoManager), new GUIContent(I18n.Tr("Connected to Other TVs"))))
                    {
                        foreach (TVPlugin plugin in others)
                            using (HArea)
                            {
                                using (DisabledScope())
                                {
                                    EditorGUILayout.ObjectField(plugin, typeof(TVPlugin), true);
                                    EditorGUILayout.ObjectField(plugin.tv, typeof(TVManager), true, GUILayout.MaxWidth(150));
                                }

                                if (GUILayout.Button(link, GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                                    using (new SaveObjectScope(plugin))
                                        plugin.tv = script;
                            }
                    }
                }
            }

            #endregion
        }

        public override bool RequiresConstantRepaint()
        {
            return EditorApplication.isPlaying && showTexturePreviews;
        }

        private void textureTransformMode(string modeName, string originName, string sizeName, string tilingName, string offsetName)
        {
            if (DrawAndGetVariableByName(modeName, out TVTextureTransformMode texTransformMode))
            {
                switch (texTransformMode)
                {
                    case TVTextureTransformMode.VRSL_HL:
                        SetVariableByName(originName, Vector2Int.zero);
                        SetVariableByName(sizeName, new Vector2Int(0, -208));
                        break;
                    case TVTextureTransformMode.VRSL_HM:
                        SetVariableByName(originName, Vector2Int.zero);
                        SetVariableByName(sizeName, new Vector2Int(0, -139));
                        break;
                    case TVTextureTransformMode.VRSL_HS:
                        SetVariableByName(originName, Vector2Int.zero);
                        SetVariableByName(sizeName, new Vector2Int(0, -92));
                        break;
                    case TVTextureTransformMode.VRSL_VL:
                        SetVariableByName(originName, Vector2Int.zero);
                        SetVariableByName(sizeName, new Vector2Int(-208, 0));
                        break;
                    case TVTextureTransformMode.VRSL_VM:
                        SetVariableByName(originName, Vector2Int.zero);
                        SetVariableByName(sizeName, new Vector2Int(-139, 0));
                        break;
                    case TVTextureTransformMode.VRSL_VS:
                        SetVariableByName(originName, Vector2Int.zero);
                        SetVariableByName(sizeName, new Vector2Int(-92, 0));
                        break;
                }
            }

            switch (texTransformMode)
            {
                case TVTextureTransformMode.ASIS:
                case TVTextureTransformMode.DISABLED:
                    break;
                case TVTextureTransformMode.NORMALIZED:
                    DrawVariablesByName(tilingName, offsetName);
                    break;
                case TVTextureTransformMode.BY_PIXELS:
                    DrawVariablesByName(originName, sizeName);
                    break;
                default: // any presets
                    using (DisabledScope()) DrawVariablesByName(originName, sizeName);
                    break;
            }
        }

        private RenderTexture createRenderTexture()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                I18n.Tr("Save new material"),
                script.gameObject.name,
                "renderTexture",
                I18n.Tr("Pick a location to store the custom material")
            );

            if (!string.IsNullOrWhiteSpace(path))
            {
                RenderTextureFormat format = RenderTextureFormat.Default;
                // if (script.enableHDR) format = RenderTextureFormat.DefaultHDR;
                var rt = new RenderTexture(2560, 1440, 0, format, RenderTextureReadWrite.Default)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 16,
                    useMipMap = true,
                    autoGenerateMips = true
                };
                AssetDatabase.CreateAsset(rt, path);
                UnityEngine.Debug.Log("RenderTexture saved to " + path);
                using (new SaveObjectScope(script)) script.customTexture = rt;
                EditorGUIUtility.PingObject(rt);
                return rt;
            }

            return null;
        }

        private void handleMaterialSwap(ATReorderableList list, ATPropertyListData propData)
        {
            if (propData.PropertyIndex == 0)
            {
                var mat = (Material)propData.Property.GetArrayElementAtIndex(propData.ElementIndex).objectReferenceValue;
                var texProp = _textureProperty.GetArrayElementAtIndex(propData.ElementIndex);
                if (mat.shader.name.StartsWith("ProTV")) texProp.SetValue("_VideoTex");
                else if (string.IsNullOrWhiteSpace(texProp.stringValue)) texProp.SetValue("_MainTex");
            }
        }

        private bool handleMaterialDrop(ATReorderableList list, UnityEngine.Object dropped, ATPropertyListData propListData)
        {
            if (dropped is Material mat && System.Array.IndexOf(script.customMaterials, mat) == -1)
            {
                var texProp = "_MainTex";
                if (mat.shader.name.StartsWith("ProTV")) texProp = "_VideoTex";
                addMaterial(_materialProperty, _textureProperty, mat, texProp);
                return true;
            }

            return false;
        }

        private static void addMaterial(SerializedProperty matProp, SerializedProperty propProp, Material mat, string prop)
        {
            var index = matProp.arraySize;
            matProp.arraySize += 1;
            propProp.arraySize = matProp.arraySize;
            matProp.GetArrayElementAtIndex(index).SetValue(mat);
            propProp.GetArrayElementAtIndex(index).SetValue(prop);
        }

        private void handleWhitelistContextMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent(I18n.Tr("Reset to Default")), false, resetWhitelist);
        }

        private void resetWhitelist()
        {
            using (new SaveObjectScope(script, "Undo resetting whitelist"))
                script.domainWhitelist = script.defaultDomains;
        }

#if AUDIOLINK_0 || AUDIOLINK_1
        private void addAudioLinkToScene()
        {
            audioLinkInScene = ATEditorUtility.GetComponentInScene<AudioLink.AudioLink>();
            if (audioLinkInScene == null)
            {
#if AUDIOLINK_1
                AudioLink.Editor.AudioLinkAssetManager.AddAudioLinkToScene();
                audioLinkInScene = ATEditorUtility.GetComponentInScene<AudioLink.AudioLink>();
                audioLinkInScene.audioSource = null;
#else
                var alGO = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(ProTVEditorUtility.audioLinkPrefab, typeof(GameObject)));
                audioLinkInScene = alGO.GetComponent<AudioLink.AudioLink>();
                audioLinkInScene.audioSource = null;
                Undo.RegisterCreatedObjectUndo(alGO, "Undo AudioLink insertion");
#endif
            }

            upsertAudioAdapter();
        }

        private void upsertAudioAdapter()
        {
            GameObject go;
            if (audioAdapterInScene == null)
            {
                go = new GameObject { name = "AudioAdapter" };
                Undo.RegisterCreatedObjectUndo(go, "Add AudioAdapter");
                audioAdapterInScene = Undo.AddComponent<AudioAdapter>(go);
            }
            else
            {
                Undo.RecordObject(audioAdapterInScene, "Modify AudioAdapter Options");
                Undo.RecordObject(audioAdapterInScene.transform, "Modify AudioAdapter Parent");
            }

            // if the ALA is connected to a TV, but does not exist as a child, do not reparent it
            // as it should be assumed that the user themselves moved the object to somewhere else in the hierarchy manually 
            bool shouldMove = audioAdapterInScene.tv == null || audioAdapterInScene.tv == audioAdapterInScene.GetComponentInParent<TVManager>();
            audioAdapterInScene.tv = script;

            // upsert a new audiolink specific speaker if one doesn't exist.
            var managers = script.GetComponentsInChildren<VPManager>(true);
            var allSpeakers = ATEditorUtility.GetComponentsInScene<VRCAVProVideoSpeaker>();
            AudioSource[] targetSpeakers = new AudioSource[managers.Length];
            for (var index = 0; index < managers.Length; index++)
            {
                var manager = managers[index];
                var videoPlayer = manager.GetComponent<VRCAVProVideoPlayer>();
                if (videoPlayer == null) continue;
                var availableSpeakers = allSpeakers.Where(s => s.VideoPlayer == videoPlayer).ToArray();
                var existingSpeaker = availableSpeakers.FirstOrDefault(s => s.gameObject.name.Contains("AudioLink"));
                if (existingSpeaker != null)
                {
                    UnityEngine.Debug.Log($"Found AudioLink Speaker {existingSpeaker.gameObject.name}");
                    targetSpeakers[index] = existingSpeaker.GetComponent<AudioSource>();
                    continue;
                }

                var t = script.transform;
                var speaker = availableSpeakers.FirstOrDefault();
                if (speaker) t = speaker.transform.parent;
                var newSpeaker = ProTVEditorUtility.CreateAVProVideoSpeaker(videoPlayer, VRCAVProVideoSpeaker.ChannelMode.StereoMix, "AudioLink");
                newSpeaker.transform.SetParent(t, false);
                newSpeaker.spatialize = false;
                newSpeaker.spatialBlend = 0f;
                newSpeaker.volume = 0.002f;
                targetSpeakers[index] = newSpeaker;
            }

            audioAdapterInScene.audioLinkInstance = audioLinkInScene;
            audioAdapterInScene.enableAudioLink = true;
            // since the tv target shifted, clear the speakers to be reassigned.
            audioAdapterInScene.targetSpeakers = targetSpeakers;
            PrefabUtility.RecordPrefabInstancePropertyModifications(audioAdapterInScene);
            go = audioAdapterInScene.gameObject;
            if (shouldMove) go.transform.SetParent(script.transform, false);
            Selection.activeGameObject = go;
        }
#endif

        private void DrawCustomInitialPlayer()
        {
            var len = script.videoManagers.Length;
            GUIContent[] vpNames = new GUIContent[len];
            int[] indexes = new int[len];
            for (var i = 0; i < len; i++)
            {
                var vm = script.videoManagers[i];
                indexes[i] = i;
                if (vm == null) vpNames[i] = new GUIContent("???");
                else vpNames[i] = new GUIContent(string.IsNullOrWhiteSpace(vm.customLabel) ? vm.gameObject.name : vm.customLabel);
            }

            EditorGUILayout.IntPopup(_defaultVideoManager, vpNames, indexes, GetPropertyLabel(_defaultVideoManager));
        }

        private const string materialsDirectory = "Assets/ProTV/Materials";
        internal static Material ReplaceWithNewMaterial(TVManager tv, Material oldMaterial)
        {
            var newMaterial = new Material(oldMaterial);
            var directory = Path.Combine(Application.dataPath, "..", materialsDirectory);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var siblings = Directory.GetFiles(directory)
                .Where(f => f.EndsWith(".mat"))
                .Select(f => f = f.Substring(directory.Length + 1).Replace(".mat", ""))
                .ToArray();
            newMaterial.name = ObjectNames.GetUniqueName(siblings, oldMaterial.name);
            // UnityEngine.Debug.Log($"{newMaterial.name} with siblings {string.Join(", ", siblings)}");
            var assetPath = Path.Combine(materialsDirectory, newMaterial.name + ".mat");
            AssetDatabase.CreateAsset(newMaterial, assetPath);
            AssetDatabase.Refresh();
            newMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            Undo.RecordObject(tv, "Update custom Materials");
            tv.customMaterials = tv.customMaterials.Select(m => m != oldMaterial ? m : newMaterial).ToArray();
            var meshes = tv.gameObject.GetComponentsInChildren<MeshRenderer>(true);
            Undo.RecordObjects(meshes, "Update mesh Materials");
            foreach (var mesh in meshes)
                mesh.sharedMaterials = mesh.sharedMaterials.Select(m => m != oldMaterial ? m : newMaterial).ToArray();

            return newMaterial;
        }
    }
}