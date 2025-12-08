using UnityEditor;

namespace ArchiTech.SDK.Editor
{
    /// <summary>
    /// Generic custom editor for ATBehaviour.
    /// Do not inherit from this class. Instead inherit from ATBehaviourEditor.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ATBehaviour), true)]
    internal sealed class ATBehaviourEditorImpl : ATBehaviourEditor
    {
        protected override void RenderChangeCheck() { }
    }
}