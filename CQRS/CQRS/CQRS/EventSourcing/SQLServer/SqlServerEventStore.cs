#region usings

using System;
using System.Transactions;
using Composable.ServiceBus;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using log4net.Core;

#endregion

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStore : IEventStore
    {
        public string ConnectionString { get; private set; }
        public IServiceBus Bus { get; private set; }

        public SqlServerEventStore(string connectionString, IServiceBus bus)
        {
            ConnectionString = connectionString;
            Bus = bus;
        }

        public IEventStoreSession OpenSession()
        {
            var singleThreadedUseGuard = new SingleThreadUseGuard();
            return new EventStoreSession(Bus, new SqlServerEventSomethingOrOther(singleThreadedUseGuard, ConnectionString), singleThreadedUseGuard);
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerEventStore(connectionString, null);
            var singleThreadedUseGuard = new SingleThreadUseGuard();
            using(var session = new SqlServerEventSomethingOrOther(singleThreadedUseGuard, connectionString))
            {
                session.ResetDB();
            }
        }
    }
}