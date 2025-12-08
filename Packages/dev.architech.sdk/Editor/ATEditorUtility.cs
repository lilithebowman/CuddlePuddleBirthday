using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using VRC.Udon;

// ReSharper disable ArrangeStaticMemberQualifier

namespace ArchiTech.SDK.Editor
{
    public static class ATEditorUtility
    {
        #region File Helpers

        /// <summary>
        /// Takes an absolute path (commonly provided from a file save dialog) and returns an AssetDatabase appropriate relative path of the file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The relative path of the asset. If the path does not reside within the projet, an empty string will be returned.</returns>
        public static string ToRelativePath(string path)
        {
            var info = new FileInfo(path);
            string absPath = info.FullName;
            string[] arr = AssetDatabase.GetAllAssetPaths();

            foreach (var t in arr)
            {
                info = new FileInfo(t);
                if (info.FullName.Equals(absPath)) return t;
            }

            return string.Empty;
        }

        #endregion

        #region Component Helpers

        public static T[] GetComponentsInSceneWithDistinctNames<T>(out string[] names) where T : Component
        {
            var rawComps = GetComponentsInSceneWithDistinctNames(typeof(T), out names);
            var comps = new T[rawComps.Length];
            System.Array.Copy(rawComps, comps, rawComps.Length);
            return comps;
        }

        public static Component[] GetComponentsInSceneWithDistinctNames(System.Type type, out string[] names)
        {
            if (!typeof(Component).IsAssignableFrom(type))
            {
                UnityEngine.Debug.LogWarning($"Type {type.FullName} must be a child type of Component.");
                names = new string[0];
                return new Component[0];
            }

            Component[] components = GetComponentsInScene(type);
            Dictionary<string, int> counts = new Dictionary<string, int>();
            var _names = components.Select(c => c.gameObject.name).ToArray();
            names = _names.Select(c =>
            {
                if (counts.ContainsKey(c))
                {
                    var i = counts[c];
                    counts[c]++;
                    c = $"[{i}] {c}";
                }
                else if (Array.IndexOf(_names, c) != Array.LastIndexOf(_names, c))
                {
                    counts.Add(c, 1);
                    c = $"[0] {c}";
                }

                return c;
            }).ToArray();
            counts.Clear();
            return components;
        }

        public static T GetComponentInNearestParent<T>(GameObject go, bool includeSelf = true) where T : Component =>
            GetComponentInNearestParent<T>(go.transform, includeSelf);

        public static T GetComponentInNearestParent<T>(Component component, bool includeSelf = true) where T : Component =>
            GetComponentInNearestParent<T>(component.transform, includeSelf);

        public static T GetComponentInNearestParent<T>(Transform t, bool includeSelf = true) where T : Component
        {
            if (t == null) return null;
            Transform parent = t;
            T component = includeSelf ? t.GetComponent<T>() : null;
            while (component == null)
            {
                // do it this way because GetComponentInParent fails for disabled game objects in older versions of unity.
                parent = parent.parent;
                if (parent == null) break;
                component = parent.GetComponent<T>();
            }

            return component;
        }

        /// <summary>
        /// Attempts to move the given component to the start of the list of components on the respective GameObject.
        /// It'll move it as far up as possible until the movement action is rejected.
        /// </summary>
        /// <param name="component">Component to move</param>
        public static void MoveComponentToTop(Component component)
        {
            if (component == null) return;
            int lastPosition = -1;
            int currentPosition = System.Array.IndexOf(component.GetComponents<Component>(), component);
            // move up until it won't move up anymore
            while (currentPosition != lastPosition)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                lastPosition = currentPosition;
                currentPosition = System.Array.IndexOf(component.GetComponents<Component>(), component);
            }
        }

