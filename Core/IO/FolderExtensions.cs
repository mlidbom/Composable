using System.IO;
using Void.Hierarchies;
using System.Linq;

namespace Void.IO
{
    public static class FolderExtensions
    {
        public static DirectoryInfo AsDirectory(this string path)
        {
            return new DirectoryInfo(path);
        }

        public static long Size(this DirectoryInfo me)
        {
            return me.FullName
                .AsHierarchy(Directory.GetDirectories).Flatten()
                .SelectMany(dir => Directory.GetFiles(dir))
                .Sum(file => new FileInfo(file).Length);
        }
    }
}