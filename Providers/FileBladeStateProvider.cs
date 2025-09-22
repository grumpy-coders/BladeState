using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Enums;
using BladeState.Models;

namespace BladeState.Providers
{
    public class FileSystemBladeStateProvider<TState> : BladeStateProvider<TState> where TState : class, new()
    {
        private readonly string _directory;

        public FileSystemBladeStateProvider(BladeStateProfile profile)
            : base(profile)
        {
            try
            {
                if (profile.FileOptions.UseTemp)
                    _directory = Path.Combine(Path.GetTempPath(), "BladeState");
                else if (!string.IsNullOrWhiteSpace(profile.FileOptions.BasePath))
                    _directory = profile.FileOptions.BasePath;
                else
                    _directory = Path.Combine(AppContext.BaseDirectory, "BladeState");

                Directory.CreateDirectory(_directory);
            }
            catch
            {
                OnStateChange(ProviderEventType.Error);
                throw;
            }
        }

        private string GetFilePath(string key) => Path.Combine(_directory, $"{key}.json");

        public override async Task SaveStateAsync(TState state, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, Profile.JsonOptions);

                try
                {
                    json = CipherState.Encrypt(json);
                }
                catch
                {
                    // If no cipher, just pass through unmodified
                }

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
