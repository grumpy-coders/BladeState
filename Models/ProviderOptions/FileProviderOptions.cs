namespace BladeState.Models.ProviderOptions;

public class FileProviderOptions
{
    public bool UseTemp { get; set; } = true;
    public string BasePath { get; set; } = string.Empty;
}