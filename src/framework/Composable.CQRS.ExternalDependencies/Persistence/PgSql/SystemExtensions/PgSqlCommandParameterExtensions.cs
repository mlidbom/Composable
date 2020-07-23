using System;
using System.Data;
using Composable.SystemCE.Linq;
using Npgsql;
using NpgsqlTypes;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class NpgsqlCommandParameterExtensions
    {
        public static NpgsqlCommand AddParameter(this NpgsqlCommand @this, string name, int value) => AddParameter(@this, name, NpgsqlDbType.Integer, value);
        public static NpgsqlCommand AddParameter(this NpgsqlCommand @this, string name, Guid value) => AddParameter(@this, name, NpgsqlDbType.Char, value.ToString(), 39);
        public static NpgsqlCommand AddDateTime2Parameter(this NpgsqlCommand @this, string name, DateTime value) => AddParameter(@this, name, NpgsqlDbType.Timestamp, value);
        //urgent:The way I understand postgres the length here is useless and thus this method is useless. See: https://www.postgresql.org/docs/current/datatype-character.html, https://www.postgresqltutorial.com/postgresql-char-varchar-text/
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

        [Obsolete]public static NpgsqlCommand DebugPrintCommandText(this NpgsqlCommand @this) => @this.Mutate(_ => Console.WriteLine(@this.CommandText));

        internal static NpgsqlParameter Nullable(NpgsqlParameter @this)
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
