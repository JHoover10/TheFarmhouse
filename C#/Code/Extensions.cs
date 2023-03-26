using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace C_Sharp;

public static class Extensions
{
    public static void BulkMerge<T>(this SqlConnection sqlConnection, IEnumerable<T> entity, string identityColumn = null, List<string> uniqueColumns = null)
    {
        using var sqlTransaction = sqlConnection.BeginTransaction();

        try
        {
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.Transaction = sqlTransaction;

            var tempTableName = sqlCommand.ToTempTable(entity);

            var onClause = new StringBuilder();
            var updateClause = new StringBuilder("UPDATE SET ");
            var insertColumns = new StringBuilder();
            var insertValues = new StringBuilder();

            foreach (var property in typeof(T).GetProperties())
            {
                updateClause.Append($"T.{property.Name} = S.{property.Name}, ");
                insertColumns.Append($"{property.Name}, ");
                insertValues.Append($"S.{property.Name}, ");
            }

            updateClause.Remove(updateClause.Length - 2, 2);
            insertColumns.Remove(insertColumns.Length - 2, 2);
            insertValues.Remove(insertValues.Length - 2, 2);

            var query = 
                @$"MERGE INTO {typeof(T).Name} T
                USING {tempTableName} S
                ON {onClause}
                WHEN MATCH THEN
                    {updateClause}
                WHEN NOT MATCHED THEN
                    INSERT ({insertColumns}) VALUES ({insertValues})";

            sqlCommand.CommandText = query;
            sqlCommand.ExecuteScalar();

            sqlTransaction.Commit();
        }
        catch 
        {
            sqlTransaction.Rollback();
            throw;
        }
    }

    public static string ToTempTable<T>(this SqlCommand sqlCommand, IEnumerable<T> entity, bool useGlobalTable = false)
    {
        var tempTableName = $"{(useGlobalTable ? "##" : "#")}{Guid.NewGuid().ToString("N")}";
        var tableColumns = new StringBuilder();

        foreach (var property in typeof(T).GetProperties())
        {
            tableColumns.AppendLine($"{property.Name} {property.GetSqlType()},");
        }

        var query =
            @$"
            IF OBJECT_ID('tempdb..{tempTableName}') IS NOT NULL
                DROP TABLE {tempTableName}
            
            CREATE TABLE {tempTableName} (
                {tableColumns}
            )
            ";

        sqlCommand.CommandText = query;
        sqlCommand.ExecuteScalar();

        using (var sqlBulkCopy = new SqlBulkCopy(sqlCommand.Connection))
        {
            sqlBulkCopy.DestinationTableName = tempTableName;
            sqlBulkCopy.WriteToServer(entity.ToDataTable());
        }

        return tempTableName;
    }

    public static DataTable ToDataTable<T>(this IEnumerable<T> items)
    {
        DataTable dataTable = new DataTable(typeof(T).Name);

        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var type = (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(property.PropertyType) : property.PropertyType);
            dataTable.Columns.Add(property.Name, type);
        }
        foreach (var item in items)
        {
            var values = new object[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                values[i] = properties[i].GetValue(item, null);
            }
            dataTable.Rows.Add(values);
        }
        
