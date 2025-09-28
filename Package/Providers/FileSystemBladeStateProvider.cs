using System.Text.Json;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState;
using GrumpyCoders.BladeState.Constants;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace GrumpyCoders.BladeState.Providers;

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
			_directory = Path.Combine(Path.GetTempPath(), Profile.InstanceName);
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

		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		await CheckTimeoutAsync(cancellationToken);
		LastAccessTime = DateTime.UtcNow;

		try
		{
			string cipherText = Profile.AutoEncrypt ? EncryptState() : JsonSerializer.Serialize(state, _jsonSerializerOptions);
			await File.WriteAllTextAsync(_filePath, cipherText, cancellationToken);
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

			string cipherState = await File.ReadAllTextAsync(_filePath, cancellationToken);
			if (string.IsNullOrWhiteSpace(cipherState))
			{
				return new TState();
			}

			if (Profile.AutoEncrypt)
			{
				cipherState = Decrypt(cipherState);
			}

			State = JsonSerializer.Deserialize<TState>(cipherState);

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


