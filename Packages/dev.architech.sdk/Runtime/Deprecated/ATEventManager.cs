using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ArchiTech.SDK.Editor")]

namespace ArchiTech.SDK
{
    internal enum ATPriorityMode
    {
        FIRST,
        HIGH,
        LOW,
        LAST
    }

    /// <summary>
    /// Base class for managing multiple listeners, sending events and assigning variables.
    /// </summary>
    [DisallowMultipleComponent, Obsolete("Use ATEventHandler instead.")]
    public abstract class ATEventManager : ATBehaviour
    {
        private readonly System.Type usbtype = typeof(UdonSharpBehaviour);

        [InspectorName("Override Subscriber Logging"), Tooltip("Should the max log level of this event manager override the max log level of all registered listeners?")]
        public bool LogLevelOverride = false;

        // === Udon Input Data Variables ===

        /// <summary>
        /// When interfacing with this class from outside of the U# context (UdonGraph/CyanTriggers/etc),
        /// assign this variable before calling events that utilize this variable. 
        /// </summary>
        [System.NonSerialized] public UdonSharpBehaviour IN_LISTENER;

        /// <summary>
        /// When interfacing with this class from outside of the U# context (UdonGraph/CyanTriggers/etc),
        /// assign this variable before calling events that utilize this variable. 
        /// </summary>
        [System.NonSerialized] public sbyte IN_PRIORITY;

        // === Event Variables === 

        // Storage for all event listener behaviors. 
        // All possible events that this TV triggers will be sent to ALL targets in order of addition
        // Stored as UdonSharpBehaviour, because outside of the C# context of U#, 
        // all UdonSharpBehaviours are just regular UdonBehaviours
        protected UdonSharpBehaviour[] _eventListeners;
        protected UdonSharpBehaviour[] _skipListeners;
        protected sbyte[] _eventListenerWeights;
        protected bool _sendEvents = false;
        protected bool _managerInit = false;
        protected bool runningEvents = false;
        private string currentEvent = null;
        public string ActiveEvent => currentEvent;

        public override void Start()
        {
            if (init) return;
            base.Start();
            // delay a few frames just in case all the udon doesn't finish initializing within the first couple.
            SendCustomEventDelayedFrames(nameof(_PostStartInit), 3);
        }

        /// <summary>
        /// Do not call directly. This is called via delayed custom event to trigger the <see cref="ArchiTech.SDK.ATEventListener._ManagerReady"/> event on all listeners.
        /// This call is delayed such that any listeners can complete their own Start phase before the ready event is called.
        /// </summary>
        public void _PostStartInit()
        {
            if (_managerInit) return; // prevent accidental duplicate calls to the init event
            _managerInit = true;
            _sendEvents = _eventListeners != null && _eventListeners.Length > 0;
            if (_sendEvents) SendManagedEvent(nameof(ATEventListener._ManagerReady));
        }

        #region Inheritence Events

        /// <summary>
        /// Called after a new listener is assigned.
        /// </summary>
        /// <param name="listener">the behaviour that has been acted upon.</param>
        /// <param name="priority">the priority of the newly added behaviour</param>
        protected virtual void _OnListenerRegistered(UdonSharpBehaviour listener, sbyte priority) { }

        /// <summary>
        /// Called after an existing listener has been removed.
        /// </summary>
        /// <param name="listener">the behaviour that has been acted upon.</param>
        protected virtual void _OnListenerUnregistered(UdonSharpBehaviour listener) { }

        /// <summary>
        /// Called after a listener has been [re]enabled.
        /// </summary>
        /// <param name="listener">the behaviour that has been acted upon.</param>
        protected virtual void _OnListenerEnabled(UdonSharpBehaviour listener) { }

        /// <summary>
        /// Called after a listener has been disabled.
        /// </summary>
        /// <param name="listener">the behaviour that has been acted upon.</param>
        protected virtual void _OnListenerDisabled(UdonSharpBehaviour listener) { }

        /// <summary>
        /// Called when a listener's position in the internal listeners list has changed.
        /// </summary>
        /// <param name="listener">the behaviour that has been acted upon.</param>
        protected virtual void _OnListenerPriorityChange(UdonSharpBehaviour listener) { }

