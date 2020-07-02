using System;
using System.Collections.Generic;
using System.Transactions;
using NotImplementedException = System.NotImplementedException;

namespace Composable.System.Transactions
{
    public class LambdaTransactionParticipant : IEnlistmentNotification
    {
        public void AddCommitTasks(params Action[] tasks) => _commitTasks.AddRange(tasks);
        public void AddPrepareTasks(params Action[] tasks) => _prepareTasks.AddRange(tasks);
        public void AddRollbackTasks(params Action[] tasks) => _rollbackTasks.AddRange(tasks);

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
}
