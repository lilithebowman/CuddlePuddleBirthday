
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(VRCPickup))]
    public class PickupAutoRespawn : SDK.ATBehaviour
    {
        public float respawnTimeInSeconds;
        public VRC_Pickup.PickupOrientation VROrientation = VRC_Pickup.PickupOrientation.Any;
        public VRC_Pickup.PickupOrientation desktopOrientation = VRC_Pickup.PickupOrientation.Grip;
        [SerializeField] private VRCObjectSync sync;
        private VRCPickup pickup;
        private Vector3 origPos;
        private Quaternion origRot;

        private float respawnTimeout;
        private bool hasSync;
        private bool hasPickup;

        public override void Start()
        {
            if (init) return;
            base.Start();
            // remainder of init code for this class goes here
            if (sync == null) sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            hasSync = sync != null;
            hasPickup = pickup != null;
            var t = transform;
            origPos = t.position;
            origRot = t.rotation;
        }

        public override void OnPickup()
        {
            pickup.orientation = isInVR ? VROrientation : desktopOrientation;
        }

        public override void OnDrop()
        {
            respawnTimer();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player.isLocal) respawnTimer();
        }

        private void respawnTimer()
        {
            respawnTimeout = Time.realtimeSinceStartup + respawnTimeInSeconds;
            SendCustomEventDelayedSeconds(nameof(_AutoRespawn), respawnTimeInSeconds + 1);
        }

        public void _AutoRespawn()
        {
            if (Time.realtimeSinceStartup < respawnTimeout) return;
            // It may have been picked up since it was abandoned
            if (hasPickup && pickup.IsHeld) return;

            if (hasSync) sync.Respawn();
            else
            {
                var t = transform;
                t.position = origPos;
                t.rotation = origRot;
            }
        }

    }
}
