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
    public LayerMask cellLayerMask;     // layer of the I3DP# objects

    [Header("Visuals (optional)")]
    public LineRenderer lineRenderer;   // to draw laser beam
    public Color beamColor = Color.white;

    [Header("UI / Blocking Objects")]
    public Transform tabletRoot;        // Root of MyroP's tablet (drag here)

    private VRC_Pickup pickup;

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

        // Only draw laser when held by local player
        if (pickup == null || local == null || pickup.currentPlayer != local)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        // Only show beam in VR to avoid mismatch with desktop reticle
        if (!local.IsUserInVR())
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        if (rayOrigin == null)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        Ray ray = new Ray(origin, dir);
        RaycastHit hit;

        Vector3 endPoint = origin + dir * maxDistance;

        // Raycast against everything to get a visual hit point
        if (Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            endPoint = hit.point;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, endPoint);
        }
    }

    public override void OnPickupUseDown()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local == null)
        {
            return;
        }

        if (pickup != null && pickup.currentPlayer != local)
        {
            return;
        }

        // Decide ray origin/direction based on VR vs desktop/mobile
        Vector3 origin;
        Vector3 dir;

        if (local.IsUserInVR() && rayOrigin != null)
        {
            origin = rayOrigin.position;
            dir = rayOrigin.forward;
        }
        else
        {
            // Desktop / Mobile: use head/reticle
            VRCPlayerApi.TrackingData head = local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            origin = head.position;
            dir = head.rotation * Vector3.forward;
        }

        Ray ray = new Ray(origin, dir);
        RaycastHit hit;

        // STEP 1: check if we hit the tablet first (all layers)
        if (Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (tabletRoot != null)
            {
                Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
                if (hitTransform != null && hitTransform.IsChildOf(tabletRoot))
                {
                    // Hit tablet: do not interact with cells
                    return;
                }
            }
        }

        // STEP 2: raycast only against Ikso3D cell layer
        if (Physics.Raycast(ray, out hit, maxDistance, cellLayerMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider == null)
            {
                return;
            }

            Ikso3DCell cell = hit.collider.GetComponentInParent<Ikso3DCell>();
            if (cell != null)
            {
                cell.OnPointerClick(playerColorIndex);
            }
        }
    }
}
