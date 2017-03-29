using System;

namespace Composable.UnitsOfWork
{
    class AttemptingToJoinSecondUnitOfWorkException : Exception
    {
        public AttemptingToJoinSecondUnitOfWorkException(IUnitOfWorkParticipant participant, IUnitOfWork unitOfWork)
            :base($"{participant.GetType()} with Id: {participant.Id} tried to join Unit: {unitOfWork.Id} while participating in unit {participant.UnitOfWork}")
        {

        }
    }
}