using System.Text.Json;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Models;

namespace GrumpyCoders.BladeStateTests;

public class TestBase
{

	public readonly BladeStateCryptography Cryptography;
	public readonly BladeStateProfile Profile;
	public CancellationToken CancellationToken { get; set; } = new();

	public TestBase()
	{
		Cryptography = new BladeStateCryptography("TestEncryptionKey123!@#");

		Profile = new()
		{
			InstanceId = $"{DateTime.Now:yyyyMMdd-hh-ss}-{Guid.NewGuid()}",
			AutoEncrypt = true,
			AutoClearOnDispose = true,
			InstanceTimeout = TimeSpan.FromSeconds(15),
			FileProviderOptions = new()
			{
				BasePath = Path.Combine(Path.GetTempPath(), "BladeStateTests")
			}
		};
	}

	public static bool CheckAppState(AppState appStateToCheck)
	{
		AppState defaultAppState = new();

		if (appStateToCheck is null && defaultAppState is null) return true;
		if (appStateToCheck is null || defaultAppState is null) return false;

		JsonSerializerOptions options = new()
		{
			WriteIndented = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			IgnoreReadOnlyProperties = false
		};

		string json1 = JsonSerializer.Serialize(appStateToCheck, options);
		string json2 = JsonSerializer.Serialize(defaultAppState, options);

		return json1 == json2;
	}

}
