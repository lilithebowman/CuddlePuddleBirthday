using UnityEngine;
using UnityEditor;

namespace ArchiTech.SDK.Editor
{
    [CustomEditor(typeof(CapsuleCollider))]
    [CanEditMultipleObjects]
    public class ATCapsuleColliderEditor : UnityEditor.Editor
    {
        protected UnityEditor.Editor editor;
        protected CapsuleCollider collider;

        private void OnEnable()
        {
            collider = (CapsuleCollider)target;
            var _editorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.CapsuleColliderEditor");
            editor = CreateEditor(target, _editorType);
            HandleEnable();
        }

        protected virtual void HandleEnable() { }

        private void OnDisable()
        {
            HandleDisable();
            DestroyImmediate(editor);
        }
        
        protected virtual void HandleDisable() { }
        
        protected virtual void HandlePreInspector() { }

        public override void OnInspectorGUI()
        {
            HandlePreInspector();
            editor.OnInspectorGUI();
            HandlePostInspector();
        }
        
        protected virtual void HandlePostInspector() { }

        public void OnSceneGUI()
        {
            if (Tools.current != Tool.Custom) return;
            collider = (CapsuleCollider)target;
            EditorGUI.BeginChangeCheck();
            Vector3 center = collider.center;
            Transform t = collider.transform;
            var position = t.position;
            var scale = t.lossyScale;
            var inverseScale = Vector3.one;
            {
                var sx = scale.x;
                var sy = scale.y;
                var sz = scale.z;
                sx = sx == 0 ? 0 : 1 / sx;
                sy = sy == 0 ? 0 : 1 / sy;
                sz = sz == 0 ? 0 : 1 / sz;
                inverseScale = new Vector3(sx, sy, sz);
            }
            var old = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            var rot = Tools.pivotRotation == PivotRotation.Local ? Quaternion.identity : Quaternion.Inverse(t.rotation);
            center = Handles.PositionHandle(Vector3.Scale(center, scale), rot);
            Handles.matrix = old;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(collider, "Update Boundary Position");
                collider.center = Vector3.Scale(center, inverseScale);
            }

        }
    }
}