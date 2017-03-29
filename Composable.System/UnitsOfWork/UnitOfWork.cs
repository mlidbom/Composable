using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Logging;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.UnitsOfWork
{
    sealed class UnitOfWork : IUnitOfWork
    {
        static readonly ILogger Log = Logger.For<UnitOfWork>();
        readonly HashSet<IUnitOfWorkParticipant> _participants = new HashSet<IUnitOfWorkParticipant>();
        readonly ISingleContextUseGuard _usageGuard;
        const int MaxCascadeLevel = 10;

        readonly Guid _id;
        Guid IUnitOfWork.Id => _id;
        readonly IUnitOfWork _this;

        internal static IUnitOfWork Create(ISingleContextUseGuard usageGuard) => new UnitOfWork(usageGuard);

        UnitOfWork(ISingleContextUseGuard usageGuard)
        {
            _usageGuard = usageGuard;
            _id = Guid.NewGuid();
            _this = this;
            Log.DebugFormat("Constructed {0}", _id);
        }

        void IUnitOfWork.AddParticipants(IEnumerable<IUnitOfWorkParticipant> unitOfWorkParticipants)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Log.Debug("Adding participants");
            unitOfWorkParticipants.ForEach(_this.AddParticipant);
        }

        void IUnitOfWork.AddParticipant(IUnitOfWorkParticipant participant)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Log.DebugFormat("Adding participant {0} {1}", participant.GetType(), participant.Id);
            if(participant.UnitOfWork != null && participant.UnitOfWork != this)
            {
                throw new AttemptingToJoinSecondUnitOfWorkException(participant, this);
            }
            participant.Join(this);
            _participants.Add(participant);
        }


        void IUnitOfWork.Commit()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Log.Debug("Commit");
            var cascadingParticipants = _participants.OfType<IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit>().ToList();

            var cascadeLevel = 0;
            while(cascadingParticipants.Select(s => s.CommitAndReportIfCommitMayHaveCausedChangesInOtherParticipantsExpectAnotherCommitSoDoNotLeaveUnitOfWork())
                .Where(result => result).Any())
            {
                if(++cascadeLevel > MaxCascadeLevel)
                {
                    throw new TooDeepCascadeLevelDetected(MaxCascadeLevel);
                }
            } //Loop until no changes may have occurred

            _participants.ForEach(participant => participant.Commit(this));
        }

        public override string ToString()
        {
            return $"Unit of work {_this.Id} with participants:\n {_participants.Select(p => $"{p.GetType()} {p.Id}") .Join("\n\t")}";
        }

        void IUnitOfWork.Rollback()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Log.Debug("Rollback");
            _participants.ForEach(
                participant =>
                    {
                        try
                        {
                            participant.Rollback(this);
                        }catch(Exception e)
                        {
                            Log.Error(e, $"Swallowing exception thrown by participant {participant.GetType()} {participant.Id}.");
                        }
                    }
                );
        }
    }
}