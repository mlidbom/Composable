namespace Composable.Messaging.Commands
{
    interface ISubCommand
    {
        string Name { get; }
        Command Command { get; }
    }
}
