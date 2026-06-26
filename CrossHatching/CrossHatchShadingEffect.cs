using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace CrossHatching;

[VideoEffect(nameof(Texts.EffectName), [VideoEffectCategories.Filtering], [nameof(Texts.TagCrossHatch), nameof(Texts.TagHatching), nameof(Texts.TagPen), nameof(Texts.TagEngraving), nameof(Texts.TagBlueprint), nameof(Texts.TagManga)], IsAviUtlSupported = false, ResourceType = typeof(Texts))]
public sealed class CrossHatchShadingEffect : VideoEffectBase
{
    public override string Label => Texts.EffectName;

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.StyleName), Description = nameof(Texts.StyleDesc), Order = 0, ResourceType = typeof(Texts))]
    [EnumComboBox]
    public CrossHatchStyle Style { get => style; set => Set(ref style, value); }
    CrossHatchStyle style = CrossHatchStyle.Pen;

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.AmountName), Description = nameof(Texts.AmountDesc), Order = 10, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Amount { get; } = new(100, 0, 100);

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.DensityName), Description = nameof(Texts.DensityDesc), Order = 20, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 3d, 80d)]
    public Animation Density { get; } = new(12, 3, 320);

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.ThicknessName), Description = nameof(Texts.ThicknessDesc), Order = 30, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0.2d, 12d)]
    public Animation Thickness { get; } = new(1.2, 0.2, 48);

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.ShadowDepthName), Description = nameof(Texts.ShadowDepthDesc), Order = 40, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 200d)]
    public Animation ShadowDepth { get; } = new(100, 0, 200);

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.FlowName), Description = nameof(Texts.FlowDesc), Order = 50, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Flow { get; } = new(40, 0, 100);

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.OutlineName), Description = nameof(Texts.OutlineDesc), Order = 60, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Outline { get; } = new(30, 0, 100);

    [Display(GroupName = nameof(Texts.ColorGroup), Name = nameof(Texts.InkColorName), Description = nameof(Texts.InkColorDesc), Order = 100, ResourceType = typeof(Texts))]
    [ColorPicker]
    public Color InkColor { get => inkColor; set => Set(ref inkColor, value); }
    Color inkColor = Color.FromArgb(255, 17, 17, 17);

    [Display(GroupName = nameof(Texts.ColorGroup), Name = nameof(Texts.PaperColorName), Description = nameof(Texts.PaperColorDesc), Order = 110, ResourceType = typeof(Texts))]
    [ColorPicker]
    public Color PaperColor { get => paperColor; set => Set(ref paperColor, value); }
    Color paperColor = Color.FromArgb(255, 246, 241, 231);

    [Display(GroupName = nameof(Texts.ColorGroup), Name = nameof(Texts.ColorModeName), Description = nameof(Texts.ColorModeDesc), Order = 120, ResourceType = typeof(Texts))]
    [EnumComboBox]
    public CrossHatchColorMode ColorMode { get => colorMode; set => Set(ref colorMode, value); }
    CrossHatchColorMode colorMode = CrossHatchColorMode.InkAndPaper;

    [Display(GroupName = nameof(Texts.DetailGroup), Name = nameof(Texts.LineLayersName), Description = nameof(Texts.LineLayersDesc), Order = 200, ResourceType = typeof(Texts))]
    [EnumComboBox]
    public CrossHatchLineLayers LineLayers { get => lineLayers; set => Set(ref lineLayers, value); }
    CrossHatchLineLayers lineLayers = CrossHatchLineLayers.Auto;

    [Display(GroupName = nameof(Texts.DetailGroup), Name = nameof(Texts.ToneSoftnessName), Description = nameof(Texts.ToneSoftnessDesc), Order = 210, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 50d)]
    public Animation ToneSoftness { get; } = new(12, 0, 50);

    [Display(GroupName = nameof(Texts.DetailGroup), Name = nameof(Texts.GrainName), Description = nameof(Texts.GrainDesc), Order = 220, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Grain { get; } = new(12, 0, 100);

    [Display(GroupName = nameof(Texts.DetailGroup), Name = nameof(Texts.SeedName), Description = nameof(Texts.SeedDesc), Order = 230, ResourceType = typeof(Texts))]
    [Range(0, 9999)]
    [DefaultValue(0)]
    [TextBoxSlider("F0", "", 0, 9999)]
    public int Seed { get => seed; set => Set(ref seed, Math.Clamp(value, 0, 9999)); }
    int seed;

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices) => new CrossHatchShadingEffectProcessor(devices, this);

    protected override IEnumerable<IAnimatable> GetAnimatables() => [Amount, Density, Thickness, ShadowDepth, Flow, Outline, ToneSoftness, Grain];
}
