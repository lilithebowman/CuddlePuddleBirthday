using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    public struct ATPropertyListData
    {
        public readonly SerializedProperty Property;

        /// <summary>
        /// 0-based value representing the index of the property based on the order added to the list.
        /// </summary>
        public readonly int PropertyIndex;

        public readonly object ElementValue;
        public readonly int ElementIndex;
        public readonly System.Type ElementType;

        public ATPropertyListData(int propertyIndex, SerializedProperty property, int elementIndex, object elementValue, System.Type elementType)
        {
            PropertyIndex = propertyIndex;
            Property = property;
            ElementIndex = elementIndex;
            ElementValue = elementValue;
            ElementType = elementType;
        }
    }


    /// <summary>
    /// This class handles wrapping up multiple array properties into a synchronized list.
    /// It intends to always keep the properties the same length, treating each entry as a tuple of data.
    /// Abstract, must be inherited by an implementation type.
    /// </summary>
    /// <typeparam name="T">The type that is inheriting from this class</typeparam>
    public abstract class ATMultiPropertyList<T> where T : ATMultiPropertyList<T>
    {
        public delegate bool OnDropDelegate(T self, UnityEngine.Object dropped, ATPropertyListData dropListData);

        public delegate bool OnDropValidateDelegate(T self, UnityEngine.Object dropped, ATPropertyListData dropListData);

        public delegate void OnChangeDelegate(T self, ATPropertyListData propListData);

        public delegate void ContextMenuDelegate(GenericMenu menu);

        private SerializedProperty[] _properties = new SerializedProperty[0];
        private System.Type[] _propertyElementTypes = System.Type.EmptyTypes;
        private GUIContent[] _labels = new GUIContent[0];
        private object[] _defaults = new object[0];
        private bool[] _hidden = new bool[0];
        public OnDropDelegate onDropObject;
        public OnDropValidateDelegate onDropValidate;
        public OnChangeDelegate onPropertyChange;
        public ContextMenuDelegate onContextMenuBuild;

        public SerializedProperty[] Properties => _properties;
        public SerializedProperty MainProperty => _properties.Length == 0 ? null : _properties[0];
        public System.Type[] Types => _propertyElementTypes;
        public GUIContent[] Labels => _labels;
        public int Size => _properties.Length == 0 ? 0 : _properties[0].arraySize;

        protected bool IsValidDrag { get; private set; } = false;

        /// <summary>
        /// A fuild build method intened to be used alongside the construction of a new instance of this class.
        /// The property provided must be an array type and will be added to the internal list of properties.
        /// The label is optional. If no provided, it will default to the property's internal label.
        /// If you do not want a label to be shown with the property, pass in <code>GUIContent.none</code> as the label parameter.
        /// </summary>
        /// <example>var list = new ATCustomMultiPropertyList().AddArrayProperty(prop1, GUIContent.none).AddArrayProperty(prop2);</example>
        /// <param name="property">an array type property</param>
        /// <param name="label">optional label to be displayed next to the property's field</param>
        /// <param name="initial"></param>
        /// <returns>the reference to this class instance</returns>
        /// <exception cref="ArgumentException">if the property provided is not an array type</exception>
        public virtual T AddArrayProperty(SerializedProperty property, GUIContent label, object initial)
        {
            if (property == null) throw new ArgumentException($"Property provided is null. Property index: {_properties.Length} - Given label: '{label}'");
            if (!property.isArray) throw new ArgumentException($"Property provided ({property.propertyPath}) is not an array type.");
            if (!_properties.Contains(property))
            {
                var propFieldInfo = property.serializedObject.targetObject.GetType().GetField(property.propertyPath, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var elementType = propFieldInfo?.FieldType.GetElementType();
                _propertyElementTypes = _propertyElementTypes.AddItem(elementType).ToArray();
                _properties = _properties.AddItem(property).ToArray();
                _labels = _labels.AddItem(label).ToArray();
                _defaults = _defaults.AddItem(initial).ToArray();
                _hidden = _hidden.AddItem(false).ToArray();
            }

            return (T)this;
        }

        /// <summary>
        /// A fuild build method intened to be used alongside the construction of a new instance of this class.
        /// The property provided must be an array type and will be added to the internal list of properties.
        /// The label is optional. If no provided, it will default to the property's internal label.
        /// If you do not want a label to be shown with the property, pass in <code>GUIContent.none</code> as the label parameter.
        /// </summary>
        /// <example>var list = new ATCustomMultiPropertyList().AddArrayProperty(prop1, GUIContent.none).AddArrayProperty(prop2);</example>
        /// <param name="property">an array type property</param>
        /// <param name="label">optional label to be displayed next to the property's field</param>
        /// <returns>the reference to this class instance</returns>
        /// <exception cref="ArgumentException">if the property provided is not an array type</exception>
        public virtual T AddArrayProperty(SerializedProperty property, GUIContent label)
        {
            return AddArrayProperty(property, label, null);
        }

        /// <summary>
        /// A fuild build method intened to be used alongside the construction of a new instance of this class.
        /// The property provided must be an array type and will be added to the internal list of properties.
        /// The label is optional. If no provided, it will default to the property's internal label.
        /// If you do not want a label to be shown with the property, pass in <code>GUIContent.none</code> as the label parameter.
        /// </summary>
        /// <example>var list = new ATCustomMultiPropertyList().AddArrayProperty(prop1, GUIContent.none).AddArrayProperty(prop2);</example>
        /// <param name="property">an array type property</param>
        /// <param name="initial">default value for the respective property</param>
        /// <returns>the reference to this class instance</returns>
        /// <exception cref="ArgumentException">if the property provided is not an array type</exception>
        public virtual T AddArrayProperty(SerializedProperty property, object initial)
        {
            return AddArrayProperty(property, null, initial);
        }

        /// <summary>
        /// A fuild build method intened to be used alongside the construction of a new instance of this class.
        /// The property provided must be an array type and will be added to the internal list of properties.
        /// The label is optional. If no provided, it will default to the property's internal label.
        /// If you do not want a label to be shown with the property, pass in <code>GUIContent.none</code> as the label parameter.
        /// </summary>
        /// <example>var list = new ATCustomMultiPropertyList().AddArrayProperty(prop1, GUIContent.none).AddArrayProperty(prop2);</example>
        /// <param name="property">an array type property</param>
        /// <returns>the reference to this class instance</returns>
        /// <exception cref="ArgumentException">if the property provided is not an array type</exception>
        public virtual T AddArrayProperty(SerializedProperty property)
        {
            return AddArrayProperty(property, null, null);
        }

        public void HideProperties(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                if (index >= _hidden.Length) continue;
                _hidden[index] = true;
            }
        }

        public void UnhideProperties(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                if (index >= _hidden.Length) continue;
                _hidden[index] = false;
            }
        }

        /// <summary>
        /// Creates a new entry at the end of the list and sets the new list items to their default values.
        /// </summary>
        public void AppendNewEntry()
        {
            if (MainProperty != null) Resize(MainProperty.arraySize + 1);
        }

        public void AppendNewEntry(object value)
        {
            if (MainProperty != null)
            {
                var index = MainProperty.arraySize;
                Resize(index + 1);
                MainProperty.GetArrayElementAtIndex(index).SetValue(value);
            }
        }

        public void ResetEntry(int index)
        {
            for (var i = 0; i < _properties.Length; i++)
            {
                var prop = _properties[i];
                if (_defaults[i] == null) prop.GetArrayElementAtIndex(index).ResetToDefaultValue();
                else prop.GetArrayElementAtIndex(index).SetValue(_defaults[i]);
            }
        }

        public void Resize() => Resize(MainProperty.arraySize);

        public void Resize(int newSize, bool relative = false)
        {
            var mainProp = MainProperty;
            if (mainProp == null) return;
            if (relative) newSize = mainProp.arraySize + newSize;
            else if (newSize < 0) newSize = mainProp.arraySize;

            for (var i = 0; i < _properties.Length; i++)
            {
                var prop = _properties[i];
                var index = prop.arraySize;
                if (index == newSize) continue;
                prop.arraySize = newSize;
                for (; index < newSize; index++)
                {
                    if (_defaults[i] == null) prop.GetArrayElementAtIndex(index).ResetToDefaultValue();
                    else prop.GetArrayElementAtIndex(index).SetValue(_defaults[i]);
                }
            }
        }

        /// <summary>
        /// Moves an entry around the list.
        /// </summary>
        /// <param name="from">sounce index</param>
        /// <param name="to">destination index</param>
        public void MoveEntry(int from, int to)
        {
            foreach (var prop in _properties)
                prop.MoveArrayElement(from, to);
        }

        /// <summary>
        /// Completely deletes an entry from the list.
        /// Internally it runs the property delete command twice.
        /// First to clear the data, second to actually delete the index, reducing the list size by one.
        /// </summary>
        /// <param name="index">target entry to remove</param>
        public void RemoveEntry(int index)
        {
            foreach (var prop in _properties)
            {
                var size = prop.arraySize;
                if (prop.arraySize <= index) continue;
                prop.GetArrayElementAtIndex(index).ResetToDefaultValue();
                prop.DeleteArrayElementAtIndex(index);
                prop.arraySize = size - 1;
            }
        }

        public void RemoveEntryByValue(object value, int propertyIndex = 0)
        {
            if (value == null) return;
            var prop = _properties[propertyIndex];
            if (_propertyElementTypes[propertyIndex] != value.GetType())
            {
                UnityEngine.Debug.LogError($"Invalid object type passed in: {value.GetType().FullName} | Expected: {_propertyElementTypes[propertyIndex].FullName}");
                return;
            }

            var size = prop.arraySize;
            for (int i = 0; i < size; i++)
            {
                if (prop.GetArrayElementAtIndex(i).GetValue() == value)
                {
                    RemoveEntry(i);
                    break;
                }
            }
        }

        public void Revert()
        {
            foreach (var prop in _properties)
                PrefabUtility.RevertPropertyOverride(prop, InteractionMode.UserAction);
        }

        public void Clear()
        {
            foreach (var prop in _properties)
                prop.arraySize = 0;
            Save();
        }

        /// <summary>
        /// Runs through each property and applies any modifications if there are any.
        /// </summary>
        protected void Save()
        {
            foreach (var prop in Properties)
            {
                var obj = prop.serializedObject;
                if (obj.hasModifiedProperties) obj.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Core method that subclasses must override for handling how the property list should be drawn.
        /// </summary>
        /// <param name="showHint">optional bool for handling the special hint flag available on ATBehaviours</param>
        /// <returns>Flag of whether or not ANY property change has occurred.</returns>
        public abstract bool DrawLayout(bool showHint = false);

        /// <summary>
        /// The standard draw function for rendering out the tuple of array properties.
        /// This does not render any header, only the properties and an entry removal button.
        /// </summary>
        /// <param name="withButtons">Whether or not to include the delete entry button at the end of the property row</param>
        /// <returns>Flag of whether or not ANY property change has occurred.</returns>
        protected bool DrawPropertyListLayout(bool withButtons = true)
        {
            int removeIndex = -1;
            bool anyChange = false;
            bool soloProp = _properties.Length == 1;
            if (!soloProp) Resize();
            for (int elementIndex = 0; elementIndex < _properties[0].arraySize; elementIndex++)
            {
                using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    for (var propertyIndex = 0; propertyIndex < _properties.Length; propertyIndex++)
                    {
                        if (_hidden[propertyIndex]) continue; // skip showing hidden properties
                        var property = _properties[propertyIndex];
                        var label = _labels[propertyIndex];
                        if (label == null)
                        {
                            if (!soloProp || !property.isArray && property.propertyType == SerializedPropertyType.Generic && property.hasChildren)
                                label = ATEditorGUIUtility.GetPropertyLabel(property, false);
                            else label = GUIContent.none;
                        }

                        using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label, property))
                        {
                            var oldChanged = GUI.changed;
                            GUI.changed = false;
                            var arrProp = property.GetArrayElementAtIndex(elementIndex);
                            GUILayoutOption[] opts = new GUILayoutOption[0];
                            if (arrProp.propertyType == SerializedPropertyType.Boolean)
                                opts = new[] { GUILayout.Width(EditorGUIUtility.labelWidth + 22) };
                            EditorGUILayout.PropertyField(arrProp, label, opts);
                            var newChanged = GUI.changed;
                            GUI.changed = oldChanged || newChanged;
                            if (newChanged)
                            {
                                anyChange = true;
                                var val = property.GetArrayElementAtIndex(elementIndex).GetValue();
                                var type = _propertyElementTypes[propertyIndex];
                                onPropertyChange?.Invoke((T)this, new ATPropertyListData(propertyIndex, property, elementIndex, val, type));
                            }
                        }
                    }

                    ATEditorGUILayout.Spacer(5f);
                    if (withButtons && GUILayout.Button("-", GUILayout.Width(20)))
                        removeIndex = elementIndex;
                }
            }

            if (removeIndex > -1)
            {
                anyChange = true;
                RemoveEntry(removeIndex);
            }

            return anyChange;
        }

        /// <summary>
        /// This method handles the ability to drag and drop objects onto the list based on the provided rect area.
        /// </summary>
        /// <param name="zone">the Rect where the dragdrop operation is allowed to be performed.</param>
        /// <returns>whether or not the drop operation completed successfully for any of the dropped objects</returns>
        protected bool HandleDragDrop(Rect zone)
        {
            if (zone == Rect.zero) return false;
            Event evt = Event.current;

            switch (evt.type)
            {
                // Cursed unity issue: When hovered over label field for like half a second, DragUpdated event is converted into Used and can no longer trigger DragPerform.
                // Why? No fucking clue. So just disable the valid drag check to prevent unexpected issues.
                case EventType.Used:
                    IsValidDrag = false;
                    break;
                case EventType.DragUpdated:
                    if (!IsValidDrag)
                    {
                        bool validateAny = false;
                        if (onDropValidate == null) onDropValidate = DefaultObjectDropValidate;
                        foreach (UnityEngine.Object objRef in DragAndDrop.objectReferences)
                        {
                            for (var i = 0; i < _properties.Length; i++)
                            {
                                var propData = new ATPropertyListData(i, _properties[i], -1, objRef, _propertyElementTypes[i]);
                                if (onDropValidate.Invoke((T)this, objRef, propData))
                                {
                                    validateAny = true;
                                    break;
                                }
                            }
                        }

                        IsValidDrag = validateAny;
                    }

                    if (!zone.Contains(evt.mousePosition) || !IsValidDrag) break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    break;
                case EventType.DragExited:
                    IsValidDrag = false;
                    break;
                case EventType.DragPerform:
                    IsValidDrag = false;
                    if (!zone.Contains(evt.mousePosition)) break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    bool addedAny = false;
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object objRef in DragAndDrop.objectReferences)
                    {
                        for (var i = 0; i < _properties.Length; i++)
                        {
                            var prop = _properties[i];
                            var elementType = _propertyElementTypes[i];
                            if (onDropObject == null) onDropObject = DefaultObjectDropHandle;
                            var dropData = new ATPropertyListData(i, prop, -1, objRef, elementType);
                            if (onDropObject.Invoke((T)this, objRef, dropData))
                            {
                                addedAny = true;
                                break;
                            }
                        }
                    }

                    return addedAny;

                case EventType.ContextClick:
                    var menu = new GenericMenu();
                    if (MainProperty.arraySize == 0) menu.AddDisabledItem(I18n.TrContent("Clear"), false);
                    else menu.AddItem(I18n.TrContent("Clear"), false, Clear);
                    if (PrefabUtility.IsPartOfAnyPrefab(MainProperty.serializedObject.targetObject))
                    {
                        var prefabOverride = Properties.Any(p => p.prefabOverride);
                        if (prefabOverride) menu.AddItem(I18n.TrContent("Revert"), false, Revert);
                        else menu.AddDisabledItem(I18n.TrContent("Revert"), false);
                    }

                    if (onContextMenuBuild != null)
                    {
                        menu.AddSeparator("");
                        onContextMenuBuild.Invoke(menu);
                    }

                    menu.ShowAsContext();
                    break;
            }

            return false;
        }

        /// <summary>
        /// This method attempts to find a property with the valid array type during a dragdrop event and creates a new list entry for it.
        /// Also takes components into account for when GameObjects are part of the dropped object list.
        /// </summary>
        /// <param name="list">a reference to the current instance of this list object</param>
        /// <param name="dropped"></param>
        /// <param name="dropListData">struct containing the data related to the drop operation</param>
        /// <returns>true if the object was successfully added to the list, false otherwise</returns>
        public virtual bool DefaultObjectDropHandle(T list, UnityEngine.Object dropped, ATPropertyListData dropListData)
        {
            // if the expected type is a Component and the dropped object is a GameObject, try to extract the component by type.
            var eType = dropListData.ElementType;
            if (typeof(Component).IsAssignableFrom(eType) && dropped is GameObject o) dropped = o.GetComponent(eType);
            if (dropped != null && eType.IsInstanceOfType(dropped))
            {
                list.AppendNewEntry();
                dropListData.Property.GetArrayElementAtIndex(dropListData.Property.arraySize - 1).objectReferenceValue = dropped;
                dropListData.Property.serializedObject.ApplyModifiedProperties();
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method attempts to find a property with the valid array type during a dragdrop event and creates a new list entry for it.
        /// Also takes components into account for when GameObjects are part of the dropped object list.
        /// </summary>
        /// <param name="list">a reference to the current instance of this list object</param>
        /// <param name="dropped"></param>
        /// <param name="dropListData">struct containing the data related to the drop operation</param>
        /// <returns>true if the object was successfully added to the list, false otherwise</returns>
        public bool DefaultObjectDropValidate(T list, UnityEngine.Object dropped, ATPropertyListData dropListData)
        {
            // if the expected type is a Component and the dropped object is a GameObject, try to extract the component by type.
            var eType = dropListData.ElementType;
            if (typeof(Component).IsAssignableFrom(eType) && dropped is GameObject o) dropped = o.GetComponent(eType);
            return dropped != null && eType.IsInstanceOfType(dropped);
        }
    }
}