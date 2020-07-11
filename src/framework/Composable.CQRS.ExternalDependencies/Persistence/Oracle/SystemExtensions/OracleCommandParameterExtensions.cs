using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleCommandParameterExtensions
    {
        public static OracleCommand AddParameter(this OracleCommand @this, string name, int value) => AddParameter(@this, name, OracleDbType.Int32, value);
        public static OracleCommand AddParameter(this OracleCommand @this, string name, Guid value) => AddParameter(@this, name, OracleDbType.Varchar2, value);
        public static OracleCommand AddDateTime2Parameter(this OracleCommand @this, string name, DateTime value) => AddParameter(@this, name, OracleDbType.TimeStamp, value);
        public static OracleCommand AddVarcharParameter(this OracleCommand @this, string name, int length, string value) => AddParameter(@this, name, OracleDbType.Varchar2, value, length);
        public static OracleCommand AddMediumTextParameter(this OracleCommand @this, string name, string value) => AddParameter(@this, name, OracleDbType.NClob, value, -1);

        public static OracleCommand AddParameter(this OracleCommand @this, OracleParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static OracleCommand AddParameter(OracleCommand @this, string name, OracleDbType type, object value, int length) => @this.AddParameter(new OracleParameter(name, type, length) {Value = value});

        public static OracleCommand AddParameter(this OracleCommand @this, string name, OracleDbType type, object value) => @this.AddParameter(new OracleParameter(name, type) {Value = value});

        public static OracleCommand AddNullableParameter(this OracleCommand @this, string name, OracleDbType type, object? value) => @this.AddParameter(Nullable(new OracleParameter(name, type) {Value = value}));

        static OracleParameter Nullable(OracleParameter @this)
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
