using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV.Extras
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UIToAnimatorProxy : ATEventHandler
    {
        [SerializeField] internal Toggle _bool;
        [SerializeField] internal Dropdown _int;
        [SerializeField] internal Slider _float;

        [SerializeField] internal Animator[] animators = new Animator[0];
        [SerializeField] internal string[] parameters = new string[0];

        private bool hasBool;
        private bool hasInt;
        private bool hasFloat;

        public override void Start()
        {
            if (init) return;
            base.Start();
            hasBool = _bool != null;
            hasInt = _int != null;
            hasFloat = _float != null;
        }

        [PublicAPI]
        public void UpdateBooleans()
        {
            bool usedBool = !hasBool;
            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null) continue;
                string paramName = parameters[i];
                if (string.IsNullOrWhiteSpace(paramName)) continue;

                foreach (var param in animator.parameters)
                {
                    Debug($"Checking param {param.name} ({param.type}) against {paramName}");
                    if (param.type != AnimatorControllerParameterType.Bool) continue;
                    if (param.name != paramName) continue;
                    animator.SetBool(paramName, _bool.isOn);
                    usedBool = true;
                }
            }

            if (!usedBool) Debug("Toggle component provided but no boolean animator parameters found");
        }

        [PublicAPI]
        public void UpdateIntegers()
        {
            bool usedInt = !hasInt;
            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null) continue;
                string paramName = parameters[i];
                if (string.IsNullOrWhiteSpace(paramName)) continue;

                foreach (var param in animator.parameters)
                {
                    if (param.type != AnimatorControllerParameterType.Bool) continue;
                    if (param.name != paramName) continue;
                    animator.SetInteger(paramName, _int.value);
                    usedInt = true;
                }
            }

            if (!usedInt) Debug("Dropdown component provided but no integer animator parameters found");
        }

        [PublicAPI]
        public void UpdateFloats()
        {
            bool usedFloat = !hasFloat;
            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null) continue;
                string paramName = parameters[i];
                if (string.IsNullOrWhiteSpace(paramName)) continue;

                foreach (var param in animator.parameters)
                {
                    if (param.type != AnimatorControllerParameterType.Float) continue;
                    if (param.name != paramName) continue;
                    animator.SetFloat(paramName, _float.value);
                    usedFloat = true;
                }
            }

            if (!usedFloat) Debug("Slider component provided but no float animator parameters found");
        }

        [PublicAPI]
        public void UpdateAll()
        {
            bool usedBool = !hasBool;
            bool usedInt = !hasInt;
            bool usedFloat = !hasFloat;
            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null) continue;
                string paramName = parameters[i];
                if (string.IsNullOrWhiteSpace(paramName)) continue;

                foreach (var param in animator.parameters)
                {
                    if (param.name != paramName) continue;
                    switch (param.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            if (hasBool)
                            {
                                animator.SetBool(paramName, _bool.isOn);
                                usedBool = true;
                            }

                            break;
                        case AnimatorControllerParameterType.Int:
                            if (hasInt)
                            {
                                animator.SetInteger(paramName, _int.value);
                                usedInt = true;
                            }

                            break;
                        case AnimatorControllerParameterType.Float:
                            if (hasFloat)
                            {
                                animator.SetFloat(paramName, _float.value);
                                usedFloat = true;
                            }

                            break;
                    }
                }
            }

            if (!usedBool) Debug("Toggle component provided but no boolean animator parameters found");
            if (!usedInt) Debug("Dropdown component provided but no integer animator parameters found");
            if (!usedFloat) Debug("Slider component provided but no float animator parameters found");
        }

        // alternate shortform API methods

        [Obsolete]
        public void UpdateBools() => UpdateBooleans();

        [Obsolete]
        public void UpdateInts() => UpdateIntegers();
    }
}