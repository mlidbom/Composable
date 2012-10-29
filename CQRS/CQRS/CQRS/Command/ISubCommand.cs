namespace Composable.CQRS.Command
{
    public interface ISubCommand {
        string Name { get; }
        Command Command { get; }
    }
}