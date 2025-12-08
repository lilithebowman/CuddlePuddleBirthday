using ArchiTech.SDK;
using UdonSharp;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistRPC : ATBehaviour
    {
        private Playlist playlist;

        public override void Start()
        {
            if (init) return;
            base.Start();
#if UNITY_2022_3_OR_NEWER
            playlist = GetComponentInParent<Playlist>(true);
#else
            playlist = GetComponentInParent<Playlist>();
#endif
        }

        public void ALL_PRIORITIZE()
        {
            Start();
            playlist.tv._SetPriorityHigh(playlist);
        }
    }
}