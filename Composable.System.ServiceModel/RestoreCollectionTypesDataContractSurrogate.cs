#region usings

using System;
using System.Collections.Generic;

#endregion

namespace Composable.System.ServiceModel
{
    public class RestoreCollectionTypesDataContractSurrogate : DataContractSurrogateAdapter
    {
        public override Type GetDataContractType(Type requestedType)
        {
            //Detect and serialize IList<T> as List<T>
            if(requestedType.IsGenericType && requestedType.IsInterface)
            {
                var genericArguments = requestedType.GetGenericArguments();
                if(genericArguments.Length == 1)
                {
                    var genericList = typeof(List<>);
                    var specificType = genericList.MakeGenericType(new[] { genericArguments[0] });
                    if(requestedType.IsAssignableFrom(specificType))
                    {
                        return specificType;
                    }
                }
            }
            return requestedType;
        }
    }
}