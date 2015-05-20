using System;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    /// <summary>
    /// <para>
    /// Publishes messages to both the <see cref="NServiceBusServiceBus"/> and the <see cref="SynchronousBus"/>.
    /// </para>
    /// 
    /// <para> 
    /// Send and SendLocal dispatches to either the <see cref="NServiceBusServiceBus"/> or the <see cref="SynchronousBus"/>.
    /// It routes messages to the <see cref="SynchronousBus"/> if it has a handler for it, and to the <see cref="NServiceBusServiceBus"/> otherwise. 
    /// </para>
    /// </summary>
    public class DualDispatchBus : IServiceBus
    {
        private readonly SynchronousBus _local;
        private readonly NServiceBusServiceBus _realBus;

        private bool _dispatchingOnSynchronousBus = false;
        private readonly SingleThreadUseGuard _usageGuard;

        public DualDispatchBus(SynchronousBus local, NServiceBusServiceBus realBus)
        {
            _usageGuard = new SingleThreadUseGuard();
            _local = local;
            _realBus = realBus;
        }

        private void DispatchOnSynchronousBus(Action action)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (_dispatchingOnSynchronousBus)
            {
                action();
                return;
            }

            _dispatchingOnSynchronousBus = true;
            using (new DisposeAction(() => _dispatchingOnSynchronousBus = false))
            {
                action();
            }
        }

        public void Publish(object message)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            DispatchOnSynchronousBus(() => _local.Publish(message));
            _realBus.Publish(message);
        }

        public void SendLocal(object message)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            //There can only be one handler in a send scenario and we let the synchronous bus get the first pick.
            if (_local.Handles(message))
            {
                DispatchOnSynchronousBus(() => _local.SendLocal(message));
            }
            else
            {
                _realBus.SendLocal(message);
            }
        }

        public void Send(object message)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            //There can only be one handler in a send scenario and we let the synchronous bus get the first pick.
            if(_local.Handles(message))
            {
                DispatchOnSynchronousBus(() => _local.Send(message));
            }
            else
            {
                _realBus.Send(message);
            }
        }

        public void Reply(object message)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            //I cannot come up with a way to decide whether to dispatch to the real bus or the sync bus except for just giving it to the sync bus if it wants it...
            if(_dispatchingOnSynchronousBus)
            {
                _local.Reply(message);
            }
            else
            {
                _realBus.Reply(message);
            }
        }
    }
}
