using System;
using Composable.UnitsOfWork;

namespace Composable.CQRS.UnitsOfWork
{
    class AttemptingToJoinSecondUnitOfWorkException : Exception
    {
        public AttemptingToJoinSecondUnitOfWorkException(IUnitOfWorkParticipant participant, UnitOfWork unitOfWork)
            :base($"{participant.GetType()} with Id: {participant.Id} tried to join Unit: {unitOfWork.Id} while participating in unit {participant.UnitOfWork}")
        {

        }
    }
}