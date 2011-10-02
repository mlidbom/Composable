using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.System.Linq;
using log4net;
using Composable.System;

namespace Composable.UnitsOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (UnitOfWork));
        private readonly HashSet<IUnitOfWorkParticipant> _participants = new HashSet<IUnitOfWorkParticipant>();
        public const int MaxCascadeLevel = 10;

        public Guid Id { get; private set; }

        public UnitOfWork()
        {            
            Id = Guid.NewGuid();
            Log.DebugFormat("Constructed {0}", Id);
        }

        public void AddParticipants(IEnumerable<IUnitOfWorkParticipant> unitOfWorkParticipants)
        {
            Log.Debug("Adding participants");
            unitOfWorkParticipants.ForEach(AddParticipant);
        }

        public void AddParticipant(IUnitOfWorkParticipant participant)
        {
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
            } //Loop until no changes may have occured

            _participants.ForEach(participant => participant.Commit(this));
        }

        public override string ToString()
        {
            return String.Format("Unit of work {0} with participants:\n {1}", Id, _participants.Select(p => String.Format("{0} {1}", p.GetType(), p.Id)).Join("\n\t"));
        }

        public void Rollback()
        {
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