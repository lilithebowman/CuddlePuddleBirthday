using System.IO;
using System.Text;
using UnityEditor;

namespace jp.lilxyzw.lilpbr
{
    internal static class ShaderModifier
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            using var sw = new StreamWriter("Packages/jp.lilxyzw.lilpbr/Shaders/settings.hlsl", false, Encoding.UTF8);
#if LIL_VRCLIGHTVOLUMES
            sw.WriteLine("#define LIL_VRCLIGHTVOLUMES");
#endif
#if LIL_LTCGI
            sw.WriteLine("#define LIL_LTCGI");
#endif
#if LIL_VRCHAT
            sw.WriteLine("#include \"platform_vrchat.hlsl\"");
#endif
        }
    }
}