        #endregion

        #region USharp API

        /// <summary>
        /// Use this method to add a listener to the event propogation system
        /// Useful for attaching multiple control panels or behaviors for various side effects to happen.
        /// </summary>
        /// <param name="listener">the behaviour to register as a listener.</param>
        /// <param name="priority">the optional value for specifying priority of the listener in relation to the others.</param>
        [PublicAPI]
        public void _RegisterListener(UdonSharpBehaviour listener, sbyte priority = 0)
        {
            if (listener == null) return; // called without setting the behavior
            _sendEvents = true;
            if (_eventListeners == null)
            {
                _eventListeners = new UdonSharpBehaviour[0];
                _skipListeners = new UdonSharpBehaviour[0];
                _eventListenerWeights = new sbyte[0];
            }

            if (System.Array.IndexOf(_eventListeners, listener) > -1)
            {
                if (IsInfoEnabled) Info($"Event listener on {listener.gameObject.name} has already been registered.");
                return;
            }

            var index = 0;
            for (; index < _eventListenerWeights.Length; index++)
            {
                if (priority < _eventListenerWeights[index]) break;
            }

            if (IsDebugEnabled) Debug($"Expanding event register to {_eventListeners.Length + 1}: Adding {listener.gameObject.name}");

            var targets = _eventListeners;
            var skips = _skipListeners;
            var weights = _eventListenerWeights;
            _eventListeners = new UdonSharpBehaviour[targets.Length + 1];
            _skipListeners = new UdonSharpBehaviour[skips.Length + 1];
            _eventListenerWeights = new sbyte[weights.Length + 1];
            var mvIndex = index + 1;
            var mvLength = targets.Length - index;
            // copy over the front half of the arrays
            System.Array.Copy(targets, 0, _eventListeners, 0, index);
            System.Array.Copy(skips, 0, _skipListeners, 0, index);
            System.Array.Copy(weights, 0, _eventListenerWeights, 0, index);
            // copy over the back half of the arrays
            System.Array.Copy(targets, index, _eventListeners, mvIndex, mvLength);
            System.Array.Copy(skips, index, _skipListeners, mvIndex, mvLength);
            System.Array.Copy(weights, index, _eventListenerWeights, mvIndex, mvLength);
            // insert the data in the target index
            _eventListeners[index] = listener;
            _eventListenerWeights[index] = priority;
            _OnListenerRegistered(listener, priority);
            // forward the ready state for registrations that happen after the init phase
            if (_managerInit) SendTargetedEvent(nameof(ATEventListener._ManagerReady), listener);
        }

        /// <summary>
        /// Removes a given listener from the registry. 
        /// </summary>
        /// <param name="listener">the behaviour to unregister as a listener.</param>
        [PublicAPI]
        public void _UnregisterListener(UdonSharpBehaviour listener)
        {
            if (listener == null) return; // called without setting the behavior
            var index = System.Array.IndexOf(_eventListeners, listener);
            if (index == -1) index = System.Array.IndexOf(_skipListeners, listener); // check for if it is disabled instead
            if (index > -1)
            {
                if (IsDebugEnabled) Debug($"Reducing event register to {_eventListeners.Length - 1}: Removing {listener.gameObject.name}");
                _eventListeners = (UdonSharpBehaviour[])ATUtility.RemoveArrayItem(_eventListeners, index, usbtype);
                _skipListeners = (UdonSharpBehaviour[])ATUtility.RemoveArrayItem(_skipListeners, index, usbtype);
                _eventListenerWeights = (sbyte[])ATUtility.RemoveArrayItem(_eventListenerWeights, index, usbtype);
            }

            _sendEvents = _eventListeners.Length > 0;
            _OnListenerUnregistered(listener);
        }

        /// <summary>
        /// Enables a given listener. This will make the listener receive events or variables again after having been disabled.
        /// </summary>
        /// <param name="listener">the listener to enable.</param>
        [PublicAPI]
        public void _EnableListener(UdonSharpBehaviour listener)
        {
            if (listener == null) return;
            var index = System.Array.IndexOf(_skipListeners, listener);
            if (index > -1)
            {
                if (IsDebugEnabled) Debug($"Enabling listener {listener.gameObject.name}");
                _eventListeners[index] = listener;
                _skipListeners[index] = null;
                _OnListenerEnabled(listener);
            }
        }

