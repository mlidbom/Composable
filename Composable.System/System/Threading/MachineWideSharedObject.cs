using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Composable.Contracts;

namespace Composable.System.Threading
{
    using Composable.System.Linq;

    class MachineWideSharedObject<TObject>
        where TObject : new()
    {
        readonly long _capacity;
        readonly MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _syncronizer;
        static readonly string DataFolder;
        static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false, long capacity = 1000_000) => new MachineWideSharedObject<TObject>(name, usePersistentFile, capacity);

        static MachineWideSharedObject()
        {
            var tempDirectory = Environment.GetEnvironmentVariable("COMPOSABLE_TEMP_DRIVE");
            if(tempDirectory.IsNullOrWhiteSpace())
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

        MachineWideSharedObject(string name, bool usePersistentFile, long capacity)
        {
            _capacity = 1000_000;
            Contract.Assert.That(typeof(TObject).IsSerializable, "Shared type must be serializeble");
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
                                             catch(IOException) {}
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

                                             using(var objectStream = new MemoryStream(buffer))
                                             {
                                                 value = (TObject)BinaryFormatter.Deserialize(objectStream);
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

        internal void Set(TObject value)
        {
            _syncronizer.Execute(() =>
                                 {
                                     using(var memoryStream = new MemoryStream())
                                     {
                                         BinaryFormatter.Serialize(memoryStream, value);
                                         var buffer = memoryStream.ToArray();

                                         using(var accessor = _file.CreateViewAccessor())
                                         {
                                             accessor.Write(0, buffer.Length); //First bytes are an int that tells how far to read when deserializing.
                                             accessor.WriteArray(4, buffer, 0, buffer.Length);
                                         }
                                     }
                                 });
        }
    }
}
