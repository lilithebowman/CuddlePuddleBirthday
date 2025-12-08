using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ReSharper disable StaticMemberInGenericType
#pragma warning disable CS0612

namespace ArchiTech.SDK.Editor
{
    public abstract class ATBaseEditor : UnityEditor.Editor
    {
        private static readonly FieldInfo stackInfo = typeof(EditorGUI).GetField("s_ChangedStack", BindingFlags.Static | BindingFlags.NonPublic);
        private MonoBehaviour _basescript;
        private readonly Dictionary<string, ATReorderableList> arrayProps = new Dictionary<string, ATReorderableList>();
        private readonly Dictionary<string, (UnityEngine.Object[], string[])> variableDropdownDetectionCache = new Dictionary<string, (UnityEngine.Object[], string[])>();
        private readonly HashSet<string> variablesDrawn = new HashSet<string>();
        private readonly List<CheckpointData> checkpoints = new List<CheckpointData>();
        private readonly System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        protected Rect lastFoldoutRect = Rect.zero;

        [I18nInspectorName("Show Hints")] protected bool showHints;

        private EditorGUI.ChangeCheckScope _check;
        private bool _forced = false;
        protected bool init;
        protected bool ForceVerticalHints { get; set; } = true;

        /// <summary>
        /// Property flag that determines if the editor should render the default UdonSharp header.
        /// The header will be drawn after the <c>LoadData()</c> call and before the <c>PreChangeCheck()</c> call
        /// </summary>
        protected virtual bool autoRenderHeader => true;

        /// <summary>
        /// Property flag that determines if the editor should render all serialized variables in a generic manner.
        /// The variables will be drawn just before of the <see cref="RenderChangeCheck()"/> call.
        /// If you want to render something above the default variables, use the <see cref="PreChangeCheck"/> method.
        /// </summary>
        protected virtual bool autoRenderVariables => true;

        /// <summary>
        /// Shorthand property for a new VerticalScope. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.VerticalScope VArea => new EditorGUILayout.VerticalScope(GUIStyle.none);

        /// <summary>
        /// Shorthand property for a new VerticalScope, but with a generic "box" style applied. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.VerticalScope VBox => new EditorGUILayout.VerticalScope("box");

        /// <summary>
        /// Shorthand property for a new HorizontalScope. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.HorizontalScope HArea => new EditorGUILayout.HorizontalScope(GUIStyle.none);

        /// <summary>
        /// Shorthand property for a new HorizontalScope, but with a generic "box" style applied. Best for using(){} scopes
        /// </summary>
        protected static EditorGUILayout.HorizontalScope HBox => new EditorGUILayout.HorizontalScope("box");

        protected static void DrawLine(params GUILayoutOption[] opts) =>
            EditorGUILayout.LabelField("", ATEditorGUILayout.IsAutoLayoutVertical() ? GUI.skin.horizontalSlider : GUI.skin.verticalSlider, opts);

        protected static EditorGUI.DisabledGroupScope DisabledScope(bool isDisabled = true)
            => new EditorGUI.DisabledGroupScope(isDisabled);

        protected class MinimumWidthScope : GUI.Scope
        {
            private readonly float oldLabelWidth;
            private readonly float oldFieldWidth;

            /// <summary>
            /// Create a disposable scope that manages the label and field minimum widths via EditorGUIUtility.
            /// </summary>
            /// <param name="labelWidth">minimum width of the property's label. Set to 0 to use unity's default value.</param>
            /// <param name="fieldWidth">minimum width of the property's field. Set to 0 to use Unity's default value.</param>
            public MinimumWidthScope(float labelWidth, float fieldWidth)
            {
                oldLabelWidth = EditorGUIUtility.labelWidth;
                oldFieldWidth = EditorGUIUtility.fieldWidth;
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUIUtility.fieldWidth = fieldWidth;
            }

            protected override void CloseScope()
            {
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUIUtility.fieldWidth = oldFieldWidth;
            }
        }

        protected class SaveObjectScope : ATEditorGUIUtility.SaveObjectScope
        {
            public SaveObjectScope(UnityEngine.Object target, string undoMessage = null) : base(target, undoMessage) { }
        }

        /// <summary>
        /// An alternative to EditorGUILayout.Space which will pick the rect dimensions
        /// based on the most recent layout group's direction (either vertical or horizontal)
        /// </summary>
        /// <param name="size">the size of the spacer that you want to render</param>
        /// <param name="expand">whether to allow the spacer to expand beyond the size value</param>
        protected static void Spacer(float size = 6f, bool expand = false) => ATEditorGUILayout.Spacer(size, expand);

        /// <summary>
        /// Shorthand property for a new ChangeCheckScope. Works in conjunction with the Save() method.
        /// </summary>
        protected EditorGUI.ChangeCheckScope ChangeCheckScope => _check ?? (_check = new EditorGUI.ChangeCheckScope());

        protected ATEditorGUIUtility.SectionScope SectionScope(string header) => new ATEditorGUIUtility.SectionScope(header);

        private int showCheckpoints = 0;
        private Stack<CheckpointData> checkpointGroups = new Stack<CheckpointData>();
        private CheckpointData lastCheckpoint = null;
        private CheckpointData firstCheckpoint = null;
        private long currentTimestamp;
        private long currentTicks;

