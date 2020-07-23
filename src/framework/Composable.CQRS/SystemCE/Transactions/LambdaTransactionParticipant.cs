using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.SystemCE.Collections;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using NotImplementedException = System.NotImplementedException;
#pragma warning disable CA1033 // Interface methods should be callable by child types

namespace Composable.SystemCE.Transactions
{
    public class LambdaTransactionParticipant : IEnlistmentNotification
    {
        public LambdaTransactionParticipant AddCommitTasks(params Action[] tasks)
        {
            _commitTasks.AddRange(tasks);
            return this;
        }
        public LambdaTransactionParticipant AddPrepareTasks(params Action[] tasks)
        {
            _prepareTasks.AddRange(tasks);
            return this;
        }
        public LambdaTransactionParticipant AddRollbackTasks(params Action[] tasks)
        {
            _rollbackTasks.AddRange(tasks);
            return this;
        }

        readonly List<Action> _rollbackTasks = new List<Action>();
        readonly List<Action> _commitTasks = new List<Action>();
        readonly List<Action> _prepareTasks = new List<Action>();

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _commitTasks.ForEach(@this => @this());
            enlistment.Done();
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            _prepareTasks.ForEach(@this => @this());
            preparingEnlistment.Prepared();
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _rollbackTasks.ForEach(@this => @this());
            enlistment.Done();
        }

        void IEnlistmentNotification.InDoubt(Enlistment enlistment) { throw new NotImplementedException(); }
    }

    static class LambdaTransactionParticipantExtensions
    {
        static readonly IThreadShared<Dictionary<string, LambdaTransactionParticipant>> Participants = ThreadShared<Dictionary<string, LambdaTransactionParticipant>>.Optimized();

        public static Transaction AddCommitTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddCommitTasks(actions));
        public static Transaction AddRollbackTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddRollbackTasks(actions));
        public static Transaction AddPrepareTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddPrepareTasks(actions));

        static Transaction UseParticipant(Transaction @this, Action<LambdaTransactionParticipant> action)
        {
            Participants.WithExclusiveAccess(participants =>
            {
                var participant = participants.GetOrAdd(@this.TransactionInformation.LocalIdentifier,
                                                        () =>
                                                        {
                                                            var created = new LambdaTransactionParticipant();
                                                            created
                                                               .AddCommitTasks(() => Participants.WithExclusiveAccess(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)))
                                                               .AddRollbackTasks(() => Participants.WithExclusiveAccess(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)));
                                                            @this.EnlistVolatile(created, EnlistmentOptions.None);
                                                            return created;
                                                        });

                action(participant);
            });
            return @this;
        }
    }
}
