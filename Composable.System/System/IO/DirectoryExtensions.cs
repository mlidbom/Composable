#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
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
            Contract.Requires(!string.IsNullOrWhiteSpace(path));
            Contract.Ensures(Contract.Result<DirectoryInfo>() != null);
            return new DirectoryInfo(path);
        }

        /// <summary>
        /// Returns a DirectoryInfo instance that points at me
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static DirectoryInfo AsDirectory(this Environment.SpecialFolder me)
        {
            Contract.Ensures(Contract.Result<DirectoryInfo>() != null);
            return Environment.GetFolderPath(me).AsDirectory();
        }


        /// <summary>
        /// Returns the size of the directory.
        /// </summary>
        public static long Size(this DirectoryInfo me)
        {
            Contract.Requires(me != null && me.FullName != null);
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
            Contract.Requires(me != null);
            me.Delete(true);
        }


        /// <summary>
        /// Returns a DirectoryInfo that pointing at a directory that is found by 
        /// following <paramref name="relativePath"/> from <paramref name="me"/>
        /// This folder may or may not exist.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static DirectoryInfo SubDir(this DirectoryInfo me, string relativePath)
        {
            Contract.Requires(me != null && !string.IsNullOrEmpty(me.FullName) && !string.IsNullOrEmpty(relativePath));
            Contract.Ensures(Contract.Result<DirectoryInfo>() != null);
            if(relativePath.First() == '\\')
            {
                relativePath = relativePath.Remove(0, 1);
            }
            return Path.Combine(me.FullName, relativePath).AsDirectory();
        }

        /// <summary>
        /// Returns a DirectoryInfo that pointing at a directory that is found by 
        /// following <paramref name="filePath"/> from <paramref name="me"/>.
        /// This file may or may not exist.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static FileInfo File(this DirectoryInfo me, string filePath)
        {
            Contract.Requires(me != null && !string.IsNullOrEmpty(filePath));
            Contract.Ensures(Contract.Result<FileInfo>() != null);
            if(filePath.First() == '\\')
            {
                filePath = filePath.Remove(0, 1);
            }
            return new FileInfo(Path.Combine(me.FullName, filePath));
        }

        /// <summary>
        /// Returns all the files in the directory tree below <paramref name="directory"/>
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> GetFilesResursive(this DirectoryInfo directory)
        {
            Contract.Requires(directory != null);
            Contract.Ensures(Contract.Result<IEnumerable<FileInfo>>() != null);
            //Contract.Ensures(Contract.Result<IEnumerable<FileInfo>>().None(file => file==null));
            return directory
                .DirectoriesRecursive()
                .SelectMany(subdir => subdir.GetFiles());
        }


        /// <summary>
        /// /// Returns all the directories in the directory tree below <paramref name="directory"/>
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static IEnumerable<DirectoryInfo> DirectoriesRecursive(this DirectoryInfo directory)
        {
            Contract.Requires(directory != null);
            Contract.Ensures(Contract.Result<IEnumerable<DirectoryInfo>>() != null);
            //Contract.Ensures(Contract.Result<IEnumerable<DirectoryInfo>>().None(dir => dir == null));
            return directory.AsHierarchy(dir => dir.GetDirectories()).Flatten().Unwrap();
        }
    }
}