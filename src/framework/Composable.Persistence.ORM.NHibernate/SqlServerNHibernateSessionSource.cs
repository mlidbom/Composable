using System.Collections.Generic;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

namespace Composable.Persistence.ORM.NHibernate
{
    public class SqlServerNHibernateSessionSource : INHibernateSessionSource
    {
        private readonly ISessionFactory _factory;
        public SqlServerNHibernateSessionSource(string connectionString, IEnumerable<Assembly> mappingAssemblies)
        {
            var configuration = new Configuration();
            configuration.DataBaseIntegration(cfg =>
                                              {
                                                  cfg.ConnectionString = connectionString;
                                                  cfg.Driver<Sql2008ClientDriver>();
                                                  cfg.Dialect<MsSql2008Dialect>();
                                                  cfg.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
                                                  cfg.SchemaAction = SchemaAutoAction.Update;
                                              });

            configuration.AddCodeMappingsFromAssemblies(mappingAssemblies);

            SchemaMetadataUpdater.QuoteTableAndColumns(configuration);

            _factory = configuration.BuildSessionFactory();
        }

        public ISession OpenSession() { return _factory.OpenSession(); }
    }
}