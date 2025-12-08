using System;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    public partial class TVManager
    {
        private readonly Vector4 DEFAULTST = new Vector4(1, 1, 0, 0);
        private readonly Vector4 DEFAULTTEXELSIZE = new Vector4(1f/16, 1f/16, 16f, 16f);

        [SerializeField] private RenderTexture _internalTexture;
        [SerializeField] private RenderTexture _globalTexture;
        private Texture _sourceTexture;
        private Vector4 _sourceTextureST;
        private bool _hasInternalTexture = false;
        private bool _hasCustomMaterials;
        private bool _customTextureIsGlobal = false;
        internal int _sourceTextureWidth;
        internal int _sourceTextureHeight;
        private int _globalTextureWidth;
        private int _globalTextureHeight;
        private Vector4 _internalTextureTexelSize;
        private Vector4 _globalTextureTexelSize;

        internal Vector4 lastGammaZoneST = Vector4.zero;
        internal Vector4 lastGlobalST = Vector4.zero;
        internal TVTextureTransformMode lastGammaZoneTransformMode = TVTextureTransformMode.ASIS;

        private const string shaderName_MainTex_ST_Override = "_MainTex_ST_Override";
        internal const string shaderNameGlobal_Udon_VideoTex = "_Udon_VideoTex";
        internal const string shaderNameGlobal_Udon_VideoData = "_Udon_VideoData";
        internal const string shaderName_VideoData = "_VideoData";
        internal const string shaderName_ForceAspect = "_ForceAspect";
        internal const string shaderName_Brightness = "_Brightness";
        internal const string shaderName_SkipGamma = "_SkipGamma";
        internal const string shaderName_GammaZone = "_GammaZone";
        internal const string shaderName_3D = "_3D";
        internal const string shaderName_FadeEdges = "_FadeEdges";
        internal const string shaderName_AspectFitMode = "_AspectFitMode";
        internal const string shaderName_AVPro = "_AVPro";

        private int shaderID_MainTex_ST_Override;
        private int shaderIDGlobal_Udon_VideoTex;
        private int shaderIDGlobal_Udon_VideoTex_ST;
        private int shaderIDGlobal_Udon_VideoTex_TexelSize;
        private int shaderIDGlobal_Udon_VideoData;
        private int[] shaderIDs_MaterialProperties = new int[0];
        private int[] shaderIDs_MaterialProperties_TexelSize = new int[0];
        private int shaderID_VideoData;
        private int shaderID_AVPro;
        private int shaderID_ForceAspect;
        private int shaderID_Brightness;
        private int shaderID_SkipGamma;
        private int shaderID_GammaZone;
        private int shaderID_3D;
        private int shaderID_FadeEdges;
        private int shaderID_AspectFitMode;

        private void SetupBlitData()
        {
            shaderID_MainTex_ST_Override = VRCShader.PropertyToID(shaderName_MainTex_ST_Override);
            shaderID_VideoData = VRCShader.PropertyToID(shaderName_VideoData);
            shaderIDGlobal_Udon_VideoTex = VRCShader.PropertyToID(shaderNameGlobal_Udon_VideoTex);
            shaderIDGlobal_Udon_VideoTex_ST = VRCShader.PropertyToID(shaderNameGlobal_Udon_VideoTex + "_ST");
            shaderIDGlobal_Udon_VideoTex_TexelSize = VRCShader.PropertyToID(shaderNameGlobal_Udon_VideoTex + "_TexelSize");
            shaderIDGlobal_Udon_VideoData = VRCShader.PropertyToID(shaderNameGlobal_Udon_VideoData);

            shaderID_ForceAspect = VRCShader.PropertyToID(shaderName_ForceAspect);
            shaderID_Brightness = VRCShader.PropertyToID(shaderName_Brightness);
            shaderID_SkipGamma = VRCShader.PropertyToID(shaderName_SkipGamma);
            shaderID_GammaZone = VRCShader.PropertyToID(shaderName_GammaZone);
            shaderID_3D = VRCShader.PropertyToID(shaderName_3D);
            shaderID_FadeEdges = VRCShader.PropertyToID(shaderName_FadeEdges);
            shaderID_AspectFitMode = VRCShader.PropertyToID(shaderName_AspectFitMode);
            shaderID_AVPro = VRCShader.PropertyToID(shaderName_AVPro);

            // derive the target property for the video texture for custom material from the TV settings.
            var matCount = customMaterials.Length;
            shaderIDs_MaterialProperties = new int[matCount];
            shaderIDs_MaterialProperties_TexelSize = new int[matCount];
            if (matCount != customMaterialProperties.Length)
                Error("Custom Material count does not equal Custom Material Properties count!!! This is unexpected and will crash the behaviour.");
            for (var index = 0; index < matCount; index++)
            {
                if (customMaterials[index] == null) continue;
                var prop = customMaterialProperties[index];
                if (string.IsNullOrWhiteSpace(prop)) prop = "_MainTex";
                shaderIDs_MaterialProperties[index] = VRCShader.PropertyToID(prop);
                shaderIDs_MaterialProperties_TexelSize[index] = VRCShader.PropertyToID(prop + "_TexelSize");
            }

            _hasCustomMaterials = matCount > 0;

            if (!_hasInternalTexture)
            {
                _internalTexture = createInternalTexture();
                _hasInternalTexture = true;
            }

            if (customTexture != null && autoResizeTexture)
            {
                if (customTexture.IsCreated()) customTexture.Release();
                _internalTextureTexelSize = DEFAULTTEXELSIZE;
                customTexture.width = 16;
                customTexture.height = 16;
            }
        }

        public void _Blit(VPManager manager)
        {
            // to enable swapping global tv target, always check for the most up to date value
            if (!init) Start();
            if (!_hasInternalTexture && !_hasCustomMaterials && !enableGSV) return; // nothing to do without a texture, material or shader globals to copy to
            if (blitMaterial == null) Start();

            var blitTexture = manager.GetVideoTexture(out var blitTextureST);
            var blitAVPro = manager.IsAVPro;
            bool latestNull = blitTexture == null;

            bool applyResizeAspect = applyAspectToResize;
            float aspect = aspectRatio;

            if (standbyOnMediaEnd && (IsEnded || IsSkipping) || standbyOnMediaPause && IsPaused || disableVideo)
            {
                latestNull = true;
                blitTexture = null;
            }

            bool isStandby = latestNull;

            // handle fallback textures
            if (latestNull && !disableStandby)
            {
                var seek = SeekPercent;
                var standbyCheck = !disableVideo;
                var showSound = standbyCheck && state == TVPlayState.PLAYING && (isLive || seek > 0f && seek < 1f);
                var _3d = (int)standby3dMode;
                if (standby3dModeSize == TV3DModeSize.Full) _3d *= -1;
                shaderVideoData.m30 = (float)_3d;
                if (errorState == TVErrorState.FAILED && errorTexture)
                {
                    latestNull = false;
                    blitTexture = errorTexture;
                    blitTextureST = DEFAULTST;
                    blitAVPro = false;
                }
                else if (IsLoadingMedia && loadingTexture)
                {
                    latestNull = false;
                    blitTexture = loadingTexture;
                    blitTextureST = DEFAULTST;
                    blitAVPro = false;
                }
                else if (showSound && soundOnlyTexture)
                {
                    latestNull = false;
                    blitTexture = soundOnlyTexture;
                    blitTextureST = DEFAULTST;
                    blitAVPro = false;
                }
                else if (standbyCheck && defaultStandbyTexture)
                {
                    latestNull = false;
                    blitTexture = defaultStandbyTexture;
                    blitTextureST = DEFAULTST;
                    blitAVPro = false;
                }
            }

            var blitTextureHeight = latestNull ? 16 : blitTexture.height;
            var blitTextureWidth = latestNull ? 16 : blitTexture.width;
            bool resizeTexture = blitTextureHeight != _sourceTextureHeight || blitTextureWidth != _sourceTextureWidth;
            _sourceTexture = blitTexture;
            _sourceTextureST = blitTextureST;
            _sourceTextureWidth = blitTextureWidth;
            _sourceTextureHeight = blitTextureHeight;

            // handle internal texture
            if (resizeTexture)
            {
                if (_internalTexture.IsCreated()) _internalTexture.Release();
                _internalTexture.width = blitTextureWidth;
                _internalTexture.height = blitTextureHeight;
                _internalTextureTexelSize = new Vector4(1f / blitTextureWidth, 1f / blitTextureHeight, blitTextureWidth, blitTextureHeight);
                if (IsDebugEnabled) Debug($"Texture updating to {blitTextureWidth}x{blitTextureHeight} ({(float)blitTextureWidth / blitTextureHeight})");
            }

            if (latestNull)
            {
                if (_internalTexture.IsCreated()) _internalTexture.Release();
            }
            else
            {
                if (!_internalTexture.IsCreated()) _internalTexture.Create();

                lastGammaZoneST = _GetGammaZoneST(_sourceTextureWidth, _sourceTextureHeight);
                blitMaterial.SetFloat(shaderID_SkipGamma, skipGamma ? 1f : 0);
                blitMaterial.SetVector(shaderID_GammaZone, lastGammaZoneST);
                blitMaterial.SetFloat(shaderID_AVPro, blitAVPro && gammaZoneTransformMode != TVTextureTransformMode.DISABLED ? 1f : 0);
                // cannot use _MainTex_ST due to unity shenanigans overriding the value during blit
                blitMaterial.SetVector(shaderID_MainTex_ST_Override, blitTextureST);
                // run the op
                VRCGraphics.Blit(blitTexture, _internalTexture, blitMaterial, 0);
            }


            Vector4 st = DEFAULTST;
            if (customTexture != null)
            {
                bool gammaTrim = trimToGammaZone && !latestNull && !isStandby && gammaZoneTransformMode != TVTextureTransformMode.ASIS;
                if (gammaTrim) st = lastGammaZoneST;
                // Handle public texture
                if (autoResizeTexture && (resizeTexture || lastGammaZoneTransformMode != gammaZoneTransformMode))
                {
                    if (customTexture.IsCreated()) customTexture.Release();
                    lastGammaZoneTransformMode = gammaZoneTransformMode;
                    if (gammaTrim)
                    {
                        blitTextureWidth = (int)Mathf.Abs(blitTextureWidth * st.x);
                        blitTextureHeight = (int)Mathf.Abs(blitTextureHeight * st.y);
                        // blit texture dimensions MUST BE AT LEAST > 16 pixels to be detected as valid video texture
                        if (blitTextureWidth <= 16) blitTextureWidth = 17;
                        if (blitTextureHeight <= 16) blitTextureHeight = 17;
                    }

                    var blitTextureAspect = latestNull ? aspect : (float)blitTextureWidth / blitTextureHeight;
                    if (applyResizeAspect && aspect > 0 && Mathf.Abs(blitTextureAspect - aspect) > 0.0001)
                    {
                        var normWidth = blitTextureWidth / aspect;
                        if (normWidth > blitTextureHeight)
                            blitTextureHeight = (int)(blitTextureHeight / (blitTextureHeight / normWidth));
                        else blitTextureWidth = (int)(blitTextureWidth / (normWidth / blitTextureHeight));
                    }

                    customTexture.width = blitTextureWidth;
                    customTexture.height = blitTextureHeight;
                }

                if (latestNull)
                {
                    if (customTexture.IsCreated()) customTexture.Release();
                }
                else
                {
                    if (!customTexture.IsCreated()) customTexture.Create();
                    // do not aspect the render if 3D mode is enabled, leave that up to a 3D shader downstream
                    blitMaterial.SetFloat(shaderID_ForceAspect, applyAspectToBlit ? aspect : 0);
                    blitMaterial.SetVector(shaderID_GammaZone, st);
                    blitMaterial.SetFloat(shaderID_Brightness, customTextureBrightness);
                    blitMaterial.SetFloat(shaderID_3D, shaderVideoData.m30);
                    blitMaterial.SetFloat(shaderID_FadeEdges, fadeEdges ? 1f : 0);
                    blitMaterial.SetInteger(shaderID_AspectFitMode, (int)aspectFitMode);
                    // run the op
                    VRCGraphics.Blit(_internalTexture, customTexture, blitMaterial, 1);
                }
            }

            if (_hasCustomMaterials)
            {
                st = lastGammaZoneST;
                for (var index = 0; index < customMaterials.Length; index++)
                {
                    var _customMaterial = customMaterials[index];
                    if (_customMaterial == null) continue;
                    var customProperty = shaderIDs_MaterialProperties[index];
                    var customPropertyTexelSize = shaderIDs_MaterialProperties_TexelSize[index];
                    _customMaterial.SetTexture(customProperty, _internalTexture);
                    _customMaterial.SetVector(customPropertyTexelSize, _internalTextureTexelSize);
                    _customMaterial.SetMatrix(shaderID_VideoData, shaderVideoData);
                    _customMaterial.SetVector(shaderID_GammaZone, st);
                }
            }

            if (enableGSV)
            {
                st = DEFAULTST;
                bool bakeGlobalTexture = bakeGlobalVideoTexture;
                if (!isStandby)
                {
                    if (globalTextureTransformMode == TVTextureTransformMode.ASIS || globalTextureTransformMode == TVTextureTransformMode.DISABLED)
                        bakeGlobalTexture = false;
                    else st = _GetGlobalTextureST(_sourceTextureWidth, _sourceTextureHeight);
                }

                if (bakeGlobalTexture)
                {
                    if (_globalTexture == null) _globalTexture = createInternalTexture();

                    blitTextureWidth = latestNull ? 16 : _internalTexture.width;
                    blitTextureHeight = latestNull ? 16 : _internalTexture.height;
                    blitTextureWidth = (int)Mathf.Abs(blitTextureWidth * st.x);
                    blitTextureHeight = (int)Mathf.Abs(blitTextureHeight * st.y);
                    // blit texture dimensions MUST BE AT LEAST > 16 pixels to be detected as valid video texture
                    if (blitTextureWidth <= 16) blitTextureWidth = 17;
                    if (blitTextureHeight <= 16) blitTextureHeight = 17;
                    resizeTexture = blitTextureWidth != _globalTextureWidth || blitTextureHeight != _globalTextureHeight;
                    _globalTextureWidth = blitTextureWidth;
                    _globalTextureHeight = blitTextureHeight;

                    if (resizeTexture)
                    {
                        if (_globalTexture.IsCreated()) _globalTexture.Release();
                        _globalTexture.width = blitTextureWidth;
                        _globalTexture.height = blitTextureHeight;
                        _globalTextureTexelSize = new Vector4(1f / blitTextureWidth, 1f / blitTextureHeight, blitTextureWidth, blitTextureHeight);
                    }

                    if (latestNull)
                    {
                        if (_globalTexture.IsCreated()) _globalTexture.Release();
                        VRCShader.SetGlobalTexture(shaderIDGlobal_Udon_VideoTex, null);
                        VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_ST, DEFAULTST);
                        VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_TexelSize, DEFAULTTEXELSIZE);
                    }
                    else
                    {
                        if (!_globalTexture.IsCreated()) _globalTexture.Create();
                        blitMaterial.SetFloat(shaderID_AVPro, 0);
                        // cannot use _MainTex_ST due to unity shenanigans overriding the value during blit
                        blitMaterial.SetVector(shaderID_MainTex_ST_Override, st);
                        VRCGraphics.Blit(_internalTexture, _globalTexture, blitMaterial, 0);
                        VRCShader.SetGlobalTexture(shaderIDGlobal_Udon_VideoTex, _globalTexture);
                        VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_ST, DEFAULTST);
                        VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_TexelSize, _globalTextureTexelSize);
                    }
                }
                else
                {
                    if (_globalTexture != null && _globalTexture.IsCreated()) _globalTexture.Release();
                    VRCShader.SetGlobalTexture(shaderIDGlobal_Udon_VideoTex, _internalTexture);
                    VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_ST, st);
                    VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_TexelSize, _internalTextureTexelSize);
                    lastGlobalST = st;
                }

                VRCShader.SetGlobalMatrix(shaderIDGlobal_Udon_VideoData, shaderVideoData);
                _customTextureIsGlobal = true;
            }
            // if globals ever get disabled, unset the custom texture global flag as well
            else if (_customTextureIsGlobal)
            {
                _customTextureIsGlobal = false;
                lastGlobalST = DEFAULTST;
                VRCShader.SetGlobalTexture(shaderIDGlobal_Udon_VideoTex, null);
                VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_ST, DEFAULTST);
                VRCShader.SetGlobalVector(shaderIDGlobal_Udon_VideoTex_TexelSize, DEFAULTTEXELSIZE);
                VRCShader.SetGlobalMatrix(shaderIDGlobal_Udon_VideoData, Matrix4x4.zero);
            }

            // bool hasReadbackEnabled = tv.enablePixelExtraction;
            // if (hasReadbackEnabled && !extractionInProgress && internalTexture.IsCreated())
            // {
            //     // ReSharper disable once SuspiciousTypeConversion.Global
            //     VRCAsyncGPUReadback.Request(internalTexture, 0, (UdonBehaviour)(Component)this);
            //     extractionInProgress = true;
            // }
        }


        private RenderTexture createInternalTexture()
        {
            RenderTextureFormat format = enableHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB64;
            RenderTextureReadWrite rw = enableHDR ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
            var tex = VRCRenderTexture.GetTemporary(16, 16, 0, format, rw, 1);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            tex.filterMode = FilterMode.Trilinear;
            // mobile phones don't seem to like ansio? Just disable it on that platform.
            if (isInVR || !isAndroid) tex.anisoLevel = 16;
            tex.useMipMap = true;
            tex.autoGenerateMips = true;
            return tex;
        }

        public Vector3 _GetGammaZoneST() => _GetGammaZoneST(_sourceTextureWidth, _sourceTextureHeight);

        public Vector4 _GetGammaZoneST(float sourceWidth, float sourceHeight) =>
            getTextureST(gammaZoneTransformMode, sourceWidth, sourceHeight, gammaZoneTiling, gammaZoneOffset, gammaZonePixelSize, gammaZonePixelOrigin, true);

        public Vector4 _GetGlobalTextureST() => _GetGlobalTextureST(_sourceTextureWidth, _sourceTextureHeight);

        public Vector4 _GetGlobalTextureST(float sourceWidth, float sourceHeight) =>
            getTextureST(globalTextureTransformMode, sourceWidth, sourceHeight, globalTextureTiling, globalTextureOffset, globalTexturePixelSize, globalTexturePixelOrigin);

        private Vector4 getTextureST(TVTextureTransformMode mode, float sourceWidth, float sourceHeight, Vector2 tiling, Vector2 offset, Vector2 size, Vector2 origin, bool clamp = false)
        {
            Vector4 st = new Vector4(1, 1, 0, 0);
            // force the values for the preset modes
            switch (mode)
            {
                case TVTextureTransformMode.VRSL_HL:
                    origin = Vector2.zero;
                    size = new Vector2(0, -208);
                    break;
                case TVTextureTransformMode.VRSL_HM:
                    origin = Vector2.zero;
                    size = new Vector2(0, -139);
                    break;
                case TVTextureTransformMode.VRSL_HS:
                    origin = Vector2.zero;
                    size = new Vector2(0, -92);
                    break;
                case TVTextureTransformMode.VRSL_VL:
                    origin = Vector2.zero;
                    size = new Vector2(-208, 0);
                    break;
                case TVTextureTransformMode.VRSL_VM:
                    origin = Vector2.zero;
                    size = new Vector2(-139, 0);
                    break;
                case TVTextureTransformMode.VRSL_VS:
                    origin = Vector2.zero;
                    size = new Vector2(-92, 0);
                    break;
            }

            switch (mode)
            {
                case TVTextureTransformMode.ASIS:
                case TVTextureTransformMode.DISABLED:
                    break;
                case TVTextureTransformMode.NORMALIZED:
                    st.x = tiling.x;
                    st.y = tiling.y;
                    st.z = offset.x;
                    st.w = offset.y;
                    break;
                default: // by pixels and any presets
                    // calculate offset/tiling from source texture pixel size.
                    float targetWidth = size.x;
                    float targetHeight = size.y;
                    float targetX = origin.x;
                    float targetY = origin.y;
                    if (targetWidth <= 0) targetWidth += sourceWidth;
                    if (targetHeight <= 0) targetHeight += sourceHeight;
                    if (targetX < 0) targetX += sourceWidth;
                    if (targetY < 0) targetY += sourceHeight;
                    st.x = targetWidth / sourceWidth;
                    st.y = targetHeight / sourceHeight;
                    st.z = targetX / sourceWidth;
                    st.w = (sourceHeight - targetHeight - targetY) / sourceHeight;
                    break;
            }

            if (st == Vector4.zero) st = new Vector4(1, 1, 0, 0);
            return st;
        }
    }
}