        private class CheckpointData
        {
            public string id;
            public string label;
            public long time;
            public long ticks;
            public long deltaPast;
            public long ticksPast;
            public long deltaFuture;
            public long ticksFuture;
            public bool stale;
            public int nestLevel;
            public CheckpointData group;
            public CheckpointData previous;
            public CheckpointData next;

            public void Stale() => stale = true;

            public void Reset()
            {
                label = null;
                time = ticks = deltaPast = ticksPast = deltaFuture = ticksFuture = 0;
                stale = false;
                nestLevel = 0;
                group = previous = next = null;
            }
        }

        private void Checkpoint(string label, out CheckpointData checkpoint)
        {
            stopwatch.Stop();
            long time = stopwatch.ElapsedMilliseconds;
            long ticks = stopwatch.ElapsedTicks;
            bool isFirst = lastCheckpoint == null;
            checkpointGroups.TryPeek(out CheckpointData currentGroup);
            string id = currentGroup == null ? label : $"{currentGroup.id}-{label}";
            checkpoint = checkpoints.FirstOrDefault(c => c.id == id);
            if (checkpoint == null)
            {
                checkpoint = new CheckpointData();
                checkpoints.Add(checkpoint);
                checkpoint.id = id;
            }

            checkpoint.Reset();
            checkpoint.label = label;
            checkpoint.previous = lastCheckpoint;
            checkpoint.deltaPast = isFirst ? 0 : time - currentTimestamp;
            checkpoint.ticksPast = isFirst ? 0 : ticks - currentTicks;
            checkpoint.time = time;
            checkpoint.ticks = ticks;
            checkpoint.group = currentGroup;
            checkpoint.nestLevel = checkpointGroups.Count;

            if (!isFirst)
            {
                // update 'future' data for the previously encountered checkpoint
                lastCheckpoint.next = checkpoint;
                lastCheckpoint.deltaFuture = time - lastCheckpoint.time;
                lastCheckpoint.ticksFuture = ticks - lastCheckpoint.ticks;
            }

            currentTimestamp = time;
            currentTicks = ticks;
            if (firstCheckpoint == null) firstCheckpoint = checkpoint;
            lastCheckpoint = checkpoint;
            stopwatch.Start();
        }

        private void ResetCheckpoints()
        {
            firstCheckpoint = null;
            lastCheckpoint = null;
        }

        protected void Checkpoint(string label)
        {
            if (showCheckpoints == 0) return;
            Checkpoint(label, out _);
        }

        protected void BeginCheckpointGroup(string label)
        {
            if (showCheckpoints == 0) return;
            Checkpoint(label, out CheckpointData checkpoint);
            checkpointGroups.Push(checkpoint);
        }

        protected void EndCheckpointGroup()
        {
            if (showCheckpoints == 0) return;
            EndCheckpointGroup(false);
        }

        private bool EndCheckpointGroup(bool finished)
        {
            if (!finished) stopwatch.Stop();
            currentTimestamp = stopwatch.ElapsedMilliseconds;
            currentTicks = stopwatch.ElapsedTicks;

            if (lastCheckpoint != null && lastCheckpoint.deltaFuture == 0)
            {
                lastCheckpoint.deltaFuture = currentTimestamp - lastCheckpoint.time;
                lastCheckpoint.ticksFuture = currentTicks - lastCheckpoint.ticks;
            }

            bool stackNotEmpty = checkpointGroups.TryPop(out CheckpointData checkpoint);
            if (stackNotEmpty)
            {
                checkpoint.deltaFuture = currentTimestamp - checkpoint.time;
                checkpoint.ticksFuture = currentTicks - checkpoint.ticks;
            }

            if (!finished) stopwatch.Start();
            return stackNotEmpty;
        }

        public sealed override void OnInspectorGUI()
        {
            _basescript = (MonoBehaviour)target;
            variablesDrawn.Clear();
            ResetCheckpoints();
            serializedObject.UpdateIfRequiredOrScript();
            stopwatch.Restart();
            DrawInspector();
            stopwatch.Stop();
            EditorGUILayout.Space();
            while (showCheckpoints > 0 && EndCheckpointGroup(true)) { }
            var time = stopwatch.ElapsedMilliseconds;
            var ticks = stopwatch.ElapsedTicks;
            var label = I18n.Tr("Inspector frame-time") + $": {time}ms";
            if (showCheckpoints == 2) label += $" ({ticks} ticks)";
            EditorGUILayout.LabelField(label);
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                showCheckpoints++;
                showCheckpoints %= 3;
            }

            EditorGUI.indentLevel++;
            if (showCheckpoints != 0 && checkpoints.Count > 0)
            {
                var checkpoint = firstCheckpoint;
                using (VArea)
                    while (checkpoint != null)
                    {
                        if (checkpoint.next == null && checkpoint.ticksFuture == 0)
                            checkpoint.ticksFuture = stopwatch.ElapsedTicks - checkpoint.ticks;
                        if (checkpoint.next == null && checkpoint.deltaFuture == 0)
                            checkpoint.deltaFuture = stopwatch.ElapsedMilliseconds - checkpoint.time;
                        label = $"{checkpoint.label}: {checkpoint.deltaFuture}ms";
                        if (showCheckpoints == 2) label += $" ({checkpoint.ticksFuture} ticks)";
                        label += checkpoint.stale ? " [Stale]" : "";
                        EditorGUI.indentLevel += checkpoint.nestLevel;
                        EditorGUILayout.LabelField(label);
                        EditorGUI.indentLevel -= checkpoint.nestLevel;
                        checkpoint = checkpoint.next;
                    }
            }

