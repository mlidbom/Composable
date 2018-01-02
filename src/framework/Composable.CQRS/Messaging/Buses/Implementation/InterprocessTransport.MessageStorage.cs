using System;
using System.Threading.Tasks;
using Composable.Contracts;
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

            internal async Task MarkAsSentAsync(TransportMessage.OutGoing outGoingMessage, EndpointId endpointId)
            {
                _connectionFactory.UseCommand(
                    command =>
                        command
                            .SetCommandText(
                                $@"
INSERT {MessageDispatching.TableName} 
            ({MessageDispatching.MessageId},  {MessageDispatching.EndpointId},  {MessageDispatching.IsReceived}) 
    VALUES (@{MessageDispatching.MessageId}, @{MessageDispatching.EndpointId}, @{MessageDispatching.IsReceived})
")
                            .AddParameter(MessageDispatching.MessageId, outGoingMessage.MessageId)
                            .AddParameter(MessageDispatching.EndpointId, endpointId.GuidValue)
                            .AddParameter(MessageDispatching.IsReceived, 0)
                            .ExecuteNonQuery());
            }

            internal async Task MarkAsReceivedAsync(TransportMessage.Response.Incoming response, EndpointId endpointId)
            {
                _connectionFactory.UseCommand(
                    command =>
                    {
                        var affectedRows = command
                                           .SetCommandText(
                                               $@"
UPDATE {MessageDispatching.TableName} 
    SET {MessageDispatching.IsReceived} = 1
WHERE {MessageDispatching.MessageId} = @{MessageDispatching.MessageId}
    AND {MessageDispatching.EndpointId} = @{MessageDispatching.EndpointId}
    AND {MessageDispatching.IsReceived} = 0
")
                                           .AddParameter(MessageDispatching.MessageId, response.RespondingToMessageId)
                                           .AddParameter(MessageDispatching.EndpointId, endpointId.GuidValue)
                                           .AddParameter(MessageDispatching.IsReceived, 1)
                                           .ExecuteNonQuery();

                        Contract.Result.Assert(affectedRows == 1);
                        return affectedRows;
                    });
            }

            public async Task SaveMessageAsync(ITransactionalExactlyOnceDeliveryMessage message)
            {
                await _connectionFactory.UseCommandAsync(
                    command =>
                        command
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

            public void Start() => SchemaManager.EnsureTablesExist(_connectionFactory);
        }
    }
}
