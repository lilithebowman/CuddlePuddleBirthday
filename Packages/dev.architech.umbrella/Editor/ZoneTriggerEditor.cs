using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UnityEditor.IMGUI.Controls;
using VRC.SDK3.Components;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable Unity.InefficientPropertyAccess

namespace ArchiTech.Umbrella.Editor
{
    [CustomEditor(typeof(ZoneTrigger))]
    public class ZoneTriggerEditor : ATTriggerActionsEditor
    {
        private GUIStyle slimBox;

        protected ZoneTrigger script;

        private Tool lastTool = Tool.Move;
        private bool isEditingTrigger = false;
        private bool hasCollider;

        private ATReorderableList enterActions;
        private ATReorderableList exitActions;

        protected override bool autoRenderVariables => false;

        private void OnEnable()
        {
            script = (ZoneTrigger)target;

            enterActions = enterActions ?? SetupActionsList(
                I18n.Tr("On Enter"),
                nameof(script.enterActionTypes),
                nameof(script.enterObjects),
                nameof(script.enterBoolData),
                nameof(script.enterIntOrEnumData),
                nameof(script.enterFloatData),
                nameof(script.enterStringData),
                nameof(script.enterStringDataExtra),
                nameof(script.enterReferenceData),
                nameof(script.enterVectorData),
                nameof(script.enterUrlData)
            );

            exitActions = exitActions ?? SetupActionsList(
                I18n.Tr("On Exit"),
                nameof(script.exitActionTypes),
                nameof(script.exitObjects),
                nameof(script.exitBoolData),
                nameof(script.exitIntOrEnumData),
                nameof(script.exitFloatData),
                nameof(script.exitStringData),
                nameof(script.exitStringDataExtra),
                nameof(script.exitReferenceData),
                nameof(script.exitVectorData),
                nameof(script.exitUrlData)
            );

            lastTool = Tools.current;
        }

        private void OnDisable()
        {
            if (isEditingTrigger)
            {
                isEditingTrigger = false;
                Tools.current = lastTool;
            }
        }

        /// <summary>
        /// Any data loading/prep that needs to take place each redraw should go here.
        /// Also a good spot to inject any branding headers for custom components,
        /// since it is called before anything is rendered to the inspector.
        /// Called once per inspector redraw.
        /// </summary>
        protected override void LoadData()
        {
            slimBox ??= ATEditorGUIUtility.slimBox;
        }

        /// <summary>
        /// Any setup, caching or prep needed for the editor goes here.
        /// Functionally equivalent to OnEnable but without unity quirks around that method.
        /// This is only called once when the inspector becomes visible in editor after the first call to LoadData()
        /// </summary>
        protected override void InitData()
        {
            hasCollider = script.TryGetComponent(out Collider _);
        }

        /// <summary>
        /// This method should draw all elements which are expected to trigger a change check.
        /// If any of the elements trigger a change, the <c>SaveData()</c> method will then also be triggered.
        /// Called once per inspector redraw.
        /// </summary>
        protected override void RenderChangeCheck()
        {
            bool isUsingCollider = script.triggerType == ZoneTriggerType.COLLIDER;
            var triggerSourceRect = Rect.zero;

            DrawCustomHeader("Trigger Zone Setup");

            using (VBox)
            {
                DrawVariablesByName(nameof(script.forceState), nameof(script.triggerType));
                if (isUsingCollider)
                {
                    if (!hasCollider) EditorGUILayout.HelpBox(I18n.Tr("No Collider found on this GameObject"), MessageType.Error);
                }
                else
                {
                    using (HArea)
                    {
                        Spacer(EditorGUIUtility.labelWidth);
                        if (GUILayout.Button(isEditingTrigger ? I18n.Tr("Stop Editing") : I18n.Tr("Edit Trigger")))
                        {
                            isEditingTrigger = !isEditingTrigger;
                            if (isEditingTrigger)
                            {
                                lastTool = Tools.current;
                                Tools.current = Tool.None;
                            }
                            else Tools.current = lastTool;
                        }

                        // if another tool is enabled, stop editing
                        if (isEditingTrigger && Tools.current != Tool.None) isEditingTrigger = false;

                        if (GUILayout.Button(I18n.Tr("Reset Trigger")))
                        {
                            Undo.RecordObject(script, "Reset Trigger Data");
                            script.triggerCenter = Vector3.zero;
                            script.triggerRadius = 0.5f;
                            script.triggerArea = Vector3.one;
                            script.useScale = true;
                            GUI.changed = false;
                            SceneView.RepaintAll();
                        }
                    }

                    Spacer(2f);
                    using (DisabledScope(!isEditingTrigger))
                    {
                        DrawVariablesByName(nameof(script.triggerCenter));

                        switch (script.triggerType)
                        {
                            case ZoneTriggerType.RANGE:
                                DrawVariablesByName(nameof(script.triggerRadius), nameof(script.useScale));
                                break;
                            case ZoneTriggerType.AREA:
                                DrawVariablesByName(nameof(script.triggerArea), nameof(script.useScale));
                                // prevent negative vector values
                                using (new SaveObjectScope(script)) script.triggerArea = Vector3.Max(script.triggerArea, Vector3.zero);
                                break;
                        }
                    }

                    if (script.triggerType != ZoneTriggerType.COLLIDER)
                        DrawVariablesByName(nameof(script.checkInterval));

                    var label = GetPropertyLabel(nameof(script.triggerSource));
                    using (HArea)
                    {
                        EditorGUILayout.PrefixLabel(label);
                        DrawVariablesByNameWithoutLabels(nameof(script.triggerSource));
                        triggerSourceRect = GUILayoutUtility.GetLastRect();
                    }
                }
            }

            DrawCustomHeader("Trigger Transitions");
            using (VBox)
            {
                drawBuiltInTriggers();
            }

            Spacer(10f);

            DrawCustomHeader("Trigger Actions");
            using (VBox)
            {
                enterActions.DrawLayout();
                Spacer(10f);
                exitActions.DrawLayout();
            }

            if (Event.current.type == EventType.Repaint && triggerSourceRect.Contains(Event.current.mousePosition))
            {
                var activeValues = Enum
                    .GetValues(script.triggerSource.GetType())
                    .Cast<Enum>().Where(script.triggerSource.HasFlag)
                    .Select(e =>
                    {
                        var attr = e.GetAttribute<InspectorNameAttribute>();
                        return attr != null ? attr.displayName : e.ToString();
                    });
                var tooltip = string.Join(" | ", activeValues);
                var ttContent = new GUIContent(tooltip);
                var ttStyle = new GUIStyle()
                {
                    fixedWidth = triggerSourceRect.width,
                    stretchHeight = false,
                    wordWrap = true,
                    fontSize = 10,
                    alignment = TextAnchor.MiddleLeft,
                    normal =
                    {
                        background = Texture2D.blackTexture,
                        textColor = Color.white
                    }
                };

                var ttHeight = ttStyle.CalcHeight(ttContent, triggerSourceRect.width);
                var ttRect = new Rect(triggerSourceRect)
                {
                    height = ttHeight,
                    y = triggerSourceRect.y + triggerSourceRect.height
                };
                GUI.Box(ttRect, ttContent, ttStyle);
            }
        }

