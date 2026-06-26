using System.ComponentModel.DataAnnotations;

namespace CrossHatching;

public enum CrossHatchColorMode
{
    [Display(Name = nameof(Texts.ColorModeInkAndPaper), Description = nameof(Texts.ColorModeInkAndPaperDesc), ResourceType = typeof(Texts))]
    InkAndPaper = 0,

    [Display(Name = nameof(Texts.ColorModePreserveSource), Description = nameof(Texts.ColorModePreserveSourceDesc), ResourceType = typeof(Texts))]
    PreserveSource = 1,
}
