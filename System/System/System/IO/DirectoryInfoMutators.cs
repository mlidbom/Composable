using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using Composable.System.Linq;

namespace Composable.System.IO
{
    /// <summary>
    /// Extensions that actually change your filesystem in some way.
    /// </summary>
    public static class DirectoryInfoMutators
    {
        /// <summary>
        /// Writes one folder into a target folder. 
        /// If the target folder does not exist it is created. 
        /// Then the contents of the source folder are written into the target folder.
        /// Existing files are overwritten.
        /// Existing directories are Written Into according to the above rules.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void WriteInto(this DirectoryInfo source, DirectoryInfo target)
        {
            Contract.Requires(source != null && target != null);
            var targetPath = target.FullName;

            //Ensure that existing target files can be overwritten..
            source.GetFilesResursive()
                .Select(file => file.FullName)
                .Select(path => path.Replace(source.FullName, targetPath))
                .Select(filePath => new FileInfo(filePath))
                .Where(file => file.Exists)
                .ForEach(file => file.IsReadOnly = false);

            FileSystem.CopyDirectory(source.FullName, targetPath, true);
        }

        /// <summary>
        /// Copies source to target. 
        /// Fails if target exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void CopyTo(this DirectoryInfo source, DirectoryInfo target)
        {
            Contract.Requires(source != null && target != null);
            FileSystem.CopyDirectory(source.FullName, target.FullName);
        }

        /// <summary>
        /// Recursively makes files and folders within <paramref name="me"/> writable.
        /// </summary>
        /// <param name="me"></param>
        public static void MakeWritable(this DirectoryInfo me)
        {
            Contract.Requires(me != null);
            me.GetFilesResursive().ForEach(file => file.IsReadOnly = false);
        }
    }
}