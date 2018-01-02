using System;
using System.Threading.Tasks;
using Composable.System.Data.SqlClient;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        internal partial class MessageStorage
        {
            readonly ISqlConnection _connectionFactory;

            public MessageStorage(ISqlConnection connectionFactory) => _connectionFactory = connectionFactory;

            internal async Task MarkAsSentAsync(TransportMessage.OutGoing outGoingMessage)
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch(Exception)
                {
                    //todo: proper exception handling here.
                    throw;
                }
            }

            internal async Task MarkAsReceivedAsync(TransportMessage.Response.Incoming response)
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch(Exception)
                {
                    //todo: proper exception handling here.
                    throw;
                }
            }

            public async Task SaveMessageAsync(ITransactionalExactlyOnceDeliveryMessage message)
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch(Exception)
                {
                    //todo: proper exception handling here.
                    throw;
                }
            }

            public void Start() => SchemaManager.EnsureTablesExist(_connectionFactory);

        }
    }
}
