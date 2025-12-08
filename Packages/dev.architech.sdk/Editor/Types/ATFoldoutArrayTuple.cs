using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    /// <summary>
    /// Basic implementation of the MultiPropertyList, using a simple foldout to contain the list.
    /// </summary>
    [Obsolete("Use ATReorderableList instead")]
    public class ATFoldoutArrayTuple : ATMultiPropertyList<ATFoldoutArrayTuple>
    {
        private readonly GUIContent _header;

        public Rect LastFoldoutRect { get; private set; }

        public ATFoldoutArrayTuple(GUIContent header = null)
        {
            _header = header;
        }

        public ATFoldoutArrayTuple(params SerializedProperty[] props)
        {
            foreach (var prop in props) base.AddArrayProperty(prop);
        }

        public override bool DrawLayout(bool showHint = false)
        {
            if (Properties.Length == 0) return false;
            var changed = false;
            var prefabOverride = Properties.Any(p => p.prefabOverride);
            SerializedProperty mainProp = Properties[0];
            Rect dropZone = LastFoldoutRect;
            using (new EditorGUILayout.VerticalScope(GUIStyle.none))
            {
                var header = _header != null ? new GUIContent(_header) : ATEditorGUIUtility.GetPropertyLabel(mainProp, showHint);
                if (!mainProp.isExpanded)
                {
                    // goofy alignment fix
                    ATEditorGUILayout.Spacer(0.5f);
                    header.text += $" ({mainProp.arraySize})";
                }

                EditorGUILayout.BeginHorizontal();
                // ReSharper disable once UseObjectOrCollectionInitializer
                var style = new GUIStyle(EditorStyles.foldout)
                {
                    font = prefabOverride ? EditorStyles.boldFont : EditorStyles.standardFont
                };
                style.margin.top = 2;
                dropZone = GUILayoutUtility.GetRect(header, style, GUILayout.ExpandWidth(true));
                LastFoldoutRect = dropZone;
                using (new ATEditorGUIUtility.ShrinkWrapLabelScope(header))
                    mainProp.isExpanded = EditorGUI.Foldout(LastFoldoutRect, mainProp.isExpanded, header, true, style);

                if (mainProp.isExpanded)
                {
                    int oldSize = mainProp.arraySize;
                    int newSize = EditorGUILayout.DelayedIntField(oldSize, GUILayout.Width(50));
                    if (oldSize < newSize) Resize(newSize);

                    ATEditorGUILayout.Spacer(5f);
                    if (GUILayout.Button("+", GUILayout.Width(20))) AppendNewEntry();
                    var newLast = GUILayoutUtility.GetLastRect();
                    dropZone.width = newLast.x - dropZone.x + newLast.width;
                    dropZone.height = Mathf.Max(newLast.height, dropZone.height);
                    EditorGUILayout.EndHorizontal();
                    ATEditorGUILayout.Spacer(5f);
                    changed = DrawPropertyListLayout();
                    ATEditorGUILayout.Spacer(5f);
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                    dropZone = Rect.zero;
                }
            } // end vertical

            if (mainProp.isExpanded && Event.current.type == EventType.ContextClick && dropZone.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                if (mainProp.arraySize == 0) menu.AddDisabledItem(I18n.TrContent("Clear"), false);
                else menu.AddItem(I18n.TrContent("Clear"), false, Clear);
                if (PrefabUtility.IsPartOfAnyPrefab(mainProp.serializedObject.targetObject))
                {
                    if (prefabOverride) menu.AddItem(I18n.TrContent("Revert"), false, Revert);
                    else menu.AddDisabledItem(I18n.TrContent("Revert"), false);
                }

                if (onContextMenuBuild != null)
                {
                    menu.AddSeparator("");
                    onContextMenuBuild.Invoke(menu);
                }

                menu.ShowAsContext();
            }

            if (dropZone != Rect.zero && IsValidDrag)
            {
                EditorGUI.DrawRect(dropZone, new Color(0, 0, 0, 0.5f));
                EditorGUI.LabelField(dropZone, I18n.Tr("Drop Zone"), new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });
            }

            changed ^= HandleDragDrop(dropZone);
            return changed;
        }
    }
}