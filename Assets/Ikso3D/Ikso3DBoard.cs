using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Ikso3DBoard : UdonSharpBehaviour
{
    [Header("Cells")]
    public Renderer[] cellRenderers;      // 27 renderers, index 0..26

    [Header("Materials")]
    public Material matBlankSphere;       // transparent / empty
    public Material[] playerMaterials;    // 9 materials, 0..8

    [UdonSynced] private int[] cellColors = new int[27]; // -1 = empty, 0..8 = player color

    private const int EMPTY = -1;

    // Simple sync throttle to avoid clogging VRChat's manual sync
    [SerializeField] private float _minSyncInterval = 0.1f; // seconds
    private float _lastSyncTime;
    private string lastSyncReason;

    private void Start()
    {
        // Local init: treat everything as empty until we receive real data via OnDeserialization.
        // This runs on every client, but remote data will override shortly after join.
        if (cellColors == null || cellColors.Length != cellRenderers.Length)
        {
            cellColors = new int[cellRenderers.Length];
        }

        for (int i = 0; i < cellColors.Length; i++)
        {
            cellColors[i] = EMPTY;
            ApplyCellVisual(i);
        }
    }

    public override void OnDeserialization()
    {
        // When network data arrives, just redraw based on synced cellColors
        if (cellColors == null || cellRenderers == null) return;

        int len = Mathf.Min(cellColors.Length, cellRenderers.Length);
        for (int i = 0; i < len; i++)
        {
            ApplyCellVisual(i);
        }
    }

    public void ClickCell(int cellIndex, int playerColorIndex)
    {
        if (cellColors == null || cellRenderers == null) return;
        if (cellIndex < 0 || cellIndex >= cellColors.Length) return;
        if (playerColorIndex < 0 || playerColorIndex >= playerMaterials.Length) return;

        int current = cellColors[cellIndex];
        int newValue = current;

        // RULES:
        // empty      -> becomes this pointer's color
        // own color  -> becomes empty
        // other color -> no-op (can't overwrite)
        if (current == EMPTY)
        {
            newValue = playerColorIndex;
        }
        else if (current == playerColorIndex)
        {
            newValue = EMPTY;
        }
        else
        {
            // Trying to paint over someone else's piece: do absolutely nothing,
            // including NO ownership change and NO RequestSerialization.
            // This avoids unnecessary network spam.
            // Debug.Log($"[Ikso3DBoard] Ignored overwrite attempt on cell {cellIndex}");
            return;
        }

        if (newValue == current)
        {
            // Nothing changed after logic; just bail.
            return;
        }

        // Apply local change
        cellColors[cellIndex] = newValue;
        ApplyCellVisual(cellIndex);

        // Take ownership only when a REAL change occurs
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && !Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(local, gameObject);
        }

        SyncBoard("ClickCell");
    }

    public void ResetBoard()
    {
        if (cellColors == null || cellRenderers == null) return;

        for (int i = 0; i < cellColors.Length; i++)
        {
            cellColors[i] = EMPTY;
            ApplyCellVisual(i);
        }

        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && !Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(local, gameObject);
        }

        SyncBoard("ResetBoard");
    }

    private void SyncBoard(string reason)
    {
        float t = Time.time;
        if (t - _lastSyncTime < _minSyncInterval)
        {
            lastSyncReason = "THROTTLED: " + reason;
            return;
        }

        _lastSyncTime = t;
        lastSyncReason = reason;
        RequestSerialization();
        // Debug.Log($"[Ikso3DBoard] SyncBoard: {reason} at {t}");
    }

    private void ApplyCellVisual(int index)
    {
        if (cellRenderers == null || index < 0 || index >= cellRenderers.Length) return;

        Renderer r = cellRenderers[index];
        if (r == null) return;

        int colorIndex = cellColors[index];

        if (colorIndex == EMPTY)
        {
            r.sharedMaterial = matBlankSphere;
        }
        else if (colorIndex >= 0 && colorIndex < playerMaterials.Length)
        {
            r.sharedMaterial = playerMaterials[colorIndex];
        }
        // else: invalid color index -> leave as-is
    }
}
