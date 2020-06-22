using System.IO;
using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Hierarchies;

namespace Composable.System.IO
{
    /// <summary/>
    static class DirectoryExtensions
    {
        /// <summary>
        /// Called on <paramref name="path"/> return a DirectoryInfo instance
        /// pointed at that path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DirectoryInfo AsDirectory(this string path)
        {
            Contract.ArgumentNotNullEmptyOrWhitespace(path, nameof(path));
            return new DirectoryInfo(path);
        }

        /// <summary>
        /// Returns the size of the directory.
        /// </summary>
        public static long Size(this DirectoryInfo me)
        {
            Contract.ArgumentNotNull(me, nameof(me));
            return me.FullName
                .AsHierarchy(Directory.GetDirectories).Flatten().Unwrap()
                .SelectMany(Directory.GetFiles)
                .Sum(file => new FileInfo(file).Length);
        }

        /// <summary>
        /// Recursively deletes everything in a airectory and the directory itself.
        /// 
        /// A more intuitive alias for <see cref="DirectoryInfo.Delete(bool)"/>
        /// called with <paramref name="me"/> and true.
        /// </summary>
        /// <param name="me"></param>
        public static void DeleteRecursive(this DirectoryInfo me)
        {
            Contract.ArgumentNotNull(me, nameof(me));
            me.Delete(true);
        }
    }
}