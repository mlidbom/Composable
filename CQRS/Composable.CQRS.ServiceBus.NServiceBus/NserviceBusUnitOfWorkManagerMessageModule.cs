#region usings

using System;
using System.Transactions;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.UnitsOfWork;
using NServiceBus;
using log4net;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NServiceBusUnitOfWorkManagerMessageModule : IMessageModule
    {
        private readonly IWindsorContainer _container;

        [ThreadStatic] private static UnitOfWork _unit;
        [ThreadStatic] private static IDisposable _disposableScope;

        private static readonly ILog Log = LogManager.GetLogger(typeof (NServiceBusUnitOfWorkManagerMessageModule));

        public NServiceBusUnitOfWorkManagerMessageModule(IWindsorContainer container)
        {
            Log.Debug("Constructor called");
            _container = container;
        }

        public void HandleBeginMessage()
        {
            Log.Debug("HandleBeginMessage called");

            Log.Debug("Creating windsor scope");
            _disposableScope = _container.Kernel.BeginScope();

            try
            {
                if (_unit != null)
                {
                    throw new UnitOfWorkNotDisposedException();
                }

                AssertAmbientTransactionPresent();
                _unit = new UnitOfWork();

                _unit.AddParticipants(_container.ResolveAll<IUnitOfWorkParticipant>());
            }
            catch (Exception e)
            {
                Log.Error("HandleBeginMessage failed", e);
                throw;
            }
        }

        public void HandleEndMessage()
        {
            Log.Debug("HandleEndMessage called");
            if (_unit == null)
            {
                Log.Debug("No unit present, bailing out.");
                return;
            }

            try
            {
                AssertAmbientTransactionPresent();
                _unit.Commit();
            }
            catch (Exception e)
            {
                Log.Error("HandleEndMessage failed rolling back", e);
                try
                {
                    _unit.Rollback();
                }catch(Exception e2)
                {
                    Log.Error("rolling back failed", e2);
                    throw;
                }
                throw;
            }
            finally
            {
                //The overly complex song and dance above is really to get safely to this line since nservicebus may call HandleEndMessage again without having called HandleBeginMessage.
                _unit = null;
                Log.Debug("Nulled out _unit");
                DisposeWindsorScope();
            }
        }

        public void HandleError()
        {
            Log.Debug("HandleError called");
            if(_unit == null)
            {
                Log.Debug("No unit present, bailing out.");
                return;
            }

            try
            {
                _unit.Rollback();
            }
            catch (Exception e)
            {
                Log.Error("HandleError failed", e);
                throw;
            }
            finally
            {
                //The overly complex song and dance above is really to get safely to this line since nservicebus may call HandleEndMessage method again without having called HandleBeginMessage.
                _unit = null;
                Log.Debug("Nulled out _unit");
                DisposeWindsorScope();
            }
        }

        private void DisposeWindsorScope()
        {
            if (_disposableScope != null)
            {
                Log.Debug("Disposing windsor scope");
                _disposableScope.Dispose();
                _disposableScope = null;
            }
            else
            {
                Log.Debug("No windsor scope to dispose");
            }
        }

        private static void AssertAmbientTransactionPresent()
        {
            if (Transaction.Current == null)
            {
                throw new NoAmbientTransactionException();
            }
        }
    }

    public class UnitOfWorkNotDisposedException : Exception
    {
        public UnitOfWorkNotDisposedException():base("The unit of work was not correctly disposed. Since NServicebus makes strange repeated calls to HandleEndMessage this may be fatal for transactionality")
        {
            
        }
    }

    public class NoAmbientTransactionException : Exception
    {
        public NoAmbientTransactionException() : base("Ambient transaction required")
        {
        }
    }
}