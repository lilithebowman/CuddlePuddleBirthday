using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(VRC_Pickup))]
public class Ikso3DLaserPointer : UdonSharpBehaviour
{
    [Header("Pointer Settings")]
    public int playerColorIndex;        // 0..8, matching materials
    public Transform rayOrigin;         // where the laser shoots from
    public float maxDistance = 10f;
    public LayerMask cellLayerMask;     // set to Ikso3DCells / Walkthrough layer

    [Header("Visuals (optional)")]
    public LineRenderer lineRenderer;   // to draw laser beam
    public Color beamColor = Color.white;
    
    [Header("UI / Blocking Objects")]
    public Transform tabletRoot;        // Root of MyroP's tablet (drag in Inspector)

    private VRC_Pickup pickup;
    private Ikso3DCell currentHoverCell; // kept for possible future hover effects
    private string lastColliderName;

    private void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
        }
    }

    private void Update()
    {
        VRCPlayerApi local = Networking.LocalPlayer;

        // Only update visuals when held by local player
        if (pickup == null || local == null || pickup.currentPlayer != local)
        {
            currentHoverCell = null;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        // Desktop / Mobile: no beam (to avoid reticle/beam mismatch)
        if (!local.IsUserInVR())
        {
            currentHoverCell = null;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        // --- VR beam from gun tip ---
        Vector3 rayStart = rayOrigin.position;
        Vector3 rayDir   = rayOrigin.forward;

        Ray ray = new Ray(rayStart, rayDir);
        RaycastHit hit;

        // First: raycast against ALL layers to find what is visually in front (tablet, walls, etc.)
        bool hitUI = Physics.Raycast(
            ray,
            out hit,
            maxDistance,
            ~0,   // everything
            QueryTriggerInteraction.Collide
        );

        Vector3 endPoint = rayStart + rayDir * maxDistance;

        if (hitUI)
        {
            endPoint = hit.point;

            // If we hit the tablet, we still draw the beam to it, but don't treat it as a cell
            if (tabletRoot != null && hit.collider.transform.IsChildOf(tabletRoot))
            {
                currentHoverCell = null;
            }
            else
            {
                // Optionally check if we're hovering a cell (for future hover effects)
                if (((1 << hit.collider.gameObject.layer) & cellLayerMask.value) != 0)
                {
                    Ikso3DCell cell = hit.collider.GetComponentInParent<Ikso3DCell>();
                    currentHoverCell = cell;
                    lastColliderName = hit.collider.name;
                }
                else
                {
                    currentHoverCell = null;
                    lastColliderName = "non-cell";
                }
            }
        }
        else
        {
            currentHoverCell = null;
            lastColliderName = "none";
        }

        // Line renderer visuals (VR only)
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
            lineRenderer.SetPosition(0, rayOrigin.position);
            lineRenderer.SetPosition(1, endPoint);
        }
    }

    public override void OnPickupUseDown()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        if (pickup == null || local == null || pickup.currentPlayer != local)
        {
            return; // Not our Use press
        }

        // Decide ray origin + direction based on VR vs desktop/mobile
        Vector3 rayStart;
        Vector3 rayDir;

        if (local.IsUserInVR())
        {
            rayStart = rayOrigin.position;
            rayDir   = rayOrigin.forward;
        }
        else
        {
            // Desktop / Mobile: use head/reticle
            var head = local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            rayStart = head.position;
            rayDir   = head.rotation * Vector3.forward;
        }

        Ray ray = new Ray(rayStart, rayDir);
        RaycastHit hit;

        // STEP 1: does the ray hit the tablet first?
        bool hitUI = Physics.Raycast(
            ray,
            out hit,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        if (hitUI && tabletRoot != null && hit.collider.transform.IsChildOf(tabletRoot))
        {
            // We aimed at the tablet: let the tablet handle it, do NOT click cells
            lastColliderName = "tablet:" + hit.collider.name;
            Debug.Log("Ikso3DLaserPointer click blocked by tablet: " + lastColliderName);
            return;
        }

        // STEP 2: raycast only against Ikso3D cells
        bool hitCell = Physics.Raycast(
            ray,
            out hit,
            maxDistance,
            cellLayerMask,
            QueryTriggerInteraction.Collide
        );

        if (!hitCell)
        {
            lastColliderName = "no-cell-hit";
            Debug.Log("Ikso3DLaserPointer clicked but hit no cell");
            return;
        }

        Ikso3DCell cellHit = hit.collider.GetComponentInParent<Ikso3DCell>();
        if (cellHit == null)
        {
            lastColliderName = "cellLayer-no-Ikso3DCell:" + hit.collider.name;
            Debug.Log("Ikso3DLaserPointer clicked on collider without Ikso3DCell: " + lastColliderName);
            return;
        }

        lastColliderName = hit.collider.name;
        cellHit.OnPointerClick(playerColorIndex);
        Debug.Log("Ikso3DLaserPointer clicked on collider: " + lastColliderName);
    }
}
