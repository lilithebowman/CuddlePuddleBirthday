using System;
using UnityEngine;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.SDK.Editor")]

namespace ArchiTech.SDK
{
    /// <summary>
    /// Base class for easily defining a listener. This will automatically register itself to the provided EventManager reference if it exists.
    /// </summary>
    [DisallowMultipleComponent, Obsolete("Use ATEventHandler instead.")]
    public abstract class ATEventListener : ATBehaviour
    {
        /// <summary><para>
        /// A proxy getter which MUST be implemented by any subclasses.
        /// This MUST return some reference that is derived from the ATEventManager.
        /// <example>
        /// Typically it will just return a local field reference like so:
        /// <code>
        /// public class CustomEventListener : ATEventListener {
        ///     [SerializeField] private CustomEventManager myEventManager;
        ///     protected override CustomEventManager EventManager { get => myEventManager; }
        ///     // etc...
        /// }
        /// </code>
        /// </example>
        /// This is done to allow sub-classes to expose their own custom field instead of the base field.
        /// </para>
        /// </summary>
        protected abstract ATEventManager EventManager { get; set; }

        /// <summary><para>
        /// A getter that can optionally be implemented by any subclasses.
        /// This is a signed byte (-128 to +127) specifying where it should be stored in relation to other listeners on the same manager.
        /// Smaller values (-128 being the smallest) means the listener gets HIGHER priority than other listeners.
        /// Listeners with HIGHER priority recieve events from the manager before others with lower priorities (bigger values).
        /// If this is not overriden in a subclass, it's default value will be 0 (middle priority)
        /// <example>
        /// <code>
        /// protected override sbyte Priority { get => -10; }
        /// </code>
        /// </example>
        /// </para>
        /// </summary>
        public virtual sbyte Priority => 0;

        public override void Start()
        {
            if (init) return;
            base.Start();
            if (EventManager != null)
            {
                EventManager._RegisterListener(this, Priority);
#if VUDON_LOGGER
                if (Logger == null) Logger = EventManager.Logger;
#endif
                if (EventManager.LogLevelOverride)
                {
                    LoggingLevel = EventManager.LoggingLevel;
                }
            }
        }

        /// <summary>
        /// This event will be called by the event manager when the manager's initialization phase has been completed.
        /// If this event listener registers after the manager's init phase is done, it will be called immediately.
        /// This typically happens if the listener is disabled by default and then enabled at some point later during the runtime.
        /// </summary>
        public virtual void _ManagerReady() { }

        public void _StartListening()
        {
            if (init) EventManager._EnableListener(this);
        }

        public void _StopListening()
        {
            if (init) EventManager._DisableListener(this);
        }

        public void _ChangeEventManager(ATEventManager m)
        {
            EventManager._UnregisterListener(this);
            EventManager = m;
            init = false;
            Start();
        }
    }
}