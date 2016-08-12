using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace CQRS.Tests
{
    public class TemporaryLocalDbManager : IDisposable
    {
        private readonly string _masterConnectionString;
        private readonly SqlServerConnectionUtilities _masterConnection; 

        public TemporaryLocalDbManager(string masterConnectionString, IWindsorContainer container = null)
        {
            _masterConnectionString = masterConnectionString; 
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);
            if(container != null)
            {
                RegisterWithContainer(container);
            }
        }

        public void RegisterWithContainer(IWindsorContainer container)
        {
            container.Register(Component.For<TemporaryLocalDbManager>().UsingFactoryMethod(() => this));//Register and resolve instance once so that it is disposed with the container
            container.Resolve<TemporaryLocalDbManager>();
        }

        private static readonly string DbDirectory = $"{nameof(TemporaryLocalDbManager)}_Databases";

        private readonly Dictionary<string, ManagedLocalDb> _managedDatabases = new Dictionary<string, ManagedLocalDb>();
        private bool _disposed;        

        public string CreateOrGetLocalDb(string dbName)
        {
            Contract.Assert(!_disposed, "Attempt to use disposed object");
            if (!_managedDatabases.ContainsKey(dbName))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DbDirectory);
                var dbFileName = $"{dbName}_{Guid.NewGuid()}.mdf";
                var dbFullFileName = Path.Combine(outputFolder, dbFileName);
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{dbFileName}] ON (NAME = N'{dbFileName}', FILENAME = '{dbFullFileName}')");

                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString);
                sqlConnectionStringBuilder.InitialCatalog = dbFileName;

                _managedDatabases.Add(dbName, new ManagedLocalDb(name: dbFileName, fileName: dbFullFileName, connectionString: sqlConnectionStringBuilder.ConnectionString));
            }

            return _managedDatabases[dbName].ConnectionString;
        }

        private void DropDatabase(ManagedLocalDb db)
        {
            using(var conn = new SqlConnection(db.ConnectionString))
            {
                SqlConnection.ClearPool(conn);
            }
            
            _masterConnection.ExecuteNonQuery($@"
                  alter database [{db.Name}] set single_user with rollback immediate
                  drop database [{db.Name}]");
        }

        public void Dispose()
        {
            InternalDispose();
            GC.SuppressFinalize(this);
        }

        protected virtual void InternalDispose()
        {
            if (!_disposed)
            {
                _managedDatabases.Values.ForEach(DropDatabase);
                _disposed = true;
            }
        }

        ~TemporaryLocalDbManager()
        {
            InternalDispose();
        }

        private class ManagedLocalDb
        {
            public string Name { get; }
            public string FileName { get; }
            public string ConnectionString { get; }
            public ManagedLocalDb(string name, string fileName, string connectionString)
            {
                Name = name;
                FileName = fileName;
                ConnectionString = connectionString;
            }
        }
    }
}