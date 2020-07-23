using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Composable.Contracts;
using Composable.Persistence;
using Composable.Serialization;
using Composable.SystemCE.CollectionsCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE.ThreadingCE
{
    class MachineWideSharedObject
    {
        protected static readonly string DataFolder = ComposableTempFolder.EnsureFolderExists("MemoryMappedFiles");
    }

    class MachineWideSharedObject<TObject> : MachineWideSharedObject, IDisposable where TObject : BinarySerialized<TObject>
    {
        const int LengthIndicatorIntegerLengthInBytes = 4;
        readonly bool _usePersistentFile;
        readonly long _capacity;
        readonly MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _synchronizer;
        bool _disposed;

        // ReSharper disable once StaticMemberInGenericType
        static readonly OptimizedThreadShared<Dictionary<string, MemoryMappedFile>> Cache = new OptimizedThreadShared<Dictionary<string, MemoryMappedFile>>(new Dictionary<string, MemoryMappedFile>());

        internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false, long capacity = 1000_000) => new MachineWideSharedObject<TObject>(name, usePersistentFile, capacity);

        MachineWideSharedObject(string name, bool usePersistentFile, long capacity)
        {
            _usePersistentFile = usePersistentFile;
            _capacity = capacity;
            var name1 = $"Composable_{name}";
            var fileName = $"{nameof(MachineWideSharedObject<TObject>)}_{name1}";
            _synchronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

            if(usePersistentFile)
            {
                _file = Cache.WithExclusiveAccess(
                    cache => cache.GetOrAdd(name,
                                            () => _synchronizer.Execute(() =>
                                            {
                                                foreach(var invalidChar in Path.GetInvalidFileNameChars())
                                                    fileName = fileName.Replace(invalidChar, '_');

                                                fileName = Path.Combine(DataFolder, fileName);

                                                try
                                                {
                                                    return MemoryMappedFile.OpenExisting(name1, desiredAccessRights: MemoryMappedFileRights.ReadWrite, inheritability: HandleInheritability.Inheritable);
                                                }
                                                catch(FileNotFoundException)
                                                {
                                                    var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                                                    return MemoryMappedFile.CreateFromFile(fileStream: fileStream, mapName: name1, capacity: capacity, access: MemoryMappedFileAccess.ReadWrite, inheritability: HandleInheritability.None, leaveOpen: false);
                                                }
                                            })));
            } else
            {
                _file = MemoryMappedFile.CreateOrOpen(name1, _capacity, MemoryMappedFileAccess.ReadWrite);
            }
        }

        internal TObject Update(Action<TObject> action)
        {
            Contract.Assert.That(!_disposed, "Attempt to use disposed object.");
            var instance = default(TObject);
            UseViewAccessor(accessor =>
            {
                instance = GetCopy(accessor);
                action(instance);
                Set(instance, accessor);
            });
            return Assert.Result.NotNull(instance);
        }

        void Set(TObject value, MemoryMappedViewAccessor accessor)
        {
            var buffer = value.Serialize();

            var requiredCapacity = buffer.Length + LengthIndicatorIntegerLengthInBytes;
            if(requiredCapacity >= _capacity)
            {
                throw new Exception($"Deserialized object exceeds storage capacity of:{_capacity} bytes with size: {requiredCapacity} bytes.");
            }

            accessor.Write(0, buffer.Length); //First bytes are an int that tells how far to read when deserializing.
            accessor.WriteArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);
        }

        internal TObject GetCopy()
        {
            var instance = default(TObject);
            UseViewAccessor(accessor => instance = GetCopy(accessor));
            return Assert.Result.NotNull(instance);
        }

        TObject GetCopy(MemoryMappedViewAccessor accessor)
        {
            Contract.Assert.That(!_disposed, "Attempt to use disposed object.");
            var value = default(TObject);

            var objectLength = accessor.ReadInt32(0);
            if(objectLength != 0)
            {
                var buffer = new byte[objectLength];
                accessor.ReadArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);

                value = BinarySerialized<TObject>.Deserialize(buffer);
            }

            if(Equals(value, default(TObject)))
            {
                Set(value = BinarySerialized<TObject>.DefaultConstructor(), accessor);
            }

            return value;
        }

        void UseViewAccessor(Action<MemoryMappedViewAccessor> action)
        {
            _synchronizer.Execute(
                () =>
                {
                    using var viewAccessor = _file!.CreateViewAccessor();
                    action(viewAccessor);
                });
        }

        public void Dispose()
        {
            if(!_disposed && !_usePersistentFile)
            {
                _file.Dispose();
            }

            _disposed = true;
        }
    }
}
