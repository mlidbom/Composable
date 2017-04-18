using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Composable.Testing;

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

    class MachineWideSharedObject<TObject> : MachineWideSharedObject where TObject : IBinarySerializeMySelf, new()
    {
        const int LengthIndicatorIntegerLengthInBytes = 4;
        readonly long _capacity;
        readonly MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _syncronizer;
        internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false, long capacity = 1000_000) => new MachineWideSharedObject<TObject>(name, usePersistentFile, capacity);

        MachineWideSharedObject(string name, bool usePersistentFile, long capacity)
        {
            _capacity = capacity;
            var fileName = $"{nameof(MachineWideSharedObject<TObject>)}_{name}";
            _syncronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

            if(usePersistentFile)
            {
                MemoryMappedFile mappedFile = null;
                _syncronizer.Execute(() =>
                                     {
                                         var actualFileName = fileName;
                                         foreach(var invalidChar in Path.GetInvalidFileNameChars())
                                         {
                                             actualFileName = actualFileName.Replace(invalidChar, '_');
                                         }

                                         actualFileName = Path.Combine(DataFolder, actualFileName);

                                         if(File.Exists(actualFileName))
                                         {
                                             try
                                             {
                                                 mappedFile = MemoryMappedFile.OpenExisting(mapName: name);
                                                 return;
                                             }
                                             catch(IOException)
                                             {
                                             }
                                         }

                                         mappedFile = MemoryMappedFile.CreateFromFile(path: actualFileName,
                                                                                      mode: FileMode.OpenOrCreate,
                                                                                      mapName: name,
                                                                                      capacity: _capacity,
                                                                                      access: MemoryMappedFileAccess.ReadWrite);
                                     });
                _file = mappedFile;
            } else
            {
                _file = MemoryMappedFile.CreateOrOpen(mapName: name, capacity: _capacity);
            }

        }

        internal void Synchronized(Action action) { _syncronizer.Execute(action); }

        internal TObject GetCopy()
        {
            TObject value = default(TObject);
            Synchronized(() =>
                                 {
                                     using(var accessor = _file.CreateViewAccessor())
                                     {

                                         var objectLength = accessor.ReadInt32(0);
                                         if(objectLength != 0)
                                         {
                                             byte[] buffer = new byte[objectLength];
                                             accessor.ReadArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);

                                             using (var objectStream = new MemoryStream(buffer))
                                             using(var reader = new BinaryReader(objectStream))
                                             {
                                                 value = new TObject();
                                                 value.Deserialize(reader);
                                             }
                                         }
                                     }
                                     if(Equals(value, default(TObject)))
                                     {
                                         Set(value = new TObject());
                                     }
                                 });
            return value;
        }

        internal TObject Update(Action<TObject> action)
        {
            TObject instance = default(TObject);
            Synchronized(() =>
                                 {
                                     instance = GetCopy();
                                     action(instance);
                                     Set(instance);
                                 });
            return instance;
        }

        void Set(TObject value)
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

                using (var accessor = _file.CreateViewAccessor())
                {
                    accessor.Write(0, buffer.Length); //First bytes are an int that tells how far to read when deserializing.
                    accessor.WriteArray(LengthIndicatorIntegerLengthInBytes, buffer, 0, buffer.Length);
                }
            }
        }
    }
}
