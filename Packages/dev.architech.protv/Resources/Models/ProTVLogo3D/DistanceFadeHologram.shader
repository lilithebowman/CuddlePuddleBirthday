// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ProTV - ThirdParty/MissStabby - Hologram"
{
	Properties
	{
		[HDR]_EmissiveColor("EmissiveColor", Color) = (1,1,1,0)
		_VideoTex("_VideoTex", 2D) = "white" {}
		_Opacity("Opacity", Float) = 1
		_MatCap("MatCap", 2D) = "white" {}
		_SheenBrightness("SheenBrightness", Float) = 3
		_FlickerMax("FlickerMax", Float) = 1.2
		_FlickerMin("FlickerMin", Float) = 0.8
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Pass
		{
			ColorMask 0
			ZWrite On
		}

		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
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
			float4 screenPosition4;
			float3 worldRefl;
		};

		uniform float4 _EmissiveColor;
		uniform sampler2D _VideoTex;
		uniform float4 _VideoTex_ST;
		uniform float _FlickerMin;
		uniform float _FlickerMax;
		uniform sampler2D _MatCap;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _SheenBrightness;
		uniform float _Opacity;


		float3 HSVToRGB( float3 c )
		{
			float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


		float3 RGBToHSV(float3 c)
		{
			float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
			float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
			float d = q.x - min( q.w, q.y );
			float e = 1.0e-10;
			return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}

		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 vertexPos4 = ase_vertex3Pos;
			float4 ase_screenPos4 = ComputeScreenPos( UnityObjectToClipPos( vertexPos4 ) );
			o.screenPosition4 = ase_screenPos4;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_normWorldNormal = normalize( ase_worldNormal );
			float fresnelNdotV12 = dot( ase_normWorldNormal, ase_worldViewDir );
			float fresnelNode12 = ( 0.2 + 0.8 * pow( max( 1.0 - fresnelNdotV12 , 0.0001 ), 0.1 ) );
			float temp_output_13_0 = ( 1.0 - fresnelNode12 );
			float temp_output_15_0 = ( temp_output_13_0 * 2.0 );
			float3 hsvTorgb47 = RGBToHSV( _EmissiveColor.rgb );
			float3 hsvTorgb48 = HSVToRGB( float3(hsvTorgb47.x,( hsvTorgb47.y * 0.2 ),( hsvTorgb47.z * 2.0 )) );
			float fresnelNdotV52 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode52 = ( 0.0 + 3.0 * pow( 1.0 - fresnelNdotV52, 1.0 ) );
			float4 lerpResult46 = lerp( float4( hsvTorgb48 , 0.0 ) , _EmissiveColor , saturate( fresnelNode52 ));
			float2 uv_VideoTex = i.uv_texcoord * _VideoTex_ST.xy + _VideoTex_ST.zw;
			float2 temp_cast_2 = (_Time.y).xx;
			float simplePerlin2D65 = snoise( temp_cast_2*2.0 );
			simplePerlin2D65 = simplePerlin2D65*0.5 + 0.5;
			float lerpResult61 = lerp( _FlickerMin , _FlickerMax , saturate( ( floor( ( simplePerlin2D65 * 4.0 ) ) * 0.25 ) ));
			float2 temp_output_35_0 = (( ( mul( float4( ase_worldNormal , 0.0 ), UNITY_MATRIX_V ).xyz * float3( 0.5,0.5,0.5 ) * 0.95 ) + float3( -0.5,-0.5,-0.5 ) )).xy;
			float mulTime39 = _Time.y * 0.1;
			float cos38 = cos( mulTime39 );
			float sin38 = sin( mulTime39 );
			float2 rotator38 = mul( temp_output_35_0 - float2( -0.5,-0.5 ) , float2x2( cos38 , -sin38 , sin38 , cos38 )) + float2( -0.5,-0.5 );
			float fresnelNdotV53 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode53 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV53, 5.0 ) );
			float4 ase_screenPos4 = i.screenPosition4;
			float4 ase_screenPosNorm4 = ase_screenPos4 / ase_screenPos4.w;
			ase_screenPosNorm4.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm4.z : ase_screenPosNorm4.z * 0.5 + 0.5;
			float screenDepth4 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm4.xy ));
			float distanceDepth4 = abs( ( screenDepth4 - LinearEyeDepth( ase_screenPosNorm4.z ) ) / ( 0.0 ) );
			float temp_output_6_0 = saturate( distanceDepth4 );
			float3 ase_vertexNormal = mul( unity_WorldToObject, float4( ase_worldNormal, 0 ) );
			ase_vertexNormal = normalize( ase_vertexNormal );
			float cos102 = cos( radians( 45.0 ) );
			float sin102 = sin( radians( 45.0 ) );
			float2 rotator102 = mul( (WorldReflectionVector( i , ase_vertexNormal )).yz - float2( -0.5,-0.5 ) , float2x2( cos102 , -sin102 , sin102 , cos102 )) + float2( -0.5,-0.5 );
			float smoothstepResult118 = smoothstep( 0.5 , 1.0 , pow( sin( ( (rotator102).x + _Time.y ) ) , 100.0 ));
			o.Emission = max( ( temp_output_15_0 * ( ( lerpResult46 * tex2D( _VideoTex, uv_VideoTex ) * lerpResult61 ) + ( tex2D( _MatCap, rotator38 ) * saturate( fresnelNode53 ) * 10.0 ) ) * pow( temp_output_6_0 , 5.0 ) ) , ( saturate( (smoothstepResult118).xxxx ) * _SheenBrightness ) ).xyz;
			o.Alpha = ( temp_output_6_0 * _Opacity );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
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
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 customPack2 : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
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
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.customPack2.xyzw = customInputData.screenPosition4;
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
				surfIN.screenPosition4 = IN.customPack2.xyzw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.worldRefl = -worldViewDir;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.NormalVertexDataNode;114;-877.197,1570.309;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldReflectionVector;100;-638.6335,1576.422;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ViewMatrixNode;31;-570.9225,442.5386;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.WorldNormalVector;36;-633.9309,299.2346;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleTimeNode;64;-1386.067,187.2474;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;101;-418.2282,1615.572;Inherit;True;FLOAT2;1;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RadiansOpNode;105;-216.313,1857.096;Inherit;False;1;0;FLOAT;45;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-426.9225,346.5386;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;65;-1006.224,156.2704;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-391.0834,622.708;Inherit;False;Constant;_Float1;Float 1;5;0;Create;True;0;0;0;False;0;False;0.95;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;102;-172.4592,1562.176;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT2;-0.5,-0.5;False;2;FLOAT;0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;8;-953.4612,-253.3703;Inherit;False;Property;_EmissiveColor;EmissiveColor;1;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;3.482202,3.482202,3.482202,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;-808.63,149.4168;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-295.9225,486.5386;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0.5;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwizzleNode;103;116.0786,1572.823;Inherit;True;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;104;133.5467,1810.026;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode;47;-671.5054,-250.887;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FloorOpNode;67;-658.63,141.4168;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;34;-151.9225,393.5386;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;-0.5,-0.5,-0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;108;284.9288,1481.596;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;39;-432.0834,770.708;Inherit;False;1;0;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;52;-1085.783,-383.1468;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;3;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;-547.3911,-79.31293;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;-362.5054,-75.88702;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-477.63,135.4168;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;35;-49.92248,385.2386;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SinOpNode;106;314.845,1591.133;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;5;-664,22.5;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;40;-558.4749,213.9828;Inherit;False;Constant;_FadeRange;FadeRange;5;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;48;-380.5054,-198.887;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FresnelNode;53;195.2221,382.9532;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;54;-812.6233,-368.2167;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;38;-226.0834,638.708;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;-0.5,-0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;110;-348.9432,136.2796;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;107;484.5292,1608.743;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;158.1313,-32.43625;Inherit;False;Property;_FlickerMin;FlickerMin;7;0;Create;True;0;0;0;False;0;False;0.8;1.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;120;165.1313,40.56375;Inherit;False;Property;_FlickerMax;FlickerMax;6;0;Create;True;0;0;0;False;0;False;1.2;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;12;67.04688,-531.3302;Inherit;False;Standard;WorldNormal;ViewDir;True;True;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0.2;False;2;FLOAT;0.8;False;3;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;19;-27.71341,585.274;Inherit;True;Property;_MatCap;MatCap;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DepthFade;4;-430,11.5;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;111.9019,781.2172;Inherit;False;Constant;_Float0;Float 0;5;0;Create;True;0;0;0;False;0;False;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;45;582.0247,381.233;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;118;609.47,1828.739;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;61;333.0625,-13.72684;Inherit;False;3;0;FLOAT;0.8;False;1;FLOAT;1.2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;46;-136.4533,-257.8737;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;10;-357.8612,-436.7704;Inherit;True;Property;_VideoTex;_VideoTex;2;0;Create;True;0;0;0;True;0;False;-1;None;d6ae13c32a86c834f94b322a545ca8ef;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;6;-187.129,18.46149;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;13;421.6285,-511.7477;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;34.53878,-271.1705;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;329.1882,594.5986;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SwizzleNode;112;708.5291,1616.557;Inherit;True;FLOAT4;0;1;2;3;1;0;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PowerNode;9;-141.8612,132.2296;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;642.4844,-523.9556;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;169.7451,-334.3192;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;109;876.8949,1628.859;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;117;806.2056,1851.05;Inherit;False;Property;_SheenBrightness;SheenBrightness;5;0;Create;True;0;0;0;False;0;False;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-57.44238,298.2582;Inherit;False;Property;_Opacity;Opacity;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;268.1968,-246.4294;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;116;1014.754,1678.509;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;46.55762,175.2582;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;51;-31.87256,-14.39646;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;57;-1177.455,47.43298;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;58;-1181.655,327.933;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector;78;761.5991,711.9951;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;82;-898.2642,1162.104;Inherit;False;Constant;_Float2;Float 1;5;0;Create;True;0;0;0;False;0;False;0.95;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;79;-964.1505,885.0557;Inherit;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-803.1033,1025.935;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;81;-659.1033,932.9347;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;-0.5,-0.5,-0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwizzleNode;83;-557.1035,924.6348;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;72;-83.29553,1120.617;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;201.9099,877.5636;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;74;24.90991,979.5636;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;71;401.1413,945.6847;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;75;617.9236,939.2526;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;85;-66.52323,1469.122;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;86;218.6822,1226.069;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;87;41.6822,1328.069;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;88;417.9136,1294.19;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;89;634.6959,1287.758;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;90;-234.506,1224.47;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;-0.5,-0.5;False;2;FLOAT;0.002;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SinOpNode;70;741.7527,949.1563;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;91;1276.818,965.0799;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;96;1269.544,1245.153;Inherit;False;Constant;_Float3;Float 3;5;0;Create;True;0;0;0;False;0;False;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;2057.431,903.2795;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;76;928.9236,947.2526;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;50;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;69;-67.24731,886.1563;Inherit;True;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;77;-251.2783,875.9653;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;-0.5,-0.5;False;2;FLOAT;0.002;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;84;-50.47501,1234.661;Inherit;True;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;92;780.1002,1303.145;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;93;967.2711,1301.242;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;693.1678,-330.8043;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;94;1805.494,961.819;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SwizzleNode;95;1607.056,949.0713;Inherit;True;FLOAT4;0;1;2;3;1;0;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;115;706.0676,-126.4714;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;11;-618.8612,-514.7704;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;864.76,-158.0556;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;ProTV - ThirdParty/MissStabby - Hologram;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;1;False;;0;False;;False;0;False;;0;False;;True;0;Custom;0.5;True;True;0;True;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;100;0;114;0
