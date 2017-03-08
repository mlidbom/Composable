#region usings

using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Hierarchies;

#endregion

namespace Composable.System.IO
{
    /// <summary/>
    [Pure]
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
            ContractTemp.Argument(() => path).NotNullEmptyOrWhiteSpace();
            return new DirectoryInfo(path);
        }

        /// <summary>
        /// Returns the size of the directory.
        /// </summary>
        public static long Size(this DirectoryInfo me)
        {
            ContractTemp.Argument(() => me).NotNull();
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
            ContractTemp.Argument(() => me).NotNull();
            me.Delete(true);
        }
    }
}