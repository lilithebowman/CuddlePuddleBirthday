#define LILPBR_PROPERTIES \
LILPBR_PROPERTY(float4,_WaterColor)\
LILPBR_PROPERTY(float4,_WaterColorFog)\
LILPBR_PROPERTY(float,_WaveTiling)\
LILPBR_PROPERTY(float,_Caustics)\
LILPBR_PROPERTY(float,_WaterFogDistance)\
LILPBR_PROPERTY(float,_VolumetricFog)

#define LILPBR_TEXTURES \
LILPBR_TEXTURE(Texture2D,_WaveNormal)\
LILPBR_TEXTURE(Texture2D,_WaveHeight)\
LILPBR_TEXTURE(Texture2D,_FoamNoiseTex)

#define LILPBR_SAMPLERS
