#undef PROTV_VARIABLES_IMPLICIT
#ifndef PROTV_CORE_VARIABLES_INCLUDED
#define PROTV_CORE_VARIABLES_INCLUDED
#define PROTV_VARIABLES_IMPLICIT
// if you want to control defining these yourself, set the above define externally and then include this file.
// That will include the functions without the field defines.

// meta pass might include the UnityStandardInput file via UnityStandardMeta.cginc
// check that _MainTex hasn't yet been defined from that file.
#ifndef UNITY_STANDARD_INPUT_INCLUDED
sampler2D _MainTex;
float4 _MainTex_ST;
#endif
float4 _MainTex_TexelSize;

// Use explicit sampler state to deal with the texture resizing
Texture2D _VideoTex;
SamplerState sampler_VideoTex;
float4 _VideoTex_ST;
float4 _VideoTex_TexelSize;
float4x4 _VideoData;
float4 _GammaZone;

uniform Texture2D _Udon_VideoTex;
uniform SamplerState sampler_Udon_VideoTex;
uniform float4 _Udon_VideoTex_ST;
uniform float4 _Udon_VideoTex_TexelSize;
uniform float4x4 _Udon_VideoData;


float _Mirror = 1;

float _3D;
float _Wide;
float _Force2D;
float _FadeEdges;

float _Aspect;
int _AspectFitMode;
float _Brightness;

uniform float _VRChatMirrorMode;
uniform float3 _VRChatMirrorCameraPos;

#endif

#ifndef PROTV_CORE_FUNCTIONS_INCLUDED
#define PROTV_CORE_FUNCTIONS_INCLUDED

#ifndef PROTV_VARIABLES_IMPLICIT
uniform float _VRChatMirrorMode;
uniform float3 _VRChatMirrorCameraPos;
float _Mirror = 1;
#endif

bool IsMirror() { return _Mirror && _VRChatMirrorMode; }

bool IsRightEye()
{
    #ifdef USING_STEREO_MATRICES
    return unity_StereoEyeIndex == 1;
    #else
    // include stereoeyeindex here cause pico is a dumb pos
    return unity_StereoEyeIndex == 1
        || _VRChatMirrorMode == 1 && mul(unity_WorldToCamera, float4(_VRChatMirrorCameraPos, 1)).x < 0;
    #endif
}

bool IsDesktop()
{
    #ifdef USING_STEREO_MATRICES
    return false;
    #else
    return _VRChatMirrorMode != 1;
    #endif
}

bool IsDefaultTextureSize(float dimensionX, float dimensionY) { return dimensionX <= 16 && dimensionY <= 16; }
bool IsDefaultTextureSize(float2 dimensions) { return IsDefaultTextureSize(dimensions.x, dimensions.y); }

bool IsLocked(float4x4 data) { return int(data._11) >> 0 & 1; }
bool IsMuted(float4x4 data) { return int(data._11) >> 1 & 1; }
bool IsLive(float4x4 data) { return int(data._11) >> 2 & 1; }
bool IsLoading(float4x4 data) { return int(data._11) >> 3 & 1; }
bool IsForce2D(float4x4 data) { return int(data._11) >> 4 & 1; }

float Seek(float4x4 data) { return data._22; }
float Mode3D(float4x4 data) { return abs(data._41); }
float Wide3D(float4x4 data) { return sign(data._41); }

bool IsTVReady(float4x4 data) { return data._14; }
bool IsTVWaiting(float4x4 data) { return data._12 == 0; }
bool IsTVStopped(float4x4 data) { return data._12 == 1; }
bool IsTVPlaying(float4x4 data) { return data._12 == 2; }
bool IsTVPaused(float4x4 data) { return data._12 == 3; }
bool IsTVActive(float4x4 data) { return data._12 > 1; }
bool IsTVHalted(float4x4 data) { return data._12 <= 1; }
bool IsMediaActive(const float4x4 data) { return IsLive(data) || Seek(data) > 0 && Seek(data) < 1; }

