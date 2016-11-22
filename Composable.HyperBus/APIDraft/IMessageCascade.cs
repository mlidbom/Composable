using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Composable.HyperBus.APIDraft
{
    public interface IMessageCascade
    {
        IHyperBus Bus { get; }
        Task<IEnumerable<IMessage>> GetParentMessages();
        IMessage Message { get; }
    }

    public interface IMessageCascade<TReturnValue> : IMessageCascade
    {
        Task<TReturnValue> GetReturnValue();
        Task<ICompletedMessageCascade<TReturnValue>> RunToEndOfActivation(IEnumerable<Guid> excludedEndpoints = null, IEnumerable<Guid> includedEndpoints = null);
    }

    public interface ICompletedMessageCascade<TReturnValue> : IMessageCascade<TReturnValue>
    {
        Task<IEnumerable<IMessage>> GetChildMessages();
    }
}