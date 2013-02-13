using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Composable.System.IO
{
    public static class FileStreamExtensions
    {
        public static IEnumerable<string> Lines(this FileStream me)
        {
            using(var reader = new StreamReader(me))
            {
                return reader.Lines().ToList();
            }
        }
    }
}
