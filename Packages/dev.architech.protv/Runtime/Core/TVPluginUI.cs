using System;
using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TVPluginUI : ATBehaviour
    {
        [NonSerialized] public bool OUT_STATE;
        [NonSerialized] public int OUT_INDEX = -1;
        [NonSerialized] public float OUT_VALUE = 0;
        [NonSerialized] public string OUT_TEXT;

        public virtual void _ManagerReady() { }
        public abstract void UpdateUI();
    }
}