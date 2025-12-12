#define LIL_VRCHAT

// Platform Variables
float _VRChatCameraMode;
uint _VRChatCameraMask;
float _VRChatMirrorMode;
float3 _VRChatMirrorCameraPos;
float3 _VRChatScreenCameraPos;
float4 _VRChatScreenCameraRot;
float3 _VRChatPhotoCameraPos;
float4 _VRChatPhotoCameraRot;

#define _HideShaderLayer _UdonHideShaderLayer
#define _VFogNoise _UdonVFogNoise
#define _VFogDensity _UdonVFogDensity
#define _VFogScrollX _UdonVFogScrollX
#define _VFogScrollZ _UdonVFogScrollZ
#define _VFogHeightScale _UdonVFogHeightScale
#define _VFogHeightOffset _UdonVFogHeightOffset
#define _VFogHeightSharpness _UdonVFogHeightSharpness

bool IsDesktop()
{
    return _VRChatCameraMode == 0;
}

bool IsCamera()
{
    return _VRChatCameraMode == 1 || _VRChatCameraMode == 2;
}

bool IsScreenshot()
{
    return _VRChatCameraMode == 3;
}

bool IsMirror()
{
    return _VRChatMirrorMode != 0;
}

// Shader Variables
bool _HideInDesktop;
bool _HideInVR;
bool _HideInCamera;
bool _HideInScreenshot;
bool _HideInMirror;
bool _HideInNotMirror;

bool IsShow(bool isVR)
{
    bool hide = false;
    hide = hide || _HideInDesktop && IsDesktop();
    hide = hide || _HideInVR && isVR;
    hide = hide || _HideInCamera && IsCamera();
    hide = hide || _HideInScreenshot && IsScreenshot();
    hide = hide || _HideInMirror && IsMirror();
    hide = hide || _HideInNotMirror && !IsMirror();
    return !hide;
}
