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

        public static Response ReadResponse(DealerSocket socket)
        {
            var message = socket.ReceiveMultipartMessage();
            var messageId = new Guid(message[0].ToByteArray());
            var result = message[1].ConvertToString();

            if(result == "OK")
            {
                var responseType = message[2].ConvertToString().AsType();
                var responseBody = message[3].ConvertToString();
                var responseObject = (IMessage)JsonConvert.DeserializeObject(responseBody, responseType, JsonSettings.JsonSerializerSettings);
                return new Response(successFull: true, messageId: messageId, result: responseObject);
            } else
            {
                return new Response(successFull: false, messageId: messageId, result: null);
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
            public bool SuccessFull { get; }
            public Guid MessageId { get; }
            public IMessage Result { get; }

            public Response(bool successFull, Guid messageId, IMessage result)
            {
                SuccessFull = successFull;
                MessageId = messageId;
                Result = result;
            }
        }
    }
}