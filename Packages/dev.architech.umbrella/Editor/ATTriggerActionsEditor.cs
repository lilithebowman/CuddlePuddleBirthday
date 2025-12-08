using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using JetBrains.Annotations;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech.Umbrella.Editor
{
    public abstract class ATTriggerActionsEditor : ATBehaviourEditor
    {
        protected override bool autoRenderVariables => false;


        protected static bool MainObjectProperty(ATReorderableList list, UnityEngine.Object dropped, ATPropertyListData propListData)
        {
            return propListData.PropertyIndex == 1 && list.DefaultObjectDropHandle(list, dropped, propListData);
        }

        protected static bool MainPropertyValidate(ATReorderableList list, UnityEngine.Object dropped, ATPropertyListData propListData)
        {
            return propListData.PropertyIndex == 1 && list.DefaultObjectDropValidate(list, dropped, propListData);
        }

        protected ATReorderableList SetupActionsList(
            string header,
            string actionTypesField,
            string objectsField,
            string boolDataField,
            string intOrEnumDataField,
            string floatDataField,
            string stringDataField,
            string stringDataExtraField,
            string referenceDataField,
            string vectorDataField,
            string urlDataField
        )
        {
            var list = new ATReorderableList(header)
                        { onDropValidate = MainPropertyValidate, onDropObject = MainObjectProperty, drawElement = RenderActions, }
                    .AddArrayProperty(serializedObject.FindProperty(actionTypesField))
                    .AddArrayProperty(serializedObject.FindProperty(objectsField))
                    .AddArrayProperty(serializedObject.FindProperty(boolDataField))
                    .AddArrayProperty(serializedObject.FindProperty(intOrEnumDataField))
                    .AddArrayProperty(serializedObject.FindProperty(floatDataField))
                    .AddArrayProperty(serializedObject.FindProperty(stringDataField))
                    .AddArrayProperty(serializedObject.FindProperty(stringDataExtraField))
                    .AddArrayProperty(serializedObject.FindProperty(referenceDataField))
                    .AddArrayProperty(serializedObject.FindProperty(vectorDataField))
                    .AddArrayProperty(serializedObject.FindProperty(urlDataField))
                ;
            _programCache?.Clear();

            return list;
        }


        protected static void RenderActions(Rect sourceRect, ATReorderableList list, int listIndex)
        {
            list.Resize(0, true);
            var properties = list.Properties;
            float remainingWidth = sourceRect.width;
            Rect drawRect = new Rect(sourceRect);
            var actionTypeProp = properties[0].GetArrayElementAtIndex(listIndex);
            var mainObjectProp = properties[1].GetArrayElementAtIndex(listIndex);
            drawRect.width = 150;
            drawRect.height = EditorGUIUtility.singleLineHeight;
            bool isChanged = false;
            var cachedChange = GUI.changed;
            GUI.changed = false;
            // render action type selection
            EditorGUI.PropertyField(drawRect, actionTypeProp, GUIContent.none);
            isChanged |= GUI.changed;
            GUI.changed |= cachedChange;
            remainingWidth -= drawRect.width;

            var actionType = (ATTriggerActionType)actionTypeProp.GetValue();
            var expectedMainObjectType = actionType.GetAttribute<ATTriggerMainObjectType>()?.type;
            // validate the main object for action type changes
            bool mainObjectIsValid = validateMainObject(mainObjectProp, expectedMainObjectType, isChanged);

            drawRect = nextDrawLocation(drawRect, remainingWidth / 3);
            // render main object

            cachedChange = GUI.changed;
            GUI.changed = false;
            if (!mainObjectIsValid) EditorGUI.PropertyField(drawRect, mainObjectProp, GUIContent.none);
            else
                switch (actionType)
                {
                    // Has no main object so skip
                    case ATTriggerActionType.DELAY:
                    case ATTriggerActionType.PLAYER_TELEPORT:
                    case ATTriggerActionType.PLAYER_TELEPORT_TO:
                    case ATTriggerActionType.PLAYER_SPEED:
                    case ATTriggerActionType.PLAYER_VELOCITY:
                    case ATTriggerActionType.PLAYER_GRAVITY:
                    case ATTriggerActionType.RESET_MOVEMENT:
                        drawRect.width = 0;
                        break;
                    case ATTriggerActionType.UDON_EVENT:
                    case ATTriggerActionType.UDON_VARIABLE:
                    case ATTriggerActionType.UDON_VARIABLE_PRIVATE:
                        renderMainObjectWithUdonBehaviourPicker(new Rect(drawRect), mainObjectProp);
                        break;
                    default:
                        if (mainObjectProp.GetValue() is GameObject)
                            EditorGUI.PropertyField(drawRect, mainObjectProp, GUIContent.none);
                        else renderMainObjectWithComponentPicker(new Rect(drawRect), mainObjectProp, expectedMainObjectType);
                        break;
                }

            isChanged |= GUI.changed;
            GUI.changed |= cachedChange;

            // validate the main object for main object changes.
            mainObjectIsValid = validateMainObject(mainObjectProp, expectedMainObjectType, isChanged);

            remainingWidth -= drawRect.width + padding;
            drawRect = nextDrawLocation(drawRect, remainingWidth);

            // render related data fields or invalid component message
            if (!mainObjectIsValid)
            {
                if (mainObjectProp.GetValue() != null) EditorGUI.LabelField(drawRect, I18n.TrContent("No valid component found for the specified Action Type."));
            }
            else
            {
                var boolProp = properties[2].GetArrayElementAtIndex(listIndex);
                var intOrEnumProp = properties[3].GetArrayElementAtIndex(listIndex);
                var floatProp = properties[4].GetArrayElementAtIndex(listIndex);
                var stringProp = properties[5].GetArrayElementAtIndex(listIndex);
                var stringExtraProp = properties[6].GetArrayElementAtIndex(listIndex);
                var objectRefProp = properties[7].GetArrayElementAtIndex(listIndex);
                var vectorProp = properties[8].GetArrayElementAtIndex(listIndex);
                var urlProp = properties[9].GetArrayElementAtIndex(listIndex);

                if (isChanged)
                {
                    // purge old data, except action type and main object
                    boolProp.ResetToDefaultValue();
                    intOrEnumProp.ResetToDefaultValue();
                    floatProp.ResetToDefaultValue();
                    stringProp.ResetToDefaultValue();
                    stringExtraProp.ResetToDefaultValue();
                    objectRefProp.ResetToDefaultValue();
                    vectorProp.ResetToDefaultValue();
                    urlProp.ResetToDefaultValue();
                }

                var dataRect = new Rect(drawRect);
                switch (actionType)
                {
                    case ATTriggerActionType.OBJECT_ENABLE:
                        renderActionDataGenericEnum<ATTriggerToggleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.OBJECT_TELEPORT:
                        renderActionDataTeleport(dataRect, objectRefProp, intOrEnumProp);
                        break;
                    case ATTriggerActionType.OBJECT_REPARENT:
                        renderActionDataGenericObjectWithToggle<Transform>(dataRect, mainObjectProp, I18n.TrContent("Target"), boolProp, new GUIContent(I18n.Tr("Stay"), I18n.Tr("Should the object keep its world position?")));
                        break;
                    case ATTriggerActionType.DELAY:
                        renderActionDataGeneric(dataRect, floatProp, I18n.TrContent("Delay in Seconds"));
                        break;
                    case ATTriggerActionType.PLAYER_TELEPORT:
                        renderActionDataPlayerTeleport(dataRect, vectorProp, boolProp);
                        break;
                    case ATTriggerActionType.PLAYER_TELEPORT_TO:
                        renderActionDataGenericObjectWithToggle<Transform>(dataRect, mainObjectProp, I18n.TrContent("Target"), boolProp, new GUIContent(I18n.Tr("Seamless"), I18n.Tr("When enabled, the player's rotation will be preserved. If disabled the player will be rotated to match the target location transform, facing the Z+ direction")));
                        break;
                    case ATTriggerActionType.PLAYER_SPEED:
                        renderActionDataPlayerSpeed(dataRect, vectorProp, boolProp);
                        break;
                    case ATTriggerActionType.PLAYER_VELOCITY:
                        renderActionDataPlayerVelocity(dataRect, vectorProp, boolProp);
                        break;
                    case ATTriggerActionType.PLAYER_GRAVITY:
                        renderActionDataGenericValueWithToggle(dataRect, floatProp, I18n.TrContent("Gravity Strength"), boolProp, new GUIContent(I18n.Tr("Additive"), I18n.Tr("Should the value be added to the existing gravity strength?")));
                        break;
                    case ATTriggerActionType.RESET_MOVEMENT:
                        // event only, no parameter drawing required
                        break;
                    case ATTriggerActionType.COLLIDER_ENABLE:
                        renderActionDataGenericEnum<ATTriggerToggleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.COLLIDER_TRIGGER:
                        renderActionDataGeneric(dataRect, boolProp, I18n.TrContent("Is Trigger"));
                        break;
                    case ATTriggerActionType.COLLIDER_BOX_CENTER:
                    case ATTriggerActionType.COLLIDER_SPHERE_CENTER:
                    case ATTriggerActionType.COLLIDER_CAPSULE_CENTER:
                        renderActionDataGenericVector3(dataRect, vectorProp, I18n.TrContent("Center"));
                        break;
                    case ATTriggerActionType.COLLIDER_SPHERE_RADIUS:
                    case ATTriggerActionType.COLLIDER_CAPSULE_RADIUS:
                        renderActionDataGeneric(dataRect, floatProp, I18n.TrContent("Radius"));
                        break;
                    case ATTriggerActionType.COLLIDER_BOX_SIZE:
                        renderActionDataGenericVector3(dataRect, vectorProp, I18n.TrContent("Size"));
                        break;
                    case ATTriggerActionType.COLLIDER_CAPSULE_HEIGHT:
                        renderActionDataGeneric(dataRect, floatProp, I18n.TrContent("Height"));
                        break;
                    case ATTriggerActionType.UDON_ENABLE:
                        renderActionDataGenericEnum<ATTriggerToggleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.UDON_EVENT:
                        renderActionDataUdonEvent(dataRect, mainObjectProp, stringProp);
                        break;
                    case ATTriggerActionType.UDON_VARIABLE:
                        renderActionDataUdonVariable(dataRect, mainObjectProp, stringProp, boolProp, intOrEnumProp, floatProp, stringExtraProp, objectRefProp, vectorProp, urlProp, false);
                        break;
                    case ATTriggerActionType.UDON_VARIABLE_PRIVATE:
                        renderActionDataUdonVariable(dataRect, mainObjectProp, stringProp, boolProp, intOrEnumProp, floatProp, stringExtraProp, objectRefProp, vectorProp, urlProp, true);
                        break;
                    case ATTriggerActionType.ANIMATOR_ENABLE:
                        renderActionDataGenericEnum<ATTriggerToggleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.ANIMATOR_PLAY:
                        renderActionDataAnimatorPlayAndCrossFade(dataRect, mainObjectProp, stringProp, floatProp, boolProp, false);
                        break;
                    case ATTriggerActionType.ANIMATOR_CROSSFADE:
                        renderActionDataAnimatorPlayAndCrossFade(dataRect, mainObjectProp, stringProp, floatProp, boolProp, true);
                        break;
                    case ATTriggerActionType.ANIMATOR_TRIGGER:
                        renderActionDataAnimatorParameters(dataRect, AnimatorControllerParameterType.Trigger, mainObjectProp, stringProp, boolProp, I18n.TrContent("Reset"));
                        break;
                    case ATTriggerActionType.ANIMATOR_BOOL:
                        renderActionDataAnimatorParameters(dataRect, AnimatorControllerParameterType.Bool, mainObjectProp, stringProp, boolProp, GUIContent.none);
                        break;
                    case ATTriggerActionType.ANIMATOR_INT:
                        renderActionDataAnimatorParameters(dataRect, AnimatorControllerParameterType.Int, mainObjectProp, stringProp, intOrEnumProp);
                        break;
                    case ATTriggerActionType.ANIMATOR_FLOAT:
                        renderActionDataAnimatorParameters(dataRect, AnimatorControllerParameterType.Float, mainObjectProp, stringProp, floatProp);
                        break;
                    case ATTriggerActionType.AUDIO_ENABLE:
                        renderActionDataGenericEnum<ATTriggerToggleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.AUDIO_ACTION:
                        renderActionDataGenericEnum<ATTriggerAudioAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.AUDIO_OPTION:
                        renderActionDataAudioOption(dataRect, intOrEnumProp, floatProp);
                        break;
                    case ATTriggerActionType.AUDIO_CLIP_PLAY:
                    case ATTriggerActionType.AUDIO_CLIP_CHANGE:
                        renderActionDataGenericObject<AudioClip>(dataRect, objectRefProp, GUIContent.none);
                        break;
                    case ATTriggerActionType.PARTICLE_ACTION:
                        renderActionDataGenericEnum<ATTriggerParticleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.TIMELINE_ENABLE:
                        renderActionDataGenericEnum<ATTriggerToggleAction>(dataRect, intOrEnumProp);
                        break;
                    case ATTriggerActionType.TIMELINE_ACTION:
                        renderActionDataGenericEnum<ATTriggerTimelineAction>(dataRect, intOrEnumProp);
                        break;
                }
            }
        }

        private static bool validateMainObject(SerializedProperty mainObjectProp, System.Type expectedType, bool doConversion)
        {
            var mainObject = (UnityEngine.Object)mainObjectProp.GetValue();
            if (expectedType == null) return true;

            bool valid = expectedType.IsInstanceOfType(mainObject);
            if (doConversion)
            {
                var found = expectedType == typeof(GameObject)
                    ? convertToGameObject(mainObject)
                    : convertToComponent(mainObject, expectedType);
                valid = found != null;
                if (valid) mainObjectProp.SetValue(found);
            }

            if (!valid) mainObjectProp.SetValue(convertToGameObject(mainObject));
            return valid;
        }

        #region Render Action Data Methods

        #region Render Action Generic

        private static void renderActionDataGeneric(Rect rect, SerializedProperty vectorProp, GUIContent label)
        {
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                EditorGUI.PropertyField(rect, vectorProp, label);
        }

        private static void renderActionDataGenericEnum<T>(Rect rect, SerializedProperty enumOrIntProp) where T : System.Enum
        {
            var action = (T)enumOrIntProp.GetValue();
            var newAction = (T)EditorGUI.EnumPopup(rect, action);
            if (!Equals(newAction, action)) enumOrIntProp.SetValue(newAction);
        }

        private static void renderActionDataGenericVector2(Rect rect, SerializedProperty vectorProp, GUIContent label)
        {
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
            {
                var v = (Vector4)vectorProp.GetValue();
                var v2 = new Vector2(v.x, v.y);
                var newV2 = EditorGUI.Vector2Field(rect, label, v2);
                if (newV2 != v2)
                {
                    v.x = newV2.x;
                    v.y = newV2.y;
                    vectorProp.SetValue(v);
                }
            }
        }

        private static void renderActionDataGenericVector3(Rect rect, SerializedProperty vectorProp, GUIContent label)
        {
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
            {
                var v = (Vector4)vectorProp.GetValue();
                var v3 = new Vector3(v.x, v.y, v.z);
                var newV3 = EditorGUI.Vector3Field(rect, label, v3);
                if (newV3 != v3)
                {
                    v.x = newV3.x;
                    v.y = newV3.y;
                    v.z = newV3.z;
                    vectorProp.SetValue(v);
                }
            }
        }


        private static void renderActionDataGenericObject<T>(Rect rect, SerializedProperty objProp, GUIContent label) where T : UnityEngine.Object
        {
            drawObjectField<T>(rect, objProp, label);
        }

        private static void renderActionDataGenericObjectWithToggle<T>(Rect rect, SerializedProperty objProp, GUIContent label, SerializedProperty toggleProp, GUIContent toggleLabel) where T : UnityEngine.Object
        {
            drawObjectFieldWithToggle<T>(rect, objProp, label, toggleProp, toggleLabel);
        }

        private static void renderActionDataGenericValueWithToggle(Rect rect, SerializedProperty valueProp, GUIContent label, SerializedProperty boolProp, GUIContent toggleLabel)
        {
            var labelWidth = ATEditorGUIUtility.GetLabelWidth(toggleLabel, boolProp);
            rect.width -= labelWidth + 22f;

            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                EditorGUI.PropertyField(rect, valueProp, label);

            rect = nextDrawLocation(rect, labelWidth + 22f);
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(toggleLabel))
                EditorGUI.PropertyField(rect, boolProp, toggleLabel);
        }

        #endregion

        #region Render Action Special

        private static void renderActionDataTeleport(Rect rect, SerializedProperty targetProp, SerializedProperty teleportActionProp)
        {
            bool hasValue = targetProp.GetValue() != null;
            if (hasValue) rect.width /= 2;
            var label = I18n.TrContent("Target");
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                drawObjectField<Transform>(rect, targetProp, label);

            if (hasValue)
            {
                rect = nextDrawLocation(rect, rect.width);
                var teleportAction = (ATTriggerTeleportAction)teleportActionProp.GetValue();
                var newAction = (ATTriggerTeleportAction)EditorGUI.EnumFlagsField(rect, teleportAction);
                if (newAction != teleportAction) teleportActionProp.SetValue((int)newAction);
            }
        }

        private static readonly float[] playerPositionValues = new float[3];
        private static readonly GUIContent[] playerPositionLabels = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };

        private static void renderActionDataPlayerTeleport(Rect rect, SerializedProperty positionProp, SerializedProperty additiveProp)
        {
            var values = (Vector4)positionProp.GetValue();
            playerPositionValues[0] = values.x;
            playerPositionValues[1] = values.y;
            playerPositionValues[2] = values.z;

            var toggleLabel = new GUIContent(I18n.Tr("Additive"), I18n.Tr("Should the player be teleported relative to their current location?"));
            var labelWidth = ATEditorGUIUtility.GetLabelWidth(toggleLabel, additiveProp);
            rect.width -= labelWidth + 22f;

            var cachedChange = GUI.changed;
            GUI.changed = false;

            EditorGUI.MultiFloatField(rect, playerPositionLabels, playerPositionValues);

            var isChanged = GUI.changed;
            GUI.changed |= cachedChange;

            if (isChanged)
            {
                values.x = playerPositionValues[0];
                values.y = playerPositionValues[1];
                values.z = playerPositionValues[2];
                positionProp.SetValue(values);
            }

            rect = nextDrawLocation(rect, labelWidth + 22f);
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(toggleLabel))
                EditorGUI.PropertyField(rect, additiveProp, toggleLabel);
        }

        private static readonly float[] playerSpeedValues = new float[4];
        private static readonly string[] playerSpeedLabels = { "Walk", "Strafe", "Run", "Jump" };
        private static readonly GUIContent[] playerSpeedContents = new GUIContent[4];

        private static void renderActionDataPlayerSpeed(Rect rect, SerializedProperty speedValuesProp, SerializedProperty additiveProp)
        {
            var values = (Vector4)speedValuesProp.GetValue();
            playerSpeedValues[0] = values.x;
            playerSpeedValues[1] = values.y;
            playerSpeedValues[2] = values.z;
            playerSpeedValues[3] = values.w;

            var toggleLabel = new GUIContent(I18n.Tr("Additive"), I18n.Tr("Should the values be added to the existing movement speed?"));
            var labelWidth = ATEditorGUIUtility.GetLabelWidth(toggleLabel, additiveProp);
            rect.width -= labelWidth + 22f;

            var cachedChange = GUI.changed;
            GUI.changed = false;

            for (int i = 0; i < playerSpeedLabels.Length; i++)
                playerSpeedContents[i].text = I18n.Tr(playerSpeedLabels[i]);

            EditorGUI.MultiFloatField(rect, playerSpeedContents, playerSpeedValues);

            var isChanged = GUI.changed;
            GUI.changed |= cachedChange;

            if (isChanged)
            {
                values.x = playerSpeedValues[0];
                values.y = playerSpeedValues[1];
                values.z = playerSpeedValues[2];
                values.w = playerSpeedValues[3];
                speedValuesProp.SetValue(values);
            }

            rect = nextDrawLocation(rect, labelWidth + 22f);
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(toggleLabel))
                EditorGUI.PropertyField(rect, additiveProp, toggleLabel);
        }

        private static readonly float[] playerVelocityValues = new float[3];
        private static readonly string[] playerVelocityLabels = { "Horizontal", "Vertical", "Forward" };
        private static readonly GUIContent[] playerVelocityContents = new GUIContent[3];

        private static void renderActionDataPlayerVelocity(Rect rect, SerializedProperty velocityValuesProp, SerializedProperty additiveProp)
        {
            var values = (Vector4)velocityValuesProp.GetValue();
            playerVelocityValues[0] = values.x;
            playerVelocityValues[1] = values.y;
            playerVelocityValues[2] = values.z;

            var toggleLabel = new GUIContent(I18n.Tr("Additive"), I18n.Tr("Should the values be added to the existing velocity?"));
            var labelWidth = ATEditorGUIUtility.GetLabelWidth(toggleLabel, additiveProp);
            rect.width -= labelWidth + 22f;

            var cachedChange = GUI.changed;
            GUI.changed = false;

            for (int i = 0; i < playerVelocityLabels.Length; i++)
                playerVelocityContents[i].text = I18n.Tr(playerVelocityLabels[i]);

            EditorGUI.MultiFloatField(rect, playerVelocityContents, playerVelocityValues);

            var isChanged = GUI.changed;
            GUI.changed |= cachedChange;

            if (isChanged)
            {
                values.x = playerVelocityValues[0];
                values.y = playerVelocityValues[1];
                values.z = playerVelocityValues[2];
                velocityValuesProp.SetValue(values);
            }

            rect = nextDrawLocation(rect, labelWidth + 22f);
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(toggleLabel))
                EditorGUI.PropertyField(rect, additiveProp, toggleLabel);
        }

        private static void renderActionDataUdonEvent(Rect rect, SerializedProperty mainObjectProp, SerializedProperty eventNameProp)
        {
            var label = I18n.TrContent("Event");
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
            {
                string[] events = getUdonEvents((UdonBehaviour)mainObjectProp.GetValue());
                string currentEvent = (string)eventNameProp.GetValue();
                var selectedIndex = System.Array.IndexOf(events, currentEvent);
                var newIndex = EditorGUI.Popup(rect, label, selectedIndex, events.Select(e => new GUIContent(e)).ToArray());
                if (selectedIndex != newIndex) eventNameProp.SetValue(events[newIndex]);
            }
        }

        private static void renderActionDataUdonVariable(Rect rect, SerializedProperty mainObjectProp, SerializedProperty fieldNameProp, SerializedProperty boolProp, SerializedProperty intOrEnumProp, SerializedProperty floatProp, SerializedProperty stringProp, SerializedProperty objectRefProp, SerializedProperty vectorProp, SerializedProperty urlProp, bool privateVariables)
        {
            var label = I18n.TrContent("Field");
            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
            {
                HeapVariable[] vars = getUdonVariables((UdonBehaviour)mainObjectProp.GetValue(), privateVariables);
                Rect fieldRect = new Rect(rect);
                fieldRect.width *= 0.5f;
                Rect valueRect = new Rect(fieldRect);
                valueRect.x += valueRect.width;

                var s_temp = fieldNameProp.stringValue?.Split(':');
                string fieldName = s_temp?[0];
                string fieldTypeKey = s_temp != null && s_temp.Length > 1 ? s_temp[1] : "";

                // CHECK OLD FIELD DATA
                int fieldIndex = System.Array.FindIndex(vars, v => v.Name == fieldName);
                HeapVariable fieldVariable = default(HeapVariable);
                SerializedProperty valueProp = null;
                if (fieldIndex > -1) fieldVariable = vars[fieldIndex];
                valueProp = fieldTypeKey switch
                {
                    "B" => boolProp,
                    "I" => intOrEnumProp,
                    "E" => intOrEnumProp,
                    "F" => floatProp,
                    "S" => stringProp,
                    "O" => objectRefProp,
                    "Y" => vectorProp,
                    "Z" => vectorProp,
                    "W" => vectorProp,
                    "U" => urlProp,
                    _ => null
                };

                // DRAW FIELD SELECTOR
                List<string> fields = vars.Select(v => $"{v.Name} ({getTypeKeyName(v.Type)})").ToList();
                var implicitVariableStart = fields.FindIndex(f => f.StartsWith("__"));
                if (implicitVariableStart > -1) fields.Insert(implicitVariableStart, "");
                GUIContent[] displayOptions = fields.Select(v => new GUIContent(v)).ToArray();

                // adjust field index for separator insertion
                if (implicitVariableStart > -1 && fieldIndex >= implicitVariableStart) fieldIndex++;
                var desiredFieldIndex = EditorGUI.Popup(fieldRect, GUIContent.none, fieldIndex, displayOptions);

                if (desiredFieldIndex != fieldIndex)
                {
                    // correct the new field index for separator insertion
                    if (implicitVariableStart > -1 && desiredFieldIndex >= implicitVariableStart) desiredFieldIndex--;
                    // clear old data
                    valueProp?.ResetToDefaultValue();

                    // update new data
                    fieldIndex = desiredFieldIndex;
                    fieldVariable = fieldIndex == -1 ? default : vars[fieldIndex];
                    fieldTypeKey = getTypeKey(fieldVariable.Type);
                    fieldNameProp.SetValue($"{fieldVariable.Name}:{fieldTypeKey}");
                    valueProp = fieldTypeKey switch
                    {
                        "B" => boolProp,
                        "I" => intOrEnumProp,
                        "E" => intOrEnumProp,
                        "F" => floatProp,
                        "S" => stringProp,
                        "O" => objectRefProp,
                        "Y" => vectorProp,
                        "Z" => vectorProp,
                        "W" => vectorProp,
                        "U" => urlProp,
                        _ => null
                    };
                    valueProp?.SetValue(fieldTypeKey switch
                    {
                        "Y" => Vector4.zero,
                        "Z" => Vector4.zero,
                        "U" => new VRCUrl(""),
                        _ => fieldVariable.Default
                    });
                }

                // DRAW VALUE PROPERTY
                if (valueProp != null)
                {
                    if (fieldTypeKey == "E")
                    {
                        var valueEnum = (System.Enum)System.Enum.ToObject(fieldVariable.Type, valueProp.GetValue());
                        var newValueEnum = EditorGUI.EnumPopup(valueRect, valueEnum);
                        if (!Equals(valueEnum, newValueEnum))
                        {
                            valueProp.SetValue(newValueEnum);
                        }
                    }
                    else if (fieldTypeKey == "U")
                    {
                        VRCUrl url = (VRCUrl)valueProp.GetValue();
                        var urlStr = url.Get();
                        var newUrlStr = EditorGUI.TextField(valueRect, GUIContent.none, urlStr);
                        if (urlStr != newUrlStr)
                        {
                            valueProp.SetValue(new VRCUrl(newUrlStr));
                        }
                    }
                    else if (fieldTypeKey == "Y")
                    {
                        Vector4 rawVec = (Vector4)valueProp.GetValue();
                        Vector2 useVec = new Vector2(rawVec.x, rawVec.y);
                        var newUseVec = EditorGUI.Vector2Field(valueRect, GUIContent.none, useVec);
                        if (!Equals(useVec, newUseVec))
                        {
                            rawVec.x = newUseVec.x;
                            rawVec.y = newUseVec.y;
                            valueProp.SetValue(rawVec);
                        }
                    }
                    else if (fieldTypeKey == "Z")
                    {
                        Vector4 rawVec = (Vector4)valueProp.GetValue();
                        Vector3 useVec = new Vector3(rawVec.x, rawVec.y, rawVec.z);
                        var newUseVec = EditorGUI.Vector3Field(valueRect, GUIContent.none, useVec);
                        if (!Equals(useVec, newUseVec))
                        {
                            rawVec.x = newUseVec.x;
                            rawVec.y = newUseVec.y;
                            rawVec.z = newUseVec.z;
                            valueProp.SetValue(rawVec);
                        }
                    }
                    else if (fieldTypeKey == "W")
                    {
                        Vector4 rawVec = (Vector4)valueProp.GetValue();
                        var newUseVec = EditorGUI.Vector4Field(valueRect, GUIContent.none, rawVec);
                        if (!Equals(rawVec, newUseVec))
                        {
                            valueProp.SetValue(newUseVec);
                        }
                    }
                    else if (fieldTypeKey == "O")
                    {
                        UnityEngine.Object obj = (UnityEngine.Object)valueProp.GetValue();
                        var newObj = EditorGUI.ObjectField(valueRect, GUIContent.none, obj, fieldVariable.Type, true);
                        if (obj != newObj)
                        {
                            valueProp.SetValue(newObj);
                        }
                    }
                    else
                    {
                        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                    }
                }
            }
        }

        private static string getTypeKey(System.Type type)
        {
            string key = "";
            if (type == null) return key;
            if (typeof(bool).IsAssignableFrom(type)) key = "B";
            else if (type.IsEnum) key = "E";
            else if (typeof(int).IsAssignableFrom(type)) key = "I";
            else if (typeof(float).IsAssignableFrom(type)) key = "F";
            else if (typeof(string).IsAssignableFrom(type)) key = "S";
            else if (typeof(VRC.SDKBase.VRCUrl).IsAssignableFrom(type)) key = "U";
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) key = "O";
            else if (typeof(Vector2).IsAssignableFrom(type)) key = "Y";
            else if (typeof(Vector3).IsAssignableFrom(type)) key = "Z";
            else if (typeof(Vector4).IsAssignableFrom(type)) key = "W";
            return key;
        }

        private static string getTypeKeyName(System.Type type)
        {
            string key = "";
            if (type == null) return key;
            if (typeof(bool).IsAssignableFrom(type)) key = "Bool";
            else if (type.IsEnum) key = type.Name;
            else if (typeof(int).IsAssignableFrom(type)) key = "Int";
            else if (typeof(float).IsAssignableFrom(type)) key = "Float";
            else if (typeof(string).IsAssignableFrom(type)) key = "String";
            else if (typeof(VRC.SDKBase.VRCUrl).IsAssignableFrom(type)) key = "Url";
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) key = type.Name;
            else if (typeof(Vector2).IsAssignableFrom(type)) key = "Vector2";
            else if (typeof(Vector3).IsAssignableFrom(type)) key = "Vector3";
            else if (typeof(Vector4).IsAssignableFrom(type)) key = "Vector4";
            return key;
        }

        private static void updateAssignedProp(HeapVariable variable, SerializedProperty prop, bool clear) { }

        private static void renderActionDataAnimatorPlayAndCrossFade(Rect rect, SerializedProperty mainObjectProp, SerializedProperty stateNameProp, SerializedProperty timeProp, SerializedProperty inSecondsProp, bool crossfade)
        {
            Animator mainObject = (Animator)mainObjectProp.GetValue();
            string currentState = (string)stateNameProp.GetValue();
            bool inSeconds = (bool)inSecondsProp.GetValue();
            float time = (float)timeProp.GetValue();

            string[] availableStates = ((AnimatorController)mainObject.runtimeAnimatorController).layers.SelectMany(
                (l, i) => l.stateMachine.states.Select(s => $"{l.name}.{s.state.name}").ToArray(),
                (r1, s1) => s1
            ).ToArray();

            if (availableStates.Length > 0)
            {
                var label = I18n.TrContent("State");
                var selectedIndex = System.Array.IndexOf(availableStates, currentState);
                var toggleWidth = 25;
                float fullWidth = rect.width;
                int newIndex = -1;
                if (stateNameProp != null && selectedIndex > -1) rect.width = (rect.width - toggleWidth) * 0.60f;
                using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                {
                    newIndex = EditorGUI.Popup(rect, label, selectedIndex, availableStates.Select(o => new GUIContent(o)).ToArray());
                    if (newIndex != selectedIndex) stateNameProp.SetValue(availableStates[newIndex]);
                }

                if (stateNameProp != null && newIndex > -1)
                {
                    string tooltipPrefix = crossfade ? "Duration" : "Start Time Offset";
                    GUIContent timeLabel = inSeconds
                        ? I18n.TrContent("Sec", $"{tooltipPrefix} (in seconds).")
                        : I18n.TrContent("Norm", $"{tooltipPrefix} (normalized).");
                    using (new ATEditorGUIUtility.ShrinkWrapLabelScope(timeLabel, timeProp))
                    {
                        rect = nextDrawLocation(rect, fullWidth - rect.width - toggleWidth);
                        var newTime = EditorGUI.FloatField(rect, timeLabel, time);
                        if (time != newTime)
                        {
                            if (newTime < 0) newTime = 0;
                            timeProp.SetValue(newTime);
                        }
                    }

                    rect = nextDrawLocation(rect, toggleWidth);
                    EditorGUI.PropertyField(rect, inSecondsProp, GUIContent.none);
                }
            }
            else EditorGUI.LabelField(rect, I18n.TrContent("No valid Animator states found."));
        }

        private static void renderActionDataAnimatorParameters(Rect rect, AnimatorControllerParameterType filterType, SerializedProperty mainObjectProp, SerializedProperty parameterNameProp, SerializedProperty parameterValueProp, GUIContent parameterValueLabel = null)
        {
            Animator mainObject = (Animator)mainObjectProp.GetValue();
            string currentParameter = (string)parameterNameProp.GetValue();

            mainObject.Rebind();
            string[] availableParameters = mainObject.parameters
                .Where(p => p.type == filterType)
                .Select(p => p.name)
                .ToArray();

            if (availableParameters.Length > 0)
            {
                var label = I18n.TrContent("Param");
                var newIndex = -1;
                var selectedIndex = System.Array.IndexOf(availableParameters, currentParameter);
                float fullWidth = rect.width;
                if (parameterValueProp != null && selectedIndex > -1) rect.width *= 0.66f;
                using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                {
                    newIndex = EditorGUI.Popup(rect, label, selectedIndex, availableParameters.Select(o => new GUIContent(o)).ToArray());
                    rect = nextDrawLocation(rect, fullWidth - rect.width);
                    if (newIndex != selectedIndex) parameterNameProp.SetValue(availableParameters[newIndex]);
                }

                if (parameterValueProp != null && newIndex > -1)
                {
                    if (parameterValueLabel == null) parameterValueLabel = I18n.TrContent("V", "Parameter value");
                    using (new ATEditorGUIUtility.ShrinkWrapLabelScope(parameterValueLabel))
                    {
                        EditorGUI.PropertyField(rect, parameterValueProp, parameterValueLabel);
                    }
                }
            }
            else EditorGUI.LabelField(rect, I18n.TrContent("No valid Animator Parameters for specified type."));
        }

        private static void renderActionDataAudioOption(Rect rect, SerializedProperty audioOptionProp, SerializedProperty dataProp)
        {
            var audioOption = (ATTriggerAudioOption)audioOptionProp.GetValue();
            var newOption = (ATTriggerAudioOption)EditorGUI.EnumPopup(rect, audioOption);
            if (newOption != audioOption) audioOptionProp.SetValue((int)newOption);
            rect = nextDrawLocation(rect, rect.width);

            var range = newOption.GetAttribute<RangeAttribute>();
            if (range == null) EditorGUI.PropertyField(rect, dataProp, GUIContent.none);
            else
            {
                float data = (float)dataProp.GetValue();
                float newData;
                switch (newOption)
                {
                    case ATTriggerAudioOption.PRIORITY:
                        newData = EditorGUI.IntSlider(rect, (int)data, (int)range.min, (int)range.max);
                        if (newData != data) dataProp.SetValue(newData);
                        break;
                    default:
                        newData = EditorGUI.Slider(rect, data, range.min, range.max);
                        if (newData != data) dataProp.SetValue(newData);
                        break;
                }
            }
        }

        #endregion

        #endregion

        #region Common Helper Methods

        private const float padding = 5f;

        private static Rect nextDrawLocation(Rect rect, float nextWidth)
        {
            rect.x += rect.width + padding;
            rect.width = nextWidth - padding;
            return rect;
        }

        private static UnityEngine.Object convertToGameObject(UnityEngine.Object from)
        {
            if (from is GameObject) return from;
            if (from is Component compontent) return compontent.gameObject;
            return null;
        }

        private static UnityEngine.Object convertToComponent(UnityEngine.Object from, System.Type type)
        {
            if (type == null || !typeof(Component).IsAssignableFrom(type)) return null;
            if (type.IsInstanceOfType(from)) return from;
            if (from is Component component) from = component.gameObject;
            if (from is GameObject go) return go.GetComponent(type);
            return null;
        }

        private static void drawObjectField<T>(Rect rect, SerializedProperty prop, GUIContent label) where T : UnityEngine.Object
        {
            var target = prop.GetValue() as T;
            var newTarget = (T)EditorGUI.ObjectField(rect, label, target, typeof(T), true);
            if (newTarget != target)
            {
                target = newTarget;
                prop.SetValue(newTarget);
            }
        }

        private static void drawObjectFieldWithToggle<T>(Rect rect, SerializedProperty objProp, GUIContent label, SerializedProperty toggleProp, GUIContent toggleLabel) where T : UnityEngine.Object
        {
            bool hasValue = objProp.GetValue() != null;
            var labelWidth = 0f;
            if (hasValue)
            {
                labelWidth = ATEditorGUIUtility.GetLabelWidth(toggleLabel, toggleProp);
                rect.width -= labelWidth + 22f;
            }

            using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label))
                drawObjectField<T>(rect, objProp, label);

            if (hasValue)
            {
                rect = nextDrawLocation(rect, labelWidth + 22f);
                using (new ATEditorGUIUtility.ShrinkWrapLabelScope(toggleLabel))
                    EditorGUI.PropertyField(rect, toggleProp, toggleLabel);
            }
        }

        private static void renderMainObjectWithComponentPicker(Rect drawRect, SerializedProperty mainObjectProp, System.Type searchType = null)
        {
            var mainObject = mainObjectProp.GetValue() as Component;
            if (mainObject == null)
            {
                if (searchType == null) searchType = typeof(Component);
                mainObject = (Component)EditorGUI.ObjectField(drawRect, GUIContent.none, null, searchType, true);
                mainObjectProp.SetValue(mainObject);
                return;
            }

            if (searchType == null) searchType = mainObject.GetType();
            var components = mainObject.gameObject.GetComponents<Component>();
            var validComponents = components.Where(c => searchType.IsInstanceOfType(c)).ToArray();
            bool hasMultipleComponents = validComponents.Length > 1;
            const int dropdownWidth = 20;
            if (hasMultipleComponents) drawRect.width -= dropdownWidth;

            mainObject = (Component)EditorGUI.ObjectField(drawRect, GUIContent.none, mainObject, searchType, true);

            if (hasMultipleComponents)
            {
                drawRect.x += drawRect.width;
                drawRect.width = dropdownWidth;
                var index = System.Array.IndexOf(validComponents, mainObject);
                var componentNames = validComponents.Select(c => new GUIContent($"{System.Array.IndexOf(components, c)}: {c.GetType().Name}")).ToArray();
                index = EditorGUI.Popup(drawRect, GUIContent.none, index, componentNames);
                if (index > -1) mainObject = validComponents[index];
            }

            mainObjectProp.SetValue(mainObject);
        }

        #endregion

        #region UdonBehviour Helper Methods

        private static void renderMainObjectWithUdonBehaviourPicker(Rect drawRect, SerializedProperty behaviourProp)
        {
            var behaviour = behaviourProp.GetValue() as Component;
            var oldBehaviour = behaviour;
            List<Component> comps = new List<Component>();
            if (behaviour != null)
            {
                Component[] goComps = behaviour.gameObject.GetComponents<Component>();
                foreach (var c in goComps)
                {
                    if (c is UdonSharpBehaviour) comps.Add(c);
                    else if (c is UdonBehaviour ub && !UdonSharpEditorUtility.IsUdonSharpBehaviour(ub)) comps.Add(c);
                }
            }

            var components = comps.ToArray();

            bool hasMultipleBehaviours = comps.Count > 1;
            const int dropdownWidth = 20;
            if (hasMultipleBehaviours) drawRect.width -= dropdownWidth;

            // proxy to the U# script temporarily for processing
            if (behaviour is UdonBehaviour ubhvr && UdonSharpEditorUtility.IsUdonSharpBehaviour(ubhvr)) behaviour = UdonSharpEditorUtility.GetProxyBehaviour(ubhvr);
            behaviour = (Component)EditorGUI.ObjectField(drawRect, GUIContent.none, behaviour, typeof(Component), true);
            if (behaviour != null && behaviour is not (UdonSharpBehaviour or UdonBehaviour))
            {
                // if provided component is not an udon behaviour type, hunt for available type (TODO support cyan triggers)
                Component b = behaviour.gameObject.GetComponent<UdonSharpBehaviour>();
                if (b == null) b = ubhvr = behaviour.gameObject.GetComponent<UdonBehaviour>();
                behaviour = b;
            }

            if (hasMultipleBehaviours)
            {
                drawRect.x += drawRect.width;
                drawRect.width = dropdownWidth;
                var index = System.Array.IndexOf(components, behaviour);
                var componentNames = components.Select((c, i) =>
                {
                    var label = $"{i}: ";
                    if (c is UdonBehaviour ub && ub.programSource != null)
                        label += $"UB_{ub.programSource.name}";
                    else label += c.GetType().Name;

                    return new GUIContent(label);
                }).ToArray();
                index = EditorGUI.Popup(drawRect, GUIContent.none, index, componentNames);
                if (index > -1) behaviour = components[index];
            }

            // unproxy the U# script so the script reference isn't lost during build
            if (behaviour is UdonSharpBehaviour usbhvr) behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(usbhvr);
            if (oldBehaviour != behaviour) behaviourProp.SetValue(behaviour);

            // TODO add support for CyanTriggers
        }

        private static HashSet<string> internalEventNames = null;

        private static string[] getUdonEvents(UdonBehaviour behaviour)
        {
            string[] options = new string[0];
            if (behaviour == null) return options;
            var opts = new List<string>();
            if (UdonSharpEditorUtility.IsUdonSharpBehaviour(behaviour))
            {
                var eventSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(behaviour);
                var (program, _) = getUBInfo(behaviour);
                var entryPoints = program.EntryPoints;

                if (internalEventNames == null)
                {
                    var InternalEventNamesInfo = typeof(VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView.UdonNodes.UdonNodeExtensions).GetField("InternalEventNames", (BindingFlags)~0);
                    internalEventNames = (HashSet<string>)InternalEventNamesInfo?.GetValue(null);
                }

                // instance methods
                var methods = eventSharpBehaviour.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (method.IsSpecialName) continue;
                    var sanitized = $"_{method.Name.Substring(0, 1).ToLower()}{method.Name.Substring(1)}";
                    if (internalEventNames?.Contains(sanitized) ?? false)
                    {
                        if (entryPoints.HasExportedSymbol(sanitized)) opts.Add(sanitized);
                    }
                    else if (entryPoints.HasExportedSymbol(method.Name)) opts.Add(method.Name);
                }

                // extension methods
                methods = ATEditorUtility.GetExtensionMethods(eventSharpBehaviour.GetType());
                foreach (var method in methods)
                {
                    if (method.IsSpecialName) continue;
                    var sanitized = $"_{method.Name.Substring(0, 1).ToLower()}{method.Name.Substring(1)}";
                    if (internalEventNames?.Contains(sanitized) ?? false)
                    {
                        if (entryPoints.HasExportedSymbol(sanitized)) opts.Add(sanitized);
                    }
                    else if (entryPoints.HasExportedSymbol(method.Name)) opts.Add(method.Name);
                }

                options = opts.ToArray();
            }
            else
            {
                var (program, _) = getUBInfo(behaviour);
                options = program.EntryPoints.GetExportedSymbols().ToArray();
            }

            System.Array.Sort(options, string.CompareOrdinal);
            return options;
        }

        private static HeapVariable[] getUdonVariables(UdonBehaviour behaviour, bool privateVariables)
        {
            if (behaviour == null) return new HeapVariable[0];
            var (_, variables) = getUBInfo(behaviour);
            return variables.Where(v => v.IsExported != privateVariables).ToArray();
        }

        private static readonly System.Type[] typesAllowed =
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(VRC.SDKBase.VRCUrl),
            typeof(UnityEngine.Object),
            typeof(System.Enum),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4)
        };

        private struct HeapVariable
        {
            public string Name;
            public uint Address;
            public bool IsExported;
            public System.Type Type;
            public object Default;
        }

        private static readonly string[] prohibitedPrefixes = { "__const", "__intnl", "__gintnl", "__refl", "__this", "__lcl" };
        private static readonly string[] prohibitedSuffixes = { "__ret", "__intnlparam" };
        private static readonly Dictionary<UdonBehaviour, (IUdonProgram, List<HeapVariable>)> _programCache = new Dictionary<UdonBehaviour, (IUdonProgram, List<HeapVariable>)>();

        private static (IUdonProgram program, List<HeapVariable> variables) getUBInfo(UdonBehaviour behaviour)
        {
            if (!_programCache.TryGetValue(behaviour, out (IUdonProgram program, List<HeapVariable> variables) cache))
            {
                // RetrieveProgram is an expensive operation, so we cache it as long as the current inspector instance is visible.
                cache.program = behaviour.programSource.SerializedProgramAsset.RetrieveProgram();

                var heap = cache.program.Heap;

                var variables = new List<HeapVariable>();
                foreach (string symbol in cache.program.SymbolTable.GetSymbols())
                {
                    if (prohibitedPrefixes.Any(p => symbol.StartsWith(p)) || prohibitedSuffixes.Any(p => symbol.EndsWith(p))) continue;
                    Type type;
                    bool isExported;
                    if (UdonSharpEditorUtility.IsUdonSharpBehaviour(behaviour))
                    {
                        try
                        {
                            if (!tryGetUdonSharpType(behaviour, symbol, out type, out isExported))
                            {
                                type = cache.program.SymbolTable.GetSymbolType(symbol);
                                isExported = cache.program.SymbolTable.HasExportedSymbol(symbol);
                            }
                        }
                        catch (NotSupportedException) // when a field exists but should not be allowed to be selected
                        {
                            continue;
                        }
                    }
                    else
                    {
                        type = cache.program.SymbolTable.GetSymbolType(symbol);
                        isExported = cache.program.SymbolTable.HasExportedSymbol(symbol);
                    }

                    if (!typesAllowed.Any(t => t.IsAssignableFrom(type)) || type.IsArray) continue;
                    var addr = cache.program.SymbolTable.GetAddressFromSymbol(symbol);
                    var Default = heap.GetHeapVariable(addr);
                    if (Default is VRC.Udon.Common.UdonGameObjectComponentHeapReference)
                        Default = null;
                    variables.Add(new HeapVariable
                    {
                        Name = symbol,
                        Address = addr,
                        IsExported = isExported,
                        Type = type,
                        Default = Default
                    });
                }

                var sortedVars = variables
                    .OrderBy(v => v.Name.StartsWith("__"))
                    .ThenBy(v => v.Name.StartsWith("_"))
                    .ThenBy(v => v.Name);

                cache.variables = sortedVars.ToList();
                _programCache.Add(behaviour, cache);
            }

            return cache;
        }

        private static bool tryGetUdonSharpType(UdonBehaviour behaviour, string symbol, out Type type, out bool isExported)
        {
            type = null;
            isExported = false;
            var script = UdonSharpEditorUtility.GetProxyBehaviour(behaviour);
            var fields = script.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                // only pull fields from classes that extend from UdonSharpBehaviour.
                var typeMatch = !f.DeclaringType?.IsAssignableFrom(typeof(UdonSharpBehaviour)) ?? false;
                if (f.Name != symbol) continue;
                if (!typeMatch || f.FieldType.IsArray || f.IsLiteral || f.IsInitOnly) throw new NotSupportedException();
                type = f.FieldType;
                isExported = f.IsPublic || f.IsAssembly || f.GetCustomAttribute<SerializeField>() != null || f.GetCustomAttribute<PublicAPIAttribute>() != null;
                return true;
            }

            return false;
        }

        #endregion
    }
}