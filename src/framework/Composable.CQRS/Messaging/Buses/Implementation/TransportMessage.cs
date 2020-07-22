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

            MessageTypes.IMessage? _message;
            readonly ITypeMapper _typeMapper;

            //performance: detect BinarySerializable and use instead.
            public MessageTypes.IMessage DeserializeMessageAndCacheForNextCall()
            {
                if(_message == null)
                {
                    _message = _serializer.DeserializeMessage(MessageType, Body);

                    Assert.State.Assert(!(_message is MessageTypes.Remotable.ExactlyOnce.IMessage actualMessage) || MessageId == actualMessage.MessageId);
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
                MessageTypeEnum = GetMessageTypeEnum(MessageType);
                Client = client;
                MessageId = messageId;
            }

            public static IReadOnlyList<InComing> ReceiveBatch(RouterSocket socket, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                var result = new List<TransportMessage.InComing>();
                NetMQMessage? receivedMessage = null;
                while(socket.TryReceiveMultipartMessage(TimeSpan.Zero, ref receivedMessage))
                {

                    var client = receivedMessage![0].ToByteArray();
                    var messageId = new Guid(receivedMessage[1].ToByteArray());
                    var messageType = new TypeId(new Guid(receivedMessage[2].ToByteArray()));
                    var messageBody = receivedMessage[3].ConvertToString();

                    result.Add(new InComing(messageBody, messageType, client, messageId, typeMapper, serializer));
                }
                return result;
            }

            static TransportMessageType GetMessageTypeEnum(Type messageType)
            {
                if(typeof(MessageTypes.Remotable.NonTransactional.IQuery).IsAssignableFrom(messageType))
                    return TransportMessageType.NonTransactionalQuery;
                if(typeof(MessageTypes.Remotable.AtMostOnce.ICommand).IsAssignableFrom(messageType))
                    return TransportMessageType.AtMostOnceCommand;
                else if(typeof(MessageTypes.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(messageType))
                    return TransportMessageType.ExactlyOnceEvent;
                if(typeof(MessageTypes.Remotable.ExactlyOnce.ICommand).IsAssignableFrom(messageType))
                    return TransportMessageType.ExactlyOnceCommand;
                else
                    throw new ArgumentOutOfRangeException();
            }

            public NetMQMessage CreateFailureResponse(AggregateException exception) => Response.Create.Failure(this, exception);

            public NetMQMessage CreateSuccessResponseWithData(object response) => Response.Create.SuccessWithData(this, response, _serializer, _typeMapper);

            public NetMQMessage CreateSuccessResponse() => Response.Create.Success(this);

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

            public static OutGoing Create(MessageTypes.Remotable.IMessage message, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                var messageId = (message as MessageTypes.Remotable.IAtMostOnceMessage)?.MessageId ?? Guid.NewGuid();
                //performance: detect implementation of BinarySerialized and use that when available
                var body = serializer.SerializeMessage(message);
                return new OutGoing(typeMapper.GetId(message.GetType()), messageId, body, message is MessageTypes.Remotable.ExactlyOnce.IMessage);
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
                SuccessWithData,
                FailureExpectedReturnValue,
                Failure,
                Received,
                Success
            }

            static class Constants
            {
                public const string NullString = "NULL";
            }

            internal static class Create
            {
                public static NetMQMessage SuccessWithData(TransportMessage.InComing incoming, object response, IRemotableMessageSerializer serializer, ITypeMapper typeMapper)
                {
                    var responseMessage = new NetMQMessage();

                    responseMessage.Append(incoming.Client);
                    responseMessage.Append(incoming.MessageId);
                    responseMessage.Append((int)ResponseType.SuccessWithData);

                    var guidValue = typeMapper.GetId(response.GetType()).GuidValue;
                       responseMessage.Append(guidValue);
                       responseMessage.Append(serializer.SerializeResponse(response));

                    return responseMessage;
                }

                public static NetMQMessage Success(TransportMessage.InComing incoming)
                {
                    var responseMessage = new NetMQMessage();

                    responseMessage.Append(incoming.Client);
                    responseMessage.Append(incoming.MessageId);
                    responseMessage.Append((int)ResponseType.Success);
                    responseMessage.Append(Constants.NullString);
                    responseMessage.Append(Constants.NullString);
                    return responseMessage;
                }

                public static NetMQMessage Failure(TransportMessage.InComing incoming, AggregateException failure)
                {
                    var response = new NetMQMessage();

                    response.Append(incoming.Client);
                    response.Append(incoming.MessageId);
                    if(incoming.Is<MessageTypes.IHasReturnValue>())
                    {
                        response.Append((int)ResponseType.FailureExpectedReturnValue);
                    } else
                    {
                        response.Append((int)ResponseType.Failure);
                    }

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
                internal readonly string? Body;
                readonly TypeId? _responseTypeId;
                readonly ITypeMapper _typeMapper;
                readonly IRemotableMessageSerializer _serializer;
                object? _result;
                internal ResponseType ResponseType { get; }
                internal Guid RespondingToMessageId { get; }

                public object DeserializeResult() => _result ??= _serializer.DeserializeResponse(_typeMapper.GetType(_responseTypeId!), Body!);

                public static IReadOnlyList<Incoming> ReceiveBatch(IReceivingSocket socket, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, int batchMaximum)
                {
                    var result = new List<Response.Incoming>();
                    NetMQMessage? received = null;
                    int fetched = 0;
                    while(fetched < batchMaximum && socket.TryReceiveMultipartMessage(TimeSpan.Zero, ref received))
                    {
                        result.Add(FromMultipartMessage(received!, typeMapper, serializer));
                        fetched++;
                    }
                    return result;
                }

                static Incoming FromMultipartMessage(NetMQMessage message, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                {
                    var messageId = new Guid(message[0].ToByteArray());
                    var type = (ResponseType)message[1].ConvertToInt32();

                    switch(type)
                    {
                        case ResponseType.SuccessWithData:
                        {
                            var responseBody = message[3].ConvertToString();
                            TypeId? responseType = null;
                            if(responseBody != Constants.NullString)
                            {
                                responseType = new TypeId(new Guid(message[2].ToByteArray()));
                            }

                            return new Incoming(type: type, respondingToMessageId: messageId, body: responseBody, responseTypeId: responseType, typeMapper: typeMapper, serializer);
                        }
                        case ResponseType.Success:
                        {
                            return new Incoming(type: type, respondingToMessageId: messageId, body: null, responseTypeId: null, typeMapper: typeMapper, serializer);
                        }
                        case ResponseType.FailureExpectedReturnValue:
                            return new Incoming(type: type, respondingToMessageId: messageId, body: message[2].ConvertToString(), responseTypeId: null, typeMapper: typeMapper, serializer);
                        case ResponseType.Failure:
                            return new Incoming(type: type, respondingToMessageId: messageId, body: message[2].ConvertToString(), responseTypeId: null, typeMapper: typeMapper, serializer);
                        case ResponseType.Received:
                            return new Incoming(type: type, respondingToMessageId: messageId, body: null, responseTypeId: null, typeMapper: typeMapper, serializer);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Incoming(ResponseType type, Guid respondingToMessageId, string? body, TypeId? responseTypeId, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                {
                    Body = body;
                    _responseTypeId = responseTypeId;
                    _typeMapper = typeMapper;
                    _serializer = serializer;
                    ResponseType = type;
                    RespondingToMessageId = respondingToMessageId;
                }
            }
        }
    }
}
