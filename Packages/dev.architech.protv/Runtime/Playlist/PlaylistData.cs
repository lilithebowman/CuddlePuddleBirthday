using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(-1)]
    public class PlaylistData : UdonSharpBehaviour
    {
        [SerializeField, FormerlySerializedAs("urls")]
        internal VRCUrl[] mainUrls;

        [SerializeField, FormerlySerializedAs("alts")]
        internal VRCUrl[] alternateUrls;

        [SerializeField] internal string[] titles;
        [SerializeField] internal string[] descriptions;
        [SerializeField] internal string[] tags;
        [SerializeField] internal Sprite[] images;

        [HideInInspector, SerializeField] internal int entriesCount = 0;
        [HideInInspector, SerializeField] internal int imagesCount = 0;
    }
}