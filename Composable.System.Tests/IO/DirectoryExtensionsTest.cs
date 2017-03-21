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
        static readonly ILogger Log = Logger.For<DirectoryExtensionsTest>();

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

            Log.Info($"Deleting directory {directory}");
            directory.AsDirectory().DeleteRecursive();
            Log.Info($"Deleted directory {directory}");


            Assert.That(directory.AsDirectory().Exists, Is.False, "Directory should have been deleted");
        }

        [Test]
        public void SizeShouldCorrectlyCalculateSize()
        {
            var directory = CreateUsableFolderPath();
            var size = CreateDirectoryHierarchy(directory, 3);

            Assert.That(directory.AsDirectory().Size(), Is.EqualTo(size));

            Log.Info($"Deleting directory {directory}");
            directory.AsDirectory().Delete(true);
            Log.Info($"Deleted directory {directory}");
        }

        static string CreateUsableFolderPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        static int CreateDirectoryHierarchy(string directoryPath, int depth)
        {
            if(depth <= 0)
            {
                return 0;
            }

            directoryPath.AsDirectory().Create();
            Log.Info($"created directory {directoryPath}");
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
                        Log.Info($"created file {file}");
                    });

            size += directoryPath.Repeat(2)
                .Select(dir => Path.Combine(dir, Guid.NewGuid().ToString()))
                .Sum(subdir => CreateDirectoryHierarchy(subdir, depth - 1));

            return size;
        }
    }
}