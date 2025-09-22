using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Models;

namespace GrumpyCoders.BladeState.Providers
{
    public class FileSystemBladeStateProvider<TState> : BladeStateProvider<TState> where TState : class, new()
    {
        private readonly string _directory;

        public FileSystemBladeStateProvider(BladeStateProfile profile)
            : base(profile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(profile.FileProviderOptions.BasePath))
                    _directory = Path.Combine(Path.GetTempPath(), Profile.InstanceName);
                else
                    _directory = Path.Combine(Profile.FileProviderOptions.BasePath, Profile.InstanceName);

                Directory.CreateDirectory(_directory);
            }
            catch
            {
                OnStateChange(ProviderEventType.Error);
                throw;
            }
        }

        private string GetFilePath(string instanceId) => Path.Combine(_directory, $"{instanceId}.json");

        public override async Task SaveStateAsync(TState state, CancellationToken cancellationToken = default)
        {

            if (Profile.AutoEncrypt)
            {
                await EncryptStateAsync(cancellationToken);
                await File.WriteAllTextAsync(GetFilePath(Profile.InstanceId), json, cancellationToken);
            }

            try
            {
                var json = JsonSerializer.Serialize(state, Profile);

                var filePath = GetFilePath(Profile.StateKey);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);

                OnStateChange(ProviderEventType.Saved);
            }
            catch
            {
                OnStateChange(ProviderEventType.Failed);
                throw;
            }
        }

        public override async Task<TState> LoadStateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePath(Profile.StateKey);

                if (!File.Exists(filePath))
                    return new TState();

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);

                try
                {
                    json = CipherState.Decrypt(json);
                }
                catch
                {
                    // If no cipher, fallback to plain JSON
                }

                var state = JsonSerializer.Deserialize<TState>(json, Profile.JsonOptions) ?? new TState();

                OnStateChange(ProviderEventType.Loaded);
                return state;
            }
            catch
            {
                OnStateChange(ProviderEventType.Failed);
                throw;
            }
        }
    }
}
