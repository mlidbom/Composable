#region usings

using System;
using System.Transactions;
using Castle.Windsor;
using Composable.UnitsOfWork;
using NServiceBus;
using log4net;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NServiceBusUnitOfWorkManagerMessageModule : IMessageModule
    {
        private readonly IWindsorContainer _container;

        [ThreadStatic] private static UnitOfWork _unit;

        private static readonly ILog Log = LogManager.GetLogger(typeof (NServiceBusUnitOfWorkManagerMessageModule));

        public NServiceBusUnitOfWorkManagerMessageModule(IWindsorContainer container)
        {
            Log.Debug("Constructor called");
            _container = container;
        }

        public void HandleBeginMessage()
        {
            Log.Debug("HandleBeginMessage called");

            try
            {
                AssertAmbientTransactionPresent();
                _unit = new UnitOfWork();

                _unit.AddParticipants(_container.ResolveAll<IUnitOfWorkParticipant>());
            }
            catch (Exception e)
            {
                Log.Error("HandleBeginMessage failed", e);
                throw;
            }
        }

        public void HandleEndMessage()
        {
            Log.Debug("HandleEndMessage called");

            try
            {
                _unit.Commit();
            }
            catch (Exception e)
            {
                Log.Error("HandleEndMessage failed", e);
                throw;
            }
        }

        public void HandleError()
        {
            Log.Debug("HandleError called");
            try
            {
                _unit.Rollback();
            }
            catch (Exception e)
            {
                Log.Error("HandleError failed", e);
                throw;
            }
        }

        private static void AssertAmbientTransactionPresent()
        {
            if (Transaction.Current == null)
            {
                throw new NoAmbientTransactionException();
            }
        }
    }

    public class NoAmbientTransactionException : Exception
    {
        public NoAmbientTransactionException() : base("Ambient transaction required")
        {
        }
    }
}