using System;
using Composable.Persistence.Common.EventStore;

namespace Composable.Persistence.DB2.EventStore
{
    class DB2EventTableColumns : EventTable.Columns
    {
        [Obsolete("DB2 does not support decimals with the required precision, so in DB2 we will use two columns")]public new const string ReadOrder = ";/()DON'T USE;/()~";
        internal static readonly string ReadOrderIntegerPart = $"{EventTable.Columns.ReadOrder}IntegerPart";
        internal static readonly string ReadOrderFractionPart = $"{EventTable.Columns.ReadOrder}FractionPart";
    }
}
