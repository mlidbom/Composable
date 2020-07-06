using System;
using System.Data;
using System.Data.SqlClient;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    static class SqlCommandParameterExtensions
    {
        public static SqlCommand AddParameter(this SqlCommand @this, string name, int value) => AddParameter(@this, name, SqlDbType.Int, value);
        public static SqlCommand AddParameter(this SqlCommand @this, string name, Guid value) => AddParameter(@this, name, SqlDbType.UniqueIdentifier, value);
        public static SqlCommand AddDateTime2Parameter(this SqlCommand @this, string name, DateTime value) => AddParameter(@this, name, SqlDbType.DateTime2, value);
        public static SqlCommand AddNVarcharParameter(this SqlCommand @this, string name, int length, string value) => AddParameter(@this, name, SqlDbType.NVarChar, value, length);
        public static SqlCommand AddNVarcharMaxParameter(this SqlCommand @this, string name, string value) => AddParameter(@this, name, SqlDbType.NVarChar, value, -1);

        public static SqlCommand AddParameter(this SqlCommand @this, SqlParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static SqlCommand AddParameter(SqlCommand @this, string name, SqlDbType type, object value, int length) => @this.AddParameter(new SqlParameter(name, type, length) {Value = value});

        public static SqlCommand AddParameter(this SqlCommand @this, string name, SqlDbType type, object value) => @this.AddParameter(new SqlParameter(name, type) {Value = value});

        public static SqlCommand AddNullableParameter(this SqlCommand @this, string name, SqlDbType type, object? value) => @this.AddParameter(Nullable(new SqlParameter(name, type) {Value = value}));

        static SqlParameter Nullable(SqlParameter @this)
        {
            @this.IsNullable = true;
            @this.Direction = ParameterDirection.Input;
            if(@this.Value == null)
            {
                @this.Value = DBNull.Value;
            }
            return @this;
        }
    }
}
