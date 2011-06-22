#region usings

using System;
using System.Transactions;
using Composable.ServiceBus;
using Composable.System.Linq;
using log4net.Core;

#endregion

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Flags]
    public enum SqlServerEventStoreConfig
    {
        Default = 0x0,
        NoBatching = 0x2  
    }

    public class SqlServerEventStore : IEventStore
    {
        public string ConnectionString { get; private set; }
        public SqlServerEventStoreConfig Config {get;private set;}
        public IServiceBus Bus { get; private set; }

        public SqlServerEventStore(string connectionString, IServiceBus bus, SqlServerEventStoreConfig config = SqlServerEventStoreConfig.Default)
        {
            ConnectionString = connectionString;
            Bus = bus;
            Config = config;
        }

        public IEventStoreSession OpenSession()
        {
            return new EventStoreSession(Bus, new SqlServerEventSomethingOrOther(this));
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerEventStore(connectionString, null);
            using(var session = new SqlServerEventSomethingOrOther(me))
            {
                session.ResetDB();
            }
        }
    }
}