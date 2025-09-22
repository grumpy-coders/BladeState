using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Enums;
using BladeState.Models;

namespace BladeState.Providers
{
	public class FileSystemBladeStateProvider<TState> : BladeStateProvider<TState> where TState : class, new()
	{
		private readonly string _directory;

		public FileSystemBladeStateProvider(BladeStateCryptography cryptography, BladeStateProfile profile) : base(cryptography, profile)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(profile.FileProviderOptions.BasePath))
				{
					_directory = Path.Combine(Path.GetTempPath(), Constants.Constants.BladeStateName);
				}
				else if (!string.IsNullOrWhiteSpace(profile.FileProviderOptions.BasePath))
				{
					_directory = profile.FileProviderOptions.BasePath;
				}

				if (!Directory.Exists(_directory))
				{
					Directory.CreateDirectory(_directory);
				}

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
				CipherState = JsonSerializer.Serialize(state);

				if (Profile.AutoEncrypt)
				{
					await EncryptStateAsync(cancellationToken);
				}

				string filePath = GetFilePath(Profile.InstanceId);
				await File.WriteAllTextAsync(filePath, CipherState, cancellationToken);
				OnStateChange(ProviderEventType.Save);
			}
			catch
			{
				OnStateChange(ProviderEventType.Error);
				throw;
			}
		}

		public override async Task<TState> LoadStateAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				string filePath = GetFilePath(Profile.InstanceId);

				if (!File.Exists(filePath))
				{
					return new TState();
				}

				CipherState = await File.ReadAllTextAsync(filePath, cancellationToken);
				if (string.IsNullOrWhiteSpace(CipherState))
				{
					return new TState();
				}

				if (Profile.AutoEncrypt)
				{
					await DecryptStateAsync(cancellationToken);
				}
				var state = JsonSerializer.Deserialize<TState>(CipherState) ?? new TState();
				OnStateChange(ProviderEventType.Load);
				return state;
			}
			catch
			{
				OnStateChange(ProviderEventType.Error);
				throw;
			}
		}

		public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				string filePath = GetFilePath(Profile.InstanceId);

				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				await Task.CompletedTask;
				OnStateChange(ProviderEventType.Clear);
			}
			catch
			{
				OnStateChange(ProviderEventType.Error);
				throw;
			}
		}

		/// <summary>
		/// Async disposal hook: cleanup persisted state before disposal.
		/// </summary>
		protected override async ValueTask DisposeAsyncCore()
		{
			try
			{
				await ClearStateAsync(CancellationToken.None).ConfigureAwait(false);
			}
			catch
			{
				// swallow or log exceptions, since Dispose must not throw
			}
			await base.DisposeAsyncCore().ConfigureAwait(false);
		}

	}
}

