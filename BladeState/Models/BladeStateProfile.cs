using System;
using BladeState.Models.ProviderOptions;
using GrumpyCoders.BladeState.Models.ProviderOptions;
namespace GrumpyCoders.BladeState.Models;

public class BladeStateProfile
{
    /// <summary>
    /// A key used to uniquely identify a Blade State instance's data.
    /// </summary>
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The name of a single Blade State instance. This is usually a static value for an application, can be used to group Blade State data.
    /// For ex: prefix keys in Redis, name the root table in a SQL database, or more generally, a friendly field used to identify Blade State data stored somewhere.
    /// </summary>
    public string InstanceName { get; set; } = Constants.Constants.BladeStateName;

    /// <summary>
    /// Times out the instance of Blade State when no data action or access has occurred. 
    /// </summary>
    public TimeSpan InstanceTimeout { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// When the InstanceTimeout has elapsed, Blade State will then save the instance data. Calls SaveStateAsync()
    /// </summary>
    public bool SaveOnInstanceTimeout { get; set; }

    /// <summary>
    /// Enables Blade State's cryptography automatically. Data is then saved in a provider ciphered then retrieved deciphered.
    /// </summary>
    public bool AutoEncrypt { get; set; } = true;

    /// <summary>
    /// When true, the state is cleared from the provider when DisposeAsync() is called.
    /// </summary>
    public bool AutoClearOnDispose { get; set; } = true;

    /// <summary>
    /// The encryption key to use for Blade State's cryptography.
    /// When not passed, Blade State will generate a unique key automatically once per Blade State instance.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Profile options specific to FileBladeStateProvider
    /// </summary>
    public FileProviderOptions FileProviderOptions { get; set; } = new();

    /// <summary>
    /// Profile options specific to SqlBladeStateProvider
    /// </summary>
    public SqlProviderOptions SqlProviderOptions { get; set; } = new();
}
