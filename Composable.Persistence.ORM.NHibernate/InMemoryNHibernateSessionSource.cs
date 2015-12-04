using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

namespace Composable.Persistence.ORM.NHibernate
{
    public class InMemoryNHibernateSessionSource : INHibernateSessionSource, IDisposable
    {
        private readonly ISessionFactory _factory;
        private readonly IDbConnection _connection;
        private readonly ISession _connectionCreatingSessionThatShouldNotBeDisposedUntilTestHasRunToEnd;

        public InMemoryNHibernateSessionSource(IEnumerable<Assembly> mappingAssemblies)
        {
            var configuration = new Configuration();
            configuration.DataBaseIntegration(cfg =>
                                              {
                                                  cfg.ConnectionString = "FullUri=file:memorydb.db?mode=memory&cache=shared";
                                                  cfg.Driver<SQLite20Driver>();
                                                  cfg.Dialect<SQLiteDialect>();
                                                  cfg.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
                                                  cfg.SchemaAction = SchemaAutoAction.Update;
                                              });

            configuration.AddCodeMappingsFromAssemblies(mappingAssemblies);

            SchemaMetadataUpdater.QuoteTableAndColumns(configuration);

            _factory = configuration.BuildSessionFactory();

            _connectionCreatingSessionThatShouldNotBeDisposedUntilTestHasRunToEnd = _factory.OpenSession();
            _connection = _connectionCreatingSessionThatShouldNotBeDisposedUntilTestHasRunToEnd.Connection;

            new SchemaExport(configuration).Execute(
                              script: false,
                              export: true,
                              justDrop: false,
                              connection: _connection,
                              exportOutput: null);
        }

        public ISession OpenSession()
        {
            return _factory.OpenSession(_connection);
        }

        public void Dispose()
        {
            _connectionCreatingSessionThatShouldNotBeDisposedUntilTestHasRunToEnd.Dispose();    
        }
    }
}

