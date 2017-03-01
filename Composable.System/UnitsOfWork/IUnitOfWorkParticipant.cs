using System;

namespace Composable.UnitsOfWork
{
    public interface IUnitOfWorkParticipant
    {
        IUnitOfWork UnitOfWork { get; }
        Guid Id { get; }
        void Join(IUnitOfWork unit);
        void Commit(IUnitOfWork unit);
        void Rollback(IUnitOfWork unit);
    }
}