        /// <summary>
        /// Disable a given listener. This will make the listener temporarily not receive any events or variables.
        /// </summary>
        /// <param name="listener">the listener to disable.</param>
        [PublicAPI]
        public void _DisableListener(UdonSharpBehaviour listener)
        {
            if (listener == null) return;
            var index = System.Array.IndexOf(_eventListeners, listener);
            if (index > -1)
            {
                if (IsDebugEnabled) Debug($"Disabling listener {listener.gameObject.name}");
                _eventListeners[index] = null;
                _skipListeners[index] = listener;
                _OnListenerDisabled(listener);
            }
        }

        /// <summary>
        /// Modifies the weight of the given listener and shifts the priority to the nearest group.
        /// Specifically if the new priority is greater than the old one, <see cref="ArchiTech.SDK.ATEventManager._SetPriorityLow(UdonSharpBehaviour)"/> is called for the given listener.
        /// Otherwise if the new priority is less than the old one, <see cref="ArchiTech.SDK.ATEventManager._SetPriorityHigh(UdonSharpBehaviour)"/> is called for the given listener.
        /// You can pass in the optional <c>noShift</c> flag as true to prevent the priority shift from occurring.
        /// </summary>
        /// <param name="listener">the listener to update priority for.</param>
        /// <param name="priority">the value for specifying priority of the listener in relation to the others.</param>
        /// <param name="noShift">an optional flag to prevent the listener from shifting the priority automatically</param>
        public void _UpdatePriority(UdonSharpBehaviour listener, sbyte priority, bool noShift = false)
        {
            if (listener == null) return;
            var index = System.Array.IndexOf(_eventListeners, listener);
            if (index == -1) index = System.Array.IndexOf(_skipListeners, listener);
            if (index > -1)
            {
                if (IsDebugEnabled) Debug($"Updating listener priority for {listener.gameObject.name} to {priority}");
                var oldWeight = _eventListenerWeights[index];
                _eventListenerWeights[index] = priority;
                if (!noShift && oldWeight != priority)
                {
                    if (oldWeight < priority) _SetPriorityLow(listener);
                    else _SetPriorityHigh(listener);
                }
            }
        }

        /// <summary>
        /// Modify the priority of the listener to the very first regardless of other listener priorities.
        /// <example>
        /// Given 5 listeners with the following priorities: Listener1:20, Listener2:128, Listener3:128, Listener4:128, Listener5:190
        /// If we call <c>_SetPriorityFirst(Listener3)</c>, the resulting order would be: Listener3:128, Listener1:20, Listener2:128, Listener3:128, Listener5:190
        /// </example>
        /// </summary>
        /// <param name="listener">the listener to modify the priority of.</param>
        [PublicAPI]
        public void _SetPriorityFirst(UdonSharpBehaviour listener) => setPriority(listener, ATPriorityMode.FIRST);

        /// <summary>
        /// Modify the priority of the listener to be the first listener in the list respective to it's assigned priority.
        /// <example>
        /// Given 5 listeners with the following priorities: Listener1:20, Listener2:128, Listener3:128, Listener4:128, Listener5:190
        /// If we call <c>_SetPriorityHigh(Listener3)</c>, the resulting order would be: Listener1:20, Listener3:128, Listener2:128, Listener4:128, Listener5:190
        /// </example>
        /// </summary>
        /// <param name="listener">the listener to modify the priority of.</param>
        [PublicAPI]
        public void _SetPriorityHigh(UdonSharpBehaviour listener) => setPriority(listener, ATPriorityMode.HIGH);

