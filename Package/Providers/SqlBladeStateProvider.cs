using System;
using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Models;

namespace GrumpyCoders.BladeState.Providers;

public class SqlBladeStateProvider<T>(Func<DbConnection> connectionFactory, BladeStateCryptography bladeStateCryptography, BladeStateProfile bladeStateProfile)
	: BladeStateProvider<T>(bladeStateCryptography, bladeStateProfile) where T : class, new()
{
	private readonly Func<DbConnection> _connectionFactory = connectionFactory;

	private bool TableExists { get; set; }

	public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return State;
		}

		await CheckTimeoutAsync(cancellationToken);
		await EnsureTableExistsAsync(cancellationToken);

		await using DbConnection connection = _connectionFactory();
		await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		if (!BladeStateRegex.AlphaNumericAndUnderscore().IsMatch(Profile.InstanceName))
		{
			throw new InvalidOperationException("The instance name is invalid");
		}

		string sql = Profile.SqlProviderOptions.SqlType switch
		{
			SqlType.SqlServer => $"SELECT TOP 1 Data FROM [{Profile.InstanceName}] WHERE InstanceId = @InstanceId",
			SqlType.Postgres => $"SELECT Data FROM \"{Profile.InstanceName}\" WHERE \"InstanceId\" = @InstanceId LIMIT 1",
			SqlType.MySql => $"SELECT Data FROM `{Profile.InstanceName}` WHERE InstanceId = @InstanceId LIMIT 1",
			SqlType.Sqlite => $"SELECT Data FROM \"{Profile.InstanceName}\" WHERE InstanceId = @InstanceId LIMIT 1",
			SqlType.None => $"SELECT Data FROM \"{Profile.InstanceName}\" WHERE InstanceId = @InstanceId FETCH FIRST 1 ROWS ONLY",
			_ => throw new NotSupportedException("SqlType is invalid")
		};

		await using DbCommand command = connection.CreateCommand();
		command.CommandText = sql;

		DbParameter instanceIdParameter = command.CreateParameter();
		instanceIdParameter.ParameterName = "@InstanceId";
		instanceIdParameter.Value = Profile.InstanceId;
		command.Parameters.Add(instanceIdParameter);

		string data = string.Empty;

		try
		{
			object result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			data = (string)result;
		}
		catch
		{
			State = new T();
			OnStateChange(ProviderEventType.Load);
			return State;
		}

		if (string.IsNullOrEmpty(data))
		{
			State = new T();
			OnStateChange(ProviderEventType.Load);
			return State;
		}

		if (Profile.AutoEncrypt)
		{
			data = Decrypt(data);
		}

		State = JsonSerializer.Deserialize<T>(data) ?? new T();


		OnStateChange(ProviderEventType.Load);
		return State;
	}

	public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		string data = Profile.AutoEncrypt ? EncryptState() : JsonSerializer.Serialize(state);

		await using DbConnection connection = _connectionFactory();
		await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		if (!BladeStateRegex.AlphaNumericAndUnderscore().IsMatch(Profile.InstanceName))
		{
			throw new InvalidOperationException("The instance name is invalid");
		}

		string sql = Profile.SqlProviderOptions.SqlType switch
		{
			SqlType.SqlServer => $@"
				MERGE [{Profile.InstanceName}] AS target
				USING (SELECT @InstanceId AS InstanceId, @Data AS Data) AS source
				ON target.InstanceId = source.InstanceId
				WHEN MATCHED THEN UPDATE SET Data = source.Data
				WHEN NOT MATCHED THEN INSERT (InstanceId, Data) VALUES (source.InstanceId, source.Data);",

			SqlType.Postgres => $@"
				INSERT INTO ""{Profile.InstanceName}"" (""InstanceId"", ""Data"")
				VALUES (@InstanceId, @Data)
				ON CONFLICT (""InstanceId"") DO UPDATE SET ""Data"" = EXCLUDED.""Data"";",

			SqlType.MySql => $@"
				INSERT INTO `{Profile.InstanceName}` (InstanceId, Data)
				VALUES (@InstanceId, @Data)
				ON DUPLICATE KEY UPDATE Data = VALUES(Data);",

			SqlType.Sqlite => $@"
				INSERT OR REPLACE INTO ""{Profile.InstanceName}"" (InstanceId, Data)
				VALUES (@InstanceId, @Data);",

			SqlType.None => $@"
				MERGE [{Profile.InstanceName}] AS target
				USING (SELECT @InstanceId AS InstanceId, @Data AS Data) AS source
				ON target.InstanceId = source.InstanceId
				WHEN MATCHED THEN UPDATE SET Data = source.Data
				WHEN NOT MATCHED THEN INSERT (InstanceId, Data) VALUES (source.InstanceId, source.Data);",

			_ => throw new NotSupportedException("Unsupported SQL type")
		};

		await using DbCommand command = connection.CreateCommand();
		command.CommandText = sql;

		DbParameter instanceIdParameter = command.CreateParameter();
		instanceIdParameter.ParameterName = "@InstanceId";
		instanceIdParameter.Value = Profile.InstanceId;
		command.Parameters.Add(instanceIdParameter);

		DbParameter dataParameter = command.CreateParameter();
		dataParameter.ParameterName = "@Data";
		dataParameter.Value = data;
		command.Parameters.Add(dataParameter);

		await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

		await CheckTimeoutAsync(cancellationToken);
		OnStateChange(ProviderEventType.Save);
	}

	public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		await using DbConnection connection = _connectionFactory();
		await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		string sql = $"DELETE FROM {GetSanitizedTableName()} WHERE InstanceId = @InstanceId";

		await using DbCommand command = connection.CreateCommand();
		command.CommandText = sql;

		DbParameter p = command.CreateParameter();
		p.ParameterName = "@InstanceId";
		p.Value = Profile.InstanceId;
		command.Parameters.Add(p);

		try
		{
			await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		}
		catch
		{
			// swallow or log
		}

		State = new T();
		await CheckTimeoutAsync(cancellationToken);
		OnStateChange(ProviderEventType.Clear);
	}

	private string GetSanitizedTableName()
	{
		if (!Regex.IsMatch(Profile.InstanceName, @"^[A-Za-z0-9_]+$"))
			throw new InvalidOperationException("Invalid table name");
		return $"[{Profile.InstanceName}]";
	}

	private async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
	{
		if (TableExists)
		{
			return;
		}

		string sql = Profile.SqlProviderOptions.SqlType switch
		{
			SqlType.SqlServer => $@"
				IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{Profile.InstanceName}' AND xtype='U')
				CREATE TABLE [{Profile.InstanceName}] (
					 InstanceId NVARCHAR(200) PRIMARY KEY,
					 Data NVARCHAR(MAX) NOT NULL
				);",

			SqlType.Postgres => $@"
				CREATE TABLE IF NOT EXISTS ""{Profile.InstanceName}"" (
					 ""InstanceId"" VARCHAR(200) PRIMARY KEY,
					 ""Data"" TEXT NOT NULL
				);",

			SqlType.MySql => $@"
				CREATE TABLE IF NOT EXISTS `{Profile.InstanceName}` (
					 InstanceId VARCHAR(200) PRIMARY KEY,
					 Data TEXT NOT NULL
				);",

			SqlType.Sqlite => $@"
				CREATE TABLE IF NOT EXISTS ""{Profile.InstanceName}"" (
					 InstanceId TEXT PRIMARY KEY,
					 Data TEXT NOT NULL
				);",

			_ => throw new NotSupportedException("Unsupported SQL type")
		};

		await using DbConnection connection = _connectionFactory();
		await connection.OpenAsync(cancellationToken);

		await using DbCommand command = connection.CreateCommand();
		command.CommandText = sql;

		try
		{
			await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			TableExists = true;
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException($"An error occurred ensuring existence of sql state table. Ex: {exception.Message}");
		}
	}

}