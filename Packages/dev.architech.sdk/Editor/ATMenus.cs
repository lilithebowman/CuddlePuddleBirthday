using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK.Editor
{
    public static class ATMenus
    {
        [MenuItem("CONTEXT/Component/Move Component To Top", false, 498)]
        internal static void MoveComponentToTop(MenuCommand menuCommand)
        {
            ATEditorUtility.MoveComponentToTop(menuCommand.context as Component);
        }

        [MenuItem("CONTEXT/Component/Move Component To Bottom", false, 499)]
        internal static void MoveComponentToBottom(MenuCommand menuCommand)
        {
            ATEditorUtility.MoveComponentToBottom(menuCommand.context as Component);
        }
    }
}