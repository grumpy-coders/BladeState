using System;
namespace BladeState.Models;

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
    public string InstanceName { get; set; } = "BladeState";

    /// <summary>
    /// NOT IMPLEMENTED: Times out the instance of Blade State when no data action or access has occurred. 
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// NOT IMPLEMENTED: When the InstanceTimeout has elapsed, Blade State will then save the instance data. Calls SaveStateAsync()
    /// </summary>
    public bool SaveOnTimeout { get; set; }

    /// <summary>
    /// Enables Blade State's cryptography automatically. Data is then saved in a provider ciphered then retrieved deciphered.
    /// </summary>
    public bool AutoEncrypt { get; set; } = true;

    /// <summary>
    /// The encryption key to use for Blade State's cryptography.
    /// When not passed, Blade State will generate a unique key automatically once per Blade State instance.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;
}
