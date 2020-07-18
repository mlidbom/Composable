using System;
using System.Data;
using IBM.Data.DB2.Core;
using IBM.Data.DB2Types;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class DB2CommandParameterExtensions
    {
        public static DB2Command AddParameter(this DB2Command @this, string name, int value) => AddParameter(@this, name, DB2Type.Integer, value);
        public static DB2Command AddParameter(this DB2Command @this, string name, Guid value) => AddParameter(@this, name, DB2Type.Char, value.ToString(), 39);
        public static DB2Command AddParameter(this DB2Command @this, string name, DateTime value) => AddParameter(@this, name, DB2Type.Timestamp, value);
        public static DB2Command AddVarcharParameter(this DB2Command @this, string name, int length, string value) => AddParameter(@this, name, DB2Type.VarChar, value, length);
        public static DB2Command AddNClobParameter(this DB2Command @this, string name, string value) => AddParameter(@this, name, DB2Type.Text, value);

        public static DB2Command AddParameter(this DB2Command @this, DB2Parameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static DB2Command AddParameter(DB2Command @this, string name, DB2Type type, object value, int length) => @this.AddParameter(new DB2Parameter(name, type, length) {Value = value});

        public static DB2Command AddParameter(this DB2Command @this, string name, DB2Type type, object value) => @this.AddParameter(new DB2Parameter(name, type) {Value = value});

        public static DB2Command AddNullableParameter(this DB2Command @this, string name, DB2Type type, object? value) => @this.AddParameter(Nullable(new DB2Parameter(name, type) {Value = value}));

        static DB2Parameter Nullable(DB2Parameter @this)
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
