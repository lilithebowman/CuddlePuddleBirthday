using UnityEditor;
using UnityEngine;

namespace jp.lilxyzw.lilpbr
{
    public static class MaterialHelper
    {
        public static void SetKeyword(this Material material, string keyword, bool enable)
        {
            material.SetKeyword(new(material.shader, keyword), enable);
        }

        public static void SetKeyword(this MaterialEditor editor, string keyword, bool enable)
        {
            foreach (Material mat in editor.targets)
            {
                mat.SetKeyword(new(mat.shader, keyword), enable);
            }
        }

        public static bool HasKeyword(this MaterialEditor editor, string keyword, out bool hasMixedValue)
        {
            hasMixedValue = false;
            bool searched = false;
            bool res = false;
            foreach (Material mat in editor.targets)
            {
                bool hasKeyword = mat.IsKeywordEnabled(keyword);
                hasMixedValue |= searched && res != hasKeyword;
                res |= hasKeyword;
                searched = true;
            }
            return res;
        }
    }
}
