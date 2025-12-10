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
    public LayerMask cellLayerMask;     // set to Ikso3DCells layer

    [Header("Visuals (optional)")]
    public LineRenderer lineRenderer;   // to draw laser beam
    public Color beamColor = Color.white;
    
    [Header("UI / Blocking Objects")]
		public Transform tabletRoot;

    private VRC_Pickup pickup;
    private Ikso3DCell currentHoverCell;
    private string lastColliderName;

    private void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        VRCPlayerApi local = Networking.LocalPlayer;

        // Only interact when held by local player
        if (pickup == null || local == null || pickup.currentPlayer != local)
        {
            currentHoverCell = null;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        // --- Decide ray origin + direction based on VR vs desktop/mobile ---
        Vector3 rayStart;
        Vector3 rayDir;

        if (local.IsUserInVR())
        {
            // VR: use gun orientation
            rayStart = rayOrigin.position;
            rayDir   = rayOrigin.forward;
        }
        else
        {
            // Desktop / Mobile: use view/reticle direction
            var head = local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            rayStart = head.position;
            rayDir   = head.rotation * Vector3.forward;
        }
				
				Ray ray = new Ray(rayStart, rayDir);
				RaycastHit hit;
				
				// FIRST: Raycast ALL LAYERS to detect tablet
				bool hitUI = Physics.Raycast(
						ray,
						out hit,
						maxDistance,
						~0,   // everything
						QueryTriggerInteraction.Collide
				);

				if (hitUI && tabletRoot != null && hit.collider.transform.IsChildOf(tabletRoot))
				{
						// We hit the tablet — block ALL interaction
						currentHoverCell = null;

						if (lineRenderer != null && local.IsUserInVR())
						{
								// OPTIONAL: still draw laser to UI hit point
								lineRenderer.enabled = true;
								lineRenderer.SetPosition(0, rayOrigin.position);
								lineRenderer.SetPosition(1, hit.point);
						}

						return;
				}
				
				// SECOND: Real Ikso3D collision check
				bool hitCells = Physics.Raycast(
						ray,
						out hit,
						maxDistance,
						cellLayerMask,
						QueryTriggerInteraction.Collide
				);
				
        // --- Hover logic ---
        if (hitCells)
        {
						lastColliderName = hit.collider.name;
            Ikso3DCell cell = hit.collider.GetComponentInParent<Ikso3DCell>();
            currentHoverCell = cell;
        }
        else
        {
						lastColliderName = "null";
            currentHoverCell = null;
        }
				
        // --- Line renderer visuals ---
        if (lineRenderer != null)
        {
            if (local.IsUserInVR())
            {
                // Show line only in VR, from gun tip
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, rayOrigin.position);
                lineRenderer.SetPosition(1,
                    hitCells ? hit.point : rayOrigin.position + rayOrigin.forward * maxDistance);
            }
            else
            {
                // Desktop / Mobile: hide line to avoid mismatch confusion
                lineRenderer.enabled = false;
            }
        }
    }

    public override void OnPickupUseDown()
    {
        if (currentHoverCell != null)
        {
            currentHoverCell.OnPointerClick(playerColorIndex);
        }
        Debug.Log("Ikso3DLaserPointer clicked on collider: " + lastColliderName);
    }
}
