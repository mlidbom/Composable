using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

#pragma warning disable 1591
namespace Composable.HyperBus.APIDraft
{
    public interface IHyperBus
    {
        Task<IMessageCascade<EmptyResource>> PublishAsync(IEvent @event);
        Task<IMessageCascade<EmptyResource>> ExecuteAsync(ICommand command);
        Task<IMessageCascade<TReturnValue>> ExecuteAsync<TReturnValue>(ICommand<TReturnValue> command);
        Task<IMessageCascade<TReturnValue>> GetAsync<TReturnValue>(IQuery<TReturnValue> query);
    }
}
