using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    /// <summary>
    /// This script detects the position of given transforms compared to their respective target transforms within a proximity.
    /// Once all the transforms are within the target proximities, the toggle logic is then activated.
    /// </summary>
    public sealed class ObjectProximityAtToggle : ATToggle
    {
        public Transform[] sources;
        public Transform[] targets;
        public float proximity = 1f;
        public bool resetWhenOutsideProximity;

        public override void Start()
        {
            if (init) return;
            base.Start();
            SendCustomEventDelayedSeconds(nameof(_CheckProximities), 3f);
        }

        public void _CheckProximities()
        {
            SendCustomEventDelayedSeconds(nameof(_CheckProximities), 3f);
            int proxDetected = 0;
            int len = sources.Length;
            for (int i = 0; i < len; i++)
            {
                float dist = Vector3.Distance(sources[i].position, targets[i].position);
                if (dist <= proximity) proxDetected++;
            }

            if (state != initialState)
            {
                if (!oneWay && proxDetected != len)
                {
                    if (resetWhenOutsideProximity) _Reset();
                    else _Activate();
                }
            }
            else if (proxDetected == len) _Activate();
        }
    }
}