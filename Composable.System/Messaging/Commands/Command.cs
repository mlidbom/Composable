namespace Composable.Messaging.Commands
{
  using Composable.DDD;

  using global::System;

  public class Command : ValueObject<Command>, ICommand
  {
    public Guid Id { get; set; }

    protected Command()
      : this(Guid.NewGuid()) { }

    protected Command(Guid id) { Id = id; }
  }
}
