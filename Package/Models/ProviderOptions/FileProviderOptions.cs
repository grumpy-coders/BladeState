using System;

namespace GrumpyCoders.BladeState.Models.ProviderOptions;

public class FileSystemProviderOptions
{
    public string BasePath { get; set; } = Environment.GetEnvironmentVariable("tmp");
}