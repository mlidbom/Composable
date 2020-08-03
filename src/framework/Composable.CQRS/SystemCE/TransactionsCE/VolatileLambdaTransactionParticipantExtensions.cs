using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE.TransactionsCE
{
    static class VolatileLambdaTransactionParticipantExtensions
    {
        static readonly IThreadShared<Dictionary<string, VolatileLambdaTransactionParticipant>> Participants = ThreadShared<Dictionary<string, VolatileLambdaTransactionParticipant>>.Optimized();

        public static Transaction AddCommitTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddCommitTasks(actions));
        public static Transaction AddRollbackTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddRollbackTasks(actions));
        public static Transaction AddPrepareTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddPrepareTasks(actions));

        static Transaction UseParticipant(Transaction @this, Action<VolatileLambdaTransactionParticipant> action)
        {
            Participants.WithExclusiveAccess(participants =>
            {
                var participant = participants.GetOrAdd(@this.TransactionInformation.LocalIdentifier,
                                                        () =>
                                                        {
                                                            var participant = new VolatileLambdaTransactionParticipant(
                                                                onCommit: () => Participants.WithExclusiveAccess(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)),
                                                                onRollback: () => Participants.WithExclusiveAccess(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)));
                                                            participant.EnsureEnlistedInAnyAmbientTransaction();
                                                            return participant;
                                                        });

                action(participant);
            });
            return @this;
        }
    }
}
