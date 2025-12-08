using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistUIPaginator : ATBehaviour
    {
        public PlaylistUI playlistUI;
        private RectTransform listContainer;
        private int perRow;
        private int perColumn;


        public override void Start()
        {
            if (init) return;
            base.Start();

            if (playlistUI == null)
            {
                SetLogPrefixLabel("<Missing Playlist Ref>");
                Warn("Must specify playlist");
                return;
            }

            listContainer = playlistUI.listContainer;
            Rect max = playlistUI.scrollView.viewport.rect;
            Rect item = ((RectTransform)playlistUI.template.transform).rect;
            perRow = Mathf.FloorToInt(max.width / item.width);
            if (perRow == 0) perRow = 1;
            perColumn = listContainer.childCount / perRow;
            SetLogPrefixLabel(playlistUI.gameObject.name);
        }

        public void _PrevPage()
        {
            if (!init) return;
            seekView(perRow * perColumn * -1 + 1);
        }

        public void _PrevRow()
        {
            if (!init) return;
            seekView(-perRow);
        }

        public void _NextRow()
        {
            if (!init) return;
            seekView(perRow);
        }

        public void _NextPage()
        {
            if (!init) return;
            seekView(perRow * perColumn - 1);
        }

        private void seekView(int shift)
        {
            playlistUI.OUT_INDEX = playlistUI.viewOffset + shift;
            playlistUI.SeekView();
        }
    }
}