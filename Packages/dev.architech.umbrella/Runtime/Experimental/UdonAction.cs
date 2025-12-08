
using UnityEngine;
using VRC.Udon;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    public enum UdonAction
    {
        [UdonActionType(typeof(GameObject), "SetActive"),
         UdonActionParam(typeof(bool), "Active")]
        GAMEOBJECT_SETACTIVE,

        [UdonActionType(typeof(Behaviour), "set Enabled"),
            UdonActionParam(typeof(bool), "Enabled")]
        BEHAVIOUR_SET_ENABLED,

        [UdonActionType(typeof(UdonBehaviour), "SetProgramVariable", true),
            UdonActionParam(typeof(string), "Variable Name", true),
            UdonActionParam(typeof(object), "Value")]
        UDONBEHAVIOUR_SETPROGRAMVARIABLE,

        [UdonActionType(typeof(UdonBehaviour), "SendCustomEvent", true),
            UdonActionParam(typeof(string), "Event Name", true)]
        UDONBEHAVIOUR_SENDCUSTOMEVENT,

        [UdonActionType(typeof(Animator), "SetTrigger", true),
            UdonActionParam(typeof(string), "Trigger Parameter", true)]
        ANIMATOR_SETTRIGGER,

        [UdonActionType(typeof(Animator), "SetBool", true),
            UdonActionParam(typeof(string), "Bool Parameter", true),
            UdonActionParam(typeof(bool), "Value")]
        ANIMATOR_SETBOOL,

        [UdonActionType(typeof(Animator), "SetInteger", true),
        UdonActionParam(typeof(string), "Integer Parameter", true),
        UdonActionParam(typeof(int), "Value")]
        ANIMATOR_SETINTEGER,

        [UdonActionType(typeof(Animator), "SetFloat", true),
         UdonActionParam(typeof(string), "Float Parameter", true),
         UdonActionParam(typeof(float), "Value")]
        ANIMATOR_SETFLOAT,

        [UdonActionType(typeof(Collider), "set IsTrigger"),
            UdonActionParam(typeof(bool), "IsTrigger")]
        COLLIDER_SET_ISTRIGGER,
    }


    // TODO figure out how to reorganize the enum usages
    // This shit is too tightly coupled to the concept of the Toggle. FIX THAT!

}