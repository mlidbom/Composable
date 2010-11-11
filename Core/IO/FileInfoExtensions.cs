using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Void.IO
{
    /// <summary>
    /// Extensions for <see cref="FileInfo"/>
    /// </summary>
    [Pure]
    public static class FileInfoExtensions
    {
    
        /// <summary>
        /// Returns a sequnce where all files have <paramref name="extensions"/> as their extension
        /// </summary>
        /// <param name="me"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> WithExtension(this IEnumerable<FileInfo> me, params string[] extensions)
        {
            Contract.Requires(me != null);
            return me.Where(file => extensions.Contains(file.Extension));
        }
    }
}