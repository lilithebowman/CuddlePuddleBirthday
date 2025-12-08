using System;
using UnityEngine;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class UdonActionTypeAttribute : PropertyAttribute
    {
        public readonly Type ActionType;
        public readonly string ActionName;
        public readonly bool InlineParameters;

        public UdonActionTypeAttribute(Type actionType, string actionName, bool inlineParameters = false)
        {
            ActionType = actionType;
            ActionName = actionName;
            InlineParameters = inlineParameters;
        }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class UdonActionParamAttribute : PropertyAttribute
    {
        public readonly Type ParameterType;
        public readonly string ParameterName;
        public readonly bool ImplicitOptions;

        public UdonActionParamAttribute(Type parameterType, string parameterName = null, bool implicitOptions = false)
        {
            ParameterType = parameterType;
            ParameterName = parameterName;
            ImplicitOptions = implicitOptions;
        }
    }
    
}