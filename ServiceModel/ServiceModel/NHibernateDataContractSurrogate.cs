using System;
using NHibernate.Collection;
using NHibernate.Proxy;

namespace Void.ServiceModel
{
    public class NHibernateDataContractSurrogate : DataContractSurrogateAdapter
    {
        public NHibernateDataContractSurrogate(OperationExtensionAttribute options)
        {
            Config = options;
        }

        protected OperationExtensionAttribute Config { get; private set; }

        public override Type GetDataContractType(Type requestedType)
        {
            // Serialize proxies as the base type
            if (typeof (INHibernateProxy).IsAssignableFrom(requestedType))
            {
                requestedType = requestedType.GetType().BaseType;
            }

            // Serialize persistent collections as the collection interface type
            if (typeof (IPersistentCollection).IsAssignableFrom(requestedType))
            {
                foreach (Type collInterface in requestedType.GetInterfaces())
                {
                    if (collInterface.IsGenericType)
                    {
                        requestedType = collInterface;
                        break;
                    }

                    if (!collInterface.Equals(typeof (IPersistentCollection)))
                    {
                        requestedType = collInterface;
                    }
                }
            }

            return requestedType;
        }

        public override object GetObjectToSerialize(object obj, Type targetType)
        {
                // Serialize proxies as the base type
            if (obj is INHibernateProxy)
            {
                // Getting the implementation of the proxy forces an initialization of the proxied object (if not yet initialized)
                obj = ((INHibernateProxy) obj).HibernateLazyInitializer.GetImplementation(); //<!------- HERE 
            }

            // Serialize persistent collections as the collection interface type
            if (obj is IPersistentCollection)
            {
                IPersistentCollection persistentCollection = (IPersistentCollection) obj;
                if (!persistentCollection.WasInitialized)
                {
                    persistentCollection.ForceInitialization();
                }
                obj = persistentCollection.Entries(null); // This returns the "wrapped" collection               
            }
            return obj;
        }
    }
}