        /// <summary>
        /// Attempts to move the given component to the end of the list of components on the respective GameObject
        /// </summary>
        /// <param name="component">Component to move</param>
        public static void MoveComponentToBottom(Component component)
        {
            if (component == null) return;
            int lastPosition = -1;
            int currentPosition = System.Array.IndexOf(component.GetComponents<Component>(), component);
            // move down until it won't move up anymore
            while (currentPosition != lastPosition)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
                lastPosition = currentPosition;
                currentPosition = System.Array.IndexOf(component.GetComponents<Component>(), component);
            }
        }

        /// <summary>
        /// Attempts to move the given component to the given index of the list of components on the respective GameObject
        /// </summary>
        /// <param name="component"></param>
        /// <param name="index"></param>
        public static void MoveComponentToIndex(Component component, int index)
        {
            if (component == null) return;
            int lastPosition = -1;
            int currentPosition = System.Array.IndexOf(component.GetComponents<Component>(), component);
            while (currentPosition != index && currentPosition != lastPosition)
            {
                // move component up or down based on position relative to the target index
                if (currentPosition < index)
                    UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
                else UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                lastPosition = currentPosition;
                currentPosition = System.Array.IndexOf(component.GetComponents<Component>(), component);
            }
        }

        /// <summary>
        /// Fetches the first occurrence of a given component type using the TryGet pattern.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="includeInactive"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryGetComponentInScene<T>(out T component, bool includeInactive = true) where T : Component
        {
            component = GetComponentInScene<T>(includeInactive);
            return component != null;
        }

        /// <summary>
        /// Fetches the first occurrence of a given component type using the TryGet pattern.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="component"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static bool TryGetComponentInScene(System.Type type, out Component component, bool includeInactive = true)
        {
            component = GetComponentInScene(type, includeInactive);
            return component != null;
        }

        /// <summary>
        /// Fetches all occurrences of a given component type using the TryGet pattern.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="includeInactive"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryGetComponentsInScene<T>(out T[] components, bool includeInactive = true) where T : Component
        {
            components = GetComponentsInScene<T>();
            return components != null && components.Length > 0;
        }

        /// <summary>
        /// Fetches all occurrences of a given component type using the TryGet pattern.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="components"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static bool TryGetComponentsInScene(System.Type type, out Component[] components, bool includeInactive = true)
        {
            components = GetComponentsInScene(type);
            return components != null && components.Length > 0;
        }

        /// <summary>
        /// Fetches the first component on the GameObject a given component is attached to.
        /// If a component doesn't exist, it will add it to the GameObject and return the new component.
        /// Implicitly adds component via editor Undo system.
        /// </summary>
        /// <param name="component"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetOrAddComponent<T>(Component component) where T : Component =>
            (T)GetOrAddComponent(component.gameObject, typeof(T), out _);

        public static T GetOrAddComponent<T>(Component component, out bool componentAdded) where T : Component =>
            (T)GetOrAddComponent(component.gameObject, typeof(T), out componentAdded);

        /// <summary>
        /// Fetches the first component a given GameObject.
        /// If a component doesn't exist, it will add it to the GameObject and return the new component.
        /// Implicitly adds component via editor Undo system.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component =>
            (T)GetOrAddComponent(gameObject, typeof(T), out _);

        public static T GetOrAddComponent<T>(GameObject gameObject, out bool componentAdded) where T : Component =>
            (T)GetOrAddComponent(gameObject, typeof(T), out componentAdded);

        /// <summary>
        /// Fetches the first component a given GameObject.
        /// If a component doesn't exist, it will add it to the GameObject and return the new component.
        /// Implicitly adds component via editor Undo system.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Component GetOrAddComponent(GameObject gameObject, System.Type type) =>
            GetOrAddComponent(gameObject, type, out _);

        public static Component GetOrAddComponent(GameObject gameObject, System.Type type, out bool componentAdded)
        {
            var component = gameObject.GetComponent(type);
            componentAdded = component == null;
            return component ?? (typeof(UdonSharpBehaviour).IsAssignableFrom(type)
                ? UdonSharpUndo.AddComponent(gameObject, type)
                : Undo.AddComponent(gameObject, type));
        }

        /// <summary>
        /// Searches through the entirey of either the current active scene or opened prefab for any components with the desired type.
        /// </summary>
        /// <param name="includeInactive">Should inactive gameobjects be searched? Defaults to true.</param>
        /// <typeparam name="T">The generic type to look for. Must derive from Component type.</typeparam>
        /// <returns>Whether any components exist of the given type.</returns>
        public static bool HasComponentInScene<T>(bool includeInactive = true) where T : Component =>
            GetComponentsInScene<T>(includeInactive).Length > 0;

        /// <summary>
        /// Searches through the entirey of either the current active scene or opened prefab for any components with the desired type.
        /// </summary>
        /// <param name="type">What type to look for. Must derive from Component type.</param>
        /// <param name="includeInactive">Should inactive gameobjects be searched? Defaults to true.</param>
        /// <returns>Whether any components exist of the given type.</returns>
        public static bool HasComponentInScene(System.Type type, bool includeInactive = true) =>
            GetComponentsInScene(type, includeInactive).Length > 0;

        /// <summary>
        /// Searches for components of the desired type in either the active scene or opened prefab and returns the first found instance of it.
        /// </summary>
        /// <param name="includeInactive">Should inactive gameobjects be searched? Defaults to true.</param>
        /// <typeparam name="T">The generic type to look for. Must derive from Component type.</typeparam>
        /// <returns>The first instance of the given type found.</returns>
        public static T GetComponentInScene<T>(bool includeInactive = true) where T : Component =>
            GetComponentsInScene<T>(includeInactive).FirstOrDefault();

        /// <summary>
        /// Searches for components of the desired type in either the active scene or opened prefab and returns the first found instance of it.
        /// </summary>
        /// <param name="type">What type to look for. Must derive from Component type.</param>
        /// <param name="includeInactive">Should inactive gameobjects be searched? Defaults to true.</param>
        /// <returns>The first instance of the given type found.</returns>
        public static Component GetComponentInScene(System.Type type, bool includeInactive = true) =>
            GetComponentsInScene(type, includeInactive).FirstOrDefault();

        /// <summary>
        /// Searches for all components of the desired type in either the active scene or opened prefab.
        /// </summary>
        /// <param name="includeInactive">Should inactive gameobjects be searched? Defaults to true.</param>
        /// <typeparam name="T">The generic type to look for. Must derive from Component type.</typeparam>
        /// <returns>A pure array of Components of the given type. Never returns null.</returns>
        public static T[] GetComponentsInScene<T>(bool includeInactive = true) where T : Component
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject[] roots = stage == null ? SceneManager.GetActiveScene().GetRootGameObjects() : new[] { stage.prefabContentsRoot };
            List<T> objects = new List<T>();
            foreach (GameObject root in roots)
                objects.AddRange(root.GetComponentsInChildren<T>(includeInactive));
            return objects.ToArray();
        }

        /// <summary>
        /// Searches for all components of the desired type in either the active scene or opened prefab.
        /// PERFORMANCE HEAVY. Call this method SPARINGLY.
        /// </summary>
        /// <param name="type">What type to look for. Must derive from Component type.</param>
        /// <param name="includeInactive">Should inactive gameobjects be searched? Defaults to true.</param>
        /// <returns>A pure array of Components of the given type. Never returns null.</returns>
        public static Component[] GetComponentsInScene(System.Type type, bool includeInactive = true)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject[] roots = stage == null ? SceneManager.GetActiveScene().GetRootGameObjects() : new[] { stage.prefabContentsRoot };
            List<Component> objects = new List<Component>();
            foreach (GameObject root in roots)
                objects.AddRange(root.GetComponentsInChildren(type, includeInactive));
            return objects.ToArray();
        }

        public static T SwapUdonSharpComponentTypeTo<T>(UdonSharpBehaviour fromBehaviour) where T : UdonSharpBehaviour
        {
            T @out = null;
            var from = new SerializedObject(fromBehaviour);
            foreach (var newScript in Resources.FindObjectsOfTypeAll<MonoScript>())
            {
                if (newScript.GetClass() == typeof(T))
                {
                    from.FindProperty("m_Script").objectReferenceValue = newScript;
                    from.ApplyModifiedProperties();
                    EditorUtility.RequestScriptReload(); // this is not supersition. unity cries if the domain isn't reloaded after the script adjustments.
                    @out = (T)from.targetObject;
                    break;
                }
            }

            return @out;
        }

        #endregion

        #region Array Helpers

        /// <seealso cref="ATUtility.CopyArray"/>
        public static T[] CopyArray<T>(T[] stale) => (T[])ATUtility.CopyArray(stale, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.NormalizeArray"/>
        public static T[] NormalizeArray<T>(T[] stale, int normalizedLength, System.Type type = null) => (T[])ATUtility.NormalizeArray(stale, normalizedLength, type);

        /// <seealso cref="ATUtility.ArrayPop"/>
        public static T[] ArrayPop<T>(T[] stale) => (T[])ATUtility.ArrayPop(stale, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.ArrayPush"/>
        public static T[] ArrayPush<T>(T[] stale, T insert) => (T[])ATUtility.ArrayPush(stale, insert, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.ArrayShift"/>
        public static T[] ArrayShift<T>(T[] stale) => (T[])ATUtility.ArrayShift(stale, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.ArrayUnshift"/>
        public static T[] ArrayUnshift<T>(T[] stale, T insert) => (T[])ATUtility.ArrayUnshift(stale, insert, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.AddArrayItem(System.Array, System.Type)"/>
        public static T[] AddArrayItem<T>(T[] stale) => (T[])ATUtility.AddArrayItem(stale, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.AddArrayItem(System.Array, int, object, System.Type)"/>
        public static T[] AddArrayItem<T>(T[] stale, int index, T insert) => (T[])ATUtility.AddArrayItem(stale, index, insert, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.MoveArrayItem"/>
        public static T[] MoveArrayItem<T>(T[] arr, int from, int to) => (T[])ATUtility.MoveArrayItem(arr, from, to);

        /// <seealso cref="ATUtility.RemoveArrayItem"/>
        public static T[] RemoveArrayItem<T>(T[] stale, int index) => (T[])ATUtility.RemoveArrayItem(stale, index, stale?.GetType().GetElementType() ?? typeof(object));

        /// <seealso cref="ATUtility.ResizeArrayAndFill"/>
        public static T[] ResizeArrayAndFill<T>(T[] stale, int newSize, T fill) => (T[])ATUtility.ResizeArrayAndFill(stale, newSize, fill, stale?.GetType().GetElementType() ?? typeof(object));

        #endregion

        #region UI Event Helpers

        public static int GetPersistentListenerIndex(UnityEventBase evt, UnityEngine.Object target, string method, object arg)
        {
            if (evt == null) return -1;
            var persistCount = evt.GetPersistentEventCount();
            if (persistCount == 0) return -1;
            // get all the necessary reflection objects
            const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance;
            var f_PersistentCallGroup = typeof(UnityEventBase).GetField("m_PersistentCalls", binding);
            if (f_PersistentCallGroup == null) return -2;
            var m_PersistentCall = f_PersistentCallGroup.FieldType.GetMethod("GetListener", binding);
            if (m_PersistentCall == null) return -2;
            var f_PersistentListenerMode = m_PersistentCall.ReturnType.GetField("m_Mode", binding);
            if (f_PersistentListenerMode == null) return -2;
            var f_ArgumentCache = m_PersistentCall.ReturnType.GetField("m_Arguments", binding);
            if (f_ArgumentCache == null) return -2;

            var persistentCalls = f_PersistentCallGroup.GetValue(evt);
            for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            {
                var persistentTarget = evt.GetPersistentTarget(i);
                if (persistentTarget != target) continue;
                var persistentMethod = evt.GetPersistentMethodName(i);
                if (persistentMethod != method) continue;

                var persistentListener = m_PersistentCall.Invoke(persistentCalls, new object[] { i });
                var persistentListenerMode = (PersistentListenerMode)f_PersistentListenerMode.GetValue(persistentListener);
                var persistentListenerArgumentCache = f_ArgumentCache.GetValue(persistentListener);

                object persistentArgument = null;
                FieldInfo argumentInfo;
                switch (persistentListenerMode)
                {
                    case PersistentListenerMode.Bool:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_BoolArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.Int:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_IntArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.Float:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_FloatArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.String:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_StringArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.Object:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_ObjectArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    // if the listener mode is void, no argument is expected so just return the index
                    case PersistentListenerMode.Void: return i;
                    default: continue;
                }

                // check if the arguments match to null/missing equivalency, if so it's a match at this point, return the index
                if (arg == null && (persistentArgument == null || persistentArgument.GetType() == typeof(UnityEngine.Object))) return i;
                // check if the arguments match, if so it's a match at this point, return the index
                if (object.Equals(arg, persistentArgument) || UnityEngine.Object.Equals(arg, persistentArgument)) return i;
            }

            return -1;
        }

        public static int GetPersistentListenerIndex(UnityEventBase evt, System.Type target, string method, object arg)
        {
            if (evt == null) return -1;
            var persistCount = evt.GetPersistentEventCount();
            if (persistCount == 0) return -1;
            // get all the necessary reflection objects
            const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance;
            var f_PersistentCallGroup = typeof(UnityEventBase).GetField("m_PersistentCalls", binding);
            if (f_PersistentCallGroup == null) return -2;
            var m_PersistentCall = f_PersistentCallGroup.FieldType.GetMethod("GetListener", binding);
            if (m_PersistentCall == null) return -2;
            var f_PersistentListenerMode = m_PersistentCall.ReturnType.GetField("m_Mode", binding);
            if (f_PersistentListenerMode == null) return -2;
            var f_ArgumentCache = m_PersistentCall.ReturnType.GetField("m_Arguments", binding);
            if (f_ArgumentCache == null) return -2;

            var persistentCalls = f_PersistentCallGroup.GetValue(evt);
            for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            {
                var persistentTarget = evt.GetPersistentTarget(i);
                var persistentMethod = evt.GetPersistentMethodName(i);
                if (persistentTarget.GetType() != target) continue;
                if (persistentMethod != method) continue;

                var persistentListener = m_PersistentCall.Invoke(persistentCalls, new object[] { i });
                var persistentListenerMode = (PersistentListenerMode)f_PersistentListenerMode.GetValue(persistentListener);
                var persistentListenerArgumentCache = f_ArgumentCache.GetValue(persistentListener);

                object persistentArgument = null;
                FieldInfo argumentInfo;
                switch (persistentListenerMode)
                {
                    case PersistentListenerMode.Bool:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_BoolArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.Int:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_IntArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.Float:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_FloatArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.String:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_StringArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    case PersistentListenerMode.Object:
                        argumentInfo = f_ArgumentCache.FieldType.GetField("m_ObjectArgument", binding);
                        if (argumentInfo == null) continue;
                        persistentArgument = argumentInfo.GetValue(persistentListenerArgumentCache);
                        break;
                    // if the listener mode is void, no argument is expected so just return the index
                    case PersistentListenerMode.Void: return i;
                    default: continue;
                }

                // check if the arguments match to null/missing equivalency, if so it's a match at this point, return the index
                if (arg == null && (persistentArgument == null || persistentArgument.GetType() == typeof(UnityEngine.Object))) return i;
                // check if the arguments match, if so it's a match at this point, return the index
                if (object.Equals(arg, persistentArgument) || UnityEngine.Object.Equals(arg, persistentArgument)) return i;
            }

            return -1;
        }

        public static int GetPersistentListenerIndex(UnityEventBase evt, UnityEngine.Object target, string method)
        {
            if (evt == null) return -1;
            var persistCount = evt.GetPersistentEventCount();
            if (persistCount == 0) return -1;
            for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            {
                var persistentTarget = evt.GetPersistentTarget(i);
                var persistentMethod = evt.GetPersistentMethodName(i);
                if (persistentTarget != target) continue;
                if (persistentMethod != method) continue;
                return i;
            }

            return -1;
        }

        public static int GetPersistentListenerIndex(UnityEventBase evt, System.Type target, string method)
        {
            if (evt == null) return -1;
            var persistCount = evt.GetPersistentEventCount();
            if (persistCount == 0) return -1;
            for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            {
                var persistentTarget = evt.GetPersistentTarget(i);
                var persistentMethod = evt.GetPersistentMethodName(i);
                if (persistentTarget.GetType() != target) continue;
                if (persistentMethod != method) continue;
                return i;
            }

            return -1;
        }

        public static int GetPersistentListenerIndex(UnityEventBase evt, UnityEngine.Object target)
        {
            if (evt == null) return -1;
            var persistCount = evt.GetPersistentEventCount();
            if (persistCount == 0) return -1;
            for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            {
                var persistentTarget = evt.GetPersistentTarget(i);
                if (persistentTarget != target) continue;
                return i;
            }

            return -1;
        }

        public static int GetPersistentListenerIndex(UnityEventBase evt, System.Type target)
        {
            if (evt == null) return -1;
            var persistCount = evt.GetPersistentEventCount();
            if (persistCount == 0) return -1;
            for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            {
                var persistentTarget = evt.GetPersistentTarget(i);
                if (persistentTarget.GetType() != target) continue;
                return i;
            }

            return -1;
        }

        public static void RemoveSelectableActionEvent(Selectable component, UnityEventBase evnt, UnityAction action)
        {
            if (component == null) return;
            string udonEventName = action.Method.Name;
            UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)action.Target);
            for (int i = 0; i < evnt.GetPersistentEventCount(); i++)
            {
                // clean up noop events
                if (evnt.GetPersistentTarget(i) == null) UnityEventTools.RemovePersistentListener(evnt, i--);
            }

            var stage = -1;
            do
            {
                stage = GetPersistentListenerIndex(evnt, behaviour, nameof(UdonBehaviour.SendCustomEvent), udonEventName);
                if (stage > -1) UnityEventTools.RemovePersistentListener(evnt, stage);
            } while (stage > -1);
        }


        public static void EnsureSelectableActionEvent(Selectable component, UnityEventBase evnt, UnityAction action)
        {
            if (component == null) return;
            string udonEventName = action.Method.Name;
            UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)action.Target);
            // clean up noop events
            for (int i = 0; i < evnt.GetPersistentEventCount(); i++)
                if (evnt.GetPersistentTarget(i) == null)
                    UnityEventTools.RemovePersistentListener(evnt, i--);

            var stage = GetPersistentListenerIndex(evnt, behaviour, nameof(UdonBehaviour.SendCustomEvent), udonEventName);

            if (stage == -1)
            {
                Undo.RecordObject(component, "Remove added UI event");
                UnityEventTools.AddStringPersistentListener(evnt, behaviour.SendCustomEvent, udonEventName);
            }
        }

        #endregion

        #region Scripting Defines Helpers

        public static void UpdatePackageScriptingDefine(string packageName, string defineName)
        {
            var hasPkg = HasPackageInProject(packageName);
            if (HasScriptingDefine(defineName) == hasPkg) return;
            Debug.Log($"Change to package {packageName} detected.");
            UpdateScriptingDefine(defineName, hasPkg);
        }

        public static void UpdateScriptingDefine(string name, bool shouldBePresent)
        {
            if (shouldBePresent) AddScriptingDefine(name);
            else RemoveScriptingDefine(name);
        }

        public static bool HasScriptingDefine(string name)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
            return defines.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        public static void AddScriptingDefine(string name)
        {
            if (!HasScriptingDefine(name))
            {
                Debug.Log($"Adding scripting define {name}");
                BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
                defines = defines.Append(name).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
            }
        }

        public static void RemoveScriptingDefine(string name)
        {
            if (HasScriptingDefine(name))
            {
                Debug.Log($"Removing scripting define {name}");
                BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
                defines = defines.Where(s => s != name).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
            }
        }

        #endregion

        #region PackageManager Helpers

        public static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string packageName)
        {
            return AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                .FirstOrDefault(x => x != null && x.name == packageName);
        }

        public static bool HasPackageInProject(string packageName) => GetPackageInfo(packageName) != null;

        #endregion

        #region Reflection Helpers

        private static readonly Dictionary<(Type, Assembly), MethodInfo[]> extMethodsCached = new Dictionary<(Type, Assembly), MethodInfo[]>();
        public static MethodInfo[] GetExtensionMethods(Type targetType, Assembly assembly)
        {
            if (!extMethodsCached.TryGetValue((targetType, assembly), out MethodInfo[] methods))
            {
                methods = assembly.GetTypes()
                    .Where(type => type.IsSealed && !type.IsGenericType && !type.IsNested)
                    .Where(type => type.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    .Where(method => method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    .Where(method => method.GetParameters().Length > 0 &&
                                     method.GetParameters()[0].ParameterType.IsAssignableFrom(targetType)).ToArray();
                extMethodsCached[(targetType, assembly)] = methods;
            }

            return methods;
        }

        // Search across all loaded assemblies
        public static MethodInfo[] GetExtensionMethods(Type targetType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => GetExtensionMethods(targetType, assembly)).ToArray();
        }

        #endregion
    }

    #region Optional Dependency Scripting Define Fixes

    // TODO: Remove this logic once UdonSharp compiler has proper support for assembly definition version defines (or udon2 comes out)

    [InitializeOnLoad]
    internal static class DependencyScriptingDefineHandler
    {
        private static bool _hasCheckedDefines = false;

        static DependencyScriptingDefineHandler()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (_hasCheckedDefines || EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;
            DefineUpdates();
            _hasCheckedDefines = true;
        }

        private static void DefineUpdates()
        {
            ATEditorUtility.UpdatePackageScriptingDefine("com.varneon.vudon.logger", "VUDON_LOGGER");
        }
    }

    #endregion
}