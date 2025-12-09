#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// PivotDoctor – Move & rotate real pivots by modifying mesh data,
/// without helper objects. Works in Edit mode only.
/// </summary>
public class PivotDoctor : EditorWindow
{
    // --- UI state ---
    private Vector3 pivotOffsetLocal = Vector3.zero;
    private Vector3 pivotRotationEuler = Vector3.zero;
    private bool processAllSelected = true;

    [MenuItem("Tools/Pivot Doctor")]
    private static void ShowWindow()
    {
        GetWindow<PivotDoctor>("Pivot Doctor");
    }

    private void OnGUI()
    {
        var selection = Selection.transforms;
        bool hasMesh = HasAnyMesh(selection);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pivot Doctor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This edits the actual mesh data:\n" +
            "- No helper GameObjects\n" +
            "- Works on MeshFilter meshes\n" +
            "- Duplicates the mesh first so your FBX asset isn’t destroyed.",
            MessageType.Info
        );

        if (!hasMesh)
        {
            EditorGUILayout.HelpBox(
                "Select at least one GameObject with a MeshFilter.",
                MessageType.Warning
            );
            return;
        }

        EditorGUILayout.Space();
        processAllSelected = EditorGUILayout.ToggleLeft("Affect ALL selected objects", processAllSelected);

