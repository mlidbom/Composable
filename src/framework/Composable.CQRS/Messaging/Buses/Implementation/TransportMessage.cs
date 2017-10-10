using System;
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
            readonly byte[] _client;
            readonly Guid _messageId;
            readonly string _body;
            readonly string _messageType;

            IMessage _message = null;

            public IMessage DeserializedPayload()
            {
                if(_message == null)
                {
                    _message = (IMessage)JsonConvert.DeserializeObject(_body, _messageType.AsType(), JsonSettings.JsonSerializerSettings);
                    Contract.State.Assert(_messageId == _message.MessageId);
                }
                return _message;
            }

            InComing(string body, string messageType, byte[] client, Guid messageId)
            {
                _body = body;
                _messageType = messageType;
                _client = client;
                _messageId = messageId;
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

            public void RespondSucess(IMessage response, RouterSocket socket)
            {
                var netMqMessage = new NetMQMessage();

                netMqMessage.Append(_client);
                netMqMessage.Append(DeserializedPayload().MessageId.ToByteArray());
                netMqMessage.Append("OK");

                if(response != null)
                {
                    netMqMessage.Append(response.GetType().FullName);
                    netMqMessage.Append(JsonConvert.SerializeObject(response, Formatting.Indented, JsonSettings.JsonSerializerSettings));
                } else
                {
                    netMqMessage.Append("NULL");
                    netMqMessage.Append("NULL");
                }

                socket.SendMultipartMessage(netMqMessage);
            }

            public void RespondError(Exception exception, RouterSocket socket)
            {
                var netMqMessage = new NetMQMessage();

                netMqMessage.Append(_client);
                netMqMessage.Append(DeserializedPayload().MessageId.ToByteArray());
                netMqMessage.Append("FAIL");

                socket.SendMultipartMessage(netMqMessage);
            }
        }

        public class OutGoing
        {
            readonly string _messageType;
            readonly Guid _messageId;
            readonly string _messageBody;

            public void Send(IOutgoingSocket socket)
            {
                var message = new NetMQMessage(4);
                message.Append(_messageId);
                message.Append(_messageType);
                message.Append(_messageBody);

                socket.SendMultipartMessage(message);
            }

            public static OutGoing Create(IMessage message)
            {
                var body = JsonConvert.SerializeObject(message, Formatting.Indented, JsonSettings.JsonSerializerSettings);
                return new OutGoing(message.GetType(), message.MessageId, body);
            }

            public OutGoing(Type messageType, Guid messageId, string messageBody)
            {
                _messageType = messageType.FullName;
                _messageId = messageId;
                _messageBody = messageBody;
            }
        }

        public class Response
        {
            readonly string _resultJson;
            readonly string _responseType;
            IMessage _result;
            public bool SuccessFull { get; }
            public Guid MessageId { get; }

            public IMessage DeserializeResult()
            {
                if(_result == null)
                {
                    if(_resultJson == "NULL")
                    {
                        return null;
                    }
                    _result = (IMessage)JsonConvert.DeserializeObject(_resultJson, _responseType.AsType(), JsonSettings.JsonSerializerSettings);
                }
                return _result;
            }

            public static Response Receive(IReceivingSocket socket)
            {
                var message = socket.ReceiveMultipartMessage();
                var messageId = new Guid(message[0].ToByteArray());
                var result = message[1].ConvertToString();

                if(result == "OK")
                {
                    var responseType = message[2].ConvertToString();
                    var responseBody = message[3].ConvertToString();
                    return new Response(successFull: true, messageId: messageId, resultJson: responseBody, responseType: responseType);
                } else
                {
                    return new Response(successFull: false, messageId: messageId, resultJson: null, responseType: null);
                }
            }

            Response(bool successFull, Guid messageId, string resultJson, string responseType)
            {
                _resultJson = resultJson;
                _responseType = responseType;
                SuccessFull = successFull;
                MessageId = messageId;
            }
        }
    }
}
