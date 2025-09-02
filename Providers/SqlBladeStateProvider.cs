using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState.Providers
{
    public class SqlBladeStateProvider<T>
    {
        private readonly DbConnection _connection;
        private readonly string _rootTable;
        public string StateId { get; }
        public T State { get; private set; }

        public SqlBladeStateProvider(DbConnection connection, string stateId, string rootTable = "")
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            StateId = stateId ?? throw new ArgumentNullException(nameof(stateId));
            _rootTable = rootTable ?? typeof(T).Name;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            State = Activator.CreateInstance<T>();
            await SaveStateInternalAsync(State, _rootTable, string.Empty, cancellationToken);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (State is null)
                throw new InvalidOperationException("State has not been initialized.");
            await SaveStateInternalAsync(State, _rootTable, string.Empty, cancellationToken);
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (State == null)
                State = Activator.CreateInstance<T>();

            await LoadStateInternalAsync(State, _rootTable, string.Empty, cancellationToken);
        }

        private async Task SaveStateInternalAsync(object currentObject, string tableName, string parentKey, CancellationToken cancellationToken)
        {
            Type type = currentObject.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Ensure table exists (with foreign key if nested)
            await EnsureTableAsync(tableName, properties, !string.IsNullOrWhiteSpace(parentKey), cancellationToken);

            List<string> columnNames = ["StateId"];
            List<string> paramNames = ["@StateId"];

            using DbCommand command = _connection.CreateCommand();
            string stateId = parentKey ?? StateId;
            DbParameter stateIdParameter = command.CreateParameter();
            stateIdParameter.ParameterName = "@StateId";
            stateIdParameter.Value = stateId;
            command.Parameters.Add(stateIdParameter);

            foreach (PropertyInfo prop in properties)
            {
                if (IsComplexType(prop.PropertyType))
                {
                    object nestedObject = prop.GetValue(currentObject);
                    if (nestedObject is not null)
                    {
                        string nestedTableName = $"{tableName}_{prop.Name}";
                        await SaveStateInternalAsync(nestedObject, nestedTableName, stateId, cancellationToken);
                    }
                }
                else
                {
                    object value = prop.GetValue(currentObject) ?? DBNull.Value;
                    string columnName = Sanitize(prop.Name);
                    string parameterName = $"@{prop.Name}";
                    columnNames.Add(columnName);
                    paramNames.Add(parameterName);
                    DbParameter parameter = command.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }
            }

            command.CommandText = $@"
                INSERT INTO {Sanitize(tableName)} ({string.Join(", ", columnNames)})
                VALUES ({string.Join(", ", paramNames)})
                ON CONFLICT(StateId) DO UPDATE SET {string.Join(", ", columnNames.Skip(1).Select(c => $"{c} = excluded.{c}"))};";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task LoadStateInternalAsync(object state, string tableName, string parentStateId, CancellationToken cancellationToken)
        {
            Type type = state.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using DbCommand cmd = _connection.CreateCommand();
            string stateId = string.IsNullOrWhiteSpace(parentStateId) ? StateId : parentStateId;
            cmd.CommandText = $"SELECT * FROM {Sanitize(tableName)} WHERE StateId = @StateId;";
            DbParameter stateIdParameter = cmd.CreateParameter();
            stateIdParameter.ParameterName = "@StateId";
            stateIdParameter.Value = stateId;
            cmd.Parameters.Add(stateIdParameter);

            using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return;

            foreach (PropertyInfo property in properties)
            {
                if (IsComplexType(property.PropertyType))
                {
                    object nestedObject = Activator.CreateInstance(property.PropertyType);
                    string nestedTableName = $"{tableName}_{property.Name}";
                    await LoadStateInternalAsync(nestedObject, nestedTableName, stateId, cancellationToken);
                    property.SetValue(state, nestedObject);
                }
                else
                {
                    int ordinal = reader.GetOrdinal(property.Name);
                    if (!reader.IsDBNull(ordinal))
                    {
                        object value = reader.GetValue(ordinal);
                        property.SetValue(state, Convert.ChangeType(value, property.PropertyType));
                    }
                }
            }
        }

        private async Task EnsureTableAsync(string tableName, PropertyInfo[] props, bool hasParent, CancellationToken cancellationToken)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE IF NOT EXISTS {Sanitize(tableName)} (");
            sb.AppendLine("  StateId TEXT NOT NULL PRIMARY KEY,");

            foreach (PropertyInfo prop in props)
            {
                if (IsComplexType(prop.PropertyType))
                    continue;

                sb.AppendLine($"  {Sanitize(prop.Name)} {GetSqlType(prop.PropertyType)},");
            }

            if (hasParent)
            {
                sb.AppendLine("  ParentStateId TEXT,");
                sb.AppendLine("  FOREIGN KEY(ParentStateId) REFERENCES {Sanitize(tableName)}(StateId)");
            }

            sb.Length--; // remove last comma/newline
            sb.AppendLine(");");

            using DbCommand cmd = _connection.CreateCommand();
            cmd.CommandText = sb.ToString();
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string GetSqlType(Type type) =>
        Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type) switch
        {
            TypeCode.Int32 => "INT",
            TypeCode.Int64 => "BIGINT",
            TypeCode.String => "NVARCHAR(MAX)",
            TypeCode.Boolean => "BIT",
            TypeCode.DateTime => "DATETIME2",
            TypeCode.Decimal => "DECIMAL(18,2)",
            TypeCode.Double => "FLOAT",
            TypeCode.Single => "REAL",
            _ => "NVARCHAR(MAX)"
        };

        private static bool IsComplexType(Type type)
        {
            return !(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime));
        }

        private static string Sanitize(string name) =>
            new([.. name.Where(char.IsLetterOrDigit)]);
    }
}
