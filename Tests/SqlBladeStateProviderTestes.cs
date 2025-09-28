using GrumpyCoders.BladeState.Providers;
using System.Data.Common;
using GrumpyCoders.BladeState.Enums;
using StackExchange.Redis.Profiling;
using Microsoft.Data.Sqlite;
namespace GrumpyCoders.BladeStateTests;

[TestClass]
public sealed class SqlBladeStateProviderTests : TestBase
{
	private readonly SqlBladeStateProvider<AppState> Provider;
	private readonly AppState AppState = new();


	private Func<DbConnection> CreateDbConnectionFactory()
	{
		if (Profile.SqlProviderOptions.SqlType == SqlType.Sqlite)
		{
			string folder = Path.Combine(Environment.GetEnvironmentVariable("tmp") ?? Path.GetTempPath(), "BladeStateProviderTests");
			Directory.CreateDirectory(folder);
			string filePath = Path.Combine(folder, "Tests.db");
			/*
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			*/

			string connectionString = $"Data Source={filePath};";
			using (var connection = new SqliteConnection(connectionString)) { connection.Open(); }
			return () => new SqliteConnection(connectionString);
		}
		throw new NotSupportedException("Only Sqlite is supported in tests");
	}

	public SqlBladeStateProviderTests()
	{
		Profile.SqlProviderOptions = new() { SqlType = SqlType.Sqlite };
		Func<DbConnection> connection = CreateDbConnectionFactory();
		Provider = new(connection, Cryptography, Profile);
	}

	[TestMethod]
	public async Task TimeoutTest()
	{
		await Provider.SaveStateAsync(AppState, CancellationToken);
		AppState state = await Provider.LoadStateAsync(CancellationToken);
		state.LastName = "Modified";
		Profile.InstanceTimeout = TimeSpan.FromMilliseconds(1);
		await Task.Delay(1, CancellationToken);
		state = await Provider.LoadStateAsync(CancellationToken);

		// State should be reset to default after timeout
		Assert.AreNotEqual("Modified", state.LastName);
	}

	[TestMethod]
	public async Task TestSaveAndRestore()
	{
		try
		{
			await Provider.SaveStateAsync(AppState, CancellationToken);

			Assert.IsTrue(CheckAppState(await Provider.LoadStateAsync(CancellationToken)));
		}
		catch (Exception exception)
		{
			Assert.Fail(exception.Message);
		}
	}

	[TestCleanup]
	public async Task CleanupAsync()
	{
		await Provider.DisposeAsync().ConfigureAwait(false);

		// TODO: This should live in DisposeAsync and be controlled by Profile.SqlProviderOptions
		await Provider.ClearStateAsync();
		if (Profile.SqlProviderOptions.SqlType == SqlType.Sqlite)
		{
			string folder = Path.Combine(Environment.GetEnvironmentVariable("tmp") ?? Path.GetTempPath(), "BladeStateProviderTests");
			string filePath = Path.Combine(folder, "Tests.db");
			if (File.Exists(filePath))
			{
				for (int i = 0; i < 10; i++)
				{
					try
					{
						if (File.Exists(filePath))
						{
							// File.Delete(filePath);
							break; // deleted
						}
					}
					catch (IOException) when (i < 9)
					{
						await Task.Delay(50); // let handles release
					}
				}
			}
		}
	}
}
