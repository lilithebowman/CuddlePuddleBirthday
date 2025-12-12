float2 lilRotateUV(float2 uv, float angle)
{
    float si,co;
    sincos(angle / 360.0 * UNITY_TWO_PI, si, co);
    float2 outuv = uv - 0.5;
    outuv = float2(
        outuv.x * co - outuv.y * si,
        outuv.x * si + outuv.y * co
    );
    outuv += 0.5;
    return outuv;
}

float2 CalcWaveUV(float2 posW, float angle)
{
    float2 uvWave = posW * 0.25;
    uvWave = lilRotateUV(uvWave, angle);
    uvWave.y += sin((uvWave.x + _Time.x) * 50) * sin((uvWave.y + _Time.x) * 30) * 0.005;
    uvWave.y += _Time.x * 0.5;
    return uvWave;
}

half3 Caustics(float2 uv, half len)
{
    half3 R = lerp(0.75, half3(1.0,0.75,0.5), saturate(len * 2));
    half3 B = lerp(0.75, half3(0.5,0.75,1.0), saturate(len * 2));
    half factor = UnpackScaleNormal(_WaveNormal.Sample(sampler_linear_repeat, uv), 1).r*0.5+0.5;
    return _WaveHeight.Sample(sampler_linear_repeat, uv).r * lerp(R,B,factor);
}

void DoVertex(inout appdata i, out float4 pos, out float4 uv01, out float4 uv23, out float4 normal, out float4 tangent, out float4 binormal, out float3 V)
{
    float3 posWorld = O2W(i.vertex);
    pos = O2P(i.vertex);
    uv01 = float4(i.uv0, i.uv1);
    uv23 = float4(i.uv2, i.uv3);
    normal.xyz = O2WNormal(i.normal);
    tangent.xyz = O2WVector(i.tangent.xyz);
    binormal.xyz = ComputeBinormal(normal.xyz, tangent.xyz, i.tangent.w);
    normal.w = posWorld.x;
    tangent.w = posWorld.y;
    binormal.w = posWorld.z;
    V = O2VDir(i.vertex);
}

bool isZeroVector(float3 a)
{
    return dot(a,a) == 0;
}

float VolumetricFog(half3 V, float3 cameraPos, float3 opaquePos, float4 posSV)
{
    float sum = 0;
    half3 L = _WorldSpaceLightPos0.xyz;
    bool isSky = isZeroVector(opaquePos);
    float noise = ibuki(posSV);
    for(int i = 1; i < 8; i++)
    {
        float3 posWorld = cameraPos - V * (i + noise) * 0.2;
        half fade = isSky ? 1 : saturate(rawDistance(opaquePos,cameraPos) - rawDistance(posWorld,cameraPos));
        half len = -posWorld.y / L.y;
        float2 uvProj = posWorld.xz + L.xz * len;
        //float2 uvCaus = CalcWaveUV(uvProj, 0) * _WaveTiling;
        //float2 uvCaus2 = CalcWaveUV(uvProj, 180) * _WaveTiling;
        float2 uvCaus0 = CalcWaveUV(uvProj,  30) * _WaveTiling;
        float2 uvCaus1 = CalcWaveUV(uvProj, 150) * _WaveTiling;
        float2 uvCaus2 = CalcWaveUV(uvProj, 270) * _WaveTiling;
        sum += (_WaveHeight.Sample(sampler_linear_repeat, uvCaus0).r * _WaveHeight.Sample(sampler_linear_repeat, uvCaus1).r * _WaveHeight.Sample(sampler_linear_repeat, uvCaus2).r) * rsqrt(i) * fade;
    }
    float LdotV = 2.5-dot(L,V) * 2;
    return sum * _VolumetricFog * LdotV;
}

