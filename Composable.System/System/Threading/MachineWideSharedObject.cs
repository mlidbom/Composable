using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Composable.Testing;

namespace Composable.System.Threading
{
    using Composable.System.Linq;

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
        readonly long _capacity;
        readonly MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _syncronizer;
        internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false, long capacity = 1000_000) => new MachineWideSharedObject<TObject>(name, usePersistentFile, capacity);

        MachineWideSharedObject(string name, bool usePersistentFile, long capacity)
        {
            _capacity = 1000_000;
            var fileName = $"{nameof(MachineWideSharedObject<TObject>)}_{name}";
            _syncronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

            if(usePersistentFile)
            {
                MemoryMappedFile mappedFile = null;
                _syncronizer.Execute(() =>
                                     {
                                         var actualFileName = fileName;
                                         Path.GetInvalidFileNameChars()
                                             .ForEach(invalidChar => actualFileName = actualFileName.Replace(invalidChar, '_'));

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
                                                                                      capacity: capacity,
                                                                                      access: MemoryMappedFileAccess.ReadWrite);
                                     });
                _file = mappedFile;
            } else
            {
                _file = MemoryMappedFile.CreateOrOpen(mapName: name, capacity: capacity);
            }

        }

        internal TObject GetCopy()
        {
            TObject value = default(TObject);
            _syncronizer.Execute(() =>
                                 {
                                     using(var accessor = _file.CreateViewAccessor())
                                     {

                                         var objectLength = accessor.ReadInt32(0);
                                         if(objectLength != 0)
                                         {
                                             byte[] buffer = new byte[objectLength];
                                             accessor.ReadArray(4, buffer, 0, buffer.Length);

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
            _syncronizer.Execute(() =>
                                 {
                                     instance = GetCopy();
                                     action(instance);
                                     Set(instance);
                                 });
            return instance;
        }

        void Set(TObject value)
        {
            _syncronizer.Execute(() =>
                                 {
                                     using (var memoryStream = new MemoryStream())
                                     using(var writer = new BinaryWriter(memoryStream))
                                     {
                                         value.Serialize(writer);
                                         var buffer = memoryStream.ToArray();

                                         using (var accessor = _file.CreateViewAccessor())
                                         {
                                             accessor.Write(0, buffer.Length); //First bytes are an int that tells how far to read when deserializing.
                                             accessor.WriteArray(4, buffer, 0, buffer.Length);
                                         }
                                     }
                                 });
        }
    }
}
