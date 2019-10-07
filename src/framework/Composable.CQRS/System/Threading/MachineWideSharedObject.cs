using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Composable.Contracts;
using Composable.Persistence;
using Composable.Serialization;

namespace Composable.System.Threading
{
    class MachineWideSharedObject
    {
        protected static readonly string DataFolder = ComposableTempFolder.EnsureFolderExists("MemoryMappedFiles");
    }

    class MachineWideSharedObject<TObject> : MachineWideSharedObject, IDisposable where TObject : BinarySerialized<TObject>
    {
        const int LengthIndicatorIntegerLengthInBytes = 4;
        readonly long _capacity;
        MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _synchronizer;
        bool _disposed;

        internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false, long capacity = 1000_000) => new MachineWideSharedObject<TObject>(name, usePersistentFile, capacity);

        MachineWideSharedObject(string name, bool usePersistentFile, long capacity)
        {
            _capacity = capacity;
            var name1 = $"Composable_{name}";
            var fileName = $"{nameof(MachineWideSharedObject<TObject>)}_{name1}";
            _synchronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            fileName = Path.Combine(DataFolder, fileName);

            _synchronizer.Execute(() =>
            {
                if(!usePersistentFile)
                {
                    _file = MemoryMappedFile.CreateOrOpen(name1, _capacity, MemoryMappedFileAccess.ReadWrite);
                } else
                {
                    try
                    {
                        _file = MemoryMappedFile.OpenExisting(name1, desiredAccessRights: MemoryMappedFileRights.ReadWrite, inheritability: HandleInheritability.None);
                    }
                    catch (FileNotFoundException)
                    {
                        _file = MemoryMappedFile.CreateFromFile(
                            fileName,
                            FileMode.OpenOrCreate,
                            name1,
                            _capacity,
                            MemoryMappedFileAccess.ReadWrite);
                    }
                }
            });
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
            if (objectLength != 0)
            {
                var buffer = new byte[objectLength];
                accessor.ReadArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);

                value = BinarySerialized<TObject>.Deserialize(buffer);
            }

            if (Equals(value, default(TObject)))
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
                    using var viewAccessor = _file.CreateViewAccessor();
                    action(viewAccessor);
                });
        }

        public void Dispose()
        {
            _file?.Dispose();
            _disposed = true;
        }
    }
}
