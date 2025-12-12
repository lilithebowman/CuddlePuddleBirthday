Shader "lilWater"
{
    Properties
    {
        [LILFoldout(Water)]
        _WaterColor ("Color", Color) = (0.27,0.53,0.73,1)
        _WaveTiling ("Tiling", Float) = 5
        _WaveNormal ("Normal Map", 2D) = "bump" {}
        _WaveHeight ("Height", 2D) = "white" {}
        _FoamNoiseTex ("Foam", 2D) = "white" {}
        _Caustics ("Caustics", Range(0,2)) = 1
        [LILFoldoutEnd]

        [LILFoldout(Fog)]
        _WaterColorFog ("Color", Color) = (0.0,0.08,0.21,1)
        _WaterFogDistance ("Distance", Float) = 0.25
        _VolumetricFog ("Volumetric Fog", Range(0,2)) = 0.5
        [LILFoldoutEnd]

        [HideInInspector]_LTCGI("",Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "LTCGI" = "_LTCGI" }
        Fog { Color (0,0,0,0) }
        ZWrite Off

        HLSLINCLUDE
        #pragma target 5.0
        #undef UNITY_SAMPLE_FULL_SH_PER_PIXEL
        #define UNITY_SAMPLE_FULL_SH_PER_PIXEL 1
        #include "settings.hlsl"

        bool IsGamma()
        {
            #ifdef UNITY_COLORSPACE_GAMMA
                return true;
            #else
                return false;
            #endif
        }
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "pbr.hlsl"
        #include "water_properties.hlsl"

        struct v2fDummy
        {
            float4 pos : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float4 uv01 : TEXCOORD0;
            float4 uv23 : TEXCOORD1;
            float4 normal : TEXCOORD2;
            float4 tangent : TEXCOORD3;
            float4 binormal : TEXCOORD4;
            float3 V : TEXCOORD5;
            UNITY_LIGHTING_COORDS(6,7)
            UNITY_FOG_COORDS(8)
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        #include "unity_birp.hlsl"

        v2fDummy vertDummy (appdata v)
        {
            v2fDummy o;
            UNITY_INITIALIZE_OUTPUT(v2fDummy, o);
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.pos = UnityObjectToClipPos(v.vertex);
            return o;
        }
        float4 fragDummy () : SV_Target { return 0; }

        #define IGNORE_GRABPASS
        #include "water_core.hlsl"

        v2f vert (appdata v)
        {
            v2f o;
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            DoVertex(v, o.pos, o.uv01, o.uv23, o.normal, o.tangent, o.binormal, o.V);
            UNITY_TRANSFER_LIGHTING(o, v.uv1);
            UNITY_TRANSFER_FOG(o,o.pos);
            return o;
        }
        ENDHLSL

        Stencil
        {
            Ref 190
        }

        // 水の内側にステンシルを書く
        // 加えて外側をZeroにし他のオブジェクトの影響を回避
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertDummy
            #pragma fragment fragDummy
            ENDHLSL
            Blend Zero One
            Cull Front
            Stencil
            {
                Pass Zero
                ZFail Replace
            }
        }

        // 水の外側から見たコースティクスとフォグ
        Pass
        {
            Name "FORWARD_UNDERWATER"
            Tags { "LightMode" = "ForwardBase" }
            Blend One OneMinusSrcAlpha, Zero One
            Cull Back

            Stencil
            {
                Comp Equal
                Pass Zero
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment VERTEXLIGHT_ON
            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #ifdef LOD_FADE_CROSSFADE
                UnityApplyDitherCrossFade(i.pos.xy);
                #endif
                half4 col = UnpackAndShadingUnderwater(i, i.normal, i.tangent, i.binormal, i.V, i.uv01, i.uv23, true, depth);
                return col;
            }
            ENDHLSL
        }

        // PassBackで裏面にステンシルを書き込み、ZFailFrontをZeroにして範囲外に描画しないように
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertDummy
            #pragma fragment fragDummy
            ENDHLSL
            Blend Zero One
            Cull Off

            Stencil
            {
                PassBack Replace
                ZFailFront Zero
            }
        }

        // 水の内側から見たコースティクスとフォグと反射
        Pass
        {
            Name "FORWARD_UNDERWATER"
            Tags { "LightMode" = "ForwardBase" }
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Front
            ZTest Always

            Stencil
            {
                Comp Equal
            }

            HLSLPROGRAM
            #define IGNORE_GRABPASS
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment VERTEXLIGHT_ON

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #ifdef LOD_FADE_CROSSFADE
                UnityApplyDitherCrossFade(i.pos.xy);
                #endif
                half4 col = UnpackAndShadingUnderwater(i, i.normal, i.tangent, i.binormal, i.V, i.uv01, i.uv23, false, depth);
                return col;
            }
            ENDHLSL
        }

        // 水の外側から見たボリュメトリックフォグ
        // 描画範囲にZeroを書き込んでフォグが二重に描画されないように
        Pass
        {
            Name "FORWARD_UNDERWATER"
            Tags { "LightMode" = "ForwardBase" }
            Blend One OneMinusSrcAlpha, Zero One
            Cull Back

            Stencil
            {
                Comp Always
                Pass Zero
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment VERTEXLIGHT_ON

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #ifdef LOD_FADE_CROSSFADE
                UnityApplyDitherCrossFade(i.pos.xy);
                #endif
                half4 col = UnpackAndShadingVolumetricFog(i, i.normal, i.tangent, i.binormal, i.V, i.uv01, i.uv23, true, depth);
                return col;
            }
            ENDHLSL
        }

        // 水の内側から見たボリュメトリックフォグ
        Pass
        {
            Name "FORWARD_UNDERWATER"
            Tags { "LightMode" = "ForwardBase" }
            Blend One OneMinusSrcAlpha, Zero One
            Cull Front
            ZTest Always

            Stencil
            {
                Comp Equal
                Pass Zero
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment VERTEXLIGHT_ON

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #ifdef LOD_FADE_CROSSFADE
                UnityApplyDitherCrossFade(i.pos.xy);
                #endif
                half4 col = UnpackAndShadingVolumetricFog(i, i.normal, i.tangent, i.binormal, i.V, i.uv01, i.uv23, false, depth);
                return col;
            }
            ENDHLSL
        }

        //GrabPass {}

        // 水の外側から見た反射と屈折
        Pass
        {
            Name "FORWARD_UNDERWATER"
            Tags { "LightMode" = "ForwardBase" }
            Blend One OneMinusSrcAlpha, Zero One
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment VERTEXLIGHT_ON

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #ifdef LOD_FADE_CROSSFADE
                UnityApplyDitherCrossFade(i.pos.xy);
                #endif
                half4 col = UnpackAndShading(i, i.normal, i.tangent, i.binormal, i.V, i.uv01, i.uv23, true, depth);
                return col;
            }
            ENDHLSL
        }
    }
    CustomEditor "jp.lilxyzw.lilpbr.PBRShaderGUI"
}