        /// <summary>
        /// Modify the priority of the listener to be the last listener in the list respective to it's assigned priority.
        /// <example>
        /// Given 5 listeners with the following priorities: Listener1:20, Listener2:128, Listener3:128, Listener4:128, Listener5:190
        /// If we call <c>_SetPriorityLow(Listener3)</c>, the resulting order would be: Listener1:20, Listener2:128, Listener4:128, Listener3:128, Listener5:190
        /// </example>
        /// </summary>
        /// <param name="listener">the listener to modify the priority of.</param>
        [PublicAPI]
        public void _SetPriorityLow(UdonSharpBehaviour listener) => setPriority(listener, ATPriorityMode.LOW);

        /// <summary>
        /// Modify the priority of the listener to the very last regardless of other listener priorities.
        /// <example>
        /// Given 5 listeners with the following priorities: Listener1:20, Listener2:128, Listener3:128, Listener4:128, Listener5:190
        /// If we call <c>_SetPriorityLast(Listener3)</c>, the resulting order would be: Listener1:20, Listener2:128, Listener4:128, Listener5:190, Listener3:128
        /// </example>
        /// </summary>
        /// <param name="listener">the listener to modify the priority of. <see cref="IN_LISTENER"/></param>
        [PublicAPI]
        public void _SetPriorityLast(UdonSharpBehaviour listener) => setPriority(listener, ATPriorityMode.LAST);

        #endregion

        #region Generic Udon API

        public void _RegisterListener()
        {
            _RegisterListener(IN_LISTENER, IN_PRIORITY);
            IN_LISTENER = null;
            IN_PRIORITY = 0;
        }

        public void _UnregisterListener()
        {
            _UnregisterListener(IN_LISTENER);
            IN_LISTENER = null;
        }

        public void _EnableListener()
        {
            _EnableListener(IN_LISTENER);
            IN_LISTENER = null;
        }

        public void _DisableListener()
        {
            _DisableListener(IN_LISTENER);
            IN_LISTENER = null;
        }

        public void _UpdatePriority()
        {
            _UpdatePriority(IN_LISTENER, IN_PRIORITY);
            IN_LISTENER = null;
            IN_PRIORITY = 0;
        }

        public void _SetPriorityFirst()
        {
            _SetPriorityFirst(IN_LISTENER);
            IN_LISTENER = null;
        }

        public void _SetPriorityHigh()
        {
            _SetPriorityHigh(IN_LISTENER);
            IN_LISTENER = null;
        }

        public void _SetPriorityLow()
        {
            _SetPriorityLow(IN_LISTENER);
            IN_LISTENER = null;
        }

        public void _SetPriorityLast()
        {
            _SetPriorityLast(IN_LISTENER);
            IN_LISTENER = null;
        }

        #endregion

        #region Helpers

        private void setPriority(UdonSharpBehaviour listener, ATPriorityMode mode)
        {
            if (listener == null) return;
            if (_eventListeners == null)
            {
                _eventListeners = new UdonSharpBehaviour[0];
                _skipListeners = new UdonSharpBehaviour[0];
                _eventListenerWeights = new sbyte[0];
            }

            var oldIndex = System.Array.IndexOf(_eventListeners, listener);
            if (oldIndex == -1) oldIndex = System.Array.IndexOf(_skipListeners, listener);
            if (oldIndex == -1)
            {
                Error("Unable to find matching event listener. Please ensure the behaviour has been registered first.");
                return;
            }

            if (IsInfoEnabled) Debug($"Updating priority for {listener.gameObject.name}");
            var newIndex = getNewPriorityIndex(oldIndex, mode);
            if (newIndex == oldIndex)
            {
                Debug("No priority change required. Skipping");
                return;
            }

            // detect left vs right vs no shifting
            int left = oldIndex, right = newIndex, shift = 0;
            if (newIndex < oldIndex)
            {
                left = newIndex;
                right = oldIndex - 1;
                shift = 1;
            }
            else if (oldIndex < newIndex)
            {
                left = oldIndex + 1;
                right = newIndex;
                shift = -1;
            }

            listener = _eventListeners[oldIndex];
            var skip = _skipListeners[oldIndex];
            var weight = _eventListenerWeights[oldIndex];
            // shift the elements between the old and new indexes into their new positions
            var index = left + shift;
            var len = right - left + 1;
            System.Array.Copy(_eventListeners, left, _eventListeners, index, len);
            System.Array.Copy(_skipListeners, left, _skipListeners, index, len);
            System.Array.Copy(_eventListenerWeights, left, _eventListenerWeights, index, len);
            // update the values for the new index to the values at the old index
            _eventListeners[newIndex] = listener;
            _skipListeners[newIndex] = skip;
            _eventListenerWeights[newIndex] = weight;

            _OnListenerPriorityChange(listener);
            if (IsDebugEnabled) logBehaviourOrder();
        }

