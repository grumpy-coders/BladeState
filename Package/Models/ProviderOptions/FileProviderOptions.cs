using System;

namespace GrumpyCoders.BladeState.Models.ProviderOptions;

public class FileProviderOptions
{
    public string BasePath { get; set; } = Environment.GetEnvironmentVariable("tmp");
}