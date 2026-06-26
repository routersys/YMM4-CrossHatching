namespace CrossHatching;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/CrossHatching;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
