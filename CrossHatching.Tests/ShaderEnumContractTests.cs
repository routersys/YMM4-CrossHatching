using CrossHatching;

namespace CrossHatching.Tests;

public sealed class ShaderEnumContractTests
{
    [Theory]
    [InlineData(CrossHatchStyle.Pen, 0)]
    [InlineData(CrossHatchStyle.Engraving, 1)]
    [InlineData(CrossHatchStyle.Manga, 2)]
    [InlineData(CrossHatchStyle.Blueprint, 3)]
    public void Style_MapsToShaderBranch(CrossHatchStyle style, int expected)
    {
        Assert.Equal(expected, (int)style);
    }

    [Theory]
    [InlineData(CrossHatchColorMode.InkAndPaper, 0)]
    [InlineData(CrossHatchColorMode.PreserveSource, 1)]
    public void ColorMode_MapsToShaderBranch(CrossHatchColorMode mode, int expected)
    {
        Assert.Equal(expected, (int)mode);
    }

    [Theory]
    [InlineData(CrossHatchLineLayers.Auto, 0)]
    [InlineData(CrossHatchLineLayers.One, 1)]
    [InlineData(CrossHatchLineLayers.Two, 2)]
    [InlineData(CrossHatchLineLayers.Three, 3)]
    [InlineData(CrossHatchLineLayers.Four, 4)]
    public void LineLayers_MapsToActiveLayerCount(CrossHatchLineLayers layers, int expected)
    {
        Assert.Equal(expected, (int)layers);
    }
}
