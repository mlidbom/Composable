using System.Globalization;

namespace Composable.System
{
    class IntEx
    {
        internal static int ParseInvariant(string intAsString) => int.Parse(intAsString, CultureInfo.InvariantCulture);
    }
}
