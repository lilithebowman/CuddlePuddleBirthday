using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(-1)]
    public class VoteZone : ATBehaviour
    {
        private VoteSkip skipTarget;
        private bool hasSkip = false;
        private bool hasZones = false;

        public override void Start()
        {
            if (init) return;
            base.Start();
            var zones = GetComponents<Collider>();
            hasZones = zones.Length > 0;
            if (hasZones)
                foreach (Collider z in zones)
                    z.isTrigger = true;
        }

        public void _SetVoteSkip(VoteSkip skip)
        {
            skipTarget = skip;
            hasSkip = true;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi p)
        {
            if (!hasSkip) return;
            skipTarget._Enter(p);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi p)
        {
            if (!hasSkip) return;
            skipTarget._Exit(p);
        }
    }
}