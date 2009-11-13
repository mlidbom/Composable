using System;
using System.IO;
using System.Runtime.Serialization;

namespace Void.ServiceModel
{
    public class WCFSerializer
    {
        public static DataContractSerializer CreateSerializer<T>()
        {
            return CreateSerializer<T>(new OperationExtensionAttribute());
        }

        public static DataContractSerializer CreateSerializer<T>(OperationExtensionAttribute config)
        {
            return new DataContractSerializer(typeof (T),
                                              null /*knownTypes*/,
                                              int.MaxValue /*maxItemsInObjectGraph*/,
                                              false /*ignoreExtensionDataObject*/,
                                              false /*preserveObjectReferences*/,
                                              config.CreateClientSurrogate() /*dataContractSurrogate*/);
        }

        public static T Serialize<T>(T instance)
        {
            return Serialize(instance, CreateSerializer<T>());
        }

        public static T Serialize<T>(T instance, OperationExtensionAttribute config)
        {
            return Serialize(instance, CreateSerializer<T>(config));
        }

        public static T Serialize<T>(T instance, DataContractSerializer serializer)
        {
            using (MemoryStream writer = new MemoryStream())
            {
                serializer.WriteObject(writer, instance);
                writer.Seek(0, 0);
                return (T) serializer.ReadObject(writer);
            }
        }

        public static T Read<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return default(T);
            }
            using (var data = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    return (T) CreateSerializer<T>().ReadObject(data);
                }
                catch (Exception)
                {
                    return default(T);
                }
            }
        }

        public static void Write<T>(T instance, string filePath)
        {
            using (var data = new FileStream(filePath, FileMode.Create))
            {
                var serializer = CreateSerializer<T>();
                serializer.WriteObject(data, instance);
            }
        }
    }
}