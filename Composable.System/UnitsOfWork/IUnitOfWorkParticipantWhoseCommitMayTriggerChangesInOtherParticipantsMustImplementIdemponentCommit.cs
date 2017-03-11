namespace Composable.UnitsOfWork
{
    interface IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit : IUnitOfWorkParticipant
    {
        bool CommitAndReportIfCommitMayHaveCausedChangesInOtherParticipantsExpectAnotherCommitSoDoNotLeaveUnitOfWork();
    }
}