bool Is3DSideBySide(float4x4 data)
{
    const int mode = abs(data._41);
    return mode == 1 || mode == 2;
}

bool Is3DOverUnder(float4x4 data)
{
    const int mode = abs(data._41);
    return mode == 3 || mode == 4;
}

bool Is3DSwapped(float4x4 data)
{
    const int mode = abs(data._41);
    return mode == 2 || mode == 3;
}

// Adjusts the UV such that the resolution is scaled to fit within the given aspect ratio, retaining the original aspect ratio 
void TVAspectRatio(inout float2 uv, const float aspect, const float2 res, const float2 center, const int fitMode)
{
    if (aspect == 0) return; // aspect of 0 means no adjustments made
    if (abs(res.x / res.y - aspect) <= 0.0001) return; // if res is close enough, skip adjustment
    float2 normalizedResolution = float2(res.x / aspect, res.y);
    // determine corrective axis
    float correctiveAxis = normalizedResolution.x > normalizedResolution.y;
    float2 correction =
        correctiveAxis
            ? float2(1, normalizedResolution.y / normalizedResolution.x) // height needs corrected
            : float2(normalizedResolution.x / normalizedResolution.y, 1); // width needs corrected
    // apply normalized correction anchored to given center
    float2 corrected = (uv - center) / correction;
    // adjust for fit-outside, multiple both axes in normalized space by the axis that was originally corrected
    if (fitMode == 1) corrected *= correctiveAxis ? correction.y : correction.x;
    uv = corrected + center;
}

#ifndef _VISIBILITY_FADE_WIDTH_IN_PIXELS
#define _VISIBILITY_FADE_WIDTH_IN_PIXELS 2
#endif
// Detection for whether the uv pixel is within the image or if it's outside the image.
// calculates against a padding width for edge fading. Configurable in custom shaders
// by defining _VISIBILITY_FADE_WIDTH_IN_PIXELS as a positive int
void TVAspectVisibility(const float2 uv, const float2 res, float4 uvClip, out float visible)
{
    // make the span of the anti-alias fix span the size of X pixels of the source texture
    const float2 uvPadding = (_VISIBILITY_FADE_WIDTH_IN_PIXELS / res);
    // get the amount of presence that the uv has on the minimum side, use uvClip to letterbox/pillarbox the visibility
    const float2 minFactor = smoothstep(uvClip.xy, uvClip.xy + uvPadding, uv);
    // get the amount of presence that the uv has on the maximum side, use uvClip to letterbox/pillarbox the visibility
    const float2 maxFactor = smoothstep(uvClip.zw, uvClip.zw - uvPadding, uv);
    // multiply them all together. If any of the factor edges are 0, it is considered not visible
    visible = maxFactor.x * maxFactor.y * minFactor.x * minFactor.y;
}

// modifies the UV and video dimensions based on the 3D mode and which eye is currently rendering
void TV3DAdjustment(
    inout float2 uv, inout float2 videoDims,
    inout float4 clip, inout float2 center,
    int mode3D, int wide3D, bool force2D
)
{
    // Calculate eye selection for stereo
    // because the UV starts at the bottom, to properly flip OVUN, we 'swap' the non-swapped mode
    const float swapMask = step(1.5, mode3D) * step(mode3D, 3.5); // true when mode3D == 2 or 3
    const float rightEye = abs(swapMask - IsRightEye());
    const float stereoEyeRight = (1.0 - force2D) * rightEye * 0.5;

    // Side-by-side modes (1 or 2)
    const float isSideBySide = step(0.5, mode3D) * step(mode3D, 2.5);
    const float2 sbsUVClipXZ = float2(stereoEyeRight, stereoEyeRight + 0.5);
    const float sbsUVX = uv.x * 0.5 + stereoEyeRight;
    const float sbsDimX = saturate(-wide3D) ? videoDims.x * 0.5 : videoDims.x;

    // Over-under modes (3 or 4)
    const float isOverUnder = step(2.5, mode3D) * step(mode3D, 4.5);
    const float2 ouUVClipYW = float2(stereoEyeRight, stereoEyeRight + 0.5);
    const float ouUVY = uv.y * 0.5 + stereoEyeRight;
    const float ouDimY = saturate(-wide3D) ? videoDims.y * 0.5 : videoDims.y;

    // Blend results based on mode
    clip = float4(0, 0, 1, 1);
    clip.xz = isSideBySide ? sbsUVClipXZ : clip.xz;
    clip.yw = isOverUnder ? ouUVClipYW : clip.yw;

    uv.x = isSideBySide ? sbsUVX : uv.x;
    uv.y = isOverUnder ? ouUVY : uv.y;

    videoDims.x = isSideBySide ? sbsDimX : videoDims.x;
    videoDims.y = isOverUnder ? ouDimY : videoDims.y;

    // determine the effective center of the uv
    center = (clip.xy + clip.zw) * 0.5;
}

