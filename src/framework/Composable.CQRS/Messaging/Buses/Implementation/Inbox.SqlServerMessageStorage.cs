using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        public interface IMessageStorage
        {
            void SaveIncomingMessage(TransportMessage.InComing message);
            void MarkAsSucceeded(TransportMessage.InComing message);
            void RecordException(TransportMessage.InComing message, Exception exception );
            void MarkAsFailed(TransportMessage.InComing message);
            Task StartAsync();
        }
    }
}
