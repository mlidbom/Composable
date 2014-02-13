namespace Composable.UnitsOfWork
{
    public interface IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit : IUnitOfWorkParticipant
    {
        bool CommitAndReportIfCommitMayHaveCausedChangesInOtherParticipantsExpectAnotherCommitSoDoNotLeaveUnitOfWork();
    }
}