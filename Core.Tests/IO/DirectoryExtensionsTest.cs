using System;
using System.IO;
using NUnit.Framework;
using Void.IO;
using Void.Linq;
using System.Linq;

namespace Core.Tests.IO
{
    [TestFixture]
    public class DirectoryExtensionsTest
    {
        [Test]
        public void AsDirectoryShouldReturnDirectoryInfoWithFullNameBeingTheOriginalString()
        {
            var dir = @"C:\";
            Assert.That(dir.AsDirectory().FullName, Is.EqualTo(dir));
        }

        [Test]
        public void SizeShouldCorrectlyCalculateSize()
        {

            string directory = CreateUsableFolderPath();
            var size = CreateDirectoryHierarchy(directory, 3);

            Assert.That(directory.AsDirectory().Size(), Is.EqualTo(size));
            directory.AsDirectory().Delete(true);
        }

        [Test]
        public void DeleteRecursiveShouldRemoveDirectoryHierarchy()
        {
            var directory = CreateUsableFolderPath();
            CreateDirectoryHierarchy(directory, 2);

            Assert.That(directory.AsDirectory().Exists, "There should be a directory at first");
            Assert.That(directory.AsDirectory().GetDirectories().Any(), Is.True, "There should be subdirectories");
            directory.AsDirectory().DeleteRecursive();
            Assert.That(directory.AsDirectory().Exists,Is.False, "Directory should have been deleted");
        }

        #region Helpers

        private static string CreateUsableFolderPath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        private static int CreateDirectoryHierarchy(string dirName, int depth)
        {
            if(depth <= 0)
            {
                return 0;
            }

            dirName.AsDirectory().Create();
            var fileContent = new Byte[100];
            var size = 0;
            dirName.Repeat(2).Select( dir => Path.Combine(dir, Guid.NewGuid().ToString())).ForEach(
                file =>
                {
                    using (var stream = File.Create(file))
                    {
                        stream.Write(fileContent, 0, fileContent.Length);
                        size += fileContent.Length;
                    }
                });

            size += dirName.Repeat(2)
                .Select(dir => Path.Combine(dir, Guid.NewGuid().ToString()))
                .Sum(subdir => CreateDirectoryHierarchy(subdir, depth - 1));

            return size;
        }

        #endregion

    }
}