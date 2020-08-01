using Composable.Persistence.EventStore.PersistenceLayer;
using IBM.Data.DB2Types;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class GetDB2DecimalExtensions
    {
        internal static DB2Decimal ToDB2DecimalIntegerPart(this ReadOrder readOrder) => DB2Decimal.Parse(readOrder.ToString().Split(".")[0]);
        internal static DB2Decimal ToDB2DecimalFractionPart(this ReadOrder readOrder) => DB2Decimal.Parse(readOrder.ToString().Split(".")[1]);
    }
}
