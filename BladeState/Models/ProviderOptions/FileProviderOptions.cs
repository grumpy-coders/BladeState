using System;

namespace BladeState.Models.ProviderOptions;

public class FileProviderOptions
{
    public string BasePath { get; set; } = Environment.GetEnvironmentVariable("tmp");
}