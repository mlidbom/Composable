using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;
using log4net;
using Composable.System;

namespace Composable.UnitsOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (UnitOfWork));
        private readonly HashSet<object> _participants = new HashSet<object>();
        public Guid Id { get; private set; }

        public UnitOfWork()
        {
            Log.Debug("Constructed");
            Id = Guid.NewGuid();
        }

        public void AddParticipants(IEnumerable<IUnitOfWorkParticipant> unitOfWorkParticipants)
        {
            Log.Debug("Adding participants");
            unitOfWorkParticipants.ForEach(AddParticipant);
        }

        public void AddParticipant(IUnitOfWorkParticipant participant)
        {
            Log.DebugFormat("Adding participant {0}", participant.Id);
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
            _participants.Cast<IUnitOfWorkParticipant>().ForEach(participant => participant.Commit(this));
        }

        public override string ToString()
        {
            return String.Format("Unit of work {0} with participants: {1}", Id, _participants.Select(p => p.ToString()).Join("\n\t"));
        }

        public void Rollback()
        {
            Log.Debug("Rollback");
            _participants.Cast<IUnitOfWorkParticipant>().ForEach(
                participant =>
                    {
                        try
                        {
                            participant.Rollback(this);
                        }catch(Exception e)
                        {
                            Log.Error("Swallowing exception thrown by participant {0}.".FormatWith(participant.Id), e);
                        }
                    }
                );
        }
    }
}