namespace Rhetos.Utilities
{
    public interface IRhetosConfiguration
    {
        string GeneratedFolder { get; }
        string GeneratedFilesCacheFolder { get; }
        string PluginsFolder { get; }
    }
}
