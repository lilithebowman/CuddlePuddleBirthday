// Holographic Parallax Poster shader
// by Silent, made with Amplify's help

// 1.1 Fixed issue where layers would stretch when close to the camera. Thanks Wunkolo!

// MIT inclusion with ProTV. Sourced from https://gitlab.com/s-ilent/parallax-poster

Shader "ProTV - ThirdParty/Silent - ParallaxPoster"
{
	Properties
	{
		[Gamma]_ParallaxScale("Parallax Scale", Range( 0 , 1)) = 1
		[Header(Parallax Layers)]
		_Layer0("Layer 0", 2D) = "white" {}
		_Layer0Height("Layer 0 Height", Range( 0 , 1)) = 1
		_Layer1("Layer 1", 2D) = "white" {}
		_Layer1Height("Layer 1 Height", Range( 0 , 1)) = 1
		_Layer1Cutoff("Layer 1 Edge Cutoff", Range( 0 , 1)) = 0
		_Layer2("Layer 2", 2D) = "white" {}
		_Layer2Height("Layer 2 Height", Range( 0 , 1)) = 1
		_Layer2Cutoff("Layer 2 Edge Cutoff", Range( 0 , 1)) = 0
		_Layer3("Layer 3", 2D) = "white" {}
		_Layer3Height("Layer 3 Height", Range( 0 , 1)) = 1
		_Layer3Cutoff("Layer 3 Edge Cutoff", Range( 0 , 1)) = 0
		_Layer4("Layer 4", 2D) = "white" {}
		_Layer4Height("Layer 4 Height", Range( 0 , 1)) = 1
		_Layer4Cutoff("Layer 4 Edge Cutoff", Range( 0 , 1)) = 0
		_Layer5("Layer 5", 2D) = "white" {}
		_Layer5Height("Layer 5 Height", Range( 0 , 1)) = 1
		_Layer5Cutoff("Layer 5 Edge Cutoff", Range( 0 , 1)) = 0
		_Layer6("Layer 6", 2D) = "white" {}
		_Layer6Height("Layer 6 Height", Range( 0 , 1)) = 1
		_Layer6Cutoff("Layer 6 Edge Cutoff", Range( 0 , 1)) = 0
		[Header(Normal Map Distortion)]
		[Toggle(_NORMALMAP)]_UseBumpMap("Use Normal Map", Float) = 0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpMapScale("Warp Scale", Float) = 0.1
		_BumpMapNormalScale("Normal Scale", Float) = 0.1
		[Header(Background Darkening)]_FresnelScale("Fresnel Scale", Float) = 3
		_FresnelPower("Fresnel Power", Float) = 2
		_BlurLevel("Background Blur Power", Range(0, 7)) = 4
		[Header(Lighting)]
		[Toggle(_SPECGLOSSMAP)]_UseSpecGlossMap("Use Property Map (M/O/E/S)", Float) = 0
		_SpecGlossMap("MOES Map", 2D) = "white" {}
		_EmissionStrength("Emission Strength", Float) = 1
		_Metallic("Metallic Scale", Range(0, 1)) = 0
		_Smoothness("Smoothness Scale", Range(0, 1)) = 1
		_Occlusion("Occlusion Scale", Range(0, 1)) = 1
		[Header(Performance Settings)]
		[Toggle(_SUNDISK_SIMPLE)]_SkipLayers2("Skip Layers 4/5/6", Float) = 0
		[Toggle(_SUNDISK_NONE)]_SkipLayers1("Skip Layers 2/3", Float) = 0
		[Header(System Settings)]
		[ToggleUI]_DebugMode("Debug: Show Coverage", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true" "PerformanceChecks"="False" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 4.0
		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _SPECGLOSSMAP
		#pragma shader_feature _SUNDISK_SIMPLE
		#pragma shader_feature _SUNDISK_NONE
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#define SAMPLE_TEXTURE2D_LOD(tex,samplerTex,coord,lod) tex.SampleLevel(samplerTex,coord, lod)
		#define SAMPLE_TEXTURE2D_BIAS(tex,samplerTex,coord,bias) tex.SampleBias(samplerTex,coord,bias)
		#define SAMPLE_TEXTURE2D_GRAD(tex,samplerTex,coord,ddx,ddy) tex.SampleGrad(samplerTex,coord,ddx,ddy)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#define SAMPLE_TEXTURE2D_LOD(tex,samplerTex,coord,lod) tex2Dlod(tex,float4(coord,0,lod))
		#define SAMPLE_TEXTURE2D_BIAS(tex,samplerTex,coord,bias) tex2Dbias(tex,float4(coord,0,bias))
		#define SAMPLE_TEXTURE2D_GRAD(tex,samplerTex,coord,ddx,ddy) tex2Dgrad(tex,coord,ddx,ddy)
		#endif//ASE Sampling Macros

		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif

		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
			float3 tangentViewDir;
		};

		uniform float _FresnelScale;
		uniform float _FresnelPower;
		uniform float _BlurLevel;

		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer0);
		uniform float4 _Layer0_ST;
		uniform float _Layer0Height;
		uniform float _ParallaxScale;
		SamplerState sampler_Layer0;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer1);
		uniform float4 _Layer1_ST;
		uniform float _Layer1Height;
		uniform float _Layer1Cutoff;
		SamplerState sampler_Layer1;

		#if !defined(_SUNDISK_NONE)
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer2);
		uniform float4 _Layer2_ST;
		uniform float _Layer2Height;
		uniform float _Layer2Cutoff;
		SamplerState sampler_Layer2;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer3);
		uniform float4 _Layer3_ST;
		uniform float _Layer3Height;
		uniform float _Layer3Cutoff;
		SamplerState sampler_Layer3;
		#endif

		#if !defined(_SUNDISK_SIMPLE)
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer4);
		uniform float4 _Layer4_ST;
		uniform float _Layer4Height;
		uniform float _Layer4Cutoff;
		SamplerState sampler_Layer4;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer5);
		uniform float4 _Layer5_ST;
		uniform float _Layer5Height;
		uniform float _Layer5Cutoff;
		SamplerState sampler_Layer5;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Layer6);
		uniform float4 _Layer6_ST;
		uniform float _Layer6Height;
		uniform float _Layer6Cutoff;
		SamplerState sampler_Layer6;
		#endif

		#if defined(_NORMALMAP)
		UNITY_DECLARE_TEX2D_NOSAMPLER(_BumpMap);
		uniform float4 _BumpMap_ST;
		uniform float _BumpMapScale;
		uniform float _BumpMapNormalScale;
		SamplerState sampler_BumpMap;
		#endif

		#if defined(_SPECGLOSSMAP)
		UNITY_DECLARE_TEX2D_NOSAMPLER(_SpecGlossMap);
		uniform float4 _SpecGlossMap_ST;
		SamplerState sampler_SpecGlossMap;
		#endif

		uniform float _EmissionStrength;
		uniform float _DebugMode;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _Occlusion;


		float AlphaBounds(float2 uv, float edge = 0)
		{
			float width = edge+fwidth(uv);
			return 1-smoothstep(0, width, length(saturate(uv) - abs(uv)));
		}

		float2 ScaleOffsetCentred(float2 uv, float4 scaleOffset)
		{
			uv -= 0.5;
			uv *= scaleOffset.xy;
			uv += 0.5;
			uv += scaleOffset.zw;
			return uv;
		}

		float2 ParallaxOffset(float height, float2 parallaxUV, float scale, float2 uvs = 0)
		{
			return ((height - 1) * parallaxUV * scale) + uvs;
		}

		float2 getUVs(float2 parallaxUV, float height, float scale, float4 scaleOffset, float2 uv)
		{
			float2 offset = ParallaxOffset(height, parallaxUV, scale);
			float2 centre = 0.5-offset;

			uv -= centre;
			uv *= scaleOffset.xy;
			uv += centre;
			uv += scaleOffset.zw;
			uv += offset;
			return uv;
		}

		float3 Heatmap(float v) {
		    float3 r = v * 2.1 - float3(1.8, 1.14, 0.3);
		    return 1.0 - r * r;
		}

	    void vert (inout appdata_full v, out Input o) 
	    {
        	UNITY_INITIALIZE_OUTPUT(Input,o);

			// Get tangent-space view direction
			TANGENT_SPACE_ROTATION;
			o.tangentViewDir = mul(rotation,  ObjSpaceViewDir(v.vertex));
	    }

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );

			float2 texcoord0 = i.uv_texcoord;

			// Re-normalize the tangent space view direction
			const float2 parallaxUV = i.tangentViewDir.xy / max(i.tangentViewDir.z, 0.0001);

			#if defined(_NORMALMAP)
			float2 uvn = texcoord0 * _BumpMap_ST.xy + _BumpMap_ST.zw;
			float3 normals = UnpackScaleNormal(SAMPLE_TEXTURE2D( _BumpMap, sampler_BumpMap, uvn ), 0.10);
			texcoord0 += normals * _BumpMapScale;
			o.Normal = lerp(o.Normal, normals, _BumpMapNormalScale);
			#endif

			float4 materialProps = float4(_Metallic, 1, _EmissionStrength, _Smoothness);
			#if defined(_SPECGLOSSMAP)
			float2 uvspec = texcoord0 * _SpecGlossMap_ST.xy + _SpecGlossMap_ST.zw;
			float4 propMap = SAMPLE_TEXTURE2D( _SpecGlossMap, sampler_SpecGlossMap, uvspec );
			materialProps.r *= propMap.r;
			materialProps.g = LerpOneTo(propMap.g, _Occlusion);
			materialProps.b *= propMap.b;
			materialProps.a *= propMap.a;
			#endif

			#if UNITY_SINGLE_PASS_STEREO
			static float4 cameraPos = float4(lerp(unity_StereoWorldSpaceCameraPos[0], unity_StereoWorldSpaceCameraPos[1], 0.5), 1);
			#else
			static float4 cameraPos = float4(_WorldSpaceCameraPos,1);
			#endif

			float3 worldViewDirCentre = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );

			float fresnelNdotV = dot( ase_worldNormal, worldViewDirCentre );
			float fresnel = ( _FresnelScale * pow( 1.0 - fresnelNdotV, max( _FresnelPower , 0.0 ) ) );
			float backgroundMask = saturate( ( 1.0 - fresnel ) );

			float2 uv0 = ScaleOffsetCentred( ParallaxOffset(_Layer0Height, parallaxUV, _ParallaxScale, texcoord0), _Layer0_ST );

			float4 layer0, layer1, layer2, layer3, layer4, layer5, layer6;
			layer0 = layer1 = layer2 = layer3 = layer4 = layer5 = layer6 = 0;

			layer0 = backgroundMask * SAMPLE_TEXTURE2D_BIAS( _Layer0, sampler_Layer0, uv0, fresnel*_BlurLevel );
			layer0.a *= 0;

			// We only want to sample the layers that are visible. 
			// But getting the offset's cheap.
			#if !defined(_SUNDISK_SIMPLE)
			float2 uv6 = getUVs( parallaxUV, _Layer6Height, _ParallaxScale, _Layer6_ST, texcoord0 );
			float2 uv5 = getUVs( parallaxUV, _Layer5Height, _ParallaxScale, _Layer5_ST, texcoord0 );
			float2 uv4 = getUVs( parallaxUV, _Layer4Height, _ParallaxScale, _Layer4_ST, texcoord0 );
			#endif
			#if !defined(_SUNDISK_NONE) 
			float2 uv3 = getUVs( parallaxUV, _Layer3Height, _ParallaxScale, _Layer3_ST, texcoord0 );
			float2 uv2 = getUVs( parallaxUV, _Layer2Height, _ParallaxScale, _Layer2_ST, texcoord0 );
			#endif
			float2 uv1 = getUVs( parallaxUV, _Layer1Height, _ParallaxScale, _Layer1_ST, texcoord0 );

			#if !defined(_SUNDISK_SIMPLE)
			float bounds6 = AlphaBounds(uv6, _Layer6Cutoff);
			float bounds5 = AlphaBounds(uv5, _Layer5Cutoff);
			float bounds4 = AlphaBounds(uv4, _Layer4Cutoff);
			#endif
			#if !defined(_SUNDISK_NONE)
			float bounds3 = AlphaBounds(uv3, _Layer3Cutoff);
			float bounds2 = AlphaBounds(uv2, _Layer2Cutoff);
			#endif
			float bounds1 = AlphaBounds(uv1, _Layer1Cutoff);

			float sampled = 0;
			static float layerEpsilon = 254.0/255.0;

			// First will always pass.
			// Before this checked against bounds, but that caused sampling artifacts.
			#if !defined(_SUNDISK_SIMPLE)
			if (layer0.a < layerEpsilon)
			{
				layer6 += SAMPLE_TEXTURE2D( _Layer6, sampler_Layer6, uv6 );
				layer6.a *= bounds6;
				sampled += 1;
			}
			layer0.a += layer6.a;
			if (layer0.a < layerEpsilon)
			{
				layer5 += SAMPLE_TEXTURE2D( _Layer5, sampler_Layer5, uv5 );
				layer5.a *= bounds5;
				sampled += 1;
			}
			layer0.a += layer5.a;
			if (layer0.a < layerEpsilon)
			{ 
				layer4 += SAMPLE_TEXTURE2D( _Layer4, sampler_Layer4, uv4 );
				layer4.a *= bounds4;
				sampled += 1;
			}
			layer0.a += layer4.a;
			#endif

			#if !defined(_SUNDISK_NONE)
			if (layer0.a < layerEpsilon)
			{ 
				layer3 += SAMPLE_TEXTURE2D( _Layer3, sampler_Layer3, uv3 );
				layer3.a *= bounds3;
				sampled += 1;
			}
			layer0.a += layer3.a;
			if (layer0.a < layerEpsilon)
			{ 
				layer2 += SAMPLE_TEXTURE2D( _Layer2, sampler_Layer2, uv2 );
				layer2.a *= bounds2;
				sampled += 1;
			}
			layer0.a += layer2.a;
			#endif

			if (layer0.a < layerEpsilon)
			{ 
				layer1 += SAMPLE_TEXTURE2D( _Layer1, sampler_Layer1, uv1 );
				layer1.a *= bounds1;
				sampled += 1;
			}
			layer0.a += layer1.a;
			
			
			float3 color = layer0;
			color = color * (1-layer1.a) + layer1 * layer1.a;
			color = color * (1-layer2.a) + layer2 * layer2.a;
			color = color * (1-layer3.a) + layer3 * layer3.a;
			color = color * (1-layer4.a) + layer4 * layer4.a;
			color = color * (1-layer5.a) + layer5 * layer5.a;
			color = color * (1-layer6.a) + layer6 * layer6.a;

			color.xyz = _DebugMode? Heatmap(sampled*0.2) : color;

			o.Albedo = color;
			o.Emission =  materialProps.b * color;
			o.Metallic = materialProps.r;
			o.Smoothness = materialProps.a;
			o.Occlusion = materialProps.g;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard vertex:vert keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" "PerformanceChecks"="False" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}