void TVGammaZoneAdjustment(inout float2 uv, inout float4 clip, inout float2 uvCenter, float4 gammaST)
{
    // Update UV to match gamma zone
    uv = uv * gammaST.xy + gammaST.zw;

    // Get the most relevant edge for the clip zone
    clip.xy = max(clip.xy, gammaST.zw);
    clip.zw = min(clip.zw, gammaST.zw + gammaST.xy);

    // Recalculate the center point based on the adjusted clip region
    uvCenter = (clip.xy + clip.zw) * 0.5;
}

#define TEMP_IS_MIRROR_MODE(mode) (step(mode-0.5, _Mirror) * (1.0 - step(mode+0.5, _Mirror)))
#define TEMP_DDX_NEGATIVE(uv) step(ddx(uv.x), -0.0001)
#define TEMP_DDY_NEGATIVE(uv) step(ddy(uv.y), -0.0001)

// adjust uv if rendering in mirror
void TVMirrorAdjustment(inout float2 uv)
{
    float mode1 = TEMP_IS_MIRROR_MODE(1);
    float mode2 = TEMP_IS_MIRROR_MODE(2);
    float2 flipMask = float2(
        mode1 * IsMirror() + mode2 * TEMP_DDX_NEGATIVE(uv), // Flip X conditions
        mode2 * TEMP_DDY_NEGATIVE(uv) // Flip Y conditions
    );
    uv = lerp(uv, 1.0 - uv, flipMask);
}

#undef TEMP_IS_MIRROR_MODE
#undef TEMP_DDX_NEGATIVE
#undef TEMP_DDY_NEGATIVE

void TVFadeEdges(inout float4 tex, float visible, bool doFade)
{
    // when fade edges is disabled, force any non-zero visibility to 1
    visible = doFade ? visible : ceil(visible);
    // blend between invisible and the sampled texture pixel based on the visibility.
    tex = lerp((0).xxxx, tex, visible);
}

#endif

#ifndef PROTV_CORE_PROCESSORS_INCLUDED
#define PROTV_CORE_PROCESSORS_INCLUDED

struct FragmentProcessingData
{
    float2 inputUV;
    float4 videoST;
    float4 gammaST;
    float4x4 videoData;
    int mode3D;
    int wide3D;
    bool force2D;
    float outputAspect;
    int aspectFit;
    bool fadeEdges;
    float brightness;
    float2 videoSize;
    bool noVideo;
};