        private void drawBuiltInTriggers()
        {
            // gonna wait till udon 2 until this gets a rework
            if (script.TryGetComponent(out VRCUiShape _)) DrawVariablesByName(nameof(script.toggleVRCUiPointer));

            DrawVariablesByName(nameof(script.canvasGroup));
            if (script.canvasGroup != null)
                DrawVariablesByName(nameof(script.canvasGroupFadeTime));
        }


        private readonly SphereBoundsHandle sphere = new SphereBoundsHandle
        {
            axes = PrimitiveBoundsHandle.Axes.All,
            wireframeColor = Color.clear
        };

        private readonly BoxBoundsHandle box = new BoxBoundsHandle
        {
            axes = PrimitiveBoundsHandle.Axes.All,
            wireframeColor = Color.clear
        };

        private void OnSceneGUI()
        {
            script = (ZoneTrigger)target;
            // no custom handles needed for collider mode
            if (script.triggerType == ZoneTriggerType.COLLIDER) return;
            var offset = script.triggerCenter;
            var range = script.triggerRadius;
            var area = script.triggerArea;
            var t = script.transform;
            var useScale = script.useScale;
            var scale = t.lossyScale;
            var position = t.position;
            var inverseScale = Vector3.one;
            if (useScale)
            {
                var sx = scale.x;
                var sy = scale.y;
                var sz = scale.z;
                sx = sx == 0 ? 0 : 1 / sx;
                sy = sy == 0 ? 0 : 1 / sy;
                sz = sz == 0 ? 0 : 1 / sz;
                inverseScale = new Vector3(sx, sy, sz);
            }

            EditorGUI.BeginChangeCheck();
            if (Tools.current == Tool.None)
            {
                var oldM = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(position, t.rotation, Vector3.one);
                var handleRotation = Tools.pivotRotation == PivotRotation.Local ? Quaternion.identity : Quaternion.Inverse(t.rotation);
                if (useScale) offset = Vector3.Scale(offset, scale);
                offset = Handles.PositionHandle(offset, handleRotation);
                if (useScale) offset = Vector3.Scale(offset, inverseScale);

                var oldColor = Handles.color;
                Handles.color = Color.green;
                switch (script.triggerType)
                {
                    case ZoneTriggerType.RANGE:
                        sphere.center = useScale ? Vector3.Scale(offset, scale) : offset;
                        var mag = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                        if (useScale) range *= mag;
                        sphere.radius = range;
                        sphere.DrawHandle();
                        range = sphere.radius;
                        if (useScale) range *= mag == 0 ? 0 : 1 / mag;
                        break;
                    case ZoneTriggerType.AREA:
                        Handles.matrix = Matrix4x4.TRS(position, t.rotation, useScale ? t.lossyScale : Vector3.one);
                        box.center = offset;
                        if (useScale) area = Vector3.Scale(area, scale);
                        box.size = area;
                        box.DrawHandle();
                        area = Vector3.Max(box.size, Vector3.zero);
                        if (useScale) area = Vector3.Scale(area, inverseScale);
                        break;
                }

                Handles.color = oldColor;
                Handles.matrix = oldM;
            }

            if (EditorGUI.EndChangeCheck())
            {
                using (new SaveObjectScope(script))
                {
                    script.triggerCenter = offset;
                    script.triggerRadius = range;
                    script.triggerArea = area;
                    init = false;
                }
            }
        }
    }
}