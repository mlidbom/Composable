using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.NewtonSoft;
using Composable.System.Reflection;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Composable.Messaging.Buses.Implementation
{
    static class TransportMessage
    {
        public class InComing
        {
            public readonly byte[] Client;
            public readonly Guid MessageId;
            readonly string _body;
            readonly string _messageType;

            IMessage _message;

            public IMessage DeserializeMessageAndCacheForNextCall()
            {
                if(_message == null)
                {
                    _message = (IMessage)JsonConvert.DeserializeObject(_body, _messageType.AsType(), JsonSettings.JsonSerializerSettings);


                    Contract.State.Assert(!(_message is IExactlyOnceDeliveryMessage) || MessageId == (_message as IExactlyOnceDeliveryMessage).MessageId);
                }
                return _message;
            }

            InComing(string body, string messageType, byte[] client, Guid messageId)
            {
                _body = body;
                _messageType = messageType;
                Client = client;
                MessageId = messageId;
            }

            public static InComing Receive(RouterSocket socket)
            {
                var receivedMessage = socket.ReceiveMultipartMessage();

                var client = receivedMessage[0].ToByteArray();
                var messageId = new Guid(receivedMessage[1].ToByteArray());
                var messageTypeString = receivedMessage[2].ConvertToString();
                var messageBody = receivedMessage[3].ConvertToString();

                return new InComing(messageBody, messageTypeString, client, messageId);
            }

            public Response.Outgoing CreateFailureResponse(Exception exception) => Response.Outgoing.Failure(this, exception);

            public Response.Outgoing CreateSuccessResponse(IMessage response) => Response.Outgoing.Success(this, response);
        }

        public class OutGoing
        {
            readonly string _messageType;
            public readonly Guid MessageId;
            readonly string _messageBody;

            public void Send(IOutgoingSocket socket)
            {
                var message = new NetMQMessage(4);
                message.Append(MessageId);
                message.Append(_messageType);
                message.Append(_messageBody);

                socket.SendMultipartMessage(message);
            }

            public static OutGoing Create(IMessage message)
            {
                var messageId = (message as IExactlyOnceDeliveryMessage)?.MessageId ?? Guid.NewGuid();
                var body = JsonConvert.SerializeObject(message, Formatting.Indented, JsonSettings.JsonSerializerSettings);
                return new OutGoing(message.GetType(), messageId, body);
            }

            OutGoing(Type messageType, Guid messageId, string messageBody)
            {
                _messageType = messageType.FullName;
                MessageId = messageId;
                _messageBody = messageBody;
            }
        }

        public class Response
        {
            static class Constants
            {
                public const string ReplySuccess = "OK";
                public const string ReplyFailure = "FAIL";
                public const string NullString = "NULL";
            }

            public class Outgoing
            {
                readonly NetMQMessage _response;

                Outgoing(NetMQMessage response) => _response = response;

                public void Send(IOutgoingSocket socket) => socket.SendMultipartMessage(_response);

                public static Outgoing Success(TransportMessage.InComing incoming, IMessage result)
                {
                    var responseMessage = new NetMQMessage();

                    responseMessage.Append(incoming.Client);
                    responseMessage.Append(incoming.MessageId);
                    responseMessage.Append(Constants.ReplySuccess);

                    if(result != null)
                    {
                        responseMessage.Append(result.GetType().FullName);
                        responseMessage.Append(JsonConvert.SerializeObject(result, Formatting.Indented, JsonSettings.JsonSerializerSettings));
                    } else
                    {
                        responseMessage.Append(Constants.NullString);
                        responseMessage.Append(Constants.NullString);
                    }
                    return new Outgoing(responseMessage);
                }

                public static Outgoing Failure(TransportMessage.InComing incoming, Exception failure)
                {
                    var response = new NetMQMessage();

                    response.Append(incoming.Client);
                    response.Append(incoming.MessageId);
                    response.Append(Constants.ReplyFailure);

                    return new Outgoing(response);
                }
            }

            public class Incoming
            {
                readonly string _resultJson;
                readonly string _responseType;
                IMessage _result;
                public bool SuccessFull { get; }
                public Guid RespondingToMessageId { get; }

                public IMessage DeserializeResult()
                {
                    if(_result == null)
                    {
                        if(_resultJson == Constants.NullString)
                        {
                            return null;
                        }
                        _result = (IMessage)JsonConvert.DeserializeObject(_resultJson, _responseType.AsType(), JsonSettings.JsonSerializerSettings);
                    }
                    return _result;
                }

                public static IReadOnlyList<Response.Incoming> ReceiveBatch(IReceivingSocket socket, int batchMaximum)
                {
                    List<Response.Incoming> result = new List<Response.Incoming>();
                    NetMQMessage received = null;
                    while(socket.TryReceiveMultipartMessage(TimeSpan.Zero, ref received))
                    {
                        result.Add(FromMultipartMessage(received));
                    }
                    return result;
                }

                static Incoming FromMultipartMessage(NetMQMessage message)
                {
                    var messageId = new Guid(message[0].ToByteArray());
                    var result = message[1].ConvertToString();

                    if(result == Constants.ReplySuccess)
                    {
                        var responseType = message[2].ConvertToString();
                        var responseBody = message[3].ConvertToString();
                        return new Incoming(successFull: true, respondingToMessageId: messageId, resultJson: responseBody, responseType: responseType);
                    } else
                    {
                        return new Incoming(successFull: false, respondingToMessageId: messageId, resultJson: null, responseType: null);
                    }
                }

                Incoming(bool successFull, Guid respondingToMessageId, string resultJson, string responseType)
                {
                    _resultJson = resultJson;
                    _responseType = responseType;
                    SuccessFull = successFull;
                    RespondingToMessageId = respondingToMessageId;
                }
            }
        }
    }
}
