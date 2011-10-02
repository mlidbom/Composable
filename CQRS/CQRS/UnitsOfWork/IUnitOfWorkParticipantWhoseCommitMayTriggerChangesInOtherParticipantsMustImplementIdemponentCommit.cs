namespace Composable.UnitsOfWork
{
    public interface IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit : IUnitOfWorkParticipant
    {
    }
}