        return dataTable;
    }

    public static void BuildInsertStatement<T>(this IDbCommand command, T value, string schema, List<string> columnsToSkip = null, bool outputCreatedValue = false)
    {
        var type = typeof(T);
        var columnBuilder = new StringBuilder();
        var valueBuilder = new StringBuilder();

        foreach (var property in type.GetProperties())
        {
            if (columnsToSkip != null && columnsToSkip.Any() && columnsToSkip.Contains(property.Name))
            {
                continue;
            }

            var columnName = property.GetCustomAttribute<ColumnNameAttribute>()?.GetName() ?? property.Name;

            columnBuilder.Append($"{columnName}, ");
            valueBuilder.Append($"{columnName}, ");

            switch (property.PropertyType)
            {
                case Type _ when property.PropertyType == typeof(JObject):
                    command.Parameters.Add(new SqlParameter($"@{columnName}", JsonConvert.SerializeObject(property.GetValue(value))));
                    break;
                default:
                    command.Parameters.Add(new SqlParameter($"@{columnName}", property.GetValue(value)));
                    break;
            }
        }

        command.CommandText = 
            $@"INSERT [{schema}].{type.Name} ({columnBuilder.ToString().Substring(0, columnBuilder.Length - 2)})
            {(outputCreatedValue ? "OUTPUT INSERTED.*" : string.Empty)}
            VALUES ({valueBuilder.ToString().Substring(0, valueBuilder.Length - 2)})";
    }

    public static T? Clone<T>(this T source)
    {
        if (source == null)
            return default(T);

        var serialized = JsonConvert.SerializeObject(source);
        return JsonConvert.DeserializeObject<T>(serialized);
    }

    public static List<T> ToList<T>(this IDataReader reader, Func<(string, int), IDataReader, T, bool> manualMapping = null)
    {
        var list = new List<T>();
        var ordinals = new Dictionary<string, int>();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            ordinals.Add(reader.GetName(i).ToUpper(), i);
        }

        while (reader.Read())
        {
            var item = Activator.CreateInstance<T>();
            list.Add(item);

            foreach (var (name, index) in ordinals)
            {
                if (manualMapping != null && manualMapping((name, index), reader, item))
                {
                    continue;
                }

                var property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
                var propertyType = property.PropertyType;

                switch (propertyType)
                {
                    case Type _ when propertyType == typeof(string):
                        if (reader.IsDBNull(index))
                            property.SetValue(item, null, null);
                        else
                            property.SetValue(item, reader.GetString(index), null);
                        break;
                    case Type _ when propertyType == typeof(bool):
                        property.SetValue(item, reader.GetBoolean(index), null);
                        break;
                    case Type _ when propertyType == typeof(short):
                        property.SetValue(item, reader.GetInt16(index), null);
                        break;
                    case Type _ when propertyType == typeof(int):
                        property.SetValue(item, reader.GetInt32(index), null);
                        break;
                    case Type _ when propertyType == typeof(long):
                        property.SetValue(item, reader.GetInt64(index), null);
                        break;
                    case Type _ when propertyType == typeof(byte[]):
                        property.SetValue(item, reader[index], null);
                        break;
                    case Type _ when propertyType == typeof(Guid):
                        if (reader.IsDBNull(index))
                            property.SetValue(item, Guid.Empty, null);
                        else
                            property.SetValue(item, reader.GetGuid(index), null);
                        break;
                    case Type _ when propertyType == typeof(JObject):
                        if (reader.IsDBNull(index))
                            property.SetValue(item, null, null);
                        else
                            property.SetValue(item, JsonConvert.DeserializeObject<JObject>(reader.GetString(index)), null);
                        break;
                    default:
                        break;
                }
            }
        }

        return list;
    }

    public static async Task<T?> ToObject<T>(this HttpContent httpContent, Func<string, T>? manualMapping = null)
    {
        var content = await httpContent.ReadAsStringAsync();

        if (manualMapping != null)
        {
            return manualMapping(content);
        }

        if (content == null)
            return default(T);

        if (content is T)
            return (T)Convert.ChangeType(content, typeof(T));

        return JsonConvert.DeserializeObject<T>(content);
    }

    private static string GetSqlType(this PropertyInfo property, string varcharLength = "MAX", int decimalPrecision = 18, int decimalScale = 2)
    {
        switch (property.PropertyType)
        {
            case Type _ when property.PropertyType == typeof(string):
                return $"VARCHAR({varcharLength})";
            case Type _ when property.PropertyType == typeof(short):
                return "smallint";
            case Type _ when property.PropertyType == typeof(int):
                return "int";
            case Type _ when property.PropertyType == typeof(long):
                return "bigint";
            case Type _ when property.PropertyType == typeof(float):
            case Type _ when property.PropertyType == typeof(decimal):
                return $"DECIMAL({decimalPrecision}, {decimalScale})";
            case Type _ when property.PropertyType == typeof(Guid):
                return "UNIQUEIDENTIFIER";
            case Type _ when property.PropertyType == typeof(DateTime):
                return "DATETIME";
            case Type _ when property.PropertyType == typeof(DateOnly):
                return "DATE";
            default:
                return null;
        }
    }
}