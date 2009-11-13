using System;
using System.Collections.Generic;

namespace Void.ServiceModel
{
    public class RestoreCollectionTypesDataContractSurrogate : DataContractSurrogateAdapter
    {
        public override Type GetDataContractType(Type requestedType)
        {
            //Detect and serialize IList<T> as List<T>
            if (requestedType.IsGenericType && requestedType.IsInterface)
            {
                var genericArguments = requestedType.GetGenericArguments();
                if (genericArguments.Length == 1)
                {
                    Type genericList = typeof (List<>);
                    Type specificType = genericList.MakeGenericType(new[] {genericArguments[0]});
                    if (requestedType.IsAssignableFrom(specificType))
                    {
                        return specificType;
                    }
                }
            }
            return requestedType;
        }
    }
}