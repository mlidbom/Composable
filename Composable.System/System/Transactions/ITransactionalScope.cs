using System;

namespace Composable.System.Transactions
{
    public interface ITransactionalScope : IDisposable
    {
        void Commit();        
    }
}