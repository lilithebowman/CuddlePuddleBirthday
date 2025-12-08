Shader "Hidden/ProTV/FullScreen"
{
    Properties
    {
        [Header(DEPRECATED)]
        [Header(Use the global texture toggle on the standard ProTV VideoScreen shader)]
        [MainTexture] _MainTex("Standby Texture", 2D) = "black" {}
        [NoScaleOffset] _VideoTex("Video Texture (Render Texture from the TV can go here)", 2D) = "" {}
        [Toggle(_USEGLOBALTEXTURE)] _UseGlobalTexture("Use Global Texture (_Udon_VideoTex)", Float) = 1
        [Gamma] _Brightness("Brightness", Float) = 1
        [Enum(None, 0, Side by Side, 1, Side By Side Swapped, 2, Over Under, 3, Over Under Swapped, 4)] _3D("Standby 3D Mode", Float) = 0
        [Enum(Half Size 3D, 2, Full Size 3D, 0)] _Wide("Standby 3D Mode Size", Float) = 2
    }
    SubShader
    {
        Tags
        {
            "Queue"="Overlay-1"
        }

        LOD 100
        Cull Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // GPU Instancing support https://docs.unity3d.com/2019.4/Documentation/Manual/GPUInstancing.html
            #pragma multi_compile_instancing
            #pragma shader_feature_local _USEGLOBALTEXTURE
            #include "UnityCG.cginc"
            #include "Packages/dev.architech.protv/Resources/Shaders/ProTVCore.cginc"
            #warning "ProTV/FullScreen is deprecated. All functionality has been merged into ProTV/VideoScreen"

            float2 getScreenUV(float4 screenPos)
            {
                float2 uv = screenPos / (screenPos.w + 0.0000000001);
                return uv;
            }

            struct vertdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // SPS-I support
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct fragdata
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                // SPS-I support
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fragdata vert(const vertdata v)
            {
                fragdata o;
                // SPS-I support
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_OUTPUT(fragdata, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeNonStereoScreenPos(o.vertex);
                return o;
            }

            float4 frag(const fragdata i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                FragmentProcessingData data = InitializeFragmentData(getScreenUV(i.screenPos));
                data.outputAspect = _ScreenParams.x / _ScreenParams.y;
                float4 tex = ProcessFragment(data);
                return tex;
            }
            ENDCG
        }
    }
    Fallback Off
}