half4 ReflectionAndFoam(v2f i, float4 posSV, float3 posWorld, half3 V, half3 tangent, half3 binormal, half3 normal, bool isFront, out float polyFade)
{
    float2 uvScreen = ScreenUV(posSV);
    half3x3 matrixTBN = half3x3(
        half3(1,0,0),
        half3(0,0,-1),
        half3(0, normal.y > 0 ? 1 : -1, 0)
    );

    ShadingParams p = (ShadingParams)0;
    p.posWorld = posWorld;
    p.V = V;
    p.origN = normal;
    p.metallic = 0;
    p.occlusion = 1;
    p.reflectance = isFront ? 0.1 : 1;
    p.specular = 0.1;
    p.T = tangent;
    p.B = binormal;
    p.anisotropy = 0;

    half waveFace = abs(normal.y);
    float3 opaquePos = GetOpaquePosW(uvScreen, V);
    half depthDiff = dot(opaquePos,opaquePos) == 0 ? 100000 : distance(opaquePos, p.posWorld);

    float2 uvWave0 = CalcWaveUV(p.posWorld.xz,  30) * _WaveTiling;
    float2 uvWave1 = CalcWaveUV(p.posWorld.xz, 150) * _WaveTiling;
    float2 uvWave2 = CalcWaveUV(p.posWorld.xz, 270) * _WaveTiling;

    half waveHeight0 = _WaveHeight.Sample(sampler_linear_repeat, uvWave0);
    half waveHeight1 = _WaveHeight.Sample(sampler_linear_repeat, uvWave1);
    half waveHeight2 = _WaveHeight.Sample(sampler_linear_repeat, uvWave2);
    half waveHeight = waveHeight0 + waveHeight1 + waveHeight2;
    waveHeight *= waveFace;
    half3 waveNormal0 = UnpackScaleNormal(_WaveNormal.Sample(sampler_linear_repeat, uvWave0), 0.15);
    half3 waveNormal1 = UnpackScaleNormal(_WaveNormal.Sample(sampler_linear_repeat, uvWave1), 0.15);
    half3 waveNormal2 = UnpackScaleNormal(_WaveNormal.Sample(sampler_linear_repeat, uvWave2), 0.15);
    half3 tangentNormal = half3(waveNormal0.xy + waveNormal1.xy + waveNormal2.xy, waveNormal0.z * waveNormal1.z * waveNormal2.z);
    p.N = mul(tangentNormal, matrixTBN);
    p.N = normalize(lerp(normal, p.N, waveFace));
    p.bentN = normalize(p.N-p.V*2);
    p.refN = p.N;

    p.smoothness = 0.98;
    p.perceptualRoughness = 1.2 - p.smoothness * 1.2;
    p.roughness = p.perceptualRoughness * p.perceptualRoughness;

    // Lighting
    p.oneMinusReflectionStrength = 1;
    half3 diff, spec, reflectionStrength;
    ComputeLights(diff, spec, reflectionStrength, p, i);
    p.oneMinusReflectionStrength *= saturate(1-reflectionStrength);

    // Mix
    half4 col = _WaterColor * waveHeight * waveHeight * 0.01;
    col.a = reflectionStrength;

    // 泡
    half NdotV = saturate(dot(p.N, p.V));
    float2 uvForm = p.posWorld.xz * 2 + _Time.x * 0.02 + waveHeight * half2(dot(tangent,p.V), dot(binormal,p.V)) * (1-NdotV) * 0.2;
    half foam = _FoamNoiseTex.Sample(sampler_linear_repeat, uvForm).r;
    half foamFactor = saturate(foam + 1 - depthDiff * 5);
    col.rgb += pow(foamFactor,2) * 1.5 * waveFace;
    col.rgb *= diff;

    col.rgb += spec;

    // 屈折
    float factor = 1 - saturate(1 / (depthDiff * depthDiff * 20 + 1));
    half3 uvN = mul(UNITY_MATRIX_V, p.N);
    float2 uvRefract = ClampScreenUV(uvScreen - uvN.xz * 0.01 * saturate(factor * factor * factor * 10) / (posSV.w + 0.1));
    opaquePos = GetOpaquePosW(uvRefract, V);
    depthDiff = isZeroVector(opaquePos) ? 100000 : distance(opaquePos, p.posWorld);

    // ポリゴンが目立たないようにフェード
    polyFade = saturate(depthDiff * 10 + waveHeight * 0.333333 - 1);
    col *= polyFade;

    #if !defined(UNITY_PASS_FORWARDADD) && !defined(IGNORE_GRABPASS)
    col.rgb += SampleScreen(uvRefract).rgb * (1-col.a);
    col.a = 1;
    #endif

    return col;
}

half4 Shading(v2f i, float4 posSV, float3 posWorld, half3 V, half3 tangent, half3 binormal, half3 normal, float2 uv[4], bool isFront, inout float depth)
{
    float a;
    half4 col = ReflectionAndFoam(i, posSV, posWorld, V, tangent, binormal, normal, isFront, a);

    ShadingParams p = (ShadingParams)0;
    p.posWorld = posWorld;
    p.V = V;
    col.rgb = col.a == 0 ? col.rgb : col.rgb / col.a;
    DoFog(i, col, p);
    col.rgb *= col.a;
    return col;
}

