using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.SDK.Editor")]

namespace ArchiTech.SDK
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-5)]
    public abstract class ATReliableSync : ATBehaviour
    {
        public bool implicitOwnership = true;

        [UdonSynced] private long _revisionCount;
        [UdonSynced] private double _revisionTime;

        private long _localRevisionCount;
        private double _localRevisionTime;
        private long _expectedRevisionCount;
        private double _expectedRevisionTime;

        protected bool retryingSync = false;
        private bool _transferInProgress = false;
        private bool _dataNeedsSync = false;

        public sealed override bool OnOwnershipRequest(VRC.SDKBase.VRCPlayerApi requestingPlayer, VRC.SDKBase.VRCPlayerApi requestedOwner)
        {
            // ownership transfer requests handle on first-come first-serve basis
            if (_transferInProgress) return false;
            _transferInProgress = true;
            return true;
        }

        public sealed override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi newOwner)
        {
            _transferInProgress = false;
            // if the new owner is not the local player but there is data queued for sync, resend the data request.
            if (!newOwner.isLocal && _dataNeedsSync) _RequestData();
        }

        public sealed override void OnPreSerialization()
        {
            _revisionCount++;
            _revisionTime = Networking.GetServerTimeInSeconds();
            _expectedRevisionCount = _revisionCount;
            _expectedRevisionTime = _revisionTime;
            _dataNeedsSync = true;
            _PreSerialization();
        }

        public sealed override void OnPostSerialization(SerializationResult res)
        {
            if (res.success)
            {
                Debug("All good");
                retryingSync = false;
                _PostSerialization();
            }
            else
            {
                Info("Failed to sync, retrying.");
                retryingSync = true;
                SendCustomEventDelayedSeconds(nameof(_RequestData), 1f);
            }
        }

        public sealed override void OnDeserialization()
        {
            // if the increment and network time does not match, skip this
            // we only want to use the latest possible values.
            if (_localRevisionCount > _revisionCount || _localRevisionTime > _revisionTime)
            {
                _DeserializationOutOfDate();
                return;
            }

            if (_dataNeedsSync)
            {
                if (_revisionCount != _expectedRevisionCount && _revisionTime != _expectedRevisionTime)
                {
                    Info("Failed to sync, retrying.");
                    retryingSync = true;
                    SendCustomEventDelayedSeconds(nameof(_RequestData), 1f);
                }
                else _dataNeedsSync = false;
            }

            _localRevisionCount = _revisionCount;
            _localRevisionTime = _revisionTime;
            _Deserialization();
        }

        public virtual void _RequestData()
        {
            if (!IsOwner)
            {
                if (implicitOwnership) Owner = localPlayer;
                else return;
            }

            Debug("Requesting Serialization");
            RequestSerialization();
        }

        protected abstract void _PreSerialization();

        protected abstract void _PostSerialization();

        protected abstract void _Deserialization();

        protected virtual void _DeserializationOutOfDate() { }
    }
}