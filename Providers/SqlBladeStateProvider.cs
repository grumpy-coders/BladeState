using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Models;

namespace BladeState.Providers;

public class SqlBladeStateProvider<T>(
    Func<DbConnection> connectionFactory,
    BladeStateCryptography bladeStateCryptography
) : BladeStateProvider<T>(bladeStateCryptography) where T : class, new()
{
    private readonly Func<DbConnection> _connectionFactory = connectionFactory;

    public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return State;

        await using DbConnection conn = _connectionFactory();
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using DbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Data FROM BladeState WHERE InstanceId = @InstanceId";
        DbParameter param = cmd.CreateParameter();
        param.ParameterName = "@InstanceId";
        param.Value = Profile.InstanceId;
        cmd.Parameters.Add(param);

        string data;

        try
        {
            object result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            data = (string)result;
        }
        catch
        {
            State = new T();
            return State;
        }

        if (Profile.AutoEncrypt)
        {
            CipherState = data;
            DecryptState();
            return State;
        }

        State = JsonSerializer.Deserialize<T>(data);
        return State;
    }

    public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        string data;

        if (Profile.AutoEncrypt)
        {
            EncryptState();
            data = CipherState;
        }
        else
        {
            data = JsonSerializer.Serialize(state);
        }

        await using DbConnection connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
MERGE BladeState AS target
USING (SELECT @InstanceId AS InstanceId, @Data AS Data) AS source
ON target.InstanceId = source.InstanceId
WHEN MATCHED THEN UPDATE SET Data = source.Data
WHEN NOT MATCHED THEN INSERT (InstanceId, Data) VALUES (source.InstanceId, source.Data);";

        DbParameter instanceIdParameter = command.CreateParameter();
        instanceIdParameter.ParameterName = "@InstanceId";
        instanceIdParameter.Value = Profile.InstanceId;
        command.Parameters.Add(instanceIdParameter);

        DbParameter dataParameter = command.CreateParameter();
        dataParameter.ParameterName = "@Data";
        dataParameter.Value = data;
        command.Parameters.Add(dataParameter);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        await using DbConnection connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM BladeState WHERE InstanceId = @InstanceId";

        DbParameter instanceIdParameter = command.CreateParameter();
        instanceIdParameter.ParameterName = "@InstanceId";
        instanceIdParameter.Value = Profile.InstanceId;
        command.Parameters.Add(instanceIdParameter);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // swallow or log exceptions
        }

        CipherState = string.Empty;
        State = new T();
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await ClearStateAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // swallow or log exceptions
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
