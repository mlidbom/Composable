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
            public IMessage Message { get; }
            readonly byte[] _client;

            InComing(IMessage message, byte[] client)
            {
                Message = message;
                _client = client;
            }

            public static InComing Receive(RouterSocket socket)
            {
                var receivedMessage = socket.ReceiveMultipartMessage();

                var client = receivedMessage[0].ToByteArray();
                var messageId = new Guid(receivedMessage[1].ToByteArray());
                var messageTypeString = receivedMessage[2].ConvertToString();
                var messageBody = receivedMessage[3].ConvertToString();
                var messageType = messageTypeString.AsType();

                var message = (IMessage)JsonConvert.DeserializeObject(messageBody, messageType, JsonSettings.JsonSerializerSettings);

                Contract.State.Assert(messageId == message.MessageId);

                return new InComing(message, client);
            }

            public void RespondSucess(IMessage response, RouterSocket socket)
            {
                var netMqMessage = new NetMQMessage();

                netMqMessage.Append(_client);
                netMqMessage.Append(Message.MessageId.ToByteArray());
                netMqMessage.Append("OK");

                netMqMessage.Append(response.GetType().FullName);
                netMqMessage.Append(JsonConvert.SerializeObject(response, Formatting.Indented, JsonSettings.JsonSerializerSettings));

                socket.SendMultipartMessage(netMqMessage);
            }

            public void RespondError(Exception exception, RouterSocket socket)
            {
                var netMqMessage = new NetMQMessage();

                netMqMessage.Append(_client);
                netMqMessage.Append(Message.MessageId.ToByteArray());
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
                socket.SendMoreFrame(_messageId.ToByteArray());
                socket.SendMoreFrame(_messageType);
                socket.SendFrame(_messageBody);
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
