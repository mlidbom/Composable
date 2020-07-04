using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System;

namespace Composable.Persistence.MySql.DocumentDb
{
    partial class MySqlDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        readonly IMySqlConnectionProvider _connectionProvider;
        readonly SchemaManager _schemaManager;
        bool _initialized;
        readonly object _lockObject = new object();
        const int UniqueConstraintViolationErrorNumber = 2627;

        internal MySqlDocumentDbPersistenceLayer(IMySqlConnectionProvider connectionProvider)
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
                                          .AddVarcharParameter("Id", 500, writeRow.IdString)
                                          .AddDateTime2Parameter("Updated", writeRow.UpdateTime)
                                          .AddParameter("TypeId", writeRow.TypeIdGuid)
                                          .AddMediumTextParameter("Value", writeRow.SerializedDocument)
                                          .ExecuteNonQuery());
                }
            });
        }

        public bool TryGet(string idString, IReadOnlyList<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? document)
        {
            EnsureInitialized();

            var documents = _connectionProvider.UseCommand(
                command => command.SetCommandText($@"
SELECT Value, ValueTypeId FROM Store {UseUpdateLock(useUpdateLock)} 
WHERE Id=@Id AND ValueTypeId  {TypeInClause(acceptableTypeIds)}")
                                  .AddVarcharParameter("Id", 500, idString)
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(1), reader.GetString(0))));
            if(documents.Count < 1)
            {
                document = null;
                return false;
            }

            document = documents[0];

            return true;
        }

        public void Add(string idString, Guid typeIdGuid, DateTime now, string serializedDocument)
        {
            EnsureInitialized();
            try
            {
                _connectionProvider.UseCommand(command =>
                {

                    command.SetCommandText(@"INSERT INTO Store(Id, ValueTypeId, Value, Created, Updated) VALUES(@Id, @ValueTypeId, @Value, @Created, @Updated)")
                           .AddVarcharParameter("Id", 500, idString)
                           .AddParameter("ValueTypeId", typeIdGuid)
                           .AddDateTime2Parameter("Created", now)
                           .AddDateTime2Parameter("Updated", now)
                           .AddMediumTextParameter("Value", serializedDocument)
                           .ExecuteNonQuery();
                });
            }
            catch(SqlException e)
            {
                if(e.Number == UniqueConstraintViolationErrorNumber)
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(idString, serializedDocument);
                }

                throw;
            }
        }

        public int Remove(string idString, IReadOnlyList<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command =>
                    command.SetCommandText($"DELETE Store WHERE Id = @Id AND ValueTypeId  {TypeInClause(acceptableTypes)}")
                           .AddVarcharParameter("Id", 500, idString)
                           .ExecuteNonQuery());
        }

        public IEnumerable<Guid> GetAllIds(IReadOnlyList<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@"SELECT Id FROM Store WHERE ValueTypeId  {TypeInClause(acceptableTypes)}")
                                  .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0)))); //bug: Huh, we store string but require them to be Guid!?
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlyList<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@"SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId {TypeInClause(acceptableTypes)} 
                                   AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadOnlyList<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@" SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId  {TypeInClause(acceptableTypes)}")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
        }

        static string TypeInClause(IEnumerable<Guid> acceptableTypeIds) { return "IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n"; }

        static string UseUpdateLock(bool useUpdateLock) => useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

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
