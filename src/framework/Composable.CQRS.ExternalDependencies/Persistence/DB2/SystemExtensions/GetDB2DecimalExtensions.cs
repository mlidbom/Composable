using Composable.Persistence.EventStore.PersistenceLayer;
using IBM.Data.DB2Types;

namespace Composable.Persistence.DB2.SystemExtensions
{
    public static class GetDB2DecimalExtensions
    {
        //DB2Decimal does maintain the full precision, but when all 19 numbers are not required to display accurately it omits all the trailing zeroes.
        internal static ReadOrder ToReadOrder(this DB2Decimal db2Decimal) => ReadOrder.Parse(db2Decimal.ToString());

        internal static DB2Decimal ToDB2Decimal(this ReadOrder readOrder) => DB2Decimal.Parse(readOrder.ToString());
    }
}
