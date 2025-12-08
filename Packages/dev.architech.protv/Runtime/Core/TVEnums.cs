using System.Runtime.CompilerServices;
using ArchiTech.SDK;

namespace ArchiTech.ProTV
{
    public enum TVPlayState
    {
        WAITING,
        STOPPED,
        PLAYING,
        PAUSED
    }

    public enum TVErrorState
    {
        NONE,
        RETRY,
        BLOCKED,
        FAILED
    }

    public enum TVPlaylistSortMode
    {
        DEFAULT,
        RANDOM,
        TITLE_ASC,
        TITLE_DESC,
        TAG_ASC,
        TAG_DESC
    }

    public enum TV3DMode
    {
        [I18nInspectorName("Not 3D")] NONE,
        [I18nInspectorName("Side by Side")] SBS,
        [I18nInspectorName("Side by Side Swapped")] SBS_SWAP,
        [I18nInspectorName("Over Under")] OVUN,
        [I18nInspectorName("Over Under Swapped")] OVUN_SWAP
    }

    public enum TV3DModeSize
    {
        [I18nInspectorName("Half Size 3D")] Half,
        [I18nInspectorName("Full Size 3D")] Full
    }

    public enum TVTextureTransformMode
    {
        [I18nInspectorName("As-Is")] ASIS,
        [I18nInspectorName("Disabled")] DISABLED,
        [I18nInspectorName("Normalized")] NORMALIZED,
        [I18nInspectorName("By Pixels")] BY_PIXELS,
        [I18nInspectorName("VRSL Presets / Horizontal / 1080")] VRSL_HL,
        [I18nInspectorName("VRSL Presets / Horizontal / 720")] VRSL_HM,
        [I18nInspectorName("VRSL Presets / Horizontal / 480")] VRSL_HS,
        [I18nInspectorName("VRSL Presets / Vertical / 1080")] VRSL_VL,
        [I18nInspectorName("VRSL Presets / Vertical / 720")] VRSL_VM,
        [I18nInspectorName("VRSL Presets / Vertical / 480")] VRSL_VS
    }

    public enum TVAspectFitMode
    {
        [I18nInspectorName("Fit Inside (lossless)")] FIT_INSIDE,
        [I18nInspectorName("Fit Outside (crop overflow)")] FIT_OUTSIDE
    }
}