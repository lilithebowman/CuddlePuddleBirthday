using System.Linq;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;

// #pragma warning disable CS0618

namespace ArchiTech.ProTV.Editor
{
    [CustomEditor(typeof(MediaControls), true)]
    public class MediaControlsEditor : TVPluginEditor
    {
        private MediaControls script;

        private MediaControls detectedParent;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (MediaControls)target;
            detectedParent = ATEditorUtility.GetComponentInNearestParent<MediaControls>(script, false);
            SetupTVReferences();
        }

        protected override void RenderChangeCheck()
        {
            if (detectedParent != null)
            {
                using (VBox)
                {
                    EditorGUILayout.LabelField(I18n.Tr("Detected nested MediaControls components. Would you like to merge this into the parent MediaControls component?"));
                    if (GUILayout.Button("Merge into Parent"))
                    {
                        ProTVEditorUtility.MergeMediaControls(script, detectedParent);
                        if (PrefabUtility.IsPartOfPrefabInstance(script))
                            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(script), PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                        Undo.DestroyObjectImmediate(script);
                        GUI.changed = false;
                        return;
                    }
                }
            }

            DrawTVReferences();

            DrawCustomHeaderLarge("Media Inputs");
            using (VBox)
            {
                DrawVariablesByName(nameof(script.mainUrlInput));
                if (script.mainUrlInput != null) DrawVariablesByName(nameof(script.mainUrlDefault));
                DrawVariablesByName(nameof(script.alternateUrlInput));
                if (script.alternateUrlInput != null) DrawVariablesByName(nameof(script.alternateUrlDefault));
                DrawVariablesByName(nameof(script.titleInput));
                if (script.titleInput != null) DrawVariablesByName(nameof(script.titleDefault));
                DrawVariablesByName(
                    nameof(script.sendInputs),
                    nameof(script.urlSwitch)
                );
            }

            DrawCustomHeaderLarge("Media Actions");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.play),
                    nameof(script.pause),
                    nameof(script.stop),
                    nameof(script.skip),
                    nameof(script.reload),
                    nameof(script.resync),
                    nameof(script.seek),
                    nameof(script.seekOffset)
                );

                DrawVariablesByName(nameof(script.playbackSpeed));

                using (HArea)
                {
                    if (DrawAndGetVariableByName(nameof(script.videoPlayerSwap), out Dropdown videoPlayerSwap) && videoPlayerSwap != null)
                    {
                        if (videoPlayerSwap.GetComponentInChildren<TextMeshProUGUI>() != null)
                            SetVariableByName(nameof(script.videoPlayerSwapUseTMP), true);
                    }

                    if (videoPlayerSwap != null)
                    {
                        var useTMP = EditorGUILayout.ToggleLeft(GetPropertyLabel(nameof(script.videoPlayerSwapUseTMP), showHints), script.videoPlayerSwapUseTMP, GUILayout.Width(75));
                        if (script.videoPlayerSwapUseTMP != useTMP) SetVariableByName(nameof(script.videoPlayerSwapUseTMP), useTMP);
                    }
                }
            }

            DrawCustomHeaderLarge("Video Controls");
            using (VBox)
            {
                using (HArea)
                {
                    if (DrawAndGetVariableByName(nameof(script.mode3dSwap), out Dropdown mode3dSwap) && mode3dSwap != null)
                    {
                        if (mode3dSwap.GetComponentInChildren<TextMeshProUGUI>() != null)
                            SetVariableByName(nameof(script.mode3dSwapUseTMP), true);
                    }

                    if (mode3dSwap != null)
                    {
                        var useTMP = EditorGUILayout.ToggleLeft(GetPropertyLabel(nameof(script.mode3dSwapUseTMP), showHints), script.mode3dSwapUseTMP, GUILayout.Width(75));
                        if (script.mode3dSwapUseTMP != useTMP) SetVariableByName(nameof(script.mode3dSwapUseTMP), useTMP);
                    }
                }

                DrawToggleIconsControls(
                    "3D Width Toggle",
                    nameof(script.width3dMode),
                    nameof(script.width3dModeIndicator),
                    nameof(script.width3dHalf),
                    nameof(script.width3dFull),
                    nameof(script.width3dHalfColor),
                    nameof(script.width3dFullColor)
                );

                DrawToggleIconsControls(
                    "Color Space Toggle",
                    nameof(script.colorSpaceCorrection),
                    nameof(script.colorSpaceCorrectionIndicator),
                    nameof(script.colorSpaceCorrected),
                    nameof(script.colorSpaceRaw),
                    nameof(script.colorSpaceCorrectedColor),
                    nameof(script.colorSpaceRawColor)
                );
            }

            DrawCustomHeaderLarge("Audio Controls");
            using (VBox)
            {
                DrawCustomHeader("Volume Slider");
                DrawVariablesByName(
                    nameof(script.volume),
                    nameof(script.volumeIndicator)
                );

                if (script.volumeIndicator != null)
                {
                    using (HArea)
                    {
                        EditorGUILayout.PrefixLabel(I18n.Tr("Icons"));
                        using (VArea)
                        {
                            const float size = 75f;
                            using (HArea) DrawVariablesByNameAsSprites(size, nameof(script.volumeHigh), nameof(script.volumeMed));
                            using (HArea) DrawVariablesByNameAsSprites(size, nameof(script.volumeLow), nameof(script.volumeOff));
                        }
                    }
                }

                DrawToggleIconsControls(
                    "Audio Mode Toggle",
                    nameof(script.audioMode),
                    nameof(script.audioModeIndicator),
                    nameof(script.audio3d),
                    nameof(script.audio2d),
                    nameof(script.audio3dColor),
                    nameof(script.audio2dColor)
                );

                DrawToggleIconsControls(
                    "Mute Toggle",
                    nameof(script.mute),
                    nameof(script.muteIndicator),
                    nameof(script.unmuted),
                    nameof(script.muted),
                    nameof(script.unmutedColor),
                    nameof(script.mutedColor)
                );
            }

            DrawCustomHeaderLarge("Behaviour Controls");
            using (VBox)
            {
                DrawToggleIconsControls(
                    "TV Lock Toggle",
                    nameof(script.tvLock),
                    nameof(script.tvLockIndicator),
                    nameof(script.unlocked),
                    nameof(script.locked),
                    nameof(script.unlockedColor),
                    nameof(script.lockedColor)
                );

                DrawToggleIconsControls(
                    "Sync Toggle",
                    nameof(script.syncMode),
                    nameof(script.syncModeIndicator),
                    nameof(script.syncEnabled),
                    nameof(script.syncDisabled),
                    nameof(script.syncEnabledColor),
                    nameof(script.syncDisabledColor)
                );

                DrawToggleIconsControls(
                    "Loop Toggle",
                    nameof(script.loopMode),
                    nameof(script.loopModeIndicator),
                    nameof(script.loopDisabled),
                    nameof(script.loopEnabled),
                    nameof(script.loopDisabledColor),
                    nameof(script.loopEnabledColor)
                );
            }

            DrawCustomHeaderLarge("Additional Visuals");
            using (VBox)
            {
                DrawVariablesByName(
                    nameof(script.emptyTitlePlaceholder),
                    nameof(script.loadingBar),
                    nameof(script.loadingSpinner),
                    nameof(script.loadingSpinnerContainer),
                    nameof(script.loadingSpinReverse),
                    nameof(script.loadingSpinSpeed)
                );

                if (script.seekOffset != null)
                    DrawTextControls(nameof(script.seekOffsetDisplay), nameof(script.seekOffsetDisplayTMP));
                DrawTextControls(nameof(script.currentTimeDisplay), nameof(script.currentTimeDisplayTMP));
                DrawTextControls(nameof(script.endTimeDisplay), nameof(script.endTimeDisplayTMP));
                DrawTextControls(nameof(script.infoDisplay), nameof(script.infoDisplayTMP));
                DrawTextControls(nameof(script.clockTimeDisplay), nameof(script.clockTimeDisplayTMP));

                if (script.seek != null)
                    DrawVariablesByName(nameof(script.realtimeSeek));
                if (script.infoDisplay != null && script.infoDisplayTMP != null)
                    DrawVariablesByName(nameof(script.showMediaOwner));
                if (script.currentTimeDisplay != null && script.currentTimeDisplayTMP != null)
                    DrawVariablesByName(nameof(script.showRemainingTime));
                if (script.mainUrlInput != null || script.alternateUrlInput != null || script.titleInput != null)
                    DrawVariablesByName(nameof(script.retainInputText));

                DrawVariablesByName(
                    nameof(script.showMediaOwner),
                    nameof(script.showRemainingTime)
                );
            }
        }

        private void DrawTextControls(string textName, string tmpName)
        {
            var label = GetPropertyLabel(textName, showHints);
            using (HArea)
            {
                EditorGUILayout.PrefixLabel(label);
                DrawVariablesByNameWithoutLabels(textName, tmpName);
            }
        }
    }
}