using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

#pragma warning disable CA1033 // Interface methods should be callable by child types

namespace Composable.SystemCE.TransactionsCE
{
    ///<summary>Getting the code for participating in a transaction right is surprisingly tricky and the failures very hard to diagnose.
    /// Use this class for all our transaction participants so we only have to get it right once.</summary>
    class VolatileLambdaTransactionParticipant : IEnlistmentNotification
    {
        readonly EnlistmentOptions _enlistmentOptions;
        readonly Action? _onEnlist;
        readonly Action<TransactionStatus>? _onTransactionCompleted;
        internal VolatileLambdaTransactionParticipant(EnlistmentOptions enlistmentOptions = EnlistmentOptions.None,
                                                      Action? onPrepare = null,
                                                      Action? onCommit = null,
                                                      Action? onRollback = null,
                                                      Action? onEnlist = null,
                                                      Action<TransactionStatus>? onTransactionCompleted = null)
        {
            _enlistmentOptions = enlistmentOptions;
            _onEnlist = onEnlist;
            _onTransactionCompleted = onTransactionCompleted;

            EnsureParticipatingInAnyCurrentTransaction();

            if(onPrepare != null) AddPrepareTasks(onPrepare);
            if(onCommit != null) AddCommitTasks(onCommit);
            if(onRollback != null) AddRollbackTasks(onRollback);
        }

        internal VolatileLambdaTransactionParticipant AddCommitTasks(params Action[] tasks)
        {
            _commitTasks.AddRange(tasks);
            return this;
        }

        internal VolatileLambdaTransactionParticipant AddPrepareTasks(params Action[] tasks)
        {
            _prepareTasks.AddRange(tasks);
            return this;
        }

        internal VolatileLambdaTransactionParticipant AddRollbackTasks(params Action[] tasks)
        {
            _rollbackTasks.AddRange(tasks);
            return this;
        }

        readonly List<Action> _rollbackTasks = new List<Action>();
        readonly List<Action> _commitTasks = new List<Action>();
        readonly List<Action> _prepareTasks = new List<Action>();

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _commitTasks.InvokeAll();
            enlistment.Done();
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                if(_enlistmentOptions.HasFlag(EnlistmentOptions.EnlistDuringPrepareRequired))
                {
                    using var transactionScope = new TransactionScope(_participatingIn!);
                    _prepareTasks.InvokeAll();
                    transactionScope.Complete();
                } else
                {
                    _prepareTasks.InvokeAll();
                }

                preparingEnlistment.Prepared();
            }
            catch(Exception exception)
            {
                preparingEnlistment.ForceRollback(exception);
            }
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _rollbackTasks.InvokeAll();
            enlistment.Done();
        }

        void IEnlistmentNotification.InDoubt(Enlistment enlistment) => enlistment.Done();

        Transaction? _participatingIn;
        internal void EnsureParticipatingInAnyCurrentTransaction()
        {
            var ambientTransaction = Transaction.Current;
            if(ambientTransaction == null) return;

            if(_participatingIn == null)
            {
                _participatingIn = ambientTransaction;
                _onEnlist?.Invoke();
                ambientTransaction.EnlistVolatile(this, _enlistmentOptions);
                if(_onTransactionCompleted != null)
                {
                    ambientTransaction.TransactionCompleted += (transaction, parameters) => _onTransactionCompleted(parameters.Transaction.TransactionInformation.Status);
                }
            } else if(_participatingIn != ambientTransaction)
            {
                throw new Exception($"Somehow switched to a new transaction. Original: {_participatingIn.TransactionInformation.LocalIdentifier} new: {ambientTransaction.TransactionInformation.LocalIdentifier}");
            }
        }
    }

    static class LambdaTransactionParticipantExtensions
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
                                                        () => new VolatileLambdaTransactionParticipant(
                                                                  onCommit:() => Participants.WithExclusiveAccess(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)),
                                                                  onRollback:() => Participants.WithExclusiveAccess(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier))));

                action(participant);
            });
            return @this;
        }
    }
}