WireConnection;101;0;100;0
WireConnection;32;0;36;0
WireConnection;32;1;31;0
WireConnection;65;0;64;0
WireConnection;102;0;101;0
WireConnection;102;2;105;0
WireConnection;66;0;65;0
WireConnection;33;0;32;0
WireConnection;33;2;37;0
WireConnection;103;0;102;0
WireConnection;47;0;8;0
WireConnection;67;0;66;0
WireConnection;34;0;33;0
WireConnection;108;0;103;0
WireConnection;108;1;104;0
WireConnection;50;0;47;3
WireConnection;49;0;47;2
WireConnection;68;0;67;0
WireConnection;35;0;34;0
WireConnection;106;0;108;0
WireConnection;48;0;47;1
WireConnection;48;1;49;0
WireConnection;48;2;50;0
WireConnection;54;0;52;0
WireConnection;38;0;35;0
WireConnection;38;2;39;0
WireConnection;110;0;68;0
WireConnection;107;0;106;0
WireConnection;19;1;38;0
WireConnection;4;1;5;0
WireConnection;4;0;40;0
WireConnection;45;0;53;0
WireConnection;118;0;107;0
WireConnection;61;0;119;0
WireConnection;61;1;120;0
WireConnection;61;2;110;0
WireConnection;46;0;48;0
WireConnection;46;1;8;0
WireConnection;46;2;54;0
WireConnection;6;0;4;0
WireConnection;13;0;12;0
WireConnection;7;0;46;0
WireConnection;7;1;10;0
WireConnection;7;2;61;0
WireConnection;27;0;19;0
WireConnection;27;1;45;0
WireConnection;27;2;28;0
WireConnection;112;0;118;0
WireConnection;9;0;6;0
WireConnection;15;0;13;0
WireConnection;26;0;7;0
WireConnection;26;1;27;0
WireConnection;109;0;112;0
WireConnection;25;0;15;0
WireConnection;25;1;26;0
WireConnection;25;2;9;0
WireConnection;116;0;109;0
WireConnection;116;1;117;0
WireConnection;16;0;6;0
WireConnection;16;1;17;0
WireConnection;58;0;64;0
WireConnection;80;0;79;0
WireConnection;81;0;80;0
WireConnection;83;0;81;0
WireConnection;73;0;69;0
WireConnection;73;1;74;0
WireConnection;71;0;73;0
WireConnection;71;1;72;0
WireConnection;75;0;71;0
WireConnection;86;0;84;0
WireConnection;86;1;87;0
WireConnection;88;0;86;0
WireConnection;88;1;85;0
WireConnection;89;0;88;0
WireConnection;90;0;83;0
WireConnection;70;0;75;0
WireConnection;91;0;76;0
WireConnection;91;1;93;0
WireConnection;91;2;96;0
WireConnection;76;0;70;0
WireConnection;69;0;77;0
WireConnection;77;0;35;0
WireConnection;84;0;90;0
WireConnection;92;0;89;0
WireConnection;93;0;92;0
WireConnection;14;0;13;0
WireConnection;14;1;15;0
WireConnection;94;0;95;0
WireConnection;95;0;91;0
WireConnection;115;0;25;0
WireConnection;115;1;116;0
WireConnection;0;2;115;0
WireConnection;0;9;16;0
ASEEND*/
//CHKSM=68A1CE4725385E7DA4AB31183CD90F0E7EC92829