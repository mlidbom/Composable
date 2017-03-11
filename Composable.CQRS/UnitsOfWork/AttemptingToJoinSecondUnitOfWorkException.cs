using System;

namespace Composable.UnitsOfWork
{
    class AttemptingToJoinSecondUnitOfWorkException : Exception
    {
        public AttemptingToJoinSecondUnitOfWorkException(IUnitOfWorkParticipant participant, UnitsOfWork.UnitOfWork unitOfWork)
            :base(String.Format("{0} with Id: {1} tried to join Unit: {2} while participating in unit {3}",
                                participant.GetType(), participant.Id, unitOfWork.Id, participant.UnitOfWork))
        {

        }
    }
}