FragmentProcessingData InitializeFragmentData(float2 uv)
{
    FragmentProcessingData data = (FragmentProcessingData)0;
    data.inputUV = uv;
#ifdef _USEGLOBALTEXTURE
    {
        data.videoST = _Udon_VideoTex_ST;
        data.videoData = _Udon_VideoData;
    }
#else
    {
        data.videoST = _VideoTex_ST;
        data.videoData = _VideoData;
    }
#endif
#ifdef _USEGLOBALTEXTURE
    _Udon_VideoTex.GetDimensions(data.videoSize.x, data.videoSize.y);
#else
    _VideoTex.GetDimensions(data.videoSize.x, data.videoSize.y);
#endif
    data.noVideo = IsDefaultTextureSize(data.videoSize);
    if (data.noVideo) data.videoSize = _MainTex_TexelSize.zw;
    data.outputAspect = _Aspect;
    data.aspectFit = _AspectFitMode;
    data.brightness = _Brightness;
    data.fadeEdges = _FadeEdges;
    data.mode3D = _3D;
    data.wide3D = _Wide;
    data.force2D = _Force2D;
    if (all(_GammaZone == (float4)0))
        _GammaZone = float4(1, 1, 0, 0);
    data.gammaST = _GammaZone;
    return data;
}

float2 ProcessFragmentUV(const FragmentProcessingData data, out float visible)
{
    float2 uv = data.inputUV;
    float2 videoDims = data.videoSize;
    bool noVideo = data.noVideo;
    const float4x4 videoData = data.videoData;

    float4 uvClip;
    float2 uvCenter;
    // use the 3D value from the TVData object when 3D is None
    const float mode3D = noVideo ? data.mode3D : Mode3D(videoData);
    const float wide3D = noVideo ? data.wide3D : Wide3D(videoData);
    // Setting force 2D to 1 makes both eyes render
    const float force2D = noVideo ? data.force2D : IsForce2D(videoData);

    float4 videoST = noVideo ? _MainTex_ST : data.videoST;
    uv = uv * videoST.xy + videoST.zw;

    // correct for any non-standard viewing orientations
    TVMirrorAdjustment(uv);
    // normalize uv space for 3d considerations
    TV3DAdjustment(uv, videoDims, uvClip, uvCenter, mode3D, wide3D, force2D);
    // update uv to ensure source image aspect is respected
    #if _CROP_GAMMAZONE// && !_USEGLOBALTEXTURE
    if (!noVideo) TVGammaZoneAdjustment(uv, uvClip, uvCenter, data.gammaST);
    #endif
    TVAspectRatio(uv, data.outputAspect, videoDims, uvCenter, data.aspectFit);
    // calculate what pixels are a part of the image or are outside the image.
    TVAspectVisibility(uv, videoDims, uvClip, visible);

    // optionally clip pixels outside the image
#if _CLIP_BORDERS
    clip((1 - ceil(visible)) * -1);
#endif

    return uv;
}

float4 ProcessFragment(const FragmentProcessingData data)
{
    // The fragment processor goes in the following order:
    // - Correct uv for respective _ST values.
    // - If the uv is detected to be rendering in a mirror, flip as needed
    // - Adjust uv for the different 3D modes
    // - Apply any necessary aspect ratio correction
    // - Detect pixels that are outside the resulting texture space
    // - Optionally clip the pixels
    // - Get the target pixel for the uv
    // - Fade edge pixels for anti-aliasing
    // - Apply a brightness multiplier to the returned pixel

    float visible;
    float2 uv = ProcessFragmentUV(data, visible);

    // sample the texture
    float4 tex;
    if (data.noVideo) tex = tex2D(_MainTex, uv);
#if _USEGLOBALTEXTURE
    else tex = _Udon_VideoTex.Sample(sampler_Udon_VideoTex, uv);
#else
    else tex = _VideoTex.Sample(sampler_VideoTex, uv);
#endif

    // blend edge pixels to black/transparent
    // serves as a poor man's anti-alias
    TVFadeEdges(tex, visible, data.fadeEdges);
    return tex * data.brightness;
}

void ProcessFragmentClipOnly(const FragmentProcessingData data)
{
    float visible;
    ProcessFragmentUV(data, visible);
    clip((1 - ceil(visible)) * -1);
}

#endif
#undef PROTV_VARIABLES_IMPLICIT
