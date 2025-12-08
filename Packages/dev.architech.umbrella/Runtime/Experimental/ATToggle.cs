using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

// ReSharper disable MemberCanBeMadeStatic.Local

namespace ArchiTech.Umbrella
{
    /// <summary>
    /// A script that tracks an internal on/off state and updates/triggers actions accordingly
    /// It supports the following actions:
    /// GameObject [SetActive]
    /// Animator [set Enabled, SetTrigger, SetBool, SetInteger, SetFloat]
    /// UdonBehaviour [set Enabled, SetProgramVariable, SendCustomEvent]
    /// (TODO more being added soon)
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ATToggle : UdonActions
    {
        [SerializeField] protected bool initialState = false;
        [SerializeField] protected bool oneWay = false;
        protected bool state = false;
        public bool[] inverseOfState = new bool[0];
        public ATToggle[] siblings = new ATToggle[0];

        public override void Start()
        {
            if (init) return;
            base.Start();

            // remainder of init code for this class goes here
            state = initialState;
            UpdateObjects();
        }

        /// <summary>
        /// Makes the toggle swap states internally and updates all object states to reflect.
        /// If there are any assigned siblings, it will force reset those back to their initial states.
        /// </summary>
        [PublicAPI]
        public virtual void _Activate()
        {
            if (oneWay && state != initialState) return;
            state = !state;
            UpdateObjects();
            if (state)
                foreach (ATToggle t in siblings)
                    t._Reset();
        }

        /// <summary>
        /// Forces the internal state back to the original value and updates the object states to reflect.
        /// </summary>
        [PublicAPI]
        public virtual void _Reset()
        {
            if (state != initialState)
            {
                state = initialState;
                UpdateObjects();
            }
        }

        /// <summary>
        /// Handles triggering state changes for all associated objects.
        /// </summary>
        protected void UpdateObjects()
        {
            if (IsDebugEnabled) Debug($"Updating objects with state {state}");
            RunActions();
        }

        protected override void ActionItem(in int actionIndex, out object dynamicValue, out bool runSignals)
        {
            bool actualState = state != inverseOfState[actionIndex];
            runSignals = actualState;
            dynamicValue = actualState;
        }
    }
}