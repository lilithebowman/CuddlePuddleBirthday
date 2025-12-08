using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchiTech.SDK;
using ArchiTech.SDK.Editor;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using Object = UnityEngine.Object;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace ArchiTech.Umbrella.Editor
{
    public abstract class UdonActionEditor : ATBehaviourEditor
    {
        private UdonActions _actionsScript;
        private UnityEngine.Object[] objects;
        private int[] actions;
        private object[][] parameterObjects;
        private int[] dynamicIndexes;
        private int[] selectedParameters = new int[0];

        protected readonly List<UnityEngine.Object> actionObjects = new List<UnityEngine.Object>();
        private ReorderableList actionList;

        protected ReorderableList ActionList
        {
            get => actionList;
        }


        /// <summary>
        /// This type is used to filter for values that can be assigned when doing implicit variable retrieval with certain contexts.
        /// Currently helps filter UdonBeahviour.SetProgramVariable action to only show variables that the returned type can be assigned to.
        /// Defaults to generic object meaning that all possible variables are shown. 
        /// </summary>
        protected virtual Type dynamicType
        {
            get => null;
        }

        /// <summary>
        /// Specify a custom height for each element that is added to the already handled internal height.
        /// This is used to create space in each element for any sort of custom fields that child classes need to make use of.
        /// </summary>
        protected virtual float customElementHeight
        {
            get => 0;
        }


        protected class ActionType : IEquatable<ActionType>
        {
            public class Parameter
            {
                public string name;
                public Type type;
                public bool implicitOptions;
            }

            public UdonAction @enum;
            public Type actionType;
            public string actionName;
            public int actionIndex;
            public bool inlineParameters;
            public List<Parameter> parameters;

            public bool Equals(ActionType other)
            {
                return Equals(@enum, other?.@enum);
            }
        }

        private static readonly List<ActionType> availableActions = getAllActions();

        private static List<ActionType> getAllActions()
        {
            var list = new List<ActionType>();
            int actionIndex = 0;
            var type = typeof(UdonAction);
            foreach (string enumName in Enum.GetNames(type))
            {
                var value = (UdonAction)Enum.Parse(type, enumName);
                var udonAction = value.GetAttribute<UdonActionTypeAttribute>();
                var udonActionParams = value.GetAttributes<UdonActionParamAttribute>();
                var @params = new List<ActionType.Parameter>();
                foreach (var param in udonActionParams)
                    @params.Add(new ActionType.Parameter()
                    {
                        name = param.ParameterName ?? $"Parameter {@params.Count}",
                        type = param.ParameterType,
                        implicitOptions = param.ImplicitOptions
                    });
                var actionType = new ActionType()
                {
                    @enum = value,
                    actionType = udonAction.ActionType,
                    actionIndex = actionIndex++,
                    actionName = udonAction.ActionName ?? enumName,
                    inlineParameters = udonAction.InlineParameters,
                    parameters = @params
                };
                list.Add(actionType);
            }

            return list;
        }

        protected virtual void OnEnable()
        {
            _actionsScript = (UdonActions)target;
            actionList = new ReorderableList(actionObjects, typeof(UnityEngine.Object), true, true, true, true)
            {
                drawHeaderCallback = renderListHeader,
                drawElementCallback = renderListElement,
                // disallow foolish negative custom heights
                elementHeight = (EditorGUIUtility.singleLineHeight + 2) * 2 + Mathf.Max(0, customElementHeight) + 5,
                onAddCallback = listAdd,
                onRemoveCallback = listRemove,
                onReorderCallbackWithDetails = listReordered
            };
            selectedParameters = new int[actionObjects.Count];
        }

        private void renderListHeader(Rect rect)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(evt.mousePosition)) break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object objRef in DragAndDrop.objectReferences)
                        {
                            listAdd(actionList);
                            objects[objects.Length - 1] = objRef;
                        }
                    }

                    break;
            }

            string text = rect.Contains(evt.mousePosition) ? I18n.Tr("Drop objects here to add to list") : "OnActivate";
            EditorGUI.LabelField(rect, text, new GUIStyle("Box"));
        }

        private void listAdd(ReorderableList list)
        {
            objects = AddArrayItem(objects, -1, null);
            actions = AddArrayItem(actions, -1, 0);
            parameterObjects = AddArrayItem(parameterObjects, -1, new object[0]);
            dynamicIndexes = AddArrayItem(dynamicIndexes, -1, -1);
            selectedParameters = AddArrayItem(selectedParameters, -1, 0);
            ForceSave();
        }

        private void listRemove(ReorderableList list)
        {
            int nextIndex = list.index;
            objects = RemoveArrayItem(objects, nextIndex);
            actions = RemoveArrayItem(actions, nextIndex);
            parameterObjects = RemoveArrayItem(parameterObjects, nextIndex);
            dynamicIndexes = RemoveArrayItem(dynamicIndexes, nextIndex);
            selectedParameters = RemoveArrayItem(selectedParameters, nextIndex);
            if (nextIndex >= objects.Length) nextIndex = objects.Length - 1;
            list.GetType().GetField("m_ActiveElement", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(list, nextIndex);
            ForceSave();
        }

        private void listReordered(ReorderableList list, int from, int to)
        {
            objects = MoveArrayItem(objects, from, to);
            actions = MoveArrayItem(actions, from, to);
            parameterObjects = MoveArrayItem(parameterObjects, from, to);
            dynamicIndexes = MoveArrayItem(dynamicIndexes, from, to);
            selectedParameters = MoveArrayItem(selectedParameters, from, to);

            ForceSave();
        }

        private void renderListAction(Rect rect, int listIndex, ref object @object, ref ActionType action, ref object[] objectParameters, ref int dynamicIndex) { }

        private void renderListParameters(Rect rect, int listIndex, ref object @object, ref ActionType action, ref object[] objectParameters, ref int dynamicIndex) { }

        private void renderListElement(Rect rect, int listIndex, bool isActive, bool isFocused)
        {
            if (listIndex >= objects.Length) return; // prevent unexpected out of bounds issues
            var @object = objects[listIndex];
            var actionIndex = actions[listIndex];
            var objectParameters = parameterObjects[listIndex];
            var dynamicIndex = dynamicIndexes[listIndex];

            var halfW = rect.width / 2;
            var rowHeight = EditorGUIUtility.singleLineHeight;


            var drawRect = new Rect(rect.x, rect.y, halfW, rowHeight);
            // proxy to the U# script temporarily for processing
            if (@object is UdonBehaviour ubhvr && UdonSharpEditorUtility.IsUdonSharpBehaviour(ubhvr)) @object = UdonSharpEditorUtility.GetProxyBehaviour(ubhvr);
            @object = EditorGUI.ObjectField(drawRect, @object, typeof(UnityEngine.Object), true);
            // unproxy the U# script so the script reference isn't lost during build
            if (@object is UdonSharpBehaviour usbhvr) @object = UdonSharpEditorUtility.GetBackingUdonBehaviour(usbhvr);
            if (GUI.changed)
            {
                // anytime the object changes, reset the active selection
                objectParameters = new object[@object != null ? availableActions[0].parameters.Count : 0];
                actionIndex = 0;
                dynamicIndex = -1;
            }

            bool hasObj = @object != null;
            GameObject rawObj = @object is Component c_obj ? (c_obj == null ? null : c_obj.gameObject) : @object as GameObject;
            if (hasObj) hasObj = rawObj != null;
            if (hasObj)
            {
                List<UnityEngine.Object> goAndComponents = new List<UnityEngine.Object> { rawObj };
                foreach (var component in rawObj.GetComponents<Component>())
                {
                    // Exclude the hidden udon behaviour that U# has on the game object since we want to reference the UdonSharpBehaviour type instead
                    if (component is UdonSharpBehaviour) continue;
                    goAndComponents.Add(component);
                }

                // get all possibe valid options for the popup
                List<Tuple<UnityEngine.Object, ActionType>> validActions = new List<Tuple<UnityEngine.Object, ActionType>>();
                foreach (var o in goAndComponents)
                    validActions.AddRange(
                        from at in availableActions
                        where at.actionType.IsInstanceOfType(o)
                        select new Tuple<Object, ActionType>(o, at)
                    );

                // get popup labels
                List<string> validActionNames = new List<string>();
                // Get popup values
                List<int> validActionIndexes = new List<int>();
                foreach (var (goOrComponent, at) in validActions)
                {
                    var dupes = goAndComponents.Where(o => o.GetType() == goOrComponent.GetType()).ToArray();
                    var indx = dupes.Length > 1 ? $"[{System.Array.IndexOf(dupes, goOrComponent)}]" : "";
                    var tmp = goOrComponent;
                    if (goOrComponent is UdonBehaviour ubvr && UdonSharpEditorUtility.IsUdonSharpBehaviour(ubvr))
                        tmp = UdonSharpEditorUtility.GetProxyBehaviour(ubvr);
                    var actionName = $"{tmp.GetType().Name}{indx}.{at.actionName}";

                    validActionNames.Add(actionName);
                    // positive indexes will always indicate exact values
                    validActionIndexes.Add(at.actionIndex);
                }

                // Get the active popup value
                var action = validActions.Where(
                    tuple => tuple.Item2.actionType.IsInstanceOfType(@object) && tuple.Item2.actionIndex == actionIndex
                ).Select(tuple => tuple.Item2).FirstOrDefault();

                if (validActions.Count > 0)
                {
                    drawRect = new Rect(rect.x + halfW, rect.y, halfW, rowHeight);
                    actionIndex = EditorGUI.IntPopup(drawRect, actionIndex, validActionNames.ToArray(), validActionIndexes.ToArray());
                    // extract the choice
                    action = availableActions[actionIndex];
                    if (GUI.changed)
                    {
                        // make sure that the new action has the object reference being matched correctly
                        @object = goAndComponents.First(o => action.actionType.IsInstanceOfType(o));
                    }

                    // make sure the parameters count is up to date to the action type
                    if (objectParameters.Length != action.parameters.Count)
                    {
                        objectParameters = new object[action.parameters.Count];
                    }

                    // render the parameters dropdown and data entry
                    drawRect = new Rect(rect.x, rect.y + rowHeight + 2, rect.width, rowHeight);
                    if (action.inlineParameters)
                        drawParametersInline(drawRect, listIndex, action, in @object, in objectParameters, ref dynamicIndex);
                    else drawParameters(drawRect, listIndex, action, in @object, in objectParameters, ref dynamicIndex);

                    if (customElementHeight > 0)
                    {
                        DrawElementExtension(new Rect(rect.x, rect.y + rowHeight + rowHeight + 4, rect.width, customElementHeight), listIndex, in @object, in action);
                    }
                }
                else
                {
                    drawRect = new Rect(rect.x, rect.y + rowHeight, rect.width, rowHeight);
                    EditorGUI.LabelField(drawRect, objects[listIndex].GetType().FullName + I18n.Tr($" - No actions available for this object"));
                }
            }

            objects[listIndex] = @object;
            actions[listIndex] = actionIndex;
            parameterObjects[listIndex] = objectParameters;
            dynamicIndexes[listIndex] = dynamicIndex;
            if (GUI.changed) ForceSave();
        }

        protected virtual void DrawElementExtension(Rect rect, int listIndex, in UnityEngine.Object @object, in ActionType action) { }

        private void drawParametersInline(Rect rect, int listIndex, ActionType action, in UnityEngine.Object obj, in object[] paramObjects, ref int dynamicIndex)
        {
            float width = rect.width / action.parameters.Count;
            Rect parameterShape = new Rect(rect.x, rect.y, width, rect.height);
            bool ubSymbolChanged = false;
            for (int parameterIndex = 0; parameterIndex < action.parameters.Count; parameterIndex++)
            {
                var selectedParameter = action.parameters[parameterIndex];
                var selectedType = selectedParameter.type;
                if (action.@enum == UdonAction.UDONBEHAVIOUR_SETPROGRAMVARIABLE && parameterIndex == 1)
                {
                    var symbol = (string)paramObjects[0];
                    var (program, variables) = getUBInfo((UdonBehaviour)obj);
                    selectedType = variables.First(v => v.name == symbol).type;
                }

                var canBeDynamic = dynamicType != null && dynamicType.IsAssignableFrom(selectedType);
                var isDynamic = dynamicIndex == parameterIndex;

                if (canBeDynamic)
                {
                    var dynamicShape = new Rect(parameterShape.x, parameterShape.y, EditorGUIUtility.singleLineHeight, parameterShape.height);
                    isDynamic = EditorGUI.Toggle(dynamicShape, isDynamic);
                    if (GUI.changed)
                    {
                        dynamicIndex = isDynamic ? parameterIndex : -1;
                    }

                    parameterShape.x += dynamicShape.width;
                    parameterShape.width -= dynamicShape.width;
                }
                else if (isDynamic)
                {
                    dynamicIndex = -1;
                    ForceSave();
                }


                if (canBeDynamic && isDynamic) EditorGUI.LabelField(parameterShape, I18n.Tr("Dynamic Value Enabled"));
                else
                {
                    var options = getOptionsFor(obj, action.@enum, out string missingMsg);
                    if (selectedParameter.implicitOptions && options != null)
                    {
                        if (options.Length == 0)
                        {
                            EditorGUI.LabelField(parameterShape, missingMsg);
                        }
                        else
                        {
                            // generic parameter rendering

                            int currentOption = System.Array.IndexOf(options, paramObjects[parameterIndex]);
                            if (currentOption < 0) currentOption = 0;
                            currentOption = EditorGUI.Popup(parameterShape, currentOption, options);
                            if (action.@enum == UdonAction.UDONBEHAVIOUR_SETPROGRAMVARIABLE && parameterIndex == 0)
                            {
                                ubSymbolChanged = (string)paramObjects[parameterIndex] != options[currentOption];
                            }

                            paramObjects[parameterIndex] = options[currentOption];
                        }
                    }
                    else if (action.@enum == UdonAction.UDONBEHAVIOUR_SETPROGRAMVARIABLE && parameterIndex == 1)
                    {
                        // special case for handling program variables
                        // instead of the parameter definition defining the field type, we pull the type from the UdonBehaviour's symbol
                        var symbol = (string)paramObjects[0];
                        var (program, variables) = getUBInfo((UdonBehaviour)obj);
                        var @var = variables.First(v => v.name == symbol);
                        if (ubSymbolChanged) paramObjects[parameterIndex] = @var.@default;
                        drawGenericParameter(parameterShape, in @var.type, ref paramObjects[parameterIndex], @var.@default);
                    }
                    else drawGenericParameter(parameterShape, in selectedParameter.type, ref paramObjects[parameterIndex], null);
                }


                parameterShape.x += width;
                parameterShape.width = width;
            }
        }

        private void drawParameters(Rect rect, int listIndex, ActionType action, in UnityEngine.Object obj, in object[] paramObjects, ref int dynamicIndex)
        {
            var halfW = rect.width / 2;
            float offset = 0f;
            Rect paramDynamic = new Rect(rect.x + offset, rect.y, EditorGUIUtility.singleLineHeight, rect.height);
            offset += paramDynamic.width;
            Rect paramSelect = new Rect(rect.x + offset, rect.y, halfW - offset, rect.height);
            offset += paramSelect.width;
            Rect paramValue = new Rect(rect.x + offset, rect.y, rect.width - offset, rect.height);
            var parameterIndex = EditorGUI.Popup(paramSelect, selectedParameters[listIndex], action.parameters.Select(p => p.name).ToArray());
            selectedParameters[listIndex] = parameterIndex;
            var selectedParameter = action.parameters[parameterIndex];
            var canBeDynamic = dynamicType != null && dynamicType.IsAssignableFrom(selectedParameter.type);
            var isDynamic = dynamicIndex == parameterIndex;
            using (new EditorGUI.DisabledScope(!canBeDynamic))
            {
                isDynamic = EditorGUI.Toggle(paramDynamic, isDynamic);
                if (GUI.changed)
                {
                    dynamicIndex = isDynamic ? parameterIndex : -1;
                }
            }

            if (isDynamic) EditorGUI.LabelField(paramValue, I18n.Tr("Dynamic Value Enabled"));
            else
            {
                var options = getOptionsFor(obj, action.@enum, out string missingMsg);
                if (selectedParameter.implicitOptions && options != null)
                {
                    if (options.Length == 0)
                    {
                        EditorGUI.LabelField(paramValue, missingMsg);
                    }
                    else
                    {
                        // generic parameter rendering

                        int currentOption = System.Array.IndexOf(options, paramObjects[parameterIndex]);
                        if (currentOption < 0) currentOption = 0;
                        currentOption = EditorGUI.Popup(paramValue, currentOption, options);
                        paramObjects[parameterIndex] = options[currentOption];
                    }
                }
                else drawGenericParameter(paramValue, in selectedParameter.type, ref paramObjects[parameterIndex], null);
            }
        }

        private string[] getOptionsFor(UnityEngine.Object obj, UdonAction @enum, out string missingMsg)
        {
            string[] options = null;
            missingMsg = "";
            var opts = new List<string>();
            switch (@enum)
            {
                // these are broken somehow. Shows up on reimport, but lost after changing the parameters list
                case UdonAction.ANIMATOR_SETBOOL:
                    missingMsg = I18n.Tr("No available Animator parameters of type:") + " Bool";
                    options = (obj as Animator)?.parameters
                        .Where(animParam => animParam.type == AnimatorControllerParameterType.Bool)
                        .Select(animParam => animParam.name)
                        .ToArray();
                    break;

                case UdonAction.ANIMATOR_SETINTEGER:
                    missingMsg = I18n.Tr("No available Animator parameters of type:") + " Integer";
                    options = (obj as Animator)?.parameters
                        .Where(animParam => animParam.type == AnimatorControllerParameterType.Int)
                        .Select(animParam => animParam.name)
                        .ToArray();
                    break;

                case UdonAction.ANIMATOR_SETFLOAT:
                    missingMsg = I18n.Tr("No available Animator parameters of type:") + " Float";
                    options = (obj as Animator)?.parameters
                        .Where(animParam => animParam.type == AnimatorControllerParameterType.Float)
                        .Select(animParam => animParam.name)
                        .ToArray();
                    break;

                case UdonAction.ANIMATOR_SETTRIGGER:
                    missingMsg = I18n.Tr("No available Animator parameters of type:") + " Trigger";
                    options = (obj as Animator)?.parameters
                        .Where(animParam => animParam.type == AnimatorControllerParameterType.Trigger)
                        .Select(animParam => animParam.name)
                        .ToArray();
                    break;

                case UdonAction.UDONBEHAVIOUR_SETPROGRAMVARIABLE:
                    missingMsg = I18n.Tr("No available Udon Variables found");
                    var varBehaviour = obj as UdonBehaviour;
                    if (UdonSharpEditorUtility.IsUdonSharpBehaviour(varBehaviour))
                    {
                        var varSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(varBehaviour);
                        var fields = varSharpBehaviour.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var f in fields)
                        {
                            // only pull fields from classes that extend from UdonSharpBehaviour.
                            var typeMatch = !f.DeclaringType?.IsAssignableFrom(typeof(UdonSharpBehaviour)) ?? false;
                            if (typeMatch && !f.FieldType.IsArray)
                                opts.Add(f.Name);
                        }

                        varBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(varSharpBehaviour);
                    }

                    if (varBehaviour != null)
                    {
                        var (program, variables) = getUBInfo(varBehaviour);

                        foreach (var @var in variables.Where(v => v.isExported))
                        {
                            // UnityEngine.Debug.Log($"Symbol {@var.name} type {@var.type.Name}");
                            if (!opts.Contains(@var.name) && !var.type.IsArray)
                                opts.Add(@var.name);
                        }

                        foreach (var @var in variables)
                        {
                            // UnityEngine.Debug.Log($"Symbol {@var.name} type {@var.type.Name}");
                            if (!opts.Contains(@var.name) && !var.type.IsArray)
                                opts.Add(@var.name);
                        }
                    }

                    options = opts.ToArray();
                    break;

                case UdonAction.UDONBEHAVIOUR_SENDCUSTOMEVENT:
                    missingMsg = I18n.Tr("No available Udon Events found");
                    var eventBehaviour = obj as UdonBehaviour;
                    if (eventBehaviour != null && UdonSharpEditorUtility.IsUdonSharpBehaviour(eventBehaviour))
                    {
                        var eventSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(eventBehaviour);
                        var (program, variables) = getUBInfo(eventBehaviour);
                        var entryPoints = program.EntryPoints;

                        var methods = eventSharpBehaviour.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var method in methods)
                        {
                            if (!method.DeclaringType?.IsAssignableFrom(typeof(UdonSharpBehaviour)) ?? false)
                            {
                                if (method.IsSpecialName) continue;
                                if (entryPoints.HasExportedSymbol(method.Name))
                                {
                                    opts.Add(method.Name);
                                    continue;
                                }

                                var sanitized = $"_{method.Name.Substring(0, 1).ToLower()}{method.Name.Substring(1)}";
                                if (entryPoints.HasExportedSymbol(sanitized)) opts.Add(sanitized);
                            }
                        }

                        options = opts.ToArray();
                    }
                    else if (eventBehaviour != null)
                    {
                        var (program, variables) = getUBInfo(eventBehaviour);
                        options = program.EntryPoints.GetExportedSymbols().ToArray();
                    }

                    break;
            }

            return options;
        }


        private struct HeapVariable
        {
            public string name;
            public uint address;
            public bool isExported;
            public Type type;
            public object @default;
        }

        private static readonly string[] prohibitedPrefixes = { "__const", "__intnl", "__gintnl", "__refl", "__this", "__lcl" };
        private static readonly string[] prohibitedSuffixes = { "__ret", "__intnlparam" };
        private readonly Dictionary<UdonBehaviour, (IUdonProgram, List<HeapVariable>)> _programCache = new Dictionary<UdonBehaviour, (IUdonProgram, List<HeapVariable>)>();

        private (IUdonProgram program, List<HeapVariable> variables) getUBInfo(UdonBehaviour behaviour)
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
                    var type = cache.program.SymbolTable.GetSymbolType(symbol);
                    if (!genericTypesAllowed.Any(t => t.IsAssignableFrom(type)) || type.IsArray) continue;
                    var addr = cache.program.SymbolTable.GetAddressFromSymbol(symbol);
                    variables.Add(new HeapVariable
                    {
                        name = symbol,
                        address = addr,
                        isExported = cache.program.SymbolTable.HasExportedSymbol(symbol),
                        type = type,
                        @default = heap.GetHeapVariable(addr)
                    });
                }

                variables.Sort((v1, v2) => string.Compare(v1.name, v2.name, StringComparison.Ordinal));

                cache.variables = variables;
                _programCache.Add(behaviour, cache);
            }

            return cache;
        }

        private void drawGenericParameter(in Rect position, in Type paramType, ref object parameterObject, object defVal)
        {
            object po = parameterObject;
            // reset the parameter if the types don't match
            if (po != null && po.GetType() != paramType) po = defVal;

            if (paramType == typeof(bool)) po = EditorGUI.Toggle(position, po != null && (bool)po);
            else if (paramType == typeof(int)) po = EditorGUI.IntField(position, po == null ? 0 : (int)po);
            else if (paramType == typeof(long)) po = EditorGUI.LongField(position, po == null ? 0 : (long)po);
            else if (paramType == typeof(float)) po = EditorGUI.FloatField(position, po == null ? 0 : (float)po);
            else if (paramType == typeof(double)) po = EditorGUI.DoubleField(position, po == null ? 0 : (double)po);
            else if (paramType == typeof(string)) po = EditorGUI.TextField(position, (string)po);
            else if (paramType == typeof(Color)) po = EditorGUI.ColorField(position, po == null ? new Color() : (Color)po);
            else if (typeof(UnityEngine.Object).IsAssignableFrom(paramType)) po = EditorGUI.ObjectField(position, (UnityEngine.Object)po, typeof(UnityEngine.Object), true);
            else if (paramType == typeof(UnityEngine.LayerMask)) po = EditorGUI.LayerField(position, po == null ? 0 : (int)po, EditorStyles.layerMaskField);
            else if (typeof(Enum).IsAssignableFrom(paramType)) po = EditorGUI.EnumPopup(position, (Enum)po);
            else if (paramType == typeof(Vector2)) po = EditorGUI.Vector2Field(position, GUIContent.none, po == null ? Vector2.zero : (Vector2)po);
            else if (paramType == typeof(Vector3)) po = EditorGUI.Vector3Field(position, GUIContent.none, po == null ? Vector3.zero : (Vector3)po);
            else if (paramType == typeof(Vector4)) po = EditorGUI.Vector4Field(position, GUIContent.none, po == null ? Vector4.zero : (Vector4)po);
            else if (paramType == typeof(Rect)) po = EditorGUI.RectField(position, po == null ? new Rect() : (Rect)po);
            else if (paramType == typeof(char)) po = EditorGUI.TextField(position, new string(new[] { po == null ? '\0' : (char)po }))[0];
            else if (paramType == typeof(AnimationCurve)) po = EditorGUI.CurveField(position, (AnimationCurve)po);
            else if (paramType == typeof(Bounds)) po = EditorGUI.BoundsField(position, po == null ? new Bounds() : (Bounds)po);
            else if (paramType == typeof(Gradient)) po = EditorGUI.GradientField(position, (Gradient)po);
            else if (paramType == typeof(Vector2Int)) po = EditorGUI.Vector2IntField(position, GUIContent.none, po == null ? Vector2Int.zero : (Vector2Int)po);
            else if (paramType == typeof(Vector3Int)) po = EditorGUI.Vector3IntField(position, GUIContent.none, po == null ? Vector3Int.zero : (Vector3Int)po);
            else if (paramType == typeof(RectInt)) po = EditorGUI.RectIntField(position, po == null ? new RectInt() : (RectInt)po);
            else if (paramType == typeof(BoundsInt)) po = EditorGUI.BoundsIntField(position, po == null ? new BoundsInt() : (BoundsInt)po);
            else if (paramType == typeof(VRCUrl)) po = new VRCUrl(EditorGUI.TextField(position, po == null ? "" : ((VRCUrl)po).Get()));

            else EditorGUI.LabelField(position, I18n.Tr("Cannot assign to type: ") + paramType.Name);

            parameterObject = po;
        }

        private readonly Type[] genericTypesAllowed =
        {
            typeof(bool),
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(Color),
            typeof(UnityEngine.Object),
            typeof(UnityEngine.LayerMask),
            typeof(Enum),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Rect),
            typeof(char),
            typeof(AnimationCurve),
            typeof(Bounds),
            typeof(Gradient),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(RectInt),
            typeof(BoundsInt),
            typeof(VRCUrl),
        };


        protected override void InitData()
        {
            objects = NormalizeArray(_actionsScript.objects, 0);
            var len = objects.Length;
            actions = NormalizeArray(_actionsScript.actions, len);
            parameterObjects = NormalizeArray(_actionsScript.parameterObjects, len);
            dynamicIndexes = NormalizeArray(_actionsScript.dynamicIndexes, len);
            selectedParameters = NormalizeArray(selectedParameters, len);

            actionObjects.Clear();
            actionObjects.AddRange(objects);
        }

        protected override void RenderChangeCheck()
        {
            // only when the list has selection focus should the keybinds react.
            if (actionList.index != -1)
            {
                var evt = Event.current;
                switch (evt.type)
                {
                    // handle list manipulation via kayboard
                    case EventType.KeyUp:
                        switch (evt.keyCode)
                        {
                            case KeyCode.Plus:
                            case KeyCode.KeypadPlus:
                                listAdd(actionList);
                                break;
                            case KeyCode.Delete:
                            case KeyCode.Minus:
                            case KeyCode.KeypadMinus:
                                listRemove(actionList);
                                break;
                        }

                        break;
                }
            }

            actionList.DoLayoutList();

            VariablesDrawn(
                nameof(UdonActions.objects),
                nameof(UdonActions.actions),
                nameof(UdonActions.parameterObjects),
                nameof(UdonActions.dynamicIndexes));
        }

        protected override void SaveData()
        {
            _actionsScript.objects = objects;
            _actionsScript.actions = actions;
            _actionsScript.parameterObjects = parameterObjects;
            _actionsScript.dynamicIndexes = dynamicIndexes;
        }
    }
}