using System;
using System.IO;
using System.Linq;
using Composable.Logging;
using Composable.System.IO;
using Composable.System.Linq;
using NUnit.Framework;

namespace Composable.Tests.IO
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
        public void DeleteRecursiveShouldRemoveDirectoryHierarchy()
        {
            var directory = CreateUsableFolderPath();
            CreateDirectoryHierarchy(directory, 2);

            Assert.That(directory.AsDirectory().Exists, "There should be a directory at first");
            Assert.That(directory.AsDirectory().GetDirectories().Any(), Is.True, "There should be subdirectories");

            SafeConsole.WriteLine("Deleting directory {0}", directory);
            directory.AsDirectory().DeleteRecursive();
            SafeConsole.WriteLine("Deleted directory {0}", directory);


            Assert.That(directory.AsDirectory().Exists, Is.False, "Directory should have been deleted");
        }

        [Test]
        public void SizeShouldCorrectlyCalculateSize()
        {
            var directory = CreateUsableFolderPath();
            var size = CreateDirectoryHierarchy(directory, 3);

            Assert.That(directory.AsDirectory().Size(), Is.EqualTo(size));

            SafeConsole.WriteLine("Deleting directory {0}", directory);
            directory.AsDirectory().Delete(true);
            SafeConsole.WriteLine("Deleted directory {0}", directory);
        }

        static string CreateUsableFolderPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        static int CreateDirectoryHierarchy(string directoryPath, int depth)
        {
            if(depth <= 0)
            {
                return 0;
            }

            directoryPath.AsDirectory().Create();
            SafeConsole.WriteLine("created directory {0}", directoryPath);
            var fileContent = new Byte[100];
            var size = 0;
            directoryPath.Repeat(2).Select(dir => Path.Combine(dir, Guid.NewGuid().ToString())).ForEach(
                file =>
                    {
                        using(var stream = File.Create(file))
                        {
                            stream.Write(fileContent, 0, fileContent.Length);
                            size += fileContent.Length;
                        }
                        SafeConsole.WriteLine("created file {0}", file);
                    });

            size += directoryPath.Repeat(2)
                .Select(dir => Path.Combine(dir, Guid.NewGuid().ToString()))
                .Sum(subdir => CreateDirectoryHierarchy(subdir, depth - 1));

            return size;
        }
    }
}