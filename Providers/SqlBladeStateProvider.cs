using System;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState.Providers
{
    public class SqlBladeStateProvider<T> : BladeStateProvider<T> where T : class, new()
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly string _tableName;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public SqlBladeStateProvider(Func<DbConnection> connectionFactory, string tableName = "BladeState")
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _tableName = tableName;
        }

        public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT StateJson FROM {_tableName} WHERE Id = @Id";

            var idParam = command.CreateParameter();
            idParam.ParameterName = "@Id";
            idParam.Value = Profile.Id;
            command.Parameters.Add(idParam);

            object? result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null || result == DBNull.Value)
                return new T();

            return JsonSerializer.Deserialize<T>((string)result, _jsonOptions) ?? new T();
        }

        public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory();
            await connection.OpenAsync(cancellationToken);

            string json = JsonSerializer.Serialize(state, _jsonOptions);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                MERGE {_tableName} AS target
                USING (SELECT @Id AS Id, @StateJson AS StateJson) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN 
                    UPDATE SET StateJson = source.StateJson
                WHEN NOT MATCHED THEN
                    INSERT (Id, StateJson) VALUES (source.Id, source.StateJson);";

            var idParam = command.CreateParameter();
            idParam.ParameterName = "@Id";
            idParam.Value = Profile.Id;
            command.Parameters.Add(idParam);

            var stateParam = command.CreateParameter();
            stateParam.ParameterName = "@StateJson";
            stateParam.Value = json;
            command.Parameters.Add(stateParam);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {_tableName} WHERE Id = @Id";

            var idParam = command.CreateParameter();
            idParam.ParameterName = "@Id";
            idParam.Value = Profile.Id;
            command.Parameters.Add(idParam);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
