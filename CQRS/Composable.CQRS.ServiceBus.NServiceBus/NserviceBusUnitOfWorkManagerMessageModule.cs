using System;
using Castle.Windsor;
using Composable.UnitsOfWork;
using NServiceBus;
using log4net;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NServiceBusUnitOfWorkManagerMessageModule : IMessageModule
    {
        private readonly IWindsorContainer _container;
         
        [ThreadStatic]private static UnitOfWork _unit;

        private static ILog Log = LogManager.GetLogger(typeof(NServiceBusUnitOfWorkManagerMessageModule));

        public NServiceBusUnitOfWorkManagerMessageModule(IWindsorContainer container)
        {
            Log.Debug("Constructor called");
            _container = container;
        }

        public void HandleBeginMessage()
        {
            Log.Debug("HandleBeginMessage called");
            _unit = new UnitOfWork();


            _unit.AddParticipants(_container.ResolveAll<IUnitOfWorkParticipant>());
        }

        public void HandleEndMessage()
        {
            Log.Debug("HandleEndMessage called");
            _unit.Commit();
        }

        public void HandleError()
        {
            Log.Debug("HandleError called");
            _unit.Rollback();
        }
    }
}