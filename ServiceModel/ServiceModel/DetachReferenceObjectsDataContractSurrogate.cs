using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Void.ServiceModel
{
    public class DetachReferenceObjectsDataContractSurrogate : DataContractSurrogateAdapter
    {
        private OperationExtensionAttribute Config { get; set; }

        public DetachReferenceObjectsDataContractSurrogate(OperationExtensionAttribute options)
        {
            Config = options;
        }

        public override object GetObjectToSerialize(object obj, Type targetType)
        {
            //Use instances containing only IDs of the reference types....
            if (Config.Options.HasFlag(Options.DetachReferenceObjects) && obj is IReferenceObject)
            {
                if (Config.ReferenceTypesToRegister == null || !Config.ReferenceTypesToRegister.Contains(targetType))
                {
                    object result = FormatterServices.GetUninitializedObject(targetType);
                    ((IReferenceObject) result).Id = ((IReferenceObject) obj).Id;
                    return result;
                }
            }
            return obj;
        }
    }
}