// 水中のコースティクスとフォグ
half4 ShadingUnderwater(v2f i, float4 posSV, float3 posWorld, half3 V, half3 tangent, half3 binormal, half3 normal, float2 uv[4], bool isFront, inout float depth)
{
    half4 col = 0;
    if(IsCameraDepthGenerated())
    {
        float2 uvScreen = ScreenUV(posSV);
        float3 opaquePos = GetOpaquePosW(uvScreen, V);
        half depthDiff = distance(opaquePos, posWorld);

        ShadingParams p = (ShadingParams)0;
        p.uv = uv;
        p.posWorld = opaquePos;
        p.V = V;
        p.metallic = 0;
        p.occlusion = 1;
        p.reflectance = 0.1;
        p.specular = 0.1;
        p.T = tangent;
        p.B = binormal;
        p.N = half3(0,1,0);
        p.bentN = p.N;
        p.refN = p.N;
        p.origN = normal;
        p.anisotropy = 0;
        p.smoothness = 1;
        p.perceptualRoughness = 1.2 - p.smoothness * 1.2;
        p.roughness = p.perceptualRoughness * p.perceptualRoughness;

        bool isSky = isZeroVector(opaquePos);
        bool isOut = rawDistance(opaquePos,GetCameraPos()) > rawDistance(posWorld,GetCameraPos()) || isSky;
        //if(isFront && isSky) discard;

        //水中から見た水面
        float polyFade;
        half4 facecol = ReflectionAndFoam(i, posSV, posWorld, V, tangent, binormal, normal, isFront, polyFade);
        if(!isFront && isOut)
        {
            half3 L = _WorldSpaceLightPos0.xyz;
            float LdotV = pow(saturate(-dot(L,V) * 10 - 9), 3);
            facecol.rgb *= 1 + LdotV * 10 * _WaterColor.rgb * _LightColor0.rgb;
            facecol.rgb *= 6 * saturate(-normal.y) + 1;
            col = facecol;
        }

        // コースティクス
        half3 oL = normalize(UnityWorldSpaceLightDir(opaquePos));
        half len = -opaquePos.y / oL.y;
        float2 uvProj = opaquePos.xz + oL.xz * len;
        float2 uvCaus0 = CalcWaveUV(uvProj,  30) * _WaveTiling;
        float2 uvCaus1 = CalcWaveUV(uvProj, 150) * _WaveTiling;
        float2 uvCaus2 = CalcWaveUV(uvProj, 270) * _WaveTiling;
        half3 caus =
            Caustics(uvCaus0, len) +
            Caustics(uvCaus1, len) +
            Caustics(uvCaus2, len);
        caus *= _Caustics;
        caus *= isFront ? polyFade : saturate(depthDiff);

        half3 diff, spec, reflectionStrength;
        ComputeLights(diff, spec, reflectionStrength, p, i);

        // 水の中
        if((isFront || !isOut) && !isSky)
        {
            col.rgb += caus * diff * _WaterColor.rgb; // コースティクス
            float darkInWater = isFront ? polyFade : (!isOut ? saturate(depthDiff * 5) : 0);
            col.a = saturate(col.a + darkInWater * 0.5); // 暗く
        }

        // フォグ
        float3 cameraPos = isFront ? posWorld : GetCameraPos();
        float3 objPos = !isFront && isOut || isSky ? posWorld : opaquePos;
        float fog = rawDistance(objPos,cameraPos) + _WaterFogDistance;
        fog = 1.0-saturate(_WaterFogDistance / fog);

        col = lerp(col, half4(_WaterColorFog.rgb * diff, 1), fog * _WaterColorFog.a);

        if(isFront) col *= polyFade;
    }
    return col;
}

