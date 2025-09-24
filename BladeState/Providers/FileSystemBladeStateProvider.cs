using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState;
using GrumpyCoders.BladeState.Constants;

namespace BladeState.Providers;

public class FileSystemBladeStateProvider<TState> : BladeStateProvider<TState> where TState : class, new()
{
	private readonly string _directory;
	private readonly string _filePath;
	private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
	public string GetFilePath() => _filePath;

	public FileSystemBladeStateProvider(BladeStateCryptography cryptography, BladeStateProfile profile) : base(cryptography, profile)
	{
		if (string.IsNullOrWhiteSpace(profile.FileProviderOptions.BasePath))
		{
			_directory = Path.Combine(Path.GetTempPath(), Constants.BladeStateName);
		}
		else if (!string.IsNullOrWhiteSpace(profile.FileProviderOptions.BasePath))
		{
			_directory = profile.FileProviderOptions.BasePath;
		}

		if (!Directory.Exists(_directory))
		{
			Directory.CreateDirectory(_directory);
		}
		_filePath = Path.Combine(_directory, $"{Profile.InstanceId}.json");
	}


	public override async Task SaveStateAsync(TState state, CancellationToken cancellationToken = default)
	{
		try
		{
			CipherState = JsonSerializer.Serialize(state, _jsonSerializerOptions);
			if (Profile.AutoEncrypt)
			{
				await EncryptStateAsync(cancellationToken);
			}

			await File.WriteAllTextAsync(_filePath, CipherState, cancellationToken);
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
			if (!File.Exists(_filePath))
			{
				return new TState();
			}

			CipherState = await File.ReadAllTextAsync(_filePath, cancellationToken);
			if (string.IsNullOrWhiteSpace(CipherState))
			{
				return new TState();
			}

			if (Profile.AutoEncrypt)
			{
				await DecryptStateAsync(cancellationToken);
			}
			else
			{
				State = JsonSerializer.Deserialize<TState>(CipherState);
			}
			CipherState = string.Empty;
			OnStateChange(ProviderEventType.Load);
			return State;
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
			if (File.Exists(_filePath))
			{
				File.Delete(_filePath);
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
			if (Profile.AutoClearOnDispose)
			{
				await ClearStateAsync(CancellationToken.None).ConfigureAwait(false);
			}
		}
		catch
		{
			// swallow or log exceptions, since Dispose must not throw
		}
		await base.DisposeAsyncCore().ConfigureAwait(false);
	}

}


