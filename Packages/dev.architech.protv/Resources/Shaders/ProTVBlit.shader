Shader "Hidden/ProTV/Blit"
{
    Properties
    {
        [HideInInspector] _MainTex ("Blit Texture", 2D) = "black" {}
        // hidden in inspector because this should only be assigned via script
        [HideInInspector] _MainTex_ST_Override ("Scale/Offset Override", Vector) = (1.0, 1.0, 0.0, 0.0)
    }
    SubShader
    {
        Pass
        {
            Name "Video Correction"
            // required for Blit to succeed on Android
            ZTest Always
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_ST_Override;

            float _AVPro;
            float _SkipGamma;
            float4 _GammaZone;

            struct vertdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragdata
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fragdata vert(vertdata v)
            {
                fragdata o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(fragdata i) : SV_Target
            {
                // because default properties are stupid while doing a Blit, allow a custom _ST override vector if the provided value is not the default one
                if (any(_MainTex_ST_Override != float4(1, 1, 0, 0))) _MainTex_ST = _MainTex_ST_Override;

                // scale/offset math for AVPro flip correction (this is the same as the TRANSFORM_TEX macro)
                float4 tex = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);

                // check that the current UV is within the defined Gamma Zone
                bool uvCheck = all(i.uv >= _GammaZone.zw) && all(i.uv <= _GammaZone.zw + _GammaZone.xy);

                #ifndef UNITY_COLORSPACE_GAMMA
                // if not in gamma colorspace, handle avpro conversion
                tex.rgb = _AVPro && !_SkipGamma && uvCheck ? GammaToLinearSpace(tex.rgb) : tex.rgb;
                #endif

                return tex;
            }
            ENDCG
        }

        Pass
        {
            Name "Video Bake"
            // required for Blit to succeed on Android
            ZTest Always
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _GammaZone;
            float _Brightness;
            float _ForceAspect;
            int _AspectFitMode;
            int _3D;
            float _FadeEdges;

            struct vertdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragdata
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // we don't need all the default variables or processors, flag as already included so they are skipped
            #define PROTV_CORE_VARIABLES_INCLUDED
            #define PROTV_CORE_PROCESSORS_INCLUDED
            #include "ProTVCore.cginc"

            fragdata vert(const vertdata v)
            {
                fragdata o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(const fragdata i) : SV_Target
            {
                float2 videoDims = _MainTex_TexelSize.zw;
                const int mode3D = abs(_3D);
                const int wide3D = sign(_3D);
                float2 uv = i.uv;

                float4 uvClip;
                float2 uvCenter;
                float visible;

                TV3DAdjustment(uv, videoDims, uvClip, uvCenter, mode3D, wide3D, true);
                TVGammaZoneAdjustment(uv, uvClip, uvCenter, _GammaZone);
                TVAspectRatio(uv, _ForceAspect, videoDims, uvCenter, _AspectFitMode);
                TVAspectVisibility(uv, videoDims, uvClip, visible);

                float4 tex = tex2D(_MainTex, uv);
                TVFadeEdges(tex, visible, _FadeEdges);
                return tex * _Brightness;
            }
            ENDCG
        }
    }
    Fallback Off
}