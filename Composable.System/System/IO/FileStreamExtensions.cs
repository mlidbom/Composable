using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Composable.System.IO
{
    public static class FileStreamExtensions
    {
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
