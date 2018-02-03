using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Composable.Serialization;

namespace Composable.System.Threading
{
    class MachineWideSharedObject
    {
        protected static readonly string DataFolder;
        static MachineWideSharedObject()
        {
            var tempDirectory = Environment.GetEnvironmentVariable("COMPOSABLE_TEMP_DRIVE");
            if (tempDirectory.IsNullOrWhiteSpace())
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), "Composable_TEMP");
            }

            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            DataFolder = Path.Combine(tempDirectory, "MemoryMappedFiles");
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
        }
    }

    class MachineWideSharedObject<TObject> : MachineWideSharedObject, IDisposable where TObject : IBinarySerializeMySelf, new()
    {
        const int LengthIndicatorIntegerLengthInBytes = 4;
        readonly long _capacity;
        readonly MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _synchronizer;

        readonly string _fileName;

        readonly string _name;

        readonly bool _usePersistentFile;

        internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false, long capacity = 1000_000) => new MachineWideSharedObject<TObject>(name, usePersistentFile, capacity);

        MachineWideSharedObject(string name, bool usePersistentFile, long capacity)
        {
            _capacity = capacity;
            _name = $"Composable_{name}";
            _usePersistentFile = usePersistentFile;
            _fileName = $"{nameof(MachineWideSharedObject<TObject>)}_{_name}";
            _synchronizer = MachineWideSingleThreaded.For($"{_fileName}_mutex");

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                _fileName = _fileName.Replace(invalidChar, '_');

            _fileName = Path.Combine(DataFolder, _fileName);

            if (!usePersistentFile)
            {
                _file = MemoryMappedFile.CreateNew(_name, _capacity, MemoryMappedFileAccess.ReadWrite);
            }
        }

        internal TObject Update(Action<TObject> action)
        {
            var instance = default(TObject);
            UseViewAccessor(accessor =>
                {
                    instance = GetCopy(accessor);
                    action(instance);
                    Set(instance, accessor);
                });
            return instance;
        }

        void Set(TObject value, MemoryMappedViewAccessor accessor)
        {
            using (var memoryStream = new MemoryStream())
            using(var writer = new BinaryWriter(memoryStream))
            {
                value.Serialize(writer);
                var buffer = memoryStream.ToArray();

                var requiredCapacity = buffer.Length + LengthIndicatorIntegerLengthInBytes;
                if(requiredCapacity >= _capacity)
                {
                    throw new Exception($"Deserialized object exceeds storage capacity of:{_capacity} bytes with size: {requiredCapacity} bytes.");
                }

                accessor.Write(0, buffer.Length); //First bytes are an int that tells how far to read when deserializing.
                accessor.WriteArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);
            }
        }

        internal TObject GetCopy()
        {
            var instance = default(TObject);
            UseViewAccessor(accessor => { instance = GetCopy(accessor); });
            return instance;
        }

        internal TObject GetCopy(MemoryMappedViewAccessor accessor)
        {
            var value = default(TObject);

            var objectLength = accessor.ReadInt32(0);
            if (objectLength != 0)
            {
                var buffer = new byte[objectLength];
                accessor.ReadArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);

                using (var objectStream = new MemoryStream(buffer))
                using (var reader = new BinaryReader(objectStream))
                {
                    value = new TObject();
                    value.Deserialize(reader);
                }
            }

            if (Equals(value, default(TObject)))
            {
                Set(value = new TObject(), accessor);
            }

            return value;
        }

        void UseViewAccessor(Action<MemoryMappedViewAccessor> action)
        {
            _synchronizer.Execute(
                () =>
                    {
                        MemoryMappedFile mappedFile;

                        if (_usePersistentFile)
                        {
                            try
                            {
                                mappedFile = MemoryMappedFile.OpenExisting(_name, desiredAccessRights: MemoryMappedFileRights.FullControl, inheritability: HandleInheritability.None);
                            }
                            catch (FileNotFoundException)
                            {
                                mappedFile = MemoryMappedFile.CreateFromFile(
                                    _fileName,
                                    FileMode.OpenOrCreate,
                                    _name,
                                    _capacity,
                                    MemoryMappedFileAccess.ReadWrite);

                            }
                        }
                        else
                        {
                            mappedFile = _file;
                        }

                        try
                        {
                            using (var viewAccessor = mappedFile.CreateViewAccessor())
                            {
                                action(viewAccessor);
                            }
                        }
                        finally
                        {
                            if (_usePersistentFile)
                            {
                                mappedFile.Dispose();
                            }
                        }
                    });
        }

        public void Dispose()
        {
            _file?.Dispose();
        }
    }
}
