using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace C_Sharp;

public static class Extensions
{
    public static string ToTempTable<T>(this SqlCommand sqlCommand, IEnumerable<T> entity, bool useGlobalTable = false)
    {
        var tempTableName = $"{(useGlobalTable ? "##" : "#")}{Guid.NewGuid().ToString("N")}";
        var tableColumns = new StringBuilder();

        foreach (var property in typeof(T).GetProperties())
        {
            tableColumns.AppendLine($"{property.Name} {property.GetSqlType(entity)},");
        }

        var query =
            @$"
            IF OBJECT_ID('tempdb..{tempTableName}') IS NOT NULL
                DROP TABLE {tempTableName}
            
            CREATE TABLE {tempTableName} (
                {tableColumns.ToString()}
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

    private static string GetSqlType<T>(this PropertyInfo property, T entity)
    {
        switch (property.PropertyType)
        {
            case Type stringType when stringType == typeof(string):
                return "VARCHAR(MAX)";

            case Type intType when intType == typeof(int):
                return "int";

            case Type longType when longType == typeof(long):
                return "bigint";

            case Type floatType when floatType == typeof(float):
            case Type deciamlType when deciamlType == typeof(decimal):
                return "DECIMAL(18, 2)";

            case Type guidType when guidType == typeof(Guid):
                return "UNIQUEIDENTIFIER";

            case Type dateTimeType when dateTimeType == typeof(DateTime):
                return "DATETIME";

            case Type dateOnlyType when dateOnlyType == typeof(DateOnly):
                return "DATE";

            default:
                return null;
        }
    }
}