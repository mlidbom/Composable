using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.System;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.DB2.DocumentDb
{
    partial class DB2DocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        readonly IComposableDB2ConnectionProvider _connectionProvider;
        readonly SchemaManager _schemaManager;
        bool _initialized;
        readonly object _lockObject = new object();
        const int UniqueConstraintViolationErrorNumber = 2627;

        internal DB2DocumentDbPersistenceLayer(IComposableDB2ConnectionProvider connectionProvider)
        {
            _schemaManager = new SchemaManager(connectionProvider);
            _connectionProvider = connectionProvider;
        }

        public void Update(IReadOnlyList<IDocumentDbPersistenceLayer.WriteRow> toUpdate)
        {
            EnsureInitialized();
            _connectionProvider.UseConnection(connection =>
            {
                foreach(var writeRow in toUpdate)
                {
                    connection.UseCommand(
                        command => command.SetCommandText("UPDATE Store SET Value = @Value, Updated = @Updated WHERE Id = @Id AND ValueTypeId = @TypeId")
                                          .AddVarcharParameter("Id", 500, writeRow.Id)
                                          .AddParameter("Updated", writeRow.UpdateTime)
                                          .AddParameter("TypeId", writeRow.TypeId)
                                          .AddNClobParameter("Value", writeRow.SerializedDocument)
                                          .ExecuteNonQuery());
                }
            });
        }

        public bool TryGet(string idString, IImmutableSet<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? document)
        {
            EnsureInitialized();

            //urgent: check if db2 does array(or whatever it might be called) parameters. Same for other storage providers. Properly parameterizing this might significantly help performance
            var documents = _connectionProvider.UseCommand(
                command => command.SetCommandText($@"
SELECT Value, ValueTypeId FROM Store {UseUpdateLock(useUpdateLock)} 
WHERE Id=@Id AND ValueTypeId  {TypeInClause(acceptableTypeIds)}")
                                  .AddVarcharParameter("Id", 500, idString)
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuidFromString(1), reader.GetString(0))));
            if(documents.Count < 1)
            {
                document = null;
                return false;
            }

            document = documents[0];

            return true;
        }

        public void Add(IDocumentDbPersistenceLayer.WriteRow row)
        {
            EnsureInitialized();
            try
            {
                _connectionProvider.UseCommand(command =>
                {
                    command.SetCommandText(@"INSERT INTO Store(Id, ValueTypeId, Value, Created, Updated) VALUES(@Id, @ValueTypeId, @Value, @Created, @Updated)")
                           .AddVarcharParameter("Id", 500, row.Id)
                           .AddParameter("ValueTypeId", row.TypeId)
                           .AddParameter("Created", row.UpdateTime)
                           .AddParameter("Updated", row.UpdateTime)
                           .AddNClobParameter("Value", row.SerializedDocument)
                           .ExecuteNonQuery();
                });
            }
            //Urgent: This is not the type or ErrorNumber likely to be used by DB2. Should this be kept around? Even in the MSSql code? It is not tested!
            catch(SqlException e)
            {
                if(e.Number == UniqueConstraintViolationErrorNumber)
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
                }

                throw;
            }
        }

        public int Remove(string idString, IImmutableSet<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command =>
                    command.SetCommandText($@"DELETE FROM Store WHERE Id = @Id AND ValueTypeId  {TypeInClause(acceptableTypes)}")
                           .AddVarcharParameter("Id", 500, idString)
                           .ExecuteNonQuery());
        }

        public IEnumerable<Guid> GetAllIds(IImmutableSet<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@"SELECT Id FROM Store WHERE ValueTypeId  {TypeInClause(acceptableTypes)}")
                                  .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0)))); //bug: Huh, we store string but require them to be Guid!?
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IImmutableSet<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@"SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId {TypeInClause(acceptableTypes)} 
                                   AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuidFromString(2), reader.GetString(1))));
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IImmutableSet<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@" SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId  {TypeInClause(acceptableTypes)}")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuidFromString(2), reader.GetString(1))));
        }

        static string TypeInClause(IEnumerable<Guid> acceptableTypeIds) { return "IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')"; }

        //Urgent: Figure out db2 equivalent.
        static string UseUpdateLock(bool _) => "";// useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

        void EnsureInitialized()
        {
            lock(_lockObject)
            {
                if(!_initialized)
                {
                    _schemaManager.EnsureInitialized();
                    _initialized = true;
                }
            }
        }
    }
}