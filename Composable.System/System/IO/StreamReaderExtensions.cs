using System.Collections.Generic;
using System.IO;

namespace Composable.System.IO
{
    ///<summary>Enumerates the lines in a streamreader.</summary>
    public static class StreamReaderExtensions
    {
        ///<summary>Streams the rows returned by the StreamReader one at a time.</summary>
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