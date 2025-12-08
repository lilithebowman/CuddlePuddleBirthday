
using JetBrains.Annotations;
using UdonSharp;
using VRC.SDKBase;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedAtToggle : ATToggle
    {
        public bool synced = false;
        [UdonSynced] private bool stateSync;
        
        public override void OnPreSerialization() {
            stateSync = state;
            if (IsDebugEnabled) Debug($"Serializing state: {state}");
        }

        public override void OnDeserialization()
        {
            if (IsDebugEnabled) Debug($"Received state: {stateSync}");
            if (synced && state != stateSync)
            {
                if (IsDebugEnabled) Debug($"State changed");
                state = stateSync;
                UpdateObjects();
            }
        }

        [PublicAPI]
        public override void _Activate()
        {
            if (oneWay && initialState != state) return;
            state = !state;
            if (synced)
            {
                Networking.SetOwner(localPlayer, gameObject);
                RequestSerialization();
            }
            UpdateObjects();
            if (state)
                foreach (ATToggle t in siblings)
                    t.SendCustomEvent(nameof(_Reset));
        }

        [PublicAPI]
        public override void _Reset()
        {
            state = initialState;
            if (synced)
            {
                Networking.SetOwner(localPlayer, gameObject);
                RequestSerialization();
            }
            UpdateObjects();
        }
    }
}