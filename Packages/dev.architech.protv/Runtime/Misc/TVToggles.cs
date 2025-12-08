using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TVToggles : TVPlugin
    {
        [SerializeField, InspectorName("Game Objects")]
        internal GameObject[] superGameObjects = new GameObject[0];

        [SerializeField, InspectorName("Colliders")]
        internal Collider[] superColliders = new Collider[0];

        [SerializeField, InspectorName("Game Objects")]
        internal GameObject[] authorizedGameObjects = new GameObject[0];

        [SerializeField, InspectorName("Colliders")]
        internal Collider[] authorizedColliders = new Collider[0];

        [SerializeField, InspectorName("Game Objects")]
        internal GameObject[] unauthorizedGameObjects = new GameObject[0];

        [SerializeField, InspectorName("Colliders")]
        internal Collider[] unauthorizedColliders = new Collider[0];

        public override void Start()
        {
            if (init) return;
            base.Start();
        }

        private void OnEnable()
        {
            if (tv == null || !tv.isReady) return;
            checkAuthState();
        }

        public override void _TvReady() => checkAuthState();

        public override void _TvAuthChange() => checkAuthState();

        private void checkAuthState()
        {
            var auth = tv._IsAuthorized();
            foreach (var go in unauthorizedGameObjects)
                if (go)
                    go.SetActive(!auth);
            foreach (var col in unauthorizedColliders)
                if (col)
                    col.enabled = !auth;
            foreach (var go in authorizedGameObjects)
                if (go)
                    go.SetActive(auth);
            foreach (var col in authorizedColliders)
                if (col)
                    col.enabled = auth;
            var super = tv._IsSuperAuthorized();
            foreach (var go in superGameObjects)
                if (go)
                    go.SetActive(super);
            foreach (var col in superColliders)
                if (col)
                    col.enabled = super;
        }
    }
}