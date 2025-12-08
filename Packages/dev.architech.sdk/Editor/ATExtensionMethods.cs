using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    public static class ATExtensionMethods
    {
        /// <summary>
        /// Helper method to examine a list of components to see if they exist on given object or any of its child objects.
        /// Uses Transform.IsChildOf internally.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool IsComponentsInChildren(this GameObject obj, params Component[] components)
        {
            if (obj == null) return false;
            bool pass = true;
            foreach (var component in components)
            {
                if (component == null) continue;
                if (!component.transform.IsChildOf(obj.transform)) pass = false;
            }

            return pass;
        }

        public static T GetFieldAttribute<T>(this UnityEngine.Object context, string fieldName, int index = 0) where T : PropertyAttribute
        {
            var attrs = GetFieldAttributes<T>(context, fieldName);
            if (index < 0) index = attrs.Length - index;
            return attrs.Length == 0 ? null : attrs[index];
        }

        public static T[] GetFieldAttributes<T>(this UnityEngine.Object context, string fieldName) where T : PropertyAttribute
        {
            List<T> list = new List<T>();
            if (context == null) return list.ToArray();
            FieldInfo field = context.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return list.ToArray();
            list.AddRange(field.GetCustomAttributes<T>());
            return list.ToArray();
        }

        public static T GetAttribute<T>(this SerializedProperty prop, int index = 0) where T : PropertyAttribute =>
            GetFieldAttribute<T>(prop.serializedObject.targetObject, prop.name, index);

        public static T[] GetAttributes<T>(this SerializedProperty prop) where T : PropertyAttribute =>
            GetFieldAttributes<T>(prop.serializedObject.targetObject, prop.name);

        public static TAttribute GetAttribute<TAttribute>(this Enum value)
            where TAttribute : Attribute
        {
            var enumType = value.GetType();
            return enumType.GetField(Enum.GetName(enumType, value)).GetCustomAttributes().OfType<TAttribute>().SingleOrDefault();
        }

        public static TAttribute[] GetAttributes<TAttribute>(this Enum value)
            where TAttribute : Attribute
        {
            var enumType = value.GetType();
            return enumType.GetField(Enum.GetName(enumType, value)).GetCustomAttributes().OfType<TAttribute>().ToArray();
        }

        public static bool TryFindProperty(this SerializedObject obj, string propName, out SerializedProperty property)
        {
            property = null;
            SerializedProperty prop = obj.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath == propName)
                    {
                        property = prop;
                        break;
                    }
                } while (prop.NextVisible(false));
            }

            return property != null;
        }

        public static System.Type GetValueType(this SerializedObject serialized, string propertyName) =>
            serialized.FindProperty(propertyName).GetValueType();

        public static System.Type GetValueType(this SerializedProperty property)
        {
            if (property == null) return null;
            System.Type parentType = property.serializedObject.targetObject.GetType();
            System.Reflection.FieldInfo fieldInfo = parentType.GetFieldViaPath(property.propertyPath);
            if (fieldInfo == null) return null;
            return property.propertyPath.Contains("Array") ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;
        }

        public static T GetValue<T>(this SerializedObject serialized, string propertyName) =>
            (T)GetValue(serialized.FindProperty(propertyName));

        public static object GetValue(this SerializedObject serialized, string propertyName) =>
            GetValue(serialized.FindProperty(propertyName));

        public static T GetValue<T>(this SerializedProperty property) =>
            (T)GetValue(property);

        public static object GetValue(this SerializedProperty property)
        {
            if (property == null) return null;
            object r = null;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
#if UNITY_2022_1_OR_NEWER
                    switch (property.numericType)
                    {
                        case SerializedPropertyNumericType.Int8:
                            r = (sbyte)property.intValue;
                            break;
                        case SerializedPropertyNumericType.Int16:
                            r = (short)property.intValue;
                            break;
                        case SerializedPropertyNumericType.Int32:
                            r = property.intValue;
                            break;
                        case SerializedPropertyNumericType.Int64:
                            r = property.longValue;
                            break;
                        case SerializedPropertyNumericType.UInt8:
                            r = (byte)property.intValue;
                            break;
                        case SerializedPropertyNumericType.UInt16:
                            r = (ushort)property.intValue;
                            break;
                        case SerializedPropertyNumericType.UInt32:
                            r = (uint)property.intValue;
                            break;
                        case SerializedPropertyNumericType.UInt64:
                            r = (ulong)property.longValue;
                            break;
                    }
#else
                    r = property.intValue;
#endif

                    break;
                case SerializedPropertyType.Boolean:
                    r = property.boolValue;
                    break;
                case SerializedPropertyType.Float:
#if UNITY_2022_1_OR_NEWER
                    switch (property.numericType)
                    {
                        case SerializedPropertyNumericType.Float:
                            r = property.floatValue;
                            break;
                        case SerializedPropertyNumericType.Double:
                            r = property.doubleValue;
                            break;
                    }

#else
                    r = property.floatValue;
#endif
                    break;
                case SerializedPropertyType.String:
                    r = property.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    r = property.colorValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    r = property.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    r = property.intValue;
                    break;
                case SerializedPropertyType.Vector2:
                    r = property.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    r = property.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    r = property.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    r = property.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    r = property.intValue;
                    break;
                case SerializedPropertyType.Character:
                    r = property.intValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    r = property.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    r = property.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    // gradientValue is marked as internal for some stupid reason in 2019. Use reflection to force the value assignment
                    var gradientValueInfo = property.GetType().GetProperty("gradientValue", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (gradientValueInfo != null) r = gradientValueInfo.GetValue(property);
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    r = property.intValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    r = property.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    r = property.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    r = property.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    r = property.boundsIntValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    r = property.quaternionValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    r = property.objectReferenceValue;
                    break;
                case SerializedPropertyType.ManagedReference:
                    r = property.managedReferenceValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    r = property.exposedReferenceValue;
                    break;
                case SerializedPropertyType.Generic:
                    r = property.boxedValue;
                    break;
            }

            return r;
        }

        public static int IndexOf(this SerializedProperty property, object val)
        {
            if (!property.isArray) return -1;
            for (int i = 0; i < property.arraySize; i++)
                if (property.GetArrayElementAtIndex(i).GetValue() == val)
                    return i;
            return -1;
        }

        public static bool Contains(this SerializedProperty property, object val)
        {
            if (!property.isArray) return property.GetValue() == val;
            return property.IndexOf(val) > -1;
        }

        public static void ResetToDefaultValue(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
#if UNITY_2022_1_OR_NEWER
                    switch (property.numericType)
                    {
                        case SerializedPropertyNumericType.Float:
                            property.floatValue = 0f;
                            break;
                        case SerializedPropertyNumericType.Double:
                            property.doubleValue = 0f;
                            break;
                    }
#else
                    property.floatValue = 0f;
#endif

                    break;
                case SerializedPropertyType.String:
                    property.stringValue = null;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.clear;
                    break;
                case SerializedPropertyType.Integer:
#if UNITY_2022_1_OR_NEWER
                    switch (property.numericType)
                    {
                        case SerializedPropertyNumericType.Int8:
                        case SerializedPropertyNumericType.Int16:
                        case SerializedPropertyNumericType.Int32:
                        case SerializedPropertyNumericType.UInt8:
                        case SerializedPropertyNumericType.UInt16:
                        case SerializedPropertyNumericType.UInt32:
                            property.intValue = 0;
                            break;
                        case SerializedPropertyNumericType.Int64:
                        case SerializedPropertyNumericType.UInt64:
                            property.doubleValue = 0;
                            break;
                    }
#else
                    property.intValue = 0;
#endif

                    break;
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.FixedBufferSize:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    break;
                case SerializedPropertyType.Character:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = AnimationCurve.Constant(0f, 1f, 0.5f);
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = new Bounds();
                    break;
                case SerializedPropertyType.Gradient:
                    // gradientValue is marked as internal for some stupid reason in 2019. Use reflection to force the value assignment
                    var gradientValueInfo = property.GetType().GetProperty("gradientValue", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (gradientValueInfo != null) gradientValueInfo.SetValue(property, new Gradient());
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = Vector2Int.zero;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = Vector3Int.zero;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = new RectInt();
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = new BoundsInt();
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = null;
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    break;
                case SerializedPropertyType.Generic:
                    if (property.isArray) property.ClearArray();
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(property.GetValueType()))
                        property.objectReferenceValue = null;
#if VRC_SDK_VRCSDK3
                    else if (typeof(VRC.SDKBase.VRCUrl).IsAssignableFrom(property.GetValueType()))
                        property.boxedValue = new VRC.SDKBase.VRCUrl("");
#endif
                    else property.managedReferenceValue = null;
                    break;
                default:
                    break;
            }
        }

        public static void FillValue(this SerializedProperty property, object val)
        {
            if (!property.isArray) property.SetValue(val);
            else
                for (int i = 0; i < property.arraySize; i++)
                    property.GetArrayElementAtIndex(i).SetValue(val);
        }

        public static void ResizeAndFill(this SerializedProperty property, int newSize, object val)
        {
            if (!property.isArray) property.SetValue(val);
            var index = property.arraySize;
            if (index == newSize) return;
            property.arraySize = newSize;
            for (; index < newSize; index++)
                property.GetArrayElementAtIndex(index).SetValue(val);
        }

        public static void SetValue(this SerializedObject serialized, string propertyName, int index, object val) =>
            serialized.FindProperty(propertyName).SetValue(index, val);

        public static void SetValue(this SerializedProperty property, int index, object val)
        {
            if (!property.isArray) property.SetValue(val);
            property.GetArrayElementAtIndex(index).SetValue(val);
        }

        public static void SetValue(this SerializedObject serialized, string propertyName, System.Array val) =>
            serialized.FindProperty(propertyName).SetValue(val);

        public static void SetValue(this SerializedProperty property, System.Array val)
        {
            if (!property.isArray) return;
            if (val == null)
            {
                property.ResetToDefaultValue();
                return;
            }

            property.arraySize = val.Length;
            for (int i = 0; i < val.Length; i++)
                property.GetArrayElementAtIndex(i).SetValue(val.GetValue(i));
        }

        public static void SetValue(this SerializedObject serialized, string propertyName, object val) =>
            serialized.FindProperty(propertyName).SetValue(val);

        public static void SetValue(this SerializedProperty property, object val)
        {
            if (property == null) return;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
#if UNITY_2022_1_OR_NEWER
                    switch (property.numericType)
                    {
                        case SerializedPropertyNumericType.Int8:
                        case SerializedPropertyNumericType.Int16:
                        case SerializedPropertyNumericType.Int32:
                        case SerializedPropertyNumericType.UInt8:
                        case SerializedPropertyNumericType.UInt16:
                        case SerializedPropertyNumericType.UInt32:
                            property.intValue = (int)val;
                            break;
                        case SerializedPropertyNumericType.Int64:
                        case SerializedPropertyNumericType.UInt64:
                            property.longValue = (long)val;
                            break;
                    }
#else
                    if (val is long lVal) property.longValue = lVal;
                    else if (val is int iVal) property.intValue = iVal;
#endif

                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = (bool)val;
                    break;
                case SerializedPropertyType.Float:
#if UNITY_2022_1_OR_NEWER
                    switch (property.numericType)
                    {
                        case SerializedPropertyNumericType.Float:
                            property.floatValue = (float)val;
                            break;
                        case SerializedPropertyNumericType.Double:
                            property.doubleValue = (double)val;
                            break;
                    }
#else
                    property.doubleValue = (double)val;
#endif

                    break;
                case SerializedPropertyType.String:
                    property.stringValue = (string)val;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = (Color)val;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.Enum:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = (Vector2)val;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = (Vector3)val;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = (Vector4)val;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = (Rect)val;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.Character:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = (AnimationCurve)val;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = (Bounds)val;
                    break;
                case SerializedPropertyType.Gradient:
                    // gradientValue is marked as internal for some stupid reason in 2019. Use reflection to force the value assignment
                    var gradientValueInfo = property.GetType().GetProperty("gradientValue", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (gradientValueInfo != null) gradientValueInfo.SetValue(property, (Gradient)val);
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = (Vector2Int)val;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = (Vector3Int)val;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = (RectInt)val;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = (BoundsInt)val;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = (Quaternion)val;
                    break;
                case SerializedPropertyType.ObjectReference:
                    UnityEngine.Debug.Log($"type check {val?.GetType().Name}");
                    if (val == null) property.objectReferenceValue = null;
                    else property.objectReferenceValue = (UnityEngine.Object)val;
                    break;
                case SerializedPropertyType.ExposedReference:
                    if (val == null) property.exposedReferenceValue = null;
                    else property.exposedReferenceValue = (UnityEngine.Object)val;
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = val;
                    break;
                case SerializedPropertyType.Generic:
                    if (property.isArray)
                    {
                        var arr = (System.Array)val;
                        property.ClearArray();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var a = arr.GetValue(i);
                            property.InsertArrayElementAtIndex(i);
                            property.GetArrayElementAtIndex(i).SetValue(arr.GetValue(i));
                        }
                    }
#if VRC_SDK_VRCSDK3
                    else if (typeof(VRC.SDKBase.VRCUrl).IsAssignableFrom(property.GetValueType()))
                        property.boxedValue = val ?? new VRC.SDKBase.VRCUrl("");
#endif
                    else property.boxedValue = val;
                    break;
                default:
                    break;
            }

            var modified = property.serializedObject.hasModifiedProperties;
            if (modified)
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public static System.Reflection.FieldInfo GetFieldViaPath(this System.Type type, string path)
        {
            while (true)
            {
                if (type == null) return null;
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var parent = type;
                var fieldInfo = parent.GetField(path, flags);
                var paths = path.Split('.');

                for (int i = 0; i < paths.Length; i++)
                {
                    fieldInfo = parent?.GetField(paths[i], flags);
                    if (fieldInfo != null)
                    {
                        if (fieldInfo.FieldType.IsArray)
                        {
                            parent = fieldInfo.FieldType.GetElementType();
                            i += 2;
                            continue;
                        }

                        if (fieldInfo.FieldType.IsGenericType)
                        {
                            parent = fieldInfo.FieldType.GetGenericArguments()[0];
                            i += 2;
                            continue;
                        }

                        parent = fieldInfo.FieldType;
                    }
                    else break;
                }

                if (fieldInfo == null)
                {
                    if (type.BaseType != null)
                    {
                        type = type.BaseType;
                        continue;
                    }

                    return null;
                }

                return fieldInfo;
            }
        }
    }
}