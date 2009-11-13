namespace ServiceModel
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    namespace Tradera.SellerTool.Integration.InventoryManagement
    {
        public class WcfSerializer
        {
            public static DataContractSerializer CreateSerializer<T>(IDataContractSurrogate surrogate)
            {
                return new DataContractSerializer(typeof(T),
                                                   null /*knownTypes*/,
                                                   int.MaxValue /*maxItemsInObjectGraph*/,
                                                   false /*ignoreExtensionDataObject*/,
                                                   false /*preserveObjectReferences*/,
                                                   surrogate /*dataContractSurrogate*/);
            }

            private static DataContractSerializer CreateSerializer<T>()
            {
                return new DataContractSerializer(typeof(T));
            }

            public static T Serialize<T>(T instance)
            {
                return Serialize<T>(instance, new DataContractSerializer(typeof(T)));
            }

            public static T Serialize<T>(T instance, IDataContractSurrogate surrogate)
            {
                return Serialize<T>(instance, CreateSerializer<T>(surrogate));
            }

            public static T Serialize<T>(T instance, DataContractSerializer serializer)
            {
                using (MemoryStream writer = new MemoryStream())
                {
                    serializer.WriteObject(writer, instance);
                    writer.Seek(0, 0);
                    return (T)serializer.ReadObject(writer);
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
                        return (T)CreateSerializer<T>().ReadObject(data);
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
}