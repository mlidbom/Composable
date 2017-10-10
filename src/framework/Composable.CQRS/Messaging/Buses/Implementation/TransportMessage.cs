using System;
using Composable.Contracts;
using Composable.NewtonSoft;
using Composable.System.Reflection;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Composable.Messaging.Buses.Implementation {
    class TransportMessage
    {
        public IMessage Message { get; }
        public byte[] Client { get; }

        TransportMessage(IMessage message, byte[] client)
        {
            Message = message;
            Client = client;
        }

        public static void Send(DealerSocket socket, IMessage message)
        {
            socket.SendMoreFrame(message.MessageId.ToByteArray());
            socket.SendMoreFrame(message.GetType().FullName);
            socket.SendFrame(JsonConvert.SerializeObject(message, Formatting.Indented, JsonSettings.JsonSerializerSettings));
        }

        public static Response ReadResponse(IReceivingSocket socket)
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

        public static TransportMessage ReadFromSocket(RouterSocket socket)
        {
            var receivedMessage = socket.ReceiveMultipartMessage();

            var client = receivedMessage[0].ToByteArray();
            var messageId = new Guid(receivedMessage[1].ToByteArray());
            var messageTypeString = receivedMessage[2].ConvertToString();
            var messageBody = receivedMessage[3].ConvertToString();
            var messageType = messageTypeString.AsType();

            var message = (IMessage)JsonConvert.DeserializeObject(messageBody, messageType, JsonSettings.JsonSerializerSettings);

            Contract.State.Assert(messageId == message.MessageId);

            return new TransportMessage(message, client);
        }

        public void RespondSucess(IMessage response, RouterSocket socket)
        {
            var netMqMessage = new NetMQMessage();

            netMqMessage.Append(Client);
            netMqMessage.Append(Message.MessageId.ToByteArray());
            netMqMessage.Append("OK");

            netMqMessage.Append(response.GetType().FullName);
            netMqMessage.Append(JsonConvert.SerializeObject(response, Formatting.Indented, JsonSettings.JsonSerializerSettings));

            socket.SendMultipartMessage(netMqMessage);
        }

        public void RespondError(Exception exception, RouterSocket socket)
        {
            var netMqMessage = new NetMQMessage();

            netMqMessage.Append(Client);
            netMqMessage.Append(Message.MessageId.ToByteArray());
            netMqMessage.Append("FAIL");

            socket.SendMultipartMessage(netMqMessage);
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

            public Response(bool successFull, Guid messageId, string resultJson, string responseType)
            {
                _resultJson = resultJson;
                _responseType = responseType;
                SuccessFull = successFull;
                MessageId = messageId;
            }
        }
    }
}