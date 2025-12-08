using UdonSharp;
using UnityEngine;

namespace ArchiTech.ProTV
{
    [
        UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync),
        RequireComponent(typeof(Renderer)),
        DefaultExecutionOrder(-9997),
        DisallowMultipleComponent
    ]
    public class RTGIUpdater : UdonSharpBehaviour
    {
        [SerializeField] private bool runOnMobile = true;
        private Renderer _renderer;

#if UNITY_STANDALONE
        protected const bool isPC = true;
#else
        protected const bool isPC = false;
#endif

        private void Start()
        {
            enabled = isPC || runOnMobile;
            _renderer = GetComponent<Renderer>();
        }

        private void LateUpdate() => _renderer.UpdateGIMaterials();
    }
}