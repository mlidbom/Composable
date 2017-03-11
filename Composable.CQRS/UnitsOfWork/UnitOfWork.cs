using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;

namespace Composable.CQRS.UnitsOfWork
{
    class UnitOfWork : IUnitOfWork
    {
        static readonly ILog Log = LogManager.GetLogger(typeof (UnitOfWork));
        readonly HashSet<IUnitOfWorkParticipant> _participants = new HashSet<IUnitOfWorkParticipant>();
        readonly ISingleContextUseGuard _usageGuard;
        const int MaxCascadeLevel = 10;

        public Guid Id { get; private set; }

        public UnitOfWork(ISingleContextUseGuard usageGuard)
        {
            _usageGuard = usageGuard;
            Id = Guid.NewGuid();
            Log.DebugFormat("Constructed {0}", Id);
        }

        public void AddParticipants(IEnumerable<IUnitOfWorkParticipant> unitOfWorkParticipants)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Log.Debug("Adding participants");
            unitOfWorkParticipants.ForEach(AddParticipant);
        }

        public void AddParticipant(IUnitOfWorkParticipant participant)
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


        public void Commit()
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
            return $"Unit of work {Id} with participants:\n {_participants.Select(p => $"{p.GetType()} {p.Id}") .Join("\n\t")}";
        }

        public void Rollback()
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
                            Log.Error("Swallowing exception thrown by participant {0} {1}.".FormatWith(participant.GetType(), participant.Id), e);
                        }
                    }
                );
        }
    }
}