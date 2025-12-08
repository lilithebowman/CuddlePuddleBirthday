using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    public enum FlingPlayerMoveMode
    {
        LOCATION,
        PLAYSPACE,
        HEAD,
        HAND
    }

    public enum FlingInterpolationMode
    {
        LINEAR,
        BOUNCE,
        PARABOLIC,

        CUBIC
        // , HERMITE 
    }

    public enum FlingPathEndMode
    {
        ONCE,
        PINGPONG,
        LOOP
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Fling : SDK.ATBehaviour
    {
        [HideInInspector] public float totalTravelDistance;

        [HideInInspector, Min(0), I18nInspectorName("Total Travel Time (seconds)")]
        public float totalTravelTime;

        [I18nInspectorName("Relative Anchors")]
        public bool relativeAnchors;

        [I18nInspectorName("Fling Object Instead")]
        public Transform flingObject;

        [I18nInspectorName("Start From Relative Offset")]
        public bool useObjectOffset;

        [I18nInspectorName("Player Movement Style")]
        public FlingPlayerMoveMode movementMode;

        [I18nInspectorName("Allow Jump to Exit")]
        public bool allowJumpExit;

        [I18nInspectorName("Retain Velocity on Exit")]
        public bool retainVelocityOnExit;

        [I18nInspectorName("Interpolate Start Position")]
        public bool smoothEntry;

        [I18nInspectorName("End of Path Mode")]
        public FlingPathEndMode pathEndMode;

        public Transform[] targetObject;
        public Vector3[] targetOffset;
        public Vector3[] targetPosition;
        public Vector3[] startAnchor;
        public Vector3[] endAnchor;

        public FlingInterpolationMode[] interpolationMode;

        // only used by the editor script to enforce tangent alignment
        public bool[] seamless;
        public AudioSource[] sfx;

        public float[] segmentTravelTime;
        [HideInInspector] public float[] segmentLength;


        private Collider[] triggers;
        private Vector3 flingObjectOffset;
        private Vector3 originalOffset;
        private Vector3 currentStart;
        private Transform currentStartObject;
        private Vector3 currentStartOffset;
        private bool currentHasStartObject;
        private Vector3 currentIn;
        private Vector3 currentOut;
        private Vector3 currentEnd;
        private Transform currentEndObject;
        private Vector3 currentEndOffset;
        private bool currentHasEndObject;
        private Quaternion lastRotation;
        private Quaternion currentRotation;
        private FlingInterpolationMode currentMode;
        private float currentTravelTime;
        private float lastTravelTime;
        private float overflow;
        private Vector3 currentVelocity;
        private Vector3 prevVelocity;
        private Vector3 lastPosition;
        private Vector3 prevPosition;

        [HideInInspector] public int currentSegment;

        // [NonSerialized] 
        [I18nInspectorName("Time Scale"), SerializeField]
        internal float timeScale = 1f;

        [I18nInspectorName("Edit Anchors"), SerializeField]
        internal bool _EDITOR_editAnchors;

        [SerializeField] internal Vector3 _EDITOR_lastLocalObjPos;

        [HideInInspector] public float normalizedTime = 0f;
        private bool isReversed = false;
        private float direction = 1f;
        private bool active;
        private bool hasFlingObject;
        private bool hasTriggers;


        public override void Start()
        {
            if (init) return;
            base.Start();

            triggers = GetComponents<Collider>();
            hasTriggers = triggers.Length > 0;
            foreach (Collider t in triggers)
            {
                t.enabled = true;
                t.isTrigger = true;
            }

            isReversed = false;
            hasFlingObject = flingObject != null;
            if (hasFlingObject && useObjectOffset) flingObjectOffset = flingObject.position - transform.position;
        }

        public override void PostLateUpdate()
        {
            if (!active) return;
            float time = Time.smoothDeltaTime;
            if (currentTravelTime == 0)
            {
                // if there is no travel time, just instantly move the player to the next position
                if (hasFlingObject) flingObject.position = currentEnd;
                else Teleport(getPlayerLocationForPosition(currentEnd), false);
                currentVelocity = Vector3.zero;
                nextTarget();
                return;
            }

            float frameDeltaScaled = time * timeScale * direction;
            float travelTimeInFrame = frameDeltaScaled / currentTravelTime;
            float adjustedTime = normalizedTime + travelTimeInFrame;

            // calculate how far over the end of the segment the current frame travels
            var adjustedTimeClamped = Mathf.Clamp(adjustedTime, 0f, 1f);
            overflow = Mathf.Abs(adjustedTimeClamped - adjustedTime);

            // check if the current frame distance exceeds the end of the current segment
            if (overflow > 0)
            {
                // currentVelocity = (lastPosition - prevPosition) / time;
                // swap to next target
                if (adjustedTimeClamped == 0f) prevTarget();
                else if (adjustedTimeClamped == 1f) nextTarget();
                if (!active) return;
                if (currentTravelTime == 0)
                {
                    // if there is no travel time, just instantly move the player to the next position
                    if (hasFlingObject) flingObject.position = currentEnd;
                    else Teleport(getPlayerLocationForPosition(currentEnd), false);
                    // when travel points are instant aka "teleport", nuke the velocity
                    currentVelocity = Vector3.zero;
                    nextTarget();
                    return;
                }

                // un-normalize the remainder of the delta for the current frame
                float frameDeltaRemainder = overflow * currentTravelTime;
                // normalize and adjust the remaining frame distance to the next target
                travelTimeInFrame = frameDeltaRemainder / currentTravelTime;
                adjustedTime = normalizedTime + travelTimeInFrame;
                adjustedTimeClamped = Mathf.Clamp(adjustedTime, 0f, 1f);
            }

            normalizedTime = adjustedTimeClamped;
            if (currentHasStartObject && isReversed) currentStart = currentStartObject.position + currentStartOffset;
            if (currentHasEndObject && !isReversed) currentEnd = currentEndObject.position + currentEndOffset;
            Vector3 pos = currentStart;
            switch (currentMode)
            {
                case FlingInterpolationMode.LINEAR:
                    pos = Vector3.Lerp(currentStart, currentEnd, normalizedTime);
                    break;
                case FlingInterpolationMode.BOUNCE:
                    // cannot cache midPoint because currentStart and currentEnd _might_ have changed since last frame.
                    Vector3 midPoint = Vector3.Lerp(currentStart, currentEnd, 0.5f);
                    float height = currentIn.y - midPoint.y;
                    pos = Algebra.GetParabolicPoint(currentStart, currentEnd, height, normalizedTime);
                    break;
                case FlingInterpolationMode.PARABOLIC:
                    pos = Algebra.GetQuadraticBezierPoint(currentStart, currentIn, currentEnd, normalizedTime, 1f);
                    break;
                case FlingInterpolationMode.CUBIC:
                    pos = Algebra.GetCubicBezierPoint(currentStart, currentIn, currentOut, currentEnd, normalizedTime, 1f);
                    break;
                // case FlingInterpolationMode.HERMITE:
                //     pos = Algebra.GetHermiteCurvePoint(currentStart, currentIn, currentOut, currentEnd, normalizedTime);
                //     break;
            }

            prevVelocity = currentVelocity;
            currentVelocity = (pos - lastPosition) / time;
            prevPosition = lastPosition;
            lastPosition = pos;
            if (hasFlingObject) flingObject.position = pos;
            else Teleport(getPlayerLocationForPosition(pos), true);
        }

        private void Teleport(Vector3 pos, bool remoteLerp)
        {
            doPlayerTeleportTo(pos, remoteLerp);
            localPlayer.SetVelocity(V3ZERO);
        }


#pragma warning disable CS0219
        [SuppressMessage("ReSharper", "ConvertToConstant.Local")]
        private void doPlayerTeleportTo(Vector3 teleportPos, bool remoteLerp)
        {
            /*
                Copyright (c) 2023 @Phasedragon on GitHub
                Additional help by @Nestorboy
                Permission is hereby granted, free of charge, to any person obtaining
                a copy of this software and associated documentation files (the
                "Software"), to deal in the Software without restriction, including
                without limitation the rights to use, copy, modify, merge, publish,
                distribute, sublicense, and/or sell copies of the Software, and to
                permit persons to whom the Software is furnished to do so, subject to
                the following conditions:
                The above copyright notice and this permission notice shall be
                included in all copies or substantial portions of the Software.

                THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
                EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
                MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
                NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
                LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
                OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
                WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
            */
            VRCPlayerApi player = localPlayer;
            Quaternion teleportRot = localPlayer.GetRotation();
            teleportRot = Quaternion.Euler(0, teleportRot.eulerAngles.y, 0);
            // This code only runs in-game because it's broken in ClientSim
#if !UNITY_EDITOR
            var orientation = VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint;
            Vector3 playerPos = player.GetPosition();
            Quaternion playerRot = player.GetRotation();
            Quaternion invPlayerRot = Quaternion.Inverse(playerRot);
            VRCPlayerApi.TrackingData origin = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            Vector3 originPos = origin.position;
            Quaternion originRot = origin.rotation;
            Vector3 offsetPos = originPos - playerPos;
            Quaternion offsetRot = invPlayerRot * originRot;
            teleportPos += teleportRot * invPlayerRot * offsetPos;
            teleportRot *= offsetRot;
#else
            var orientation = VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint;
#endif
            player.TeleportTo(teleportPos, teleportRot, orientation, remoteLerp);
        }
#pragma warning restore CS0219


        public override void OnPlayerTriggerStay(VRCPlayerApi plyr)
        {
            if (!hasFlingObject && plyr.isLocal) _Enter();
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!hasFlingObject && allowJumpExit && active && value)
            {
                _Exit();
                var jumpVelocity = Vector3.up * localPlayer.GetJumpImpulse();
                localPlayer.SetVelocity(getVelocity() + jumpVelocity);
            }
        }

        private Vector3 getVelocity()
        {
            Vector3 vel = Vector3.zero;
            if (retainVelocityOnExit) vel += currentVelocity;
            return vel;
        }

        [PublicAPI]
        public void _Enter()
        {
            // don't allow Enter to be triggered multiple times without corresponding Exit being triggered first
            if (active) return;
            if (IsDebugEnabled) Debug("Enter");
            active = true;
            normalizedTime = 0f;
            if (hasTriggers)
                foreach (var t in triggers)
                    t.enabled = false;
            currentSegment = -1;
            nextTarget();
            Vector3 objPos = transform.position;
            if (!smoothEntry) currentStart = objPos;
            else if (hasFlingObject) currentStart = flingObjectOffset + objPos;
            else currentStart = getTrackingPosition(movementMode);
            originalOffset = currentStart - objPos;
        }

        private Vector3 getPlayerLocationForPosition(Vector3 targetPos)
        {
            Vector3 localPos = localPlayer.GetPosition();
            Vector3 shiftTo = getTrackingPosition(movementMode);
            return targetPos - shiftTo + localPos;
        }

        private Vector3 getTrackingPosition(FlingPlayerMoveMode moveMode)
        {
            switch (moveMode)
            {
                case FlingPlayerMoveMode.PLAYSPACE:
                    return localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
                case FlingPlayerMoveMode.HEAD:
                    return localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                case FlingPlayerMoveMode.HAND:
                    // todo add left hand support
                    return localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                default:
                    return localPlayer.GetPosition();
            }
        }

        private void prevTarget()
        {
            if (currentSegment > -1 && sfx[currentSegment] != null) sfx[currentSegment].Play();
            currentSegment--;
            if (IsDebugEnabled) Debug($"Switching targets to {currentSegment}");
            if (currentSegment <= -1)
            {
                switch (pathEndMode)
                {
                    case FlingPathEndMode.ONCE:
                        _Exit();
                        return;
                    case FlingPathEndMode.PINGPONG:
                        currentSegment++; // retain the same segment
                        direction *= -1f; // flip direction
                        isReversed = direction < 0; // cache comparison
                        return; // same segment, no need to reassign the cached data
                    case FlingPathEndMode.LOOP:
                        currentSegment = targetPosition.Length - 1; // shift to the last segment
                        break;
                }
            }

            normalizedTime = 1f;

            currentEndObject = currentStartObject;
            currentEndOffset = currentStartOffset;
            currentHasEndObject = currentEndObject != null;
            currentEnd = currentStart;

            if (currentSegment == 0)
            {
                // target the transform source with the original offset
                var t = transform;
                currentStartObject = t;
                currentStartOffset = originalOffset;
                currentHasStartObject = true;
                currentStart = t.position + originalOffset;
            }
            else
            {
                // gets target information from previous segment
                var index = currentSegment - 1;
                currentStartObject = targetObject[index];
                currentStartOffset = targetOffset[index];
                currentHasStartObject = currentStartObject != null;
                if (currentHasStartObject) currentStart = currentStartObject.position + currentStartOffset;
                else currentStart = targetPosition[index];
            }

            currentIn = startAnchor[currentSegment];
            currentOut = endAnchor[currentSegment];
            currentMode = interpolationMode[currentSegment];
            currentTravelTime = segmentTravelTime[currentSegment];
        }

        private void nextTarget()
        {
            if (currentSegment > -1 && sfx[currentSegment] != null) sfx[currentSegment].Play();
            currentSegment++;
            if (IsDebugEnabled) Debug($"Switching targets to {currentSegment}");
            if (currentSegment >= targetPosition.Length)
            {
                switch (pathEndMode)
                {
                    case FlingPathEndMode.ONCE:
                        _Exit();
                        return;
                    case FlingPathEndMode.PINGPONG:
                        currentSegment--; // retain the same segment
                        direction *= -1f; // flip direction
                        isReversed = direction < 0; // cache comparison
                        return; // same segment, no need to reassign the cached data
                    case FlingPathEndMode.LOOP:
                        currentSegment = 0;
                        break;
                }
            }

            normalizedTime = 0f;

            currentStartObject = currentEndObject;
            currentStartOffset = currentEndOffset;
            currentStart = currentEnd;
            currentHasStartObject = currentStartObject != null;

            currentEndObject = targetObject[currentSegment];
            currentEndOffset = targetOffset[currentSegment];
            currentHasEndObject = currentEndObject != null;
            currentEnd = targetPosition[currentSegment];

            currentIn = startAnchor[currentSegment];
            currentOut = endAnchor[currentSegment];
            currentMode = interpolationMode[currentSegment];
            currentTravelTime = segmentTravelTime[currentSegment];
        }

        [PublicAPI]
        public void _Exit()
        {
            if (IsDebugEnabled) Debug("Exit");
            active = false;
            if (hasTriggers)
                foreach (var t in triggers)
                    t.enabled = true;
            if (hasFlingObject) flingObject.position = transform.position + originalOffset;
            else
            {
                var vel = getVelocity();
                if (IsDebugEnabled) Debug($"Exiting with velocity {vel}");
                localPlayer.SetVelocity(vel);
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (targetPosition == null || targetPosition.Length == 0) return; // skip when targets are empty
            Gizmos.color = Color.magenta;
            int resolution = 60; // hardcoded placeholder
            // default the start of the arc to the game object's position just as a reference point
            Vector3 from = transform.position;
            Vector3 positionDiff = from - _EDITOR_lastLocalObjPos;
            _EDITOR_lastLocalObjPos = from;
            for (int i = 0; i < targetPosition.Length; i++)
            {
                Transform obj = targetObject[i];
                Vector3 shift = targetOffset[i];
                Vector3 to = targetPosition[i];
                Vector3 inAnchor = startAnchor[i];
                Vector3 outAnchor = endAnchor[i];
                Vector3 lastPoint = from;
                Vector3 nextPoint = lastPoint;
                Vector3 newPositionDiff = Vector3.zero;
                bool targetFocused = obj != null && Selection.activeTransform == obj;
                if (targetFocused)
                {
                    var pos = obj.position;
                    // track the object's positional change from last check
                    newPositionDiff = pos + shift - to;
                    // update target position to object position
                    to = pos + shift;

                    // update anchors based on the relative movement of their respective targets
                    if (relativeAnchors)
                    {
                        inAnchor += positionDiff;
                        positionDiff = newPositionDiff;
                        outAnchor += positionDiff;
                    }
                }

                float weight = 1f;
                // renders the bezier path
                for (int j = 1; j <= resolution; j++)
                {
                    var t = j / (float)resolution;
                    switch (interpolationMode[i])
                    {
                        case FlingInterpolationMode.LINEAR:
                            nextPoint = Vector3.Lerp(from, to, t);
                            break;
                        case FlingInterpolationMode.BOUNCE:
                            float height = inAnchor.y - Vector3.Lerp(from, to, 0.5f).y;
                            nextPoint = Algebra.GetParabolicPoint(from, to, height, t);
                            break;
                        case FlingInterpolationMode.PARABOLIC:
                            nextPoint = Algebra.GetQuadraticBezierPoint(from, inAnchor, to, t, weight);
                            break;
                        case FlingInterpolationMode.CUBIC:
                            nextPoint = Algebra.GetCubicBezierPoint(from, inAnchor, outAnchor, to, t, weight);
                            break;
                        // case FlingInterpolationMode.HERMITE:
                        //     nextPoint = Algebra.GetHermiteCurvePoint(from, inAnchor, outAnchor, to, t);
                        //     break;
                    }

                    Gizmos.DrawLine(lastPoint, nextPoint);
                    lastPoint = nextPoint;
                }

                if (targetFocused)
                {
                    targetPosition[i] = to;
                    if (relativeAnchors)
                    {
                        startAnchor[i] = inAnchor;
                        endAnchor[i] = outAnchor;
                    }
                }

                from = to;
            }
        }
#endif
    }
}