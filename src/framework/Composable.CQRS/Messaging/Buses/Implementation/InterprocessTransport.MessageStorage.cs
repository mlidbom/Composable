using System;
using System.Threading.Tasks;
using Composable.System.Data.SqlClient;
using Newtonsoft.Json;

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
                    await _connectionFactory.UseCommandAsync(
                        async command =>
                            await command
                                  .SetCommandText(
                                      $@"
INSERT {OutboxMessages.TableName} 
            ({OutboxMessages.MessageId},  {OutboxMessages.TypeId},  {OutboxMessages.Body}) 
    VALUES (@{OutboxMessages.MessageId}, @{OutboxMessages.TypeId}, @{OutboxMessages.Body})
")
                                  .AddParameter(OutboxMessages.MessageId, message.MessageId)
                                  .AddParameter(OutboxMessages.TypeId, TypeId.FromType(message.GetType()).GuidValue)
                                  .AddNVarcharMaxParameter(OutboxMessages.Body, JsonConvert.SerializeObject(message))
                                  .ExecuteNonQueryAsync());
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
