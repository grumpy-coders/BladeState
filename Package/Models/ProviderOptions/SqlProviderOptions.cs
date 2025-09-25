using GrumpyCoders.BladeState.Enums;

namespace GrumpyCoders.BladeState.Models.ProviderOptions;

public class SqlProviderOptions
{
    /// <summary>
    /// When set to SqlType.None it will use ANSI SQL syntax (agnostic to data backend)
    /// </summary>
    public SqlType SqlType { get; set; } = SqlType.None;
}