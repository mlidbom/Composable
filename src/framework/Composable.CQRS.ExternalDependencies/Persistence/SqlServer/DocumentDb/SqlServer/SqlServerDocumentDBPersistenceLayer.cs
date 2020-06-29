using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System;

namespace Composable.Persistence.SqlServer.DocumentDb.SqlServer
{
    partial class SqlServerDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        readonly ISqlServerConnectionProvider _connectionProvider;
        readonly SchemaManager _schemaManager;
        bool _initialized;
        readonly object _lockObject = new object();
        const int UniqueConstraintViolationErrorNumber = 2627;

        internal SqlServerDocumentDbPersistenceLayer(ISqlServerConnectionProvider connectionProvider)
        {
            _schemaManager = new SchemaManager(connectionProvider);
            _connectionProvider = connectionProvider;
        }

        public void Update(IReadOnlyList<IDocumentDbPersistenceLayer.WriteRow> toUpdate)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            foreach(var writeRow in toUpdate)
            {
                connection.UseCommand(
                    command => command.SetCommandText("UPDATE Store SET Value = @Value, Updated = @Updated WHERE Id = @Id AND ValueTypeId = @TypeId")
                                      .AddNVarcharParameter("Id", 500, writeRow.IdString)
                                       .AddDateTime2Parameter("Updated", writeRow.UpdateTime)
                                      .AddParameter("TypeId", writeRow.TypeIdGuid)
                                      .AddNVarcharMaxParameter("Value", writeRow.SerializedDocument)
                                      .ExecuteNonQuery());
            }
        }

        public bool TryGet(string idString, IReadOnlyList<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? document)
        {
            EnsureInitialized();

            var lockHint = useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

            var documents = _connectionProvider.UseCommand(
                command => command.SetCommandText($@"
SELECT Value, ValueTypeId FROM Store {lockHint} 
WHERE Id=@Id AND ValueTypeId  {TypeInClause(acceptableTypeIds)}")
                                  .AddNVarcharParameter("Id", 500, idString)
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
                           .AddNVarcharParameter("Id", 500, idString)
                           .AddParameter("ValueTypeId", typeIdGuid)
                           .AddDateTime2Parameter("Created", now)
                           .AddDateTime2Parameter("Updated", now)
                           .AddNVarcharMaxParameter("Value", serializedDocument)
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

        public int Remove(string idString, IReadOnlyList<Guid> acceptableTypeIds)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command =>
                    command.SetCommandText($"DELETE Store WHERE Id = @Id AND ValueTypeId  {TypeInClause(acceptableTypeIds)}")
                           .AddNVarcharParameter("Id", 500, idString)
                           .ExecuteNonQuery());
        }

        public int RemoveAll(Guid typeIdGuid)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(command => command.SetCommandText("DELETE Store WHERE ValueTypeId = @TypeId")
                                                                    .AddParameter("TypeId", typeIdGuid)
                                                                    .ExecuteNonQuery());
        }

        public IEnumerable<Guid> GetAllIds(IReadOnlyList<Guid> acceptableTypeIds)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@"SELECT Id FROM Store WHERE ValueTypeId  {TypeInClause(acceptableTypeIds)}")
                                  .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0)))); //bug: Huh, we store string but require them to be Guid!?
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlyList<Guid> getAcceptableTypes)
        {
            EnsureInitialized();
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@"SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId {TypeInClause(getAcceptableTypes)} 
                                   AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadOnlyList<Guid> acceptableTypeIds)
        {
            return _connectionProvider.UseCommand(
                command => command.SetCommandText($@" SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId  {TypeInClause(acceptableTypeIds)}")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
        }

        static string TypeInClause(IEnumerable<Guid> acceptableTypeIds) { return "IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n"; }

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
