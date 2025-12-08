
using ArchiTech.SDK;
using UnityEngine;
using VRC.Udon;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    public abstract class UdonActions : ATBehaviour
    {
        [HideInInspector] public UnityEngine.Object[] objects = new UnityEngine.Object[0];
        [HideInInspector] public int[] actions = new int[0];
        [HideInInspector] public object[][] parameterObjects = new object[0][];
        [HideInInspector] public int[] dynamicIndexes = new int[0];

        protected void RunActions()
        {
            for (var i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null) continue;
                var action = actions[i];
                if (action == -1) continue;
                var t = obj.GetType();
                var actionParams = parameterObjects[i];
                var dynamicIndex = dynamicIndexes[i];
                ActionItem(i, out object dynamicValue, out bool runSignals);
                HandleAction((UdonAction)action, in obj, in actionParams, in dynamicIndex, in dynamicValue, runSignals);
            }
        }

        protected virtual void ActionItem(in int actionIndex, out object dynamicValue, out bool runSignals)
        {
            dynamicValue = null;
            runSignals = true;
        }

        protected void HandleAction(UdonAction action, in UnityEngine.Object target, in object[] actionParams, in int dynamicIndex, in object dynamicValue, bool runSignals = true)
        {
            switch (action)
            {
                case UdonAction.GAMEOBJECT_SETACTIVE:
                {
                    ((GameObject)target).SetActive((bool)(dynamicIndex == 0 ? dynamicValue : actionParams[0]));
                    break;
                }
                case UdonAction.ANIMATOR_SETTRIGGER:
                {
                    if (runSignals) ((Animator)target).SetTrigger((string)(dynamicIndex == 0 ? dynamicValue : actionParams[0]));
                    break;
                }
                case UdonAction.ANIMATOR_SETBOOL:
                {
                    string param0 = (string)(dynamicIndex == 0 ? dynamicValue : actionParams[0]);
                    bool param1 = (bool)(dynamicIndex == 1 ? dynamicValue : actionParams[1]);
                    ((Animator)target).SetBool(param0, param1);
                    break;
                }
                case UdonAction.ANIMATOR_SETINTEGER:
                {
                    string param0 = (string)(dynamicIndex == 0 ? dynamicValue : actionParams[0]);
                    int param1 = (int)(dynamicIndex == 1 ? dynamicValue : actionParams[1]);
                    ((Animator)target).SetInteger(param0, param1);
                    break;
                }
                case UdonAction.ANIMATOR_SETFLOAT:
                {
                    string param0 = (string)(dynamicIndex == 0 ? dynamicValue : actionParams[0]);
                    float param1 = (float)(dynamicIndex == 1 ? dynamicValue : actionParams[1]);
                    ((Animator)target).SetFloat(param0, param1);
                    break;
                }
                case UdonAction.UDONBEHAVIOUR_SETPROGRAMVARIABLE:
                {
                    string param0 = (string)(dynamicIndex == 0 ? dynamicValue : actionParams[0]);
                    object param1 = dynamicIndex == 0 ? dynamicValue : actionParams[0];
                    ((UdonBehaviour)target).SetProgramVariable(param0, param1);
                    break;
                }
                case UdonAction.UDONBEHAVIOUR_SENDCUSTOMEVENT:
                {
                    if (runSignals) ((UdonBehaviour)target).SendCustomEvent((string)(dynamicIndex == 0 ? dynamicValue : actionParams[0]));
                    break;
                }
                case UdonAction.COLLIDER_SET_ISTRIGGER:
                {
                    ((Collider)target).isTrigger = (bool)(dynamicIndex == 0 ? dynamicValue : actionParams[0]);
                    break;
                }
                // case UdonActions.BEHAVIOUR_SET_ENABLED:
                // {
                //     ((Behaviour)target).enabled = (bool)(dynamicIndex == 0 ? dynamicValue : actionParams[0]);
                //     break;
                // }
            }
        }

    }
}