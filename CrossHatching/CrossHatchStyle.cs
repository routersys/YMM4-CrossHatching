using System.ComponentModel.DataAnnotations;

namespace CrossHatching;

public enum CrossHatchStyle
{
    [Display(Name = nameof(Texts.StylePen), Description = nameof(Texts.StylePenDesc), ResourceType = typeof(Texts))]
    Pen = 0,

    [Display(Name = nameof(Texts.StyleEngraving), Description = nameof(Texts.StyleEngravingDesc), ResourceType = typeof(Texts))]
    Engraving = 1,

    [Display(Name = nameof(Texts.StyleManga), Description = nameof(Texts.StyleMangaDesc), ResourceType = typeof(Texts))]
    Manga = 2,

    [Display(Name = nameof(Texts.StyleBlueprint), Description = nameof(Texts.StyleBlueprintDesc), ResourceType = typeof(Texts))]
    Blueprint = 3,
}