// 水中のフォグ
half4 ShadingVolumetricFog(v2f i, float4 posSV, float3 posWorld, half3 V, half3 tangent, half3 binormal, half3 normal, float2 uv[4], bool isFront, inout float depth)
{
    half4 col = 0;
    if(IsCameraDepthGenerated())
    {
        float2 uvScreen = ScreenUV(posSV);
        float3 opaquePos = GetOpaquePosW(uvScreen, V);
        half depthDiff = distance(opaquePos, posWorld);

        ShadingParams p = (ShadingParams)0;
        p.uv = uv;
        p.posWorld = opaquePos;
        p.V = V;
        p.metallic = 0;
        p.occlusion = 1;
        p.reflectance = 0.1;
        p.specular = 0.1;
        p.T = tangent;
        p.B = binormal;
        p.N = half3(0,1,0);
        p.bentN = p.N;
        p.refN = p.N;
        p.origN = normal;
        p.anisotropy = 0;
        p.smoothness = 1;
        p.perceptualRoughness = 1.2 - p.smoothness * 1.2;
        p.roughness = p.perceptualRoughness * p.perceptualRoughness;

        bool isSky = isZeroVector(opaquePos);
        bool isOut = rawDistance(opaquePos,GetCameraPos()) > rawDistance(posWorld,GetCameraPos()) || isSky;

        half3 diff, spec, reflectionStrength;
        ComputeLights(diff, spec, reflectionStrength, p, i);

        // フォグ
        float3 cameraPos = isFront ? posWorld : GetCameraPos();
        float3 objPos = !isFront && isOut ? posWorld : opaquePos;

        //if(!isFront)
        {
            col = lerp(0, half4(_WaterColor.rgb * diff, 1), VolumetricFog(V,cameraPos,objPos,posSV));
        }

        DoFog(i, col, p);
    }
    return col;
}

half4 UnpackAndShading(v2f i, float4 inormal, float4 itangent, float4 ibinormal, float3 iV, float4 iuv01, float4 iuv23, bool isFront, inout float depth)
{
    float3 posWorld = float3(inormal.w, itangent.w, ibinormal.w);
    half3 V = GetVDir(posWorld, iV);
    half3 tangent = normalize(itangent.xyz);
    half3 binormal = normalize(ibinormal.xyz);
    half3 normal = inormal.xyz;
    if(!isFront)
    {
        tangent = -tangent;
        binormal = -binormal;
        normal = -normal;
    }
    if(dot(normal, V) < 0) normal = Ortho(normal, V);
    normal = normalize(normal);
    float2 uv[4];
    uv[0] = iuv01.xy;
    uv[1] = iuv01.zw;
    uv[2] = iuv23.xy;
    uv[3] = iuv23.zw;
    return Shading(i, i.pos, posWorld, V, tangent, binormal, normal, uv, isFront, depth);
}

half4 UnpackAndShadingUnderwater(v2f i, float4 inormal, float4 itangent, float4 ibinormal, float3 iV, float4 iuv01, float4 iuv23, bool isFront, inout float depth)
{
    float3 posWorld = float3(inormal.w, itangent.w, ibinormal.w);
    half3 V = GetVDir(posWorld, iV);
    half3 tangent = normalize(itangent.xyz);
    half3 binormal = normalize(ibinormal.xyz);
    half3 normal = inormal.xyz;
    if(!isFront)
    {
        tangent = -tangent;
        binormal = -binormal;
        normal = -normal;
    }
    if(dot(normal, V) < 0) normal = Ortho(normal, V);
    normal = normalize(normal);
    float2 uv[4];
    uv[0] = iuv01.xy;
    uv[1] = iuv01.zw;
    uv[2] = iuv23.xy;
    uv[3] = iuv23.zw;
    return ShadingUnderwater(i, i.pos, posWorld, V, tangent, binormal, normal, uv, isFront, depth);
}

half4 UnpackAndShadingVolumetricFog(v2f i, float4 inormal, float4 itangent, float4 ibinormal, float3 iV, float4 iuv01, float4 iuv23, bool isFront, inout float depth)
{
    float3 posWorld = float3(inormal.w, itangent.w, ibinormal.w);
    half3 V = GetVDir(posWorld, iV);
    half3 tangent = normalize(itangent.xyz);
    half3 binormal = normalize(ibinormal.xyz);
    half3 normal = inormal.xyz;
    if(!isFront)
    {
        tangent = -tangent;
        binormal = -binormal;
        normal = -normal;
    }
    if(dot(normal, V) < 0) normal = Ortho(normal, V);
    normal = normalize(normal);
    float2 uv[4];
    uv[0] = iuv01.xy;
    uv[1] = iuv01.zw;
    uv[2] = iuv23.xy;
    uv[3] = iuv23.zw;
    return ShadingVolumetricFog(i, i.pos, posWorld, V, tangent, binormal, normal, uv, isFront, depth);
}
