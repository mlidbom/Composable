using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Xml;

namespace Void.ServiceModel
{
    internal class SerializationOperationBehavior : DataContractSerializerOperationBehavior
    {
        protected SerializationOperationBehavior(OperationDescription operation, OperationExtensionAttribute serializationOptions)
            : base(operation)
        {
            Config = serializationOptions;
        }

        protected OperationExtensionAttribute Config { get; private set; }

        public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
        {
            return CreateDataContractSerializer(type, name, ns, knownTypes);
        }

        private static XmlObjectSerializer CreateDataContractSerializer(Type type, string name, string ns, IList<Type> knownTypes)
        {
            return CreateDataContractSerializer(type, name, ns, knownTypes);
        }



        public IDataContractSurrogate CreateServerSurrogate()
        {

                return DataContractSurrogateLink.Chain(
                    new IDataContractSurrogate[]
                    {
                        new RestoreCollectionTypesDataContractSurrogate(),
                        new DetachReferenceObjectsDataContractSurrogate(Config),
                        new NHibernateDataContractSurrogate(Config)                                                                  
                    });
        }

        //todo:use serversurrogate
        public IDataContractSurrogate CreateClientSurrogate()
        {
            return DataContractSurrogateLink.Chain(
                new IDataContractSurrogate[]
                {
                    new RestoreCollectionTypesDataContractSurrogate(),
                    new DetachReferenceObjectsDataContractSurrogate(Config),
                    new NHibernateDataContractSurrogate(Config),
                    new AttachReferenceObjectsDataContractSurrogate(Config)
                });
        }
    }

    internal class ServerSerializationOperationBehavior : SerializationOperationBehavior
    {
        public ServerSerializationOperationBehavior(OperationDescription operationDescription, OperationExtensionAttribute serializationOptions)
            : base(operationDescription, serializationOptions)
        {
        }

        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
        {
            var surrogate = CreateServerSurrogate();
            return new DataContractSerializer(type, name, ns, knownTypes,
                                              int.MaxValue /*maxItemsInObjectGraph*/,
                                              false /*ignoreExtensionDataObject*/,
                                              false /*preserveObjectReferences*/,
                                              surrogate /*dataContractSurrogate*/);
        }
    }

    internal class ClientSerializationOperationBehavior : SerializationOperationBehavior
    {
        public ClientSerializationOperationBehavior(OperationDescription operationDescription, OperationExtensionAttribute serializationOptions)
            : base(operationDescription, serializationOptions)
        {
        }

     

        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
        {
            var surrogate = CreateClientSurrogate();
            return new DataContractSerializer(type, name, ns, knownTypes,
                                              int.MaxValue /*maxItemsInObjectGraph*/,
                                              false /*ignoreExtensionDataObject*/,
                                              false /*preserveObjectReferences*/,
                                              surrogate /*dataContractSurrogate*/);
        }
    }
}