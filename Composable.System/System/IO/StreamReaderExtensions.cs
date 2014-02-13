using System.Collections.Generic;
using System.IO;

namespace Composable.System.IO
{
    public static class StreamReaderExtensions
    {
        public static IEnumerable<string> Lines(this StreamReader me)
        {
            string row;
            while ((row = me.ReadLine()) != null)
            {
                yield return row;
            }
        }
    }
}