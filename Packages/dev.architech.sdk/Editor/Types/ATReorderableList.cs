using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    /// <summary>
    /// An implementation of Unity's ReorderableList type but wrapped in a MultiPropertyList format.
    /// </summary>
    public class ATReorderableList : ATMultiPropertyList<ATReorderableList>
    {
        public delegate void DrawElementDelegate(Rect rect, ATReorderableList list, int index);

        public DrawElementDelegate drawElement;
        public ReorderableList.ElementHeightCallbackDelegate getElementHeight;
        private readonly ReorderableList listRef;
        private readonly ATBaseEditor editor = null;
        protected readonly List<UnityEngine.Object> objs = new List<UnityEngine.Object>();

        public readonly GUIContent Header;
        private bool changeDetected;
        public Rect HeaderRect { get; private set; }
        
        public int Index
        {
            get => listRef.index;
            set => listRef.index = value;
        }

        public ATReorderableList() : this(null, GUIContent.none) { }

        public ATReorderableList(ATBaseEditor editor, string headerText) : this(headerText)
        {
            this.editor = editor;
        }

        public ATReorderableList(string headerText) : this(headerText == null ? GUIContent.none : new GUIContent(headerText)) { }

        public ATReorderableList(ATBaseEditor editor, GUIContent header) : this(header ?? GUIContent.none)
        {
            this.editor = editor;
        }

        public ATReorderableList(GUIContent header)
        {
            Header = header;
            listRef = new ReorderableList(objs, typeof(UnityEngine.Object), true, true, true, true)
            {
                drawHeaderCallback = renderListHeader,
                drawElementCallback = renderListElement,
                elementHeightCallback = listElementHeight,
                onAddCallback = listAdd,
                onRemoveCallback = listRemove,
                onReorderCallbackWithDetails = listReordered
            };
            Save();
        }

        public override ATReorderableList AddArrayProperty(SerializedProperty property, GUIContent label)
        {
            var r = base.AddArrayProperty(property, label);
            resize();
            return r;
        }

        public override ATReorderableList AddArrayProperty(SerializedProperty property, GUIContent label, object initial)
        {
            var r = base.AddArrayProperty(property, label, initial);
            resize();
            return r;
        }

        public override ATReorderableList AddArrayProperty(SerializedProperty property, object initial)
        {
            var r = base.AddArrayProperty(property, initial);
            resize();
            return r;
        }

        public override ATReorderableList AddArrayProperty(SerializedProperty property)
        {
            var r = base.AddArrayProperty(property);
            resize();
            return r;
        }

        private void resize()
        {
            objs.Clear();
            if (Properties.Length == 0) return;
            objs.AddRange(new UnityEngine.Object[Size]);
        }

        private float listElementHeight(int listIndex)
        {
            if (MainProperty.isExpanded) return 0;
            getElementHeight = getElementHeight ?? (_ => EditorGUIUtility.singleLineHeight);
            return getElementHeight.Invoke(listIndex);
        }

        private void listAdd(ReorderableList list)
        {
            AppendNewEntry();
            changeDetected = true;
            resize();
#if UNITY_2022_3_OR_NEWER
            list.Select(list.count - 1);
#endif
        }

        private void listRemove(ReorderableList list)
        {
            int nextIndex = list.index;
            if (nextIndex > -1)
            {
                changeDetected = true;
                RemoveEntry(nextIndex);
            }

            var mainProp = Properties[0];
            if (nextIndex >= mainProp.arraySize) nextIndex = mainProp.arraySize - 1;
            resize();
#if UNITY_2022_3_OR_NEWER
            list.Select(nextIndex);
#endif
        }

        private void listReordered(ReorderableList list, int from, int to)
        {
            if (from != to)
            {
                changeDetected = true;
                MoveEntry(from, to);
            }
        }

        private void renderListHeader(Rect rect)
        {
            if (Properties.Length == 0) return;
            GUIContent text = rect.Contains(Event.current.mousePosition) && IsValidDrag ? I18n.TrContent("Drop objects here to add to list") : new GUIContent(Header);
            if (HandleDragDrop(rect))
            {
                changeDetected = true;
                SetFoldout(true);
                resize();
            }

            EditorGUI.LabelField(rect, text, ATEditorGUIUtility.slimBox);
            var oldChanged = GUI.changed;
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            GUI.changed = false;
            var foldoutIsOpen = EditorGUI.Foldout(rect, !MainProperty.isExpanded, new GUIContent(""), true);
            if (GUI.changed) SetFoldout(foldoutIsOpen);
            GUI.changed ^= oldChanged;
            EditorGUI.indentLevel = oldIndent;
            HeaderRect = rect;
        }

        private void renderListElement(Rect rect, int listIndex, bool isActive, bool isFocused)
        {
            if (rect.height == 0) return; // list item is effectively hidden, don't render
            drawElement = drawElement ?? defaultDrawElement;
            var oldChanged = GUI.changed;
            GUI.changed = false;
            drawElement.Invoke(rect, this, listIndex);
            changeDetected ^= GUI.changed;
            GUI.changed ^= oldChanged;
            Save();
        }

        private static void defaultDrawElement(Rect rect, ATReorderableList list, int index)
        {
            var properties = list.Properties;
            var labels = list.Labels;
            var drawRect = new Rect(rect);
            const float leftPad = 5f;
            var drawWidth = drawRect.width / properties.Length;
            drawRect.height = EditorGUIUtility.singleLineHeight;

            var mainPropHasValue = HasObjectValue(properties[0].GetArrayElementAtIndex(index));

            float[] propWidths = new float[properties.Length];
            bool[] propFlex = new bool[properties.Length];
            System.Array.Fill(propWidths, drawWidth);
            System.Array.Fill(propFlex, true);
            float excessWidth = 0;

            for (var i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                if (list.Types[i] == typeof(bool))
                {
                    var label = labels[i] ?? ATEditorGUIUtility.GetPropertyLabel(prop, false);
                    using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label, prop))
                    {
                        var oldWidth = propWidths[i];
                        var newWidth = EditorGUIUtility.labelWidth + 22;
                        propWidths[i] = newWidth;
                        propFlex[i] = false;
                        excessWidth += (oldWidth - newWidth);
                    }
                }
            }

            var flexPropCount = propFlex.Count(b => b);
            excessWidth = flexPropCount > 0 ? (excessWidth - leftPad) / flexPropCount : 0;

            for (int i = 0; i < propWidths.Length; i++)
                if (propFlex[i])
                    propWidths[i] += excessWidth;

            for (var i = 0; i < properties.Length; i++)
            {
                if (i == 0 || mainPropHasValue)
                {
                    var prop = properties[i];
                    var label = labels[i] ?? ATEditorGUIUtility.GetPropertyLabel(prop, false);
                    var currentElement = prop.GetArrayElementAtIndex(index);
                    using (new ATEditorGUIUtility.ShrinkWrapLabelScope(label, prop))
                    {
                        drawRect.width = propWidths[i];
                        EditorGUI.PropertyField(drawRect, currentElement, label, false);
                    }

                    if (i == 0)
                    {
                        drawRect.width -= leftPad;
                        drawRect.x += leftPad;
                    }

                    drawRect.x += drawRect.width + leftPad;
                }
            }
        }

        public override bool DrawLayout(bool showHints = false)
        {
            if (listRef == null) return false; // init phase failed or threw, skip draw phase
            if (objs.Count != MainProperty.arraySize) resize();
            changeDetected = false;
            // only when the list has selection focus should the keybinds react.
            if (listRef.index > -1 && listRef.HasKeyboardControl())
            {
                var evt = Event.current;
                switch (evt.type)
                {
                    // handle list manipulation via kayboard
                    case EventType.KeyDown:
                        switch (evt.keyCode)
                        {
                            case KeyCode.KeypadPlus:
                                listAdd(listRef);
                                break;
#if !UNITY_2022_3_OR_NEWER
                            case KeyCode.Delete:
#endif
                            case KeyCode.KeypadMinus:
                                listRemove(listRef);
                                break;
#if UNITY_2022_3_OR_NEWER
                            case KeyCode.Delete:
                                // prevent internal delete from running and do our own
                                evt.Use();
                                listRemove(listRef);
                                break;
#endif
                        }

                        break;
                }
            }

            listRef.DoLayoutList();
            if (editor != null) editor.VariablesDrawn(Properties.Select(p => p.propertyPath).ToArray());

            return changeDetected;
        }

        public static bool HasObjectValue(SerializedProperty prop, int index = 0)
        {
            if (prop.isArray)
            {
                if (prop.arraySize == 0) return true;
                prop = prop.GetArrayElementAtIndex(index);
            }
            return prop.propertyType != SerializedPropertyType.ObjectReference || prop.objectReferenceValue != null;
        }

        public void SetFoldout(bool open)
        {
            MainProperty.isExpanded = !open;
            typeof(ReorderableList).GetMethod("InvalidateForGUI", (BindingFlags)~0)?.Invoke(listRef, new object[0]);
        }
    }
}