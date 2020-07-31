using Composable.Persistence.EventStore.PersistenceLayer;
using Oracle.ManagedDataAccess.Types;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class GetOracleDecimalExtensions
    {
        //OracleDecimal does maintain the full precision, but when all 19 numbers are not required to display accurately it omits all the trailing zeroes.
        internal static ReadOrder ToReadOrder(this OracleDecimal oracleDecimal) =>
            oracleDecimal.IsInt
                ? ReadOrder.Parse($"{oracleDecimal}.{0:D19}")
                : ReadOrder.Parse(oracleDecimal.ToString(), bypassScaleTest: true);

        internal static OracleDecimal ToOracleDecimal(this ReadOrder readOrder) => OracleDecimal.Parse(readOrder.ToString());
    }
}