            EditorGUI.indentLevel--;
            lastFoldoutRect = Rect.zero;
        }

        protected virtual void DrawInspector()
        {
            serializedObject.Update();
            LoadData();
            if (!init)
            {
                init = true;
                variableDropdownDetectionCache.Clear();
                InitData();
                HandleSave();
            }

            Header();
            showHints = EditorGUILayout.Toggle(GetPropertyLabel(this, nameof(showHints)), showHints);
            using (ChangeCheckScope)
            {
                RenderChangeCheck();
                if (autoRenderVariables) DrawVariables();
                HandleSave();
            }

            Footer();
        }

        /// <summary>
        /// Helper method for dealing with saving data onto the respective target script.
        /// Called for both <see cref="InitData()"/> and <see cref="RenderChangeCheck()"/> contexts.
        /// </summary>
        protected virtual void HandleSave()
        {
            bool doSave = _forced;
            if (!doSave && stackInfo != null)
            {
                // circumvent the stupid "did you call BeginChangeCheck first" error debug to prevent confusion
                var stack = (Stack<bool>)stackInfo.GetValue(null);
                doSave = stack != null && stack.Count > 0 && _check != null && _check.changed;
            }

            if (doSave)
            {
                if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
                using (new SaveObjectScope(_basescript, $"Modify {_basescript.GetType().Name} Content"))
                    SaveData();
            }

            if (_check != null)
            {
                _check.Dispose();
                _check = null;
            }

            _forced = false;
        }

        /// <summary>
        /// Helper method to flag the GUI as having been changed.
        /// Only affects the <c>RenderChangeCheck()</c> method.
        /// Is a NO-OP elsewhere.
        /// </summary>
        protected void ForceSave()
        {
            if (_check != null) GUI.changed = true;
            _forced = true;
        }


        /// <summary>
        /// Any data loading/prep that needs to take place each redraw should go here.
        /// Also a good spot to inject any branding headers for custom components,
        /// since it is called before anything is rendered to the inspector.
        /// Called once per inspector redraw.
        /// </summary>
        protected virtual void LoadData() { }

        /// <summary>
        /// Any setup, caching or prep needed for the editor goes here.
        /// This method will NOT respond to GUI changes like <see cref="RenderChangeCheck()"/> does, but you can trigger <see cref="SaveData()"/> by calling <see cref="ForceSave()"/>.<br/>
        /// If you wish to trigger this method again, set init = false<br/>
        /// This is only called once when the inspector becomes visible in editor after the first call to <see cref="LoadData()"/>
        /// </summary>
        protected virtual void InitData() { }

        /// <summary>
        /// Obsolete, change to <see cref="Header()"/> instead.
        /// </summary>
        [Obsolete]
        protected virtual void PreChangeCheck() { }

        /// <summary>
        /// This method is generally for drawing editor elements that do not need to be retained long-term.
        /// Good for toggles that temporarily enable certain editing modes for the given inspector or special script-specific headers.
        /// Called once per inspector redraw.
        /// </summary>
        protected virtual void Header()
        {
            PreChangeCheck(); // backwards compat
        }

        /// <summary>
        /// This method should draw all elements which are expected to trigger a change check.
        /// If any of the elements trigger a change, the <see cref="SaveData()"/> method will then also be triggered.
        /// <see cref="SaveData()"/> can also be triggered by manually calling <see cref="ForceSave()"/>.
        /// Called once per inspector redraw.
        /// </summary>
        protected abstract void RenderChangeCheck();

        /// <summary>
        /// For any logic related to resolving and saving data, put that under this method.
        /// This method implicitly has Undo logic against the component script being inspected already setup,
        /// so you do not need to do that yourself.
        /// Called once when a change check has been triggered or <see cref="ForceSave()"/> has been called.
        /// </summary>
        protected virtual void SaveData() { }

        /// <summary>
        /// Obsolete, change to <see cref="Footer()"/> instead.
        /// </summary>
        [Obsolete]
        protected virtual void PostChangeCheck() { }

        /// <summary>
        /// If you have any closing/finalizing logic that is not part of the change check process, you can put it in this method.
        /// Also a good spot to put any meta data like script/asset version numbers or inspector execution time values.
        /// Called once per inspector redraw.
        /// </summary>
        protected virtual void Footer()
        {
            PostChangeCheck(); // backwards compat
        }


        /// <seealso cref="ATUtility.CopyArray"/>
        protected static T[] CopyArray<T>(T[] stale) => ATEditorUtility.CopyArray(stale);

        /// <seealso cref="ATUtility.NormalizeArray"/>
        protected static T[] NormalizeArray<T>(T[] stale, int normalizedLength, System.Type type = null) => ATEditorUtility.NormalizeArray(stale, normalizedLength, type);

        /// <seealso cref="ATUtility.ArrayPop"/>
        protected static T[] ArrayPop<T>(T[] stale) => ATEditorUtility.ArrayPop(stale);

        /// <seealso cref="ATUtility.ArrayPush"/>
        protected static T[] ArrayPush<T>(T[] stale, T insert) => ATEditorUtility.ArrayPush(stale, insert);

