using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    static class TransportMessage
    {
        internal enum TransportMessageType
        {
            ExactlyOnceEvent,
            AtMostOnceCommand,
            ExactlyOnceCommand,
            NonTransactionalQuery
        }

        internal class InComing
        {
            internal readonly byte[] Client;
            internal readonly Guid MessageId;
            readonly IRemotableMessageSerializer _serializer;
            internal readonly string Body;
            internal readonly TypeId MessageTypeId;
            internal readonly Type MessageType;
            internal readonly TransportMessageType MessageTypeEnum;
            internal bool Is<TType>() => typeof(TType).IsAssignableFrom(MessageType);

            BusApi.IMessage _message;
            readonly ITypeMapper _typeMapper;

            //performance: detect BinarySerializable and use instead.
            public BusApi.IMessage DeserializeMessageAndCacheForNextCall()
            {
                if(_message == null)
                {
                    _message = _serializer.DeserializeMessage(MessageType, Body);

                    Assert.State.Assert(!(_message is BusApi.Remotable.ExactlyOnce.IMessage actualMessage) || MessageId == actualMessage.DeduplicationId);
                }
                return _message;
            }

            InComing(string body, TypeId messageTypeId, byte[] client, Guid messageId, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                _serializer = serializer;
                _typeMapper = typeMapper;
                Body = body;
                MessageTypeId = messageTypeId;
                MessageType = typeMapper.GetType(messageTypeId);
                MessageTypeEnum = GetMessageType(MessageType);
                Client = client;
                MessageId = messageId;
            }

            public static IReadOnlyList<InComing> ReceiveBatch(RouterSocket socket, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                var result = new List<TransportMessage.InComing>();
                NetMQMessage? receivedMessage = null;
                while(socket.TryReceiveMultipartMessage(TimeSpan.Zero, ref receivedMessage))
                {

                    var client = receivedMessage[0].ToByteArray();
                    var messageId = new Guid(receivedMessage[1].ToByteArray());
                    var messageType = new TypeId(new Guid(receivedMessage[2].ToByteArray()));
                    var messageBody = receivedMessage[3].ConvertToString();

                    result.Add(new InComing(messageBody, messageType, client, messageId, typeMapper, serializer));
                }
                return result;
            }

            static TransportMessageType GetMessageType(Type messageType)
            {
                if(typeof(BusApi.Remotable.NonTransactional.IQuery).IsAssignableFrom(messageType))
                    return TransportMessageType.NonTransactionalQuery;
                if(typeof(BusApi.Remotable.AtMostOnce.ICommand).IsAssignableFrom(messageType))
                    return TransportMessageType.AtMostOnceCommand;
                else if(typeof(BusApi.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(messageType))
                    return TransportMessageType.ExactlyOnceEvent;
                if(typeof(BusApi.Remotable.ExactlyOnce.ICommand).IsAssignableFrom(messageType))
                    return TransportMessageType.ExactlyOnceCommand;
                else
                    throw new ArgumentOutOfRangeException();
            }

            public NetMQMessage CreateFailureResponse(AggregateException exception) => Response.Create.Failure(this, exception);

            public NetMQMessage CreateSuccessResponse(object response) => Response.Create.Success(this, response, _serializer, _typeMapper);

            public NetMQMessage CreatePersistedResponse() => Response.Create.Persisted(this);
        }

        internal class OutGoing
        {
            public bool IsExactlyOnceDeliveryMessage { get; }
            public readonly Guid MessageId;

            readonly TypeId _messageType;
            readonly string _messageBody;

            public void Send(IOutgoingSocket socket)
            {
                var message = new NetMQMessage(4);
                message.Append(MessageId);
                message.Append(_messageType.GuidValue);
                message.Append(_messageBody);

                socket.SendMultipartMessage(message);
            }

            public static OutGoing Create(BusApi.Remotable.IMessage message, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                var messageId = (message as BusApi.Remotable.IAtMostOnceMessage)?.DeduplicationId ?? Guid.NewGuid();
                //performance: detect implementation of BinarySerialized and use that when available
                var body = serializer.SerializeMessage(message);
                return new OutGoing(typeMapper.GetId(message.GetType()), messageId, body, message is BusApi.Remotable.ExactlyOnce.IMessage);
            }

            OutGoing(TypeId messageType, Guid messageId, string messageBody, bool isExactlyOnceDeliveryMessage)
            {
                IsExactlyOnceDeliveryMessage = isExactlyOnceDeliveryMessage;
                _messageType = messageType;
                MessageId = messageId;
                _messageBody = messageBody;
            }
        }

        internal static class Response
        {
            internal enum ResponseType
            {
                Success,
                Failure,
                Received
            }

            static class Constants
            {
                public const string NullString = "NULL";
            }

            internal static class Create
            {
                public static NetMQMessage Success(TransportMessage.InComing incoming, object response, IRemotableMessageSerializer serializer, ITypeMapper typeMapper)
                {
                    var responseMessage = new NetMQMessage();

                    responseMessage.Append(incoming.Client);
                    responseMessage.Append(incoming.MessageId);
                    responseMessage.Append((int)ResponseType.Success);

                    if(response != null)
                    {
                        var guidValue = typeMapper.GetId(response.GetType()).GuidValue;
                        responseMessage.Append(guidValue);
                        responseMessage.Append(serializer.SerializeResponse(response));
                    } else
                    {
                        responseMessage.Append(Constants.NullString);
                        responseMessage.Append(Constants.NullString);
                    }
                    return responseMessage;
                }

                public static NetMQMessage Failure(TransportMessage.InComing incoming, AggregateException failure)
                {
                    var response = new NetMQMessage();

                    response.Append(incoming.Client);
                    response.Append(incoming.MessageId);
                    response.Append((int)ResponseType.Failure);

                    response.Append(failure.InnerExceptions.Count == 1 ? failure.InnerException.ToString() : failure.ToString());

                    return response;
                }

                public static NetMQMessage Persisted(InComing incoming)
                {
                    var responseMessage = new NetMQMessage();

                    responseMessage.Append(incoming.Client);
                    responseMessage.Append(incoming.MessageId);
                    responseMessage.Append((int)ResponseType.Received);
                    return responseMessage;
                }
            }

            internal class Incoming
            {
                internal readonly string Body;
                readonly TypeId? _responseTypeId;
                readonly ITypeMapper _typeMapper;
                object? _result;
                internal ResponseType ResponseType { get; }
                internal Guid RespondingToMessageId { get; }

                public object? DeserializeResult(IRemotableMessageSerializer serializer)
                {
                    if(_result == null)
                    {
                        if(Body == Constants.NullString)
                        {
                            return null;
                        }
                        _result = serializer.DeserializeResponse(_typeMapper.GetType(_responseTypeId!), Body);
                    }
                    return _result;
                }

                public static IReadOnlyList<Response.Incoming> ReceiveBatch(IReceivingSocket socket, ITypeMapper typeMapper, int batchMaximum)
                {
                    var result = new List<Response.Incoming>();
                    NetMQMessage? received = null;
                    int fetched = 0;
                    while(fetched < batchMaximum && socket.TryReceiveMultipartMessage(TimeSpan.Zero, ref received))
                    {
                        result.Add(FromMultipartMessage(received, typeMapper));
                        fetched++;
                    }
                    return result;
                }

                static Incoming FromMultipartMessage(NetMQMessage message, ITypeMapper typeMapper)
                {
                    var messageId = new Guid(message[0].ToByteArray());
                    var type = (ResponseType)message[1].ConvertToInt32();

                    switch(type)
                    {
                        case ResponseType.Success:
                        {
                            var responseBody = message[3].ConvertToString();
                            TypeId? responseType = null;
                            if(responseBody != Constants.NullString)
                            {
                                responseType = new TypeId(new Guid(message[2].ToByteArray()));
                            }

                            return new Incoming(type: type, respondingToMessageId: messageId, body: responseBody, responseTypeId: responseType, typeMapper: typeMapper);
                        }
                        case ResponseType.Failure:
                            return new Incoming(type: type, respondingToMessageId: messageId, body: message[2].ConvertToString(), responseTypeId: null, typeMapper: typeMapper);
                        case ResponseType.Received:
                            return new Incoming(type: type, respondingToMessageId: messageId, body: null, responseTypeId: null, typeMapper: typeMapper);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Incoming(ResponseType type, Guid respondingToMessageId, string body, TypeId? responseTypeId, ITypeMapper typeMapper)
                {
                    Body = body;
                    _responseTypeId = responseTypeId;
                    _typeMapper = typeMapper;
                    ResponseType = type;
                    RespondingToMessageId = respondingToMessageId;
                }
            }
        }
    }
}