        // ---------------- Move Pivot ----------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Move Pivot (Position)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This moves the pivot by a LOCAL OFFSET.\n" +
            "Example: (0, 1, 0) will move the pivot +1 unit on the object's local Y axis.",
            MessageType.None
        );

        pivotOffsetLocal = EditorGUILayout.Vector3Field("Pivot Offset (local)", pivotOffsetLocal);

        if (GUILayout.Button("Apply Pivot Offset"))
        {
            ApplyToSelection(t =>
            {
                MovePivotLocalOffset(t, pivotOffsetLocal);
            });
        }

        // ---------------- Rotate Pivot ----------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rotate Pivot (Orientation)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Rotates the pivot's orientation WITHOUT moving the mesh in world space.\n" +
            "Example: (0, 90, 0) rotates the pivot 90° around local Y.\n\n" +
            "Internally: vertices are rotated by the inverse, transform is rotated by the delta.",
            MessageType.None
        );

        pivotRotationEuler = EditorGUILayout.Vector3Field("Pivot Rotation (delta, degrees)", pivotRotationEuler);

        if (GUILayout.Button("Apply Pivot Rotation"))
        {
            ApplyToSelection(t =>
            {
                RotatePivot(t, Quaternion.Euler(pivotRotationEuler));
            });
        }

        // ---------------- Right Way Up ----------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Right Way Up", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Workflow:\n" +
            "1. Rotate the object(s) in the Scene view so they visually look correct.\n" +
            "2. Click this button.\n\n" +
            "The current local rotation is baked into the mesh and the Transform's rotation is reset to (0,0,0).\n" +
            "Result: The object stays where it is, but its pivot axes are now 'right way up'.",
            MessageType.None
        );

        if (GUILayout.Button("Bake Current Rotation As Right Way Up"))
        {
            ApplyToSelection(BakeCurrentRotationAsRightWayUp);
        }

        // ---------------- Quick Actions ----------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Center Pivot to Mesh Bounds"))
        {
            ApplyToSelection(CenterPivotToBounds);
        }

        if (GUILayout.Button("Reset Window Values"))
        {
            pivotOffsetLocal = Vector3.zero;
            pivotRotationEuler = Vector3.zero;
        }
    }

    // ----------------- CORE LOGIC -----------------

    private static bool HasAnyMesh(Transform[] transforms)
    {
        if (transforms == null || transforms.Length == 0) return false;
        foreach (var t in transforms)
        {
            if (!t) continue;
            var mf = t.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) return true;
        }
        return false;
    }

    private void ApplyToSelection(System.Action<Transform> action)
    {
        var selection = processAllSelected ? Selection.transforms : new[] { Selection.activeTransform };

        Undo.SetCurrentGroupName("Pivot Doctor Operation");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var t in selection)
        {
            if (!t) continue;
            var mf = t.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) continue;

            action(t);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    /// <summary>
    /// Duplicate the mesh so we never edit the imported FBX/asset directly.
    /// If it's already an instance (not stored in the AssetDatabase), we just reuse it.
    /// </summary>
    private static Mesh GetWritableMesh(MeshFilter mf)
    {
        if (!mf || !mf.sharedMesh) return null;
        var shared = mf.sharedMesh;

#if UNITY_EDITOR
        // If this mesh is part of an asset (FBX, .asset), duplicate it.
        if (AssetDatabase.Contains(shared))
        {
            var clone = Object.Instantiate(shared);
            clone.name = shared.name + "_PivotDoctor";
            Undo.RecordObject(mf, "Assign cloned mesh");
            mf.sharedMesh = clone;
            return clone;
        }
#endif
        // Already an instance in the scene, safe to edit.
        return shared;
    }

    /// <summary>
    /// Move pivot by a local-space offset (delta).
    /// Positive offset moves the pivot in that local direction, while keeping the mesh in place in world space.
    /// </summary>
    private static void MovePivotLocalOffset(Transform t, Vector3 localOffset)
    {
        if (localOffset == Vector3.zero) return;

        var mf = t.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh mesh = GetWritableMesh(mf);
        if (!mesh) return;

        Undo.RegisterCompleteObjectUndo(mesh, "Move Pivot (mesh)");
        Undo.RecordObject(t, "Move Pivot (transform)");

        Vector3[] vertices = mesh.vertices;

        // Move vertices by the *negative* offset so geometry stays put relative to world.
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= localOffset;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Move the transform in world space so it follows the "new origin" / pivot.
        Vector3 worldOffset = t.TransformVector(localOffset);
        t.position += worldOffset;

        EditorUtility.SetDirty(mesh);
    }

    /// <summary>
    /// Rotate pivot orientation around the object's local origin.
    /// Geometry stays visually in the same place; pivot (transform axes) rotates.
    /// </summary>
    private static void RotatePivot(Transform t, Quaternion localRotationDelta)
    {
        if (localRotationDelta == Quaternion.identity) return;

        var mf = t.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh mesh = GetWritableMesh(mf);
        if (!mesh) return;

        Undo.RegisterCompleteObjectUndo(mesh, "Rotate Pivot (mesh)");
        Undo.RecordObject(t, "Rotate Pivot (transform)");

        Vector3[] vertices = mesh.vertices;

        // Rotate the vertices by the INVERSE of the pivot rotation delta.
        // Transform rotation is multiplied by delta, so net world-space result is unchanged.
        Quaternion inv = Quaternion.Inverse(localRotationDelta);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = inv * vertices[i];
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Now rotate the transform's local rotation by the delta.
        t.localRotation = t.localRotation * localRotationDelta;

        EditorUtility.SetDirty(mesh);
    }

    /// <summary>
    /// Centers the pivot to the mesh's bounds center (in local space).
    /// </summary>
    private static void CenterPivotToBounds(Transform t)
    {
        var mf = t.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh mesh = GetWritableMesh(mf);
        if (!mesh) return;

        Undo.RegisterCompleteObjectUndo(mesh, "Center Pivot (mesh)");
        Undo.RecordObject(t, "Center Pivot (transform)");

        mesh.RecalculateBounds();
        Bounds b = mesh.bounds;

        Vector3 center = b.center;
        Vector3[] vertices = mesh.vertices;

        // Shift vertices so that bounds center becomes the origin.
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Move transform to keep mesh visually in same world-space position.
        Vector3 worldOffset = t.TransformVector(center);
        t.position += worldOffset;

        EditorUtility.SetDirty(mesh);
    }

    /// <summary>
    /// Bake the current local rotation into the mesh so that the Transform's rotation becomes identity.
    /// Result: the object looks the same in the scene, but its pivot axes are "right way up".
    /// </summary>
    private static void BakeCurrentRotationAsRightWayUp(Transform t)
    {
        if (!t) return;

        var mf = t.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh mesh = GetWritableMesh(mf);
        if (!mesh) return;

        Quaternion localRot = t.localRotation;
        if (localRot == Quaternion.identity) return;

        Undo.RegisterCompleteObjectUndo(mesh, "Bake Rotation (mesh)");
        Undo.RecordObject(t, "Bake Rotation (transform)");

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;

        // Apply current local rotation to all vertices so we can then zero out the transform rotation.
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = localRot * vertices[i];
        }

        // Rotate normals if they exist
        if (normals != null && normals.Length == vertices.Length)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = localRot * normals[i];
            }
            mesh.normals = normals;
        }

        // Rotate tangents if they exist
        if (tangents != null && tangents.Length == vertices.Length)
        {
            for (int i = 0; i < tangents.Length; i++)
            {
                Vector4 tan = tangents[i];
                Vector3 dir = new Vector3(tan.x, tan.y, tan.z);
                dir = localRot * dir;
                tangents[i] = new Vector4(dir.x, dir.y, dir.z, tan.w);
            }
            mesh.tangents = tangents;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        // If normals existed we rotated them, so no need to recalc; otherwise you *can* recalc if you want:
        if (normals == null || normals.Length == 0)
            mesh.RecalculateNormals();

        // Now zero out the local rotation so pivot axes are reset.
        t.localRotation = Quaternion.identity;

        EditorUtility.SetDirty(mesh);
    }
}
#endif
