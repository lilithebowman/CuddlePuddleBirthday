using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]
// ReSharper disable Unity.InefficientPropertyAccess

namespace ArchiTech.Umbrella
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(LineRenderer))]
    public class Tether : SDK.ATBehaviour
    {
        private const VRCPlayerApi.TrackingDataType rightHandTracking = VRCPlayerApi.TrackingDataType.RightHand;
        private const VRCPlayerApi.TrackingDataType leftHandTracking = VRCPlayerApi.TrackingDataType.LeftHand;
        private const VRCPlayerApi.TrackingDataType headTracking = VRCPlayerApi.TrackingDataType.Head;
        private VRCPlayerApi.TrackingDataType trackingFrom;

        public float tetherLength = 10f;
        public LayerMask searchMask = -1;
        public float pointerLerpSpeed = 0.8f;


        private LineRenderer line;
        private MeshCollider walkable;
        private LineRenderer pointer;
        private MeshFilter container;
        private ParticleSystem pointerParticles;

        private bool equipped;
        private bool triggerActive;
        private Vector3[] tetherPoints = new Vector3[2];
        private Vector3[] pointerPoints = new Vector3[2];

        private Vector3 startPos;
        private bool hasStartPos;
        private Vector3 endPos;
        private bool hasEndPos;
        private Vector3 scanPos;
        private bool nextLockPosIsEndPos; // used for determining which joint to target next when in rope mode

        private bool hasPointer;
        private bool hasPointerParticles;


        private readonly Vector3 rayDirection = Vector3.forward + new Vector3(0.8f, 0f, 0f);
        private readonly int[] tris = new int[12] { 0, 1, 2, 1, 2, 3, 3, 2, 1, 2, 1, 0 };

        public override void Start()
        {
            if (init) return;
            base.Start();

            line = GetComponent<LineRenderer>();
            walkable = GetComponent<MeshCollider>();
            container = GetComponent<MeshFilter>();
            tetherPoints = new Vector3[2];
            line.positionCount = 2;
            line.SetPositions(tetherPoints);
            walkable.sharedMesh = container.mesh;
            transform.rotation = Quaternion.Euler(90, 0, 0); // force the line to always be facing up. (Z+ local = Y+ global)
            line.alignment = LineAlignment.TransformZ;
            // walkable.gameObject.layer = 1 << 2; // force onto the IgnoreRaycast layer
            var p = transform.Find("Pointer");
            if (p != null)
            {
                pointer = p.GetComponent<LineRenderer>();
                pointerParticles = p.GetComponent<ParticleSystem>();
                hasPointer = pointer != null;
                hasPointerParticles = pointerParticles != null;
                if (hasPointer)
                {
                    pointerPoints = new Vector3[2];
                    pointer.positionCount = 2;
                    pointer.SetPositions(pointerPoints);
                }

                if (hasPointerParticles)
                {
                    var em = pointerParticles.emission;
                    em.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E)) equipped = !equipped;
            if (equipped && triggerActive) checkForTetherSpot();
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!equipped) return;
            if (args.handType == HandType.LEFT) return;
            if (triggerActive != value)
            {
                triggerActive = value;
                if (value) walkable.enabled = false;
                else
                {
                    walkable.enabled = true;
                    lockTether();
                }
            }
        }


        private void checkForTetherSpot()
        {
            // Raycast check for tether spot, cache if matched
            if (IsDebugEnabled) Debug("Checking for tether spot");
            Vector3 pos;
            Vector3 dir;
            if (isInVR)
            {
                VRCPlayerApi.TrackingData data = localPlayer.GetTrackingData(rightHandTracking);
                pos = data.position;
                dir = data.rotation * rayDirection;
                if (hasPointer) pointerPoints[0] = pos;
            }
            else
            {
                VRCPlayerApi.TrackingData data = localPlayer.GetTrackingData(headTracking);
                pos = data.position;
                dir = data.rotation * V3FORWARD;
                if (hasPointer) pointerPoints[0] = Vector3.Lerp(localPlayer.GetPosition(), pos, 0.9f);
            }

            RaycastHit hit;

            // Debug.DrawRay(pos, dir, Color.red);
            // explicitly Raycast for everything except the "IgnoreRaycast" layer
            var nextPos = nextLockPosIsEndPos ? 1 : 0;
            var lastPos = nextLockPosIsEndPos ? 0 : 1;
            if (Physics.Raycast(pos, dir, out hit, tetherLength, searchMask) && Vector3.Distance(hit.point, line.GetPosition(lastPos)) <= tetherLength)
            {
                var hitPos = hit.point;
                scanPos = Vector3.Lerp(scanPos, hitPos, pointerLerpSpeed * Vector3.Distance(hitPos, scanPos));
                if (hasPointerParticles)
                {
                    var t = pointerParticles.transform;
                    var em = pointerParticles.emission;
                    em.enabled = true;
                    t.position = scanPos;
                    t.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                }

                if (hasPointer) pointerPoints[1] = scanPos;
                line.SetPosition(nextLockPosIsEndPos ? 1 : 0, scanPos);
                if (IsDebugEnabled) Debug("Tether Spot Found");
            }
            else
            {
                scanPos = V3ZERO;
                line.SetPosition(0, startPos);
                line.SetPosition(1, endPos);
                if (hasPointerParticles)
                {
                    var em = pointerParticles.emission;
                    em.enabled = false;
                }
            }

            if (hasPointer)
            {
                var r = new Ray(pos, dir);
                pointerPoints[1] = r.GetPoint(0.25f);
                pointer.SetPositions(pointerPoints);
            }
        }

        private void lockTether()
        {
            if (!scanPos.Equals(V3ZERO))
            {
                if (nextLockPosIsEndPos)
                {
                    endPos = scanPos;
                    tetherPoints[1] = scanPos;
                    hasEndPos = true;
                    if (IsWarnEnabled) Warn("Locking End Tether");
                }
                else
                {
                    startPos = scanPos;
                    tetherPoints[0] = scanPos;
                    hasStartPos = true;
                    if (IsWarnEnabled) Warn("Locking Start Tether");
                }

                nextLockPosIsEndPos = !nextLockPosIsEndPos;
            }

            if (IsDebugEnabled) Debug("Clearing Tether Search");
            if (hasPointer)
            {
                pointerPoints[0] = V3ZERO;
                pointerPoints[1] = V3ZERO;
                pointer.SetPositions(pointerPoints);
            }

            if (hasPointerParticles)
            {
                var em = pointerParticles.emission;
                em.enabled = false;
            }

            line.SetPositions(tetherPoints);
            scanPos = V3ZERO;
            if (hasStartPos && hasEndPos) bakeLine();
        }

        private void bakeLine()
        {
            if (IsDebugEnabled) Debug("Baking Walkable Tether");
            Mesh mesh = container.mesh;
            line.BakeMesh(mesh, true);
            Vector3[] verts = mesh.vertices;
            // stupid fuckin mesh.
            for (int i = 0; i < verts.Length; i++)
                verts[i] = transform.InverseTransformPoint(verts[i]);
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.MarkModified();
            // force the mesh collider to recalculate the mesh reference
            walkable.enabled = false;
            walkable.enabled = true;
        }
    }
}