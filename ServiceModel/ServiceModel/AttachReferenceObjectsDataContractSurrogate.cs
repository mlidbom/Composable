using System;
using System.Linq;

namespace Void.ServiceModel
{
    public class AttachReferenceObjectsDataContractSurrogate : DataContractSurrogateAdapter
    {
        public AttachReferenceObjectsDataContractSurrogate(OperationExtensionAttribute serializationOptions)
        {
            Config = serializationOptions;
        }

        private OperationExtensionAttribute Config { get; set; }

        public static Func<IReferenceObject, IReferenceObject> RegisterInstanceDelegate { get; set; }
        public static Func<IReferenceObject, IReferenceObject> FetchInstanceDelegate { get; set; }


        public override object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj is IReferenceObject)
            {
                if (Config.ReferenceTypesToRegister != null && Config.ReferenceTypesToRegister.Contains(targetType))
                {
                    return RegisterInstanceDelegate((IReferenceObject) obj);
                }

                if (Config.Options.HasFlag(Options.AttachReferenceObjects))
                {
                    return FetchInstanceDelegate((IReferenceObject) obj);
                }
            }
            return obj;
        }
    }
}