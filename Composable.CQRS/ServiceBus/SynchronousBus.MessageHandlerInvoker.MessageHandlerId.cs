using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    public partial class SynchronousBus
    {
        private static partial class MessageHandlerInvoker
        {
            private class MessageHandlerId
            {
                private Type ImplementingClass { get; set; }
                private Type GenericInterfaceImplemented { get; set; }

                public MessageHandlerId(Type implementingClass, Type genericInterfaceImplemented)
                {
                    ImplementingClass = implementingClass;
                    GenericInterfaceImplemented = genericInterfaceImplemented;
                }

                override public bool Equals(object other)
                {
                    if(other == null || GetType() != other.GetType())
                    {
                        return false;
                    }

                    return Equals((MessageHandlerId)other);
                }

                private bool Equals(MessageHandlerId other)
                {
                    return other.ImplementingClass == ImplementingClass && other.GenericInterfaceImplemented == GenericInterfaceImplemented;
                }

                override public int GetHashCode()
                {
                    return ImplementingClass.GetHashCode() + GenericInterfaceImplemented.GetHashCode();
                }
            }
        }
    }
}
