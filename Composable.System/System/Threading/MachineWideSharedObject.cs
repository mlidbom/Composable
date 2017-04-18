using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using Composable.Contracts;

namespace Composable.System.Threading
{
    class MachineWideSharedObject<TObject>
        where TObject : new()
    {
        readonly long _capacity;
        readonly MemoryMappedFile _file;
        readonly MachineWideSingleThreaded _syncronizer;

        internal static MachineWideSharedObject<TObject> For(string name) => new MachineWideSharedObject<TObject>(name, 100000);

        MachineWideSharedObject(string name, long capacity)
        {
            _capacity = capacity;
            Contract.Assert.That(typeof(TObject).IsSerializable, "Shared type must be serializeble");
            var fileName = $"{nameof(MachineWideSharedObject<TObject>)}_{name}";
            _syncronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");
            _file = MemoryMappedFile.CreateOrOpen(fileName, capacity);
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
                                                 value = (TObject)new BinaryFormatter().Deserialize(objectStream);
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
                                     var binaryFormatter = new BinaryFormatter();
                                     using(var memoryStream = new MemoryStream())
                                     {
                                         binaryFormatter.Serialize(memoryStream, value);
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
