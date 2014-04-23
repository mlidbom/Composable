using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Composable.System.IO
{
    ///<summary>Enumerates the lines in a FileStream.</summary>
    public static class FileStreamExtensions
    {
        ///<summary>Streams the rows returned by the FileStream one at a time.</summary>
        public static IEnumerable<string> Lines(this FileStream me)
        {
            Contract.Requires(me != null);
            using(var reader = new StreamReader(me))
            {
                return reader.Lines().ToList();
            }
        }
    }
}
