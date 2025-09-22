namespace GrumpyCoders.BladeState.Models.ProviderOptions;

public class FileProviderOptions
{
    /// <summary>
    /// When empty this will default to temp directory
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}