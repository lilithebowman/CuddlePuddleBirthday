using System;
using ArchiTech.SDK;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using System.Runtime.CompilerServices;
using UdonSharp;
using Object = UnityEngine.Object;

[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]
// ReSharper disable MemberCanBeMadeStatic.Local

namespace ArchiTech.Umbrella
{
    public enum ZoneTriggerType
    {
        [I18nInspectorName("Range (Sphere)")] RANGE,
        [I18nInspectorName("Area (Box)")] AREA,

        [I18nInspectorName("Collider (On This Object)")]
        COLLIDER
    }

    [System.Flags]
    public enum ZoneTriggerTrackType
    {
        [I18nInspectorName("Player Position")] POSITION = (1 << 0),

        [I18nInspectorName("Player Viewpoint")]
        HEAD = (1 << 1),
        [I18nInspectorName("Right Hand")] RIGHT_HAND = (1 << 2),
        [I18nInspectorName("Left Hand")] LEFT_HAND = (1 << 3),

        [I18nInspectorName("Playspace Origin")]
        ORIGIN = (1 << 4)
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ZoneTrigger : ATTriggerActions
    {
        public ZoneTriggerType triggerType = ZoneTriggerType.RANGE;

        [InspectorName("Trigger Radius"), Min(0)]
        public float triggerRadius = 0.5f;

        [InspectorName("Trigger Size")] public Vector3 triggerArea = Vector3.one;
        public Vector3 triggerCenter = Vector3.zero;
        public bool useScale = true;
        [SerializeField] internal float checkInterval = 0.15f;

        public ZoneTriggerTrackType triggerSource = ZoneTriggerTrackType.HEAD;
        [InspectorName("Force Initial State")] public bool forceState;


        [SerializeField] internal ATTriggerActionType[] enterActionTypes = new ATTriggerActionType[0];
        [SerializeField] internal Object[] enterObjects = new Object[0];
        [SerializeField] internal int[] enterIntOrEnumData = new int[0];
        [SerializeField] internal bool[] enterBoolData = new bool[0];
        [SerializeField] internal float[] enterFloatData = new float[0];
        [SerializeField] internal string[] enterStringData = new string[0];
        [SerializeField] internal string[] enterStringDataExtra = new string[0];
        [SerializeField] internal Object[] enterReferenceData = new Object[0];
        [SerializeField] internal Vector4[] enterVectorData = new Vector4[0];
        [SerializeField] internal VRCUrl[] enterUrlData = new VRCUrl[0];

        [SerializeField] internal ATTriggerActionType[] exitActionTypes = new ATTriggerActionType[0];
        [SerializeField] internal Object[] exitObjects = new Object[0];
        [SerializeField] internal int[] exitIntOrEnumData = new int[0];
        [SerializeField] internal bool[] exitBoolData = new bool[0];
        [SerializeField] internal float[] exitFloatData = new float[0];
        [SerializeField] internal string[] exitStringData = new string[0];
        [SerializeField] internal string[] exitStringDataExtra = new string[0];
        [SerializeField] internal Object[] exitReferenceData = new Object[0];
        [SerializeField] internal Vector4[] exitVectorData = new Vector4[0];
        [SerializeField] internal VRCUrl[] exitUrlData = new VRCUrl[0];

        // canvas group fade transition
        [SerializeField] internal CanvasGroup canvasGroup;
        [SerializeField, Min(0)] internal float canvasGroupFadeTime = 1.5f;
        [SerializeField] internal bool toggleVRCUiPointer = true;


        private Vector3[] positions;
        private Collider interaction;
        private bool active;
        private int colliderZoneStack = 0;
        private float lastFadeTime;
        private bool canvasGroupInstantFade;
        private bool hasEnterActions;
        private bool hasExitActions;
        private bool hasCanvasGroup;
        private bool hasInteraction;
        private bool isFading = false;
        private bool hasPositionTracking = false;
        private bool hasHeadTracking = false;
        private bool hasLeftHandTracking = false;
        private bool hasRightHandTracking = false;
        private bool hasOriginTracking;

        public override void Start()
        {
            if (init) return;
            base.Start();
            // only grab the collider if a VRCUiShape exists on the object
            if (GetComponent(typeof(VRCUiShape)) != null) interaction = GetComponent<Collider>();
            hasEnterActions = enterActionTypes != null;
            hasExitActions = exitActionTypes != null;
            hasInteraction = interaction != null;
            hasCanvasGroup = canvasGroup != null;
            if (hasCanvasGroup) canvasGroup.alpha = 0f; // set to hidden by default, will auto-fade in as needed
            if (hasInteraction) interaction.enabled = false;
            canvasGroupInstantFade = canvasGroupFadeTime == 0;
            lastFadeTime = Time.realtimeSinceStartup;
            SendCustomEventDelayedFrames(nameof(CheckInitialState), 2);
            // when not using unity's physics, wake up the zone watch logic
            if (triggerType != ZoneTriggerType.COLLIDER)
                SendCustomEventDelayedFrames(nameof(UpdateZoneWatch), 2);
            else // disallow non-trigger colliders when in collider trigger type
            {
                foreach (var col in GetComponents<Collider>())
                    col.isTrigger = true;
            }

            var size = 0;
            int v = (int)triggerSource;
            if ((v & (int)ZoneTriggerTrackType.POSITION) != 0)
            {
                hasPositionTracking = true;
                size++;
            }

            if ((v & (int)ZoneTriggerTrackType.HEAD) != 0)
            {
                hasHeadTracking = true;
                size++;
            }

            if ((v & (int)ZoneTriggerTrackType.LEFT_HAND) != 0)
            {
                hasLeftHandTracking = true;
                size++;
            }

            if ((v & (int)ZoneTriggerTrackType.RIGHT_HAND) != 0)
            {
                hasRightHandTracking = true;
                size++;
            }

            if ((v & (int)ZoneTriggerTrackType.ORIGIN) != 0)
            {
                hasOriginTracking = true;
                size++;
            }

            positions = new Vector3[size];
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi p)
        {
            if (p.isLocal && triggerType == ZoneTriggerType.COLLIDER)
            {
                colliderZoneStack++;
                if (colliderZoneStack == 1) UpdateState(true);
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi p)
        {
            if (p.isLocal && triggerType == ZoneTriggerType.COLLIDER)
            {
                colliderZoneStack--;
                if (colliderZoneStack == 0) UpdateState(false);
                if (colliderZoneStack < 0) colliderZoneStack = 0;
            }
        }

        public void Reset()
        {
            colliderZoneStack = 0;
            active = false;
            UpdateState(false);
        }

        public void CheckInitialState()
        {
            Debug($"Checking initial state {forceState}");
            if (forceState) UpdateState(false);
        }

        public void UpdateZoneWatch()
        {
            if (!(gameObject.activeInHierarchy && enabled && VRC.SDKBase.Utilities.IsValid(localPlayer)))
            {
                if (triggerType != ZoneTriggerType.COLLIDER)
                    SendCustomEventDelayedSeconds(nameof(UpdateZoneWatch), checkInterval);
                return;
            }

            var inside = active;
            if (!DelayIsActiveSelf) inside = CheckPositions();

            if (forceState || inside != active && !DelayIsActiveOther || DelayIsComplete)
                UpdateState(inside);

            if (triggerType != ZoneTriggerType.COLLIDER || DelayIsActiveSelf)
            {
                var interval = checkInterval;
                if (DelayIsProcessing) interval = Mathf.Min(DelayTimeRemaining, interval);

                // Trace($"Next ZoneWatch check {DelayTime} ({DelayTimeRemaining}s/{interval}s)");
                SendCustomEventDelayedSeconds(nameof(UpdateZoneWatch), interval);
            }
        }

        private bool CheckPositions()
        {
            bool inside = false;
            int index = 0;
            if (hasPositionTracking) positions[index++] = localPlayer.GetPosition();
            if (hasHeadTracking) positions[index++] = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            if (hasLeftHandTracking) positions[index++] = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            if (hasRightHandTracking) positions[index++] = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            if (hasOriginTracking) positions[index++] = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;

            var t = transform;
            var scale = t.lossyScale;
            var center = useScale ? Vector3.Scale(triggerCenter, scale) : triggerCenter;
            switch (triggerType)
            {
                case ZoneTriggerType.RANGE:
                    var range = triggerRadius;
                    if (useScale) range *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

                    foreach (var position in positions)
                    {
                        // examine the position from the perspective of the range's local space
                        var distance = Vector3.Distance(center, t.InverseTransformPoint(position));
                        inside = distance <= range;
                        if (inside) break;
                    }

                    break;
                case ZoneTriggerType.AREA:
                    var area = useScale ? Vector3.Scale(triggerArea, scale) : triggerArea;
                    Bounds bounds = new Bounds(center, area);
                    foreach (var position in positions)
                    {
                        // examine the position from the perspective of the area's local space
                        inside = bounds.Contains(t.InverseTransformPoint(position));
                        if (inside) break;
                    }

                    break;
            }

            return inside;
        }

        private void UpdateState(bool state)
        {
            if (!DelayIsActiveSelf) active = state;
            forceState = false;
            if (IsDebugEnabled) Debug($"Running state change: {(state ? "On Enter" : "On Exit")}");
            if (hasInteraction && toggleVRCUiPointer) interaction.enabled = active;
            if (active && hasEnterActions)
                HandleActions(enterActionTypes, enterObjects, enterBoolData, enterIntOrEnumData, enterFloatData, enterStringData, enterStringDataExtra, enterReferenceData, enterVectorData, enterUrlData);
            if (!active && hasExitActions)
                HandleActions(exitActionTypes, exitObjects, exitBoolData, exitIntOrEnumData, exitFloatData, exitStringData, exitStringDataExtra, exitReferenceData, exitVectorData, exitUrlData);
            // if fade loop is sleeping, wake it up
            if (hasCanvasGroup && !isFading)
            {
                lastFadeTime = Time.realtimeSinceStartup;
                UpdateFade();
            }
        }

        public void UpdateFade()
        {
            // handle lerp transitions
            if (!hasCanvasGroup) return;

            var nextFadeTime = Time.realtimeSinceStartup;
            // get the delta from the last time this logic was called.
            var fadeDelta = nextFadeTime - lastFadeTime;
            // setup some variables, cheap udon copy operations
            float target, direction;
            if (active)
            {
                target = 1f;
                direction = 1f;
            }
            else
            {
                target = 0f;
                direction = -1f;
            }

            isFading = false; // clear the flag

            if (hasCanvasGroup)
            {
                // if alpha is not at the target yet
                var currentAlpha = canvasGroup.alpha;
                if (currentAlpha != target)
                {
                    // set to target if no fade time provided
                    if (canvasGroupInstantFade) canvasGroup.alpha = target;
                    // or calculate the next fade amount from the current frame differential
                    else
                    {
                        // when fading from 1 to 0, the increment must be negative so multiply by direction
                        var fadeIncrement = fadeDelta / canvasGroupFadeTime * direction;
                        // use Mathf.Clamp01 to prevent the increment from exceeding the normalized boundries
                        canvasGroup.alpha = Mathf.Clamp01(currentAlpha + fadeIncrement);
                    }

                    isFading = true;
                }
            }

            lastFadeTime = nextFadeTime;
            if (isFading) SendCustomEventDelayedFrames(nameof(UpdateFade), 1);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR

        private const string contextMenuBasePath = "CONTEXT/ZoneTrigger";
        private const string persistVisualsMenuItem = contextMenuBasePath + "/Persist Visuals";

        private void OnDrawGizmos()
        {
            bool persistGizmos = Menu.GetChecked(persistVisualsMenuItem);
            if (Selection.activeGameObject != gameObject && !persistGizmos) return;
            var oldColor = Gizmos.color;
            var oldM = Gizmos.matrix;
            var t = transform;
            var position = t.position;
            var scale = t.lossyScale;
            if (triggerType == ZoneTriggerType.RANGE)
            {
                var center = useScale ? Vector3.Scale(triggerCenter, scale) : triggerCenter;
                Gizmos.matrix = Matrix4x4.TRS(position, t.rotation, Vector3.one);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(Vector3.zero, center);
                Gizmos.color = Color.green;
                var radius = triggerRadius;
                if (useScale) radius *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Gizmos.DrawWireSphere(center, radius);
                // wrong. needs better math
                // var oldHColor = Handles.color;
                // Handles.color = Color.green;
                // var cam = SceneView.currentDrawingSceneView.camera;
                // Handles.DrawWireDisc(actualCenter, actualCenter - cam.transform.position, radius);
                // Handles.color = oldHColor;
            }
            else if (triggerType == ZoneTriggerType.AREA)
            {
                Gizmos.matrix = Matrix4x4.TRS(position, t.rotation, useScale ? t.lossyScale : Vector3.one);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(Vector3.zero, triggerCenter);
                Gizmos.color = Color.green;
                var area = triggerArea;
                if (useScale) area = Vector3.Scale(area, scale);
                Gizmos.DrawWireCube(triggerCenter, area);
            }
            else if (triggerType == ZoneTriggerType.COLLIDER)
            {
                Gizmos.matrix = t.localToWorldMatrix;
                Gizmos.color = Color.yellow;
                var colliders = GetComponents<Collider>();
                foreach (var col in colliders)
                {
                    if (col is BoxCollider box) Gizmos.DrawLine(Vector3.zero, box.center);
                    else if (col is SphereCollider shpere) Gizmos.DrawLine(Vector3.zero, shpere.center);
                    else if (col is CapsuleCollider cap) Gizmos.DrawLine(Vector3.zero, cap.center);
                }
            }

            Gizmos.matrix = oldM;
            Gizmos.color = oldColor;
        }

        [MenuItem(persistVisualsMenuItem)]
        public static void TogglePersist()
        {
            Menu.SetChecked(persistVisualsMenuItem, !Menu.GetChecked(persistVisualsMenuItem));
        }
#endif
    }
}