        private int getNewPriorityIndex(int oldIndex, ATPriorityMode mode)
        {
            int len = _eventListenerWeights.Length;
            int newIndex = oldIndex;
            int oldWeight;
            switch (mode)
            {
                case ATPriorityMode.FIRST:
                    newIndex = 0;
                    break;
                case ATPriorityMode.HIGH:
                    oldWeight = _eventListenerWeights[oldIndex];
                    for (; newIndex > -1; newIndex--)
                        if (_eventListenerWeights[newIndex] < oldWeight)
                        {
                            // need to actually be after the checked index, fix off-by-one
                            newIndex++;
                            break;
                        }
                    if (newIndex == -1)
                        newIndex = 0; // all weights to the left were the same value. Set to start of array index.
                    break;
                case ATPriorityMode.LOW:
                    oldWeight = _eventListenerWeights[oldIndex];
                    for (; newIndex < len; newIndex++)
                        if (_eventListenerWeights[newIndex] > oldWeight)
                            break;
                    if (newIndex == len)
                        newIndex = len - 1; // all weights to the right were the same value. Set to end of array index.
                    break;
                case ATPriorityMode.LAST:
                    newIndex = len - 1;
                    break;
            }

            return newIndex;
        }

        private void logBehaviourOrder()
        {
            string txt = "Priorities: ";
            for (int i = 0; i < _eventListeners.Length; i++)
            {
                var l = _eventListeners[i];
                if (l == null) l = _skipListeners[i];
                var n = l.gameObject.name;
                var p = _eventListenerWeights[i];
                txt += $"{n} [{p}], ";
            }

            Debug(txt);
        }

        #endregion

        #region Listener Actions

        /// <summary>
        /// Sends a given custom event to the desired listener behaviour.
        /// </summary>
        /// <param name="eventName">the given event to send to each of the behaviours</param>
        /// <param name="listener">the listener that will recieve the event</param>
        [RecursiveMethod]
        protected void SendTargetedEvent(string eventName, UdonSharpBehaviour listener)
        {
            if (_sendEvents)
            {
                runningEvents = true;
                currentEvent = eventName;
                if (IsDebugEnabled) Debug($"Forwarding event {eventName} to 1 listener");
                if (listener != null) listener.SendCustomEvent(eventName);
                currentEvent = null;
                runningEvents = false;
            }
        }

        /// <summary>
        /// Sends a given custom event to all registered listeners in order.
        /// </summary>
        /// <param name="eventName">the given event to send to each of the behaviours</param>
        [RecursiveMethod]
        protected void SendManagedEvent(string eventName)
        {
            if (_sendEvents)
            {
                runningEvents = true;
                currentEvent = eventName;
                if (IsDebugEnabled) Debug($"Forwarding event {eventName} to {_eventListeners.Length} listeners");
                foreach (var listener in _eventListeners)
                    if (listener != null)
                        listener.SendCustomEvent(eventName);
                currentEvent = null;
                runningEvents = false;
            }
        }

        /// <summary>
        /// Assigns a given value to a particular variable on each of the behaviours in order.
        /// </summary>
        /// <param name="variableName">the given variable to set on each of the behaviours</param>
        /// <param name="value">the data to assign to the given variable</param>
        [RecursiveMethod]
        protected void SendManagedVariable(string variableName, object value)
        {
            if (_sendEvents)
            {
                if (IsDebugEnabled) Debug($"Forwarding variable {variableName} to {_eventListeners.Length} listeners");
                foreach (var listener in _eventListeners)
                    if (listener != null)
                        listener.SetProgramVariable(variableName, value);
            }
        }

        #endregion
    }
}