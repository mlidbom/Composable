using System;
using System.Collections.Generic;

namespace Composable.UnitsOfWork
{
    interface IUnitOfWork
    {
        Guid Id { get; }
        void AddParticipants(IEnumerable<IUnitOfWorkParticipant> unitOfWorkParticipants);
        void AddParticipant(IUnitOfWorkParticipant participant);
        void Commit();
        void Rollback();
    }
}