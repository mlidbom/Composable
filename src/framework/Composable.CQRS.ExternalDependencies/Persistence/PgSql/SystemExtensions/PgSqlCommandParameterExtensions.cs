using System;
using System.Data;
using Composable.SystemCE;
using Npgsql;
using NpgsqlTypes;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class NpgsqlCommandParameterExtensions
    {
        public static NpgsqlCommand AddParameter(this NpgsqlCommand @this, string name, int value) => AddParameter(@this, name, NpgsqlDbType.Integer, value);
        public static NpgsqlCommand AddParameter(this NpgsqlCommand @this, string name, Guid value) => AddParameter(@this, name, NpgsqlDbType.Char, value.ToString(), 39);
        public static NpgsqlCommand AddTimestampWithTimeZone(this NpgsqlCommand @this, string name, DateTime value) => AddParameter(@this, name, NpgsqlDbType.TimestampTz, value.ToUniversalTimeSafely());
        public static NpgsqlCommand AddVarcharParameter(this NpgsqlCommand @this, string name, int length, string value) => AddParameter(@this, name, NpgsqlDbType.Varchar, value, length);
        public static NpgsqlCommand AddMediumTextParameter(this NpgsqlCommand @this, string name, string value) => AddParameter(@this, name, NpgsqlDbType.Text, value, -1);

        public static NpgsqlCommand AddParameter(this NpgsqlCommand @this, NpgsqlParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static NpgsqlCommand AddParameter(NpgsqlCommand @this, string name, NpgsqlDbType type, object value, int length) => @this.AddParameter(new NpgsqlParameter(name, type, length) {Value = value});

        public static NpgsqlCommand AddParameter(this NpgsqlCommand @this, string name, NpgsqlDbType type, object value) => @this.AddParameter(new NpgsqlParameter(name, type) {Value = value});

        public static NpgsqlCommand AddNullableParameter(this NpgsqlCommand @this, string name, NpgsqlDbType type, object? value) => @this.AddParameter(Nullable(new NpgsqlParameter(name, type) {Value = value}));

        static NpgsqlParameter Nullable(NpgsqlParameter @this)
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
