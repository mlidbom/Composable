namespace Composable.Messaging.Commands
{
    public interface ISubCommand
    {
        string Name { get; }
        Command Command { get; }
    }
}
