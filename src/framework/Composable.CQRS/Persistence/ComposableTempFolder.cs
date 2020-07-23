using System;
using System.IO;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence
{
    using SPath = Path;

    ///<summary>Manages the Temp folder in a machine wide thread safe manner.</summary>
    static class ComposableTempFolder
    {
        static readonly MachineWideSingleThreaded WithMachineWideLock = MachineWideSingleThreaded.For(nameof(ComposableTempFolder));
        static readonly string DefaultPath = SPath.Combine(SPath.GetTempPath(), "Composable_TEMP");
        static readonly string Path = EnsureFolderExists();
        internal static readonly bool IsOverridden = Path != DefaultPath;

        internal static string EnsureFolderExists(string folderName) => WithMachineWideLock.Execute(() =>
        {
            var folder = SPath.Combine(Path, folderName);
            if(!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        });

        static string EnsureFolderExists()
        {
            return WithMachineWideLock.Execute(() =>
            {
                var path = Environment.GetEnvironmentVariable("COMPOSABLE_TEMP_DRIVE");
                if(path.IsNullEmptyOrWhiteSpace())
                {
                    path = DefaultPath;
                }

                if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            });
        }
    }
}
