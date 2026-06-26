using System.ComponentModel.DataAnnotations;

namespace CrossHatching;

public enum CrossHatchLineLayers
{
    [Display(Name = nameof(Texts.LineLayersAuto), ResourceType = typeof(Texts))]
    Auto = 0,

    [Display(Name = nameof(Texts.LineLayers1), ResourceType = typeof(Texts))]
    One = 1,

    [Display(Name = nameof(Texts.LineLayers2), ResourceType = typeof(Texts))]
    Two = 2,

    [Display(Name = nameof(Texts.LineLayers3), ResourceType = typeof(Texts))]
    Three = 3,

    [Display(Name = nameof(Texts.LineLayers4), ResourceType = typeof(Texts))]
    Four = 4,
}
