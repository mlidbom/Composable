using System.IO;
using Void.Hierarchies;
using System.Linq;

namespace Void.IO
{
    public static class DirectoryExtensions
    {
        /// <summary>
        /// Called on <paramref name="path"/> return a DirectoryInfo instance 
        /// pointed at that path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DirectoryInfo AsDirectory(this string path)
        {
            return new DirectoryInfo(path);
        }


        /// <summary>
        /// Returns the size of the directory.
        /// </summary>
        public static long Size(this DirectoryInfo me)
        {
            return me.FullName
                .AsHierarchy(Directory.GetDirectories).Flatten()
                .SelectMany(dir => Directory.GetFiles(dir))
                .Sum(file => new FileInfo(file).Length);
        }

        public static void DeleteRecursive(this DirectoryInfo me)
        {
            me.Delete(true);
        }
    }
}