using UnityEditor;

namespace ArchiTech.SDK.Editor
{
    
    /// <summary>
    /// Generic custom editor for ATEventManager.
    /// Do not inherit from this class. Instead inherit from ATEventManagerEditor.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ATEventHandler), true)]
    internal sealed class ATEventHandlerEditorImpl : ATEventHandlerEditor
    {
        protected override void RenderChangeCheck() { }
    }
}