        /// <seealso cref="ATUtility.ArrayShift"/>
        protected static T[] ArrayShift<T>(T[] stale) => ATEditorUtility.ArrayShift(stale);

        /// <seealso cref="ATUtility.ArrayUnshift"/>
        protected static T[] ArrayUnshift<T>(T[] stale, T insert) => ATEditorUtility.ArrayUnshift(stale, insert);

        /// <seealso cref="ATUtility.AddArrayItem(System.Array, System.Type)"/>
        protected static T[] AddArrayItem<T>(T[] stale) => ATEditorUtility.AddArrayItem(stale);

        /// <seealso cref="ATUtility.AddArrayItem(System.Array, int, object, System.Type)"/>
        protected static T[] AddArrayItem<T>(T[] stale, int index, T insert) => ATEditorUtility.AddArrayItem(stale, index, insert);

        /// <seealso cref="ATUtility.MoveArrayItem"/>
        protected static T[] MoveArrayItem<T>(T[] arr, int from, int to) => ATEditorUtility.MoveArrayItem(arr, from, to);

        /// <seealso cref="ATUtility.RemoveArrayItem"/>
        protected static T[] RemoveArrayItem<T>(T[] stale, int index) => ATEditorUtility.RemoveArrayItem(stale, index);

        protected object GetVariableByName(string varName) =>
            serializedObject.FindProperty(varName).GetValue();

        [Obsolete("Use property.GetValue() instead")]
        protected static void GetVariableByProperty(SerializedProperty property, object val) =>
            property.GetValue();

        protected void SetVariableByName(string varName, object val) =>
            serializedObject.FindProperty(varName).SetValue(val);

        protected void SetVariableByName(SerializedObject serialized, string varName, object val) =>
            serialized.FindProperty(varName).SetValue(val);

        [Obsolete("Use property.SetValue(object) instead")]
        protected static void SetVariableByProperty(SerializedProperty property, object val) =>
            property.SetValue(val);


        /// <summary>
        /// List of any properties you with to mark has already drawn.
        /// Use this if you render the properties manually and not via the DrawVariables* methods.
        /// </summary>
        /// <param name="names"></param>
        protected internal void VariablesDrawn(params string[] names)
        {
            foreach (var n in names) variablesDrawn.Add(n);
        }

        private bool drawProperty(SerializedProperty prop, GUIContent label, params GUILayoutOption[] opts)
        {
            var modified = false;
            var pp = prop.propertyPath;
            if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
            {
                ATReorderableList list;
                if (!arrayProps.TryGetValue(pp, out list))
                {
                    if (label == GUIContent.none) label = null;
                    list = new ATReorderableList(label).AddArrayProperty(prop.serializedObject.FindProperty(pp), GUIContent.none);
                    arrayProps.Add(pp, list);
                }

                modified = list.DrawLayout();
            }
            else modified = EditorGUILayout.PropertyField(prop, label, true, opts);

            variablesDrawn.Add(pp);
            return modified;
        }

        /// <summary>
        /// Draws all remaining serialized properties on this behaviour.
        /// If properties have been dynamically drawn or manually added the names to the VariablesDrawn list,
        /// this method will skip drawing those props.
        /// If <see cref="autoRenderVariables"/> returns true, this will be implicitly called AFTER <see cref="RenderChangeCheck()"/>
        /// </summary>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariables() => DrawVariables(serializedObject);

        protected bool DrawVariables(SerializedObject serialized)
        {
            var modified = false;
            SerializedProperty prop = serialized.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath == "m_Script")
                        continue;
                    if (variablesDrawn.Contains(prop.propertyPath))
                        continue;
                    modified ^= drawProperty(prop, GetPropertyLabel(prop, showHints));
                } while (prop.NextVisible(false));
            }

            if (serialized.hasModifiedProperties)
            {
                modified = true;
                serialized.ApplyModifiedProperties();
            }

            return modified;
        }

        /// <summary>
        /// Draws specific serialized properties on this behaviour
        /// </summary>
        /// <param name="varNames">the listing of variable names to be drawn</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariablesByName(params string[] varNames) =>
            DrawVariablesByName(serializedObject, varNames, new GUILayoutOption[0]);

        protected bool DrawVariablesByName(SerializedObject serialized, params string[] varNames) =>
            DrawVariablesByName(serialized, varNames, new GUILayoutOption[0]);

        /// <summary>
        /// Draws specific serialized properties on this behaviour
        /// </summary>
        /// <param name="varNames">the listing of variable names to be drawn</param>
        /// <param name="opts">optional array of GUILayoutOptions to pass into the property field render</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariablesByName(string[] varNames, params GUILayoutOption[] opts) =>
            DrawVariablesByName(serializedObject, varNames, opts);

        /// <summary>
        /// Draws specific serialized properties on this behaviour
        /// </summary>
        /// <param name="serialized"></param>
        /// <param name="varNames">the listing of variable names to be drawn</param>
        /// <param name="opts">optional array of GUILayoutOptions to pass into the property field render</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariablesByName(SerializedObject serialized, string[] varNames, params GUILayoutOption[] opts)
        {
            var modified = false;
            SerializedProperty prop = serialized.GetIterator();
            foreach (var varName in varNames)
            {
                prop.Reset();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.propertyPath == "m_Script")
                            continue;
                        if (prop.propertyPath != varName)
                            continue;
                        bool vertical = ATEditorGUILayout.IsAutoLayoutVertical();
                        if (!vertical) EditorGUILayout.BeginVertical();
                        modified ^= drawProperty(prop, GetPropertyLabel(prop, showHints), opts);
                        if (!vertical) EditorGUILayout.EndVertical();
                        break; // current varName found, proceed to next in list
                    } while (prop.NextVisible(false));
                }
            }

            if (serialized.hasModifiedProperties)
            {
                modified = true;
                serialized.ApplyModifiedProperties();
                serialized.UpdateIfRequiredOrScript();
            }

            return modified;
        }

        protected bool DrawAndGetVariableByName<T>(string varName, out T res, params GUILayoutOption[] opts) =>
            DrawAndGetVariableByName(serializedObject, varName, out res, opts);

        protected bool DrawAndGetVariableByName<T>(SerializedObject serialized, string varName, out T res, params GUILayoutOption[] opts)
        {
            var modified = false;
            SerializedProperty prop = serialized.GetIterator();

            prop.Reset();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath == "m_Script")
                        continue;
                    if (prop.propertyPath != varName)
                        continue;
                    modified ^= drawProperty(prop, GetPropertyLabel(prop, showHints), opts);
                    break; // current varName found, proceed to next in list
                } while (prop.NextVisible(false));
            }

            if (serialized.hasModifiedProperties)
            {
                modified = true;
                serialized.ApplyModifiedProperties();
                serialized.UpdateIfRequiredOrScript();
            }

            res = (T)prop.GetValue();
            return modified;
        }

        protected bool DrawVariablesByNameWithoutLabels(params string[] varNames) =>
            DrawVariablesByNameWithoutLabels(serializedObject, varNames, new GUILayoutOption[0]);

        protected bool DrawVariablesByNameWithoutLabels(SerializedObject serialized, params string[] varNames) =>
            DrawVariablesByNameWithoutLabels(serialized, varNames, new GUILayoutOption[0]);

        protected bool DrawVariablesByNameWithoutLabels(string[] varNames, params GUILayoutOption[] opts) =>
            DrawVariablesByNameWithoutLabels(serializedObject, varNames, opts);


        /// <summary>
        /// Draws specific serialized properties on this behaviour
        /// </summary>
        /// <param name="serialized"></param>
        /// <param name="varNames">the listing of variable names to be drawn</param>
        /// <param name="opts">optional array of GUILayoutOptions to pass into the property field render</param>
        /// <returns>whether or not any of the variables have been modified</returns>
        protected bool DrawVariablesByNameWithoutLabels(SerializedObject serialized, string[] varNames, params GUILayoutOption[] opts)
        {
            var modified = false;
            SerializedProperty prop = serialized.GetIterator();
            foreach (var varName in varNames)
            {
                prop.Reset();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.propertyPath == "m_Script")
                            continue;
                        if (prop.propertyPath != varName)
                            continue;
                        modified ^= drawProperty(prop, GUIContent.none, opts);
                        break; // current varName found, proceed to next in list
                    } while (prop.NextVisible(false));
                }
            }

            if (serialized.hasModifiedProperties)
            {
                modified = true;
                serialized.ApplyModifiedProperties();
                serialized.UpdateIfRequiredOrScript();
            }

            return modified;
        }

        protected bool DrawVariablesByNameWithLabel(GUIContent label, params string[] varNames) =>
            DrawVariablesByNameWithLabel(serializedObject, label, varNames, new GUILayoutOption[0]);

        protected bool DrawVariablesByNameWithLabel(GUIContent label, string[] varNames, params GUILayoutOption[] opts) =>
            DrawVariablesByNameWithLabel(serializedObject, label, varNames, opts);

        protected bool DrawVariablesByNameWithLabel(SerializedObject serialized, GUIContent label, params string[] varNames) =>
            DrawVariablesByNameWithLabel(serialized, label, varNames, new GUILayoutOption[0]);

        /// <summary>
        /// Draws specific serialized properties on this behaviour
        /// </summary>
        /// <param name="serialized"></param>
        /// <param name="label">custom GUICotent label to apply to the property render</param>
        /// <param name="varNames">the listing of variable names to be drawn</param>
        /// <param name="opts">optional array of GUILayoutOptions to pass into the property field render</param>
        /// <returns>whether or not any of the variables have been modified</returns>
        protected bool DrawVariablesByNameWithLabel(SerializedObject serialized, GUIContent label, string[] varNames, params GUILayoutOption[] opts)
        {
            var modified = false;
            SerializedProperty prop = serialized.GetIterator();
            foreach (var varName in varNames)
            {
                prop.Reset();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.propertyPath == "m_Script")
                            continue;
                        if (prop.propertyPath != varName)
                            continue;
                        modified ^= drawProperty(prop, label, opts);
                        label = GUIContent.none;
                        break; // current varName found, proceed to next in list
                    } while (prop.NextVisible(false));
                }
            }

            if (serialized.hasModifiedProperties)
            {
                modified = true;
                serialized.ApplyModifiedProperties();
                serialized.UpdateIfRequiredOrScript();
            }

            return modified;
        }

        protected bool DrawVariablesWithLabel(GUIContent label, params SerializedProperty[] properties) =>
            DrawVariablesWithLabel(label, properties, new GUILayoutOption[0]);

        protected bool DrawVariablesWithLabel(GUIContent label, SerializedProperty[] properties, params GUILayoutOption[] opts)
        {
            var modified = false;
            List<SerializedObject> serializedObjects = new List<SerializedObject>(properties.Length);
            foreach (var prop in properties)
            {
                if (prop == null) continue;
                serializedObjects.Add(prop.serializedObject);
                modified ^= drawProperty(prop, label, opts);
                label = GUIContent.none;
            }

            foreach (var serialized in serializedObjects.Distinct())
            {
                if (serialized.hasModifiedProperties)
                {
                    modified = true;
                    serialized.ApplyModifiedProperties();
                    serialized.UpdateIfRequiredOrScript();
                }
            }

            return modified;
        }

        protected bool DrawVariablesByNameAsType(System.Type type, params string[] varNames) =>
            DrawVariablesByNameAsType(serializedObject, type, varNames, new GUILayoutOption[0]);

        protected bool DrawVariablesByNameAsType(SerializedObject serialized, System.Type type, params string[] varNames) =>
            DrawVariablesByNameAsType(serialized, type, varNames, new GUILayoutOption[0]);

        protected bool DrawVariablesByNameAsType(System.Type type, string[] varNames, params GUILayoutOption[] opts) =>
            DrawVariablesByNameAsType(serializedObject, type, varNames, opts);

        protected bool DrawVariablesByNameAsType(SerializedObject serialized, System.Type type, string[] varNames, params GUILayoutOption[] opts)
        {
            SerializedProperty prop = serialized.GetIterator();
            foreach (var varName in varNames)
            {
                prop.Reset();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.propertyPath == "m_Script")
                            continue;
                        if (prop.propertyPath != varName)
                            continue;
                        prop.objectReferenceValue = EditorGUILayout.ObjectField(GetPropertyLabel(prop, showHints), prop.objectReferenceValue, type, true, opts);
                        variablesDrawn.Add(prop.propertyPath);
                        break; // current varName found, proceed to next in list
                    } while (prop.NextVisible(false));
                }
            }

            var modified = serialized.hasModifiedProperties;
            if (modified) serialized.ApplyModifiedProperties();
            return modified;
        }

        protected bool DrawVariablesByNameAsSprites(float size, params string[] varNames) => DrawVariablesByNameAsImageType(serializedObject, typeof(Sprite), size, varNames);
        protected bool DrawVariablesByNameAsSprites(SerializedObject serialized, float size, params string[] varNames) => DrawVariablesByNameAsImageType(serialized, typeof(Sprite), size, varNames);
        protected bool DrawVariablesByNameAsSprites(params string[] varNames) => DrawVariablesByNameAsImageType(serializedObject, typeof(Sprite), 75f, varNames);
        protected bool DrawVariablesByNameAsSprites(SerializedObject serialized, params string[] varNames) => DrawVariablesByNameAsImageType(serialized, typeof(Sprite), 75f, varNames);
        protected bool DrawVariablesByNameAsTextures(float size, params string[] varNames) => DrawVariablesByNameAsImageType(serializedObject, typeof(Texture), size, varNames);
        protected bool DrawVariablesByNameAsTextures(SerializedObject serialized, float size, params string[] varNames) => DrawVariablesByNameAsImageType(serialized, typeof(Texture), size, varNames);
        protected bool DrawVariablesByNameAsTextures(params string[] varNames) => DrawVariablesByNameAsImageType(serializedObject, typeof(Texture), 75f, varNames);
        protected bool DrawVariablesByNameAsTextures(SerializedObject serialized, params string[] varNames) => DrawVariablesByNameAsImageType(serialized, typeof(Texture), 75f, varNames);

        protected bool DrawVariablesByNameAsImageType(System.Type type, float size, params string[] varNames) =>
            DrawVariablesByNameAsImageType(serializedObject, type, new Vector2(size, size), varNames);

        protected bool DrawVariablesByNameAsImageType(System.Type type, Vector2 size, params string[] varNames) =>
            DrawVariablesByNameAsImageType(serializedObject, type, size, varNames);

        protected bool DrawVariablesByNameAsImageType(SerializedObject serialized, System.Type type, float size, params string[] varNames) =>
            DrawVariablesByNameAsImageType(serialized, type, new Vector2(size, size), varNames);

        protected bool DrawVariablesByNameAsImageType(SerializedObject serialized, System.Type type, Vector2 size, params string[] varNames)
        {
            SerializedProperty prop = serialized.GetIterator();
            foreach (var varName in varNames)
            {
                prop.Reset();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.propertyPath == "m_Script")
                            continue;
                        if (prop.propertyPath != varName)
                            continue;

                        using (VArea)
                        {
                            var label = GetPropertyLabel(prop);
                            EditorGUILayout.LabelField(label, GUILayout.MinWidth(size.x));
                            EditorGUILayout.ObjectField(prop, type, GUIContent.none, GUILayout.Width(size.x), GUILayout.Height(size.y));
                        }

                        variablesDrawn.Add(prop.propertyPath);
                        break; // current varName found, proceed to next in list
                    } while (prop.NextVisible(false));
                }
            }

            var modified = serialized.hasModifiedProperties;
            if (modified)
            {
                serialized.ApplyModifiedProperties();
                serialized.UpdateIfRequiredOrScript();
            }

            return modified;
        }

        /// <summary>
        /// Draws serialized properties on this behaviour for those which match the given type
        /// </summary>
        /// <param name="type">the expected property type to check for</param>
        /// <param name="opts">optional array of GUILayoutOptions to pass into the property field render</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariablesByType(System.Type type, params GUILayoutOption[] opts)
        {
            var modified = false;
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath == "m_Script")
                        continue;
                    System.Type containedType = prop.serializedObject.targetObject.GetType();
                    System.Reflection.FieldInfo field = containedType.GetField(prop.propertyPath, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (field?.FieldType != type)
                        continue;
                    modified ^= drawProperty(prop, GetPropertyLabel(prop, showHints), opts);
                } while (prop.NextVisible(false));
            }

            if (serializedObject.hasModifiedProperties)
            {
                modified = true;
                serializedObject.ApplyModifiedProperties();
                serializedObject.UpdateIfRequiredOrScript();
            }

            return modified;
        }

        /// <summary>
        /// Draws serialized variables on this behaviour, for propertyPaths that start with a given string
        /// </summary>
        /// <param name="prefix">the variable name prefix to check for</param>
        /// <param name="opts">optional array of GUILayoutOptions to pass into the property field render</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariablesByPrefix(string prefix, params GUILayoutOption[] opts)
        {
            var modified = false;
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath == "m_Script")
                        continue;
                    if (!prop.propertyPath.StartsWith(prefix))
                        continue;
                    modified ^= drawProperty(prop, GetPropertyLabel(prop, showHints), opts);
                } while (prop.NextVisible(false));
            }

            if (serializedObject.hasModifiedProperties)
            {
                modified = true;
                serializedObject.ApplyModifiedProperties();
                serializedObject.UpdateIfRequiredOrScript();
            }

            return modified;
        }

        protected bool DrawVariableWithDropdown(string varName, params GUILayoutOption[] opts) =>
            DrawVariableWithDropdown(serializedObject.FindProperty(varName), opts);

        /// <summary>
        /// Draws a custom dropdown field for selecting from some given options and updates the property value.
        /// </summary>
        /// <param name="property">target to update and pull type info from for the dropdown</param>
        /// <param name="opts">GUILayout options for property field layout.</param>
        /// <returns>whether any of the variables have been modified</returns>
        protected bool DrawVariableWithDropdown(SerializedProperty property, params GUILayoutOption[] opts)
        {
            using (VArea)
            {
                var label = GetPropertyLabel(property, showHints);
                using (HArea)
                using (ATEditorGUI.PropertyDropdown)
                    EditorGUILayout.PropertyField(property, label, opts);
            }

            variablesDrawn.Add(property.propertyPath);
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.UpdateIfRequiredOrScript();
                return true;
            }

            return false;
        }

        protected bool DrawSectionWithDropdowns(string sectionName, params string[] fields)
        {
            if (target == null) return false;
            bool isChanged = false;
            using (SectionScope(sectionName))
            {
                foreach (string field in fields)
                    if (serializedObject.TryFindProperty(field, out _))
                        isChanged |= DrawVariableWithDropdown(field);
            }

            return isChanged;
        }

        protected static void DrawCustomHeaderLarge(string header, bool inline = false) =>
            DrawCustomHeader(header, inline, 1);

        protected static void DrawCustomHeaderSmall(string header, bool inline = false) =>
            DrawCustomHeader(header, inline, -1);

        /// <summary>
        /// Draws some text in the same style as the [Header] attribute.
        /// </summary>
        /// <param name="header">the desired text to draw</param>
        /// <param name="inline">Whether or not the header should be handled as a PrefixLabel instead LabelField</param>
        /// <param name="fontSizeDelta"></param>
        protected static void DrawCustomHeader(string header, bool inline = false, int fontSizeDelta = 0)
        {
            if (!inline) Spacer(5f);
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = EditorStyles.largeLabel.fontSize + fontSizeDelta };
            if (inline) EditorGUILayout.LabelField(I18n.Tr(header, 1), style, GUILayout.Width(EditorGUIUtility.labelWidth));
            else EditorGUILayout.LabelField(I18n.Tr(header, 1), style);
        }

        /// <summary>
        /// Draws some text in the same style as the [Header] attribute.
        /// </summary>
        /// <param name="header">the desired content to draw</param>
        /// <param name="inline">Whether or not the header should be handled as a PrefixLabel instead LabelField</param>
        protected static void DrawCustomHeader(GUIContent header, bool inline = false)
        {
            if (!inline) Spacer(5f);
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = EditorStyles.largeLabel.fontSize };
            if (inline) EditorGUILayout.LabelField(header, style, GUILayout.Width(EditorGUIUtility.labelWidth));
            else EditorGUILayout.LabelField(header, style);
        }

        /// <summary>
        /// Creates a foldout element based on the given propery's isExpanded value.
        /// </summary>
        /// <param name="varName">name of the property to draw the foldout for</param>
        /// <param name="label">custom GUICotent label to apply to the property render</param>
        /// <returns>whether the foldout is open or not</returns>
        protected bool DrawCustomFoldout(string varName, GUIContent label = null)
        {
            var property = serializedObject.FindProperty(varName);
            if (property == null)
            {
                UnityEngine.Debug.LogError($"Property {varName} not found on {target.GetType().FullName}");
                return false;
            }

            return DrawCustomFoldout(property, label);
        }

        /// <summary>
        /// Creates a foldout element based on the given propery's isExpanded value.
        /// </summary>
        /// <param name="property">property to draw the foldout for</param>
        /// <param name="label">custom GUICotent label to apply to the property render</param>
        /// <returns></returns>
        protected bool DrawCustomFoldout(SerializedProperty property, GUIContent label = null)
        {
            if (label == null) label = GetPropertyLabel(property, showHints);
            using (HArea)
            {
                var changed = GUI.changed;
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
                GUI.changed = changed;
            }

            return property.isExpanded;
        }

        /// <summary>
        /// Draws a foldout style element for a given boolean variable.
        /// </summary>
        /// <param name="varName">name of the property to draw the foldout for</param>
        /// <returns>whether the foldout is open or not</returns>
        protected bool DrawFoldoutForToggle(string varName) => DrawFoldoutForToggle(varName, out _);

        /// <summary>
        /// Draws a foldout style element for a given boolean variable.
        /// </summary>
        /// <param name="varName">name of the property to draw the foldout for</param>
        /// <param name="changed">Whether the toggle property value changed this draw call</param>
        /// <returns>whether the foldout is open or not</returns>
        protected bool DrawFoldoutForToggle(string varName, out bool changed)
        {
            changed = false;
            SerializedProperty property = serializedObject.FindProperty(varName);
            if (property == null)
            {
                UnityEngine.Debug.LogError($"Property {varName} not found on {target.GetType().FullName}");
                return false;
            }

            return DrawFoldoutForToggle(property, out changed);
        }

        /// <summary>
        /// Draws a foldout style element for a given boolean variable.
        /// </summary>
        /// <param name="property">property to draw the foldout for</param>
        /// <returns>whether the foldout is open or not</returns>
        protected bool DrawFoldoutForToggle(SerializedProperty property) => DrawFoldoutForToggle(property, out _);

        /// <summary>
        /// Draws a foldout style element for a given boolean variable.
        /// </summary>
        /// <param name="property">property to draw the foldout for</param>
        /// <param name="changed">Whether the toggle property value changed this draw call</param>
        /// <returns>whether the foldout is open or not</returns>
        protected bool DrawFoldoutForToggle(SerializedProperty property, out bool changed)
        {
            changed = false;
            if (property.propertyType != SerializedPropertyType.Boolean)
            {
                UnityEngine.Debug.LogError($"Property {property.propertyPath} on {target.GetType().FullName} is not a boolean type");
                return false;
            }

            var _changed = GUI.changed;
            EditorGUI.indentLevel++;
            changed = EditorGUILayout.Foldout(property.boolValue, GetPropertyLabel(property, showHints), true);
            EditorGUI.indentLevel--;
            lastFoldoutRect = GUILayoutUtility.GetLastRect();
            if (GUI.changed)
            {
                GUI.changed = _changed;
                property.boolValue = changed;
                serializedObject.ApplyModifiedProperties();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a GUIContent for a given property. Considers usage of InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="prop">the desired property to resolve the label for</param>
        /// <param name="showHint">flag for whether or not to have the tooltip displayed as separate text. Default is false which is normal tooltip hover behavior</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        protected static GUIContent GetPropertyLabel(SerializedProperty prop, bool showHint, GUIStyle style = null) =>
            ATEditorGUIUtility.GetPropertyLabel(prop, showHint, style);

        /// <summary>
        /// Generates a GUIContent for a given property. Considers usage of InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="prop">the desired property to resolve the label for</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        protected GUIContent GetPropertyLabel(SerializedProperty prop, GUIStyle style = null) => GetPropertyLabel(prop, showHints, style);

        /// <summary>
        /// Generates a GUIContent for a given property. Accepts usage of both InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="context">object to inspect for the fieldName</param>
        /// <param name="fieldName">field to check for related attributes</param>
        /// <param name="showHint">flag for whether or not to have the tooltip displayed as separate text. Default is false which is normal tooltip hover behavior</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        protected static GUIContent GetPropertyLabel(UnityEngine.Object context, string fieldName, bool showHint = false, GUIStyle style = null) =>
            ATEditorGUIUtility.GetPropertyLabel(context, fieldName, showHint, style);

        /// <summary>
        /// Generates a GUIContent for a given property. Accepts usage of both InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="fieldName">field to check for related attributes</param>
        /// <param name="showHint">flag for whether or not to have the tooltip displayed as separate text. Default is false which is normal tooltip hover behavior</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        protected GUIContent GetPropertyLabel(string fieldName, bool showHint, GUIStyle style = null) =>
            GetPropertyLabel(_basescript, fieldName, showHint, style);

        /// <summary>
        /// Generates a GUIContent for a given property. Accepts usage of both InspectorName and Tooltip attributes.
        /// </summary>
        /// <param name="fieldName">field to check for related attributes</param>
        /// <param name="style">optional custom style for the tooltip when <c>showHint</c> is true</param>
        /// <returns>respective GUIContent with the text and tooltip assigned</returns>
        protected GUIContent GetPropertyLabel(string fieldName, GUIStyle style = null) =>
            GetPropertyLabel(_basescript, fieldName, showHints, style);
    }
}