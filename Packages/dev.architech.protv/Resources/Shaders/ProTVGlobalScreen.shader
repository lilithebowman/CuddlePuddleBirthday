Shader "Hidden/ProTV/GlobalScreen"
{
    Properties
    {
        [Header(DEPRECATED)]
        [Header(Use the global texture toggle on the standard ProTV VideoScreen shader)]
        [Space(10)]
        [MainTexture] _MainTex("Standby Texture", 2D) = "black" {}

        _Aspect("Target Aspect Ratio (0 to ignore)", Float) = 1.77777
        [Gamma] _Brightness("Brightness", Float) = 1
        [Gamma] _GIBrightness("Global Illumination Brightness", Float) = 3
        [Enum(Disabled, 0, Standard, 1, Dynamic, 2)] _Mirror("Mirror Flip Mode", Float) = 1
        [Enum(None, 0, Side by Side, 1, Side By Side Swapped, 2, Over Under, 3, Over Under Swapped, 4)] _3D("Standby 3D Mode", Float) = 0
        [Enum(Half Size 3D, 2, Full Size 3D, 0)] _Wide("Standby 3D Mode Size", Float) = 2
        [ToggleUI] _Force2D("Force Standby to 2D", Float) = 0
        [ToggleUI] _Clip("Clip Aspect", Float) = 0
        [ToggleUI] _Fog("Enable Fog", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2
    }
    SubShader
    {
        Pass
        {
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vertBase
            #pragma fragment fragBase
            // GPU Instancing support https://docs.unity3d.com/2019.4/Documentation/Manual/GPUInstancing.html
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Packages/dev.architech.protv/Resources/Shaders/ProTVCore.cginc"
            #warning "ProTV/GloablScreen is deprecated. All functionality has been merged into ProTV/VideoScreen"

            struct vertdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // SPS-I support
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct fragdata
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                // SPS-I support
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                // fog support
                UNITY_FOG_COORDS(1)
            };

            fragdata vertBase(const vertdata v)
            {
                fragdata o;
                // SPS-I support
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_OUTPUT(fragdata, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // fog support
                UNITY_TRANSFER_FOG(o, o.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 fragBase(const fragdata i) : SV_Target
            {
                // - Run fragment solver
                // - Adjust for fog
                // - Apply brightness adjustment
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                FragmentProcessingData data = InitializeFragmentData(i.uv);
                float4 tex = ProcessFragment(data);
                // apply fog adjustment
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    if (_Fog) UNITY_APPLY_FOG(i.fogCoord, tex);
                #endif
                return tex;
            }
            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass is not used during regular rendering.
        Pass
        {
            Name "META"
            Tags
            {
                "LightMode"="Meta"
            }
            Cull Off
            CGPROGRAM
            #include "UnityStandardMeta.cginc"
            #include "Packages/dev.architech.protv/Resources/Shaders/ProTVCore.cginc"

            float _GIBrightness;

            float4 frag_meta2(const v2f_meta i): SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                FragmentProcessingData data = InitializeFragmentData(i.uv);
                float brightness = data.brightness;
                data.brightness = 1;
                float4 tex = ProcessFragment(data);
                o.Albedo = half3(tex.rgb) * brightness;
                o.Emission = half3(tex.rgb) * _GIBrightness;
                return UnityMetaFragment(o);
            }

            #pragma vertex vert_meta
            #pragma fragment frag_meta2
            ENDCG
        }


        Pass
        {
            Name "SHADOWCASTER"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vertShadow(const appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return 0;
            }
            ENDCG
        }
    }
    Fallback Off
}