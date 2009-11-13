using System;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Void.ServiceModel
{
    [Flags]
    public enum Options
    {
        ManageNHibernateCollections = 2,
        //ManageNHibernateProxies = 4,
        DetachReferenceObjects = 8,
        AttachReferenceObjects = 16,

        Defaults = ManageNHibernateCollections | DetachReferenceObjects | AttachReferenceObjects
    }

    public class OperationExtensionAttribute : Attribute, IOperationBehavior
    {
        public Options Options { get; set; }
        public Type[] ReferenceTypesToRegister { get; set; }



        public OperationExtensionAttribute()
        {
            Options = Options.Defaults;
        }

        void IOperationBehavior.AddBindingParameters(OperationDescription description, BindingParameterCollection parameters)
        {
        }

        void IOperationBehavior.ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
        {
            IOperationBehavior innerBehavior = new ClientSerializationOperationBehavior(description, this);
            innerBehavior.ApplyClientBehavior(description, proxy);
        }

        void IOperationBehavior.ApplyDispatchBehavior(OperationDescription description, DispatchOperation dispatch)
        {
            IOperationBehavior innerBehavior = new ServerSerializationOperationBehavior(description, this);
            innerBehavior.ApplyDispatchBehavior(description, dispatch);
        }

        public void Validate(OperationDescription description)
        {
        }

        public IDataContractSurrogate CreateClientSurrogate()
        {
            return new ClientSerializationOperationBehavior(null, this).CreateClientSurrogate();
        }
    }
}