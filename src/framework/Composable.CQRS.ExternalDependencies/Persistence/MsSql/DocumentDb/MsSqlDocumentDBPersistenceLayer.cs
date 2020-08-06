using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Schema = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.MsSql.DocumentDb
{
    partial class MsSqlDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        readonly IMsSqlConnectionPool _connectionPool;
        readonly SchemaManager _schemaManager;
        bool _initialized;

        internal MsSqlDocumentDbPersistenceLayer(IMsSqlConnectionPool connectionPool)
        {
            _schemaManager = new SchemaManager(connectionPool);
            _connectionPool = connectionPool;
        }

        public void Update(IReadOnlyList<IDocumentDbPersistenceLayer.WriteRow> toUpdate)
        {
            EnsureInitialized();
            _connectionPool.UseConnection(connection =>
            {
                foreach(var writeRow in toUpdate)
                {
                    connection.UseCommand(
                        command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                          .AddNVarcharParameter(Schema.Id, 500, writeRow.Id)
                                          .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                          .AddParameter(Schema.ValueTypeId, writeRow.TypeId)
                                          .AddNVarcharMaxParameter(Schema.Value, writeRow.SerializedDocument)
                                          .ExecuteNonQuery());
                }
            });
        }

        public bool TryGet(string idString, IReadonlySetCEx<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? document)
        {
            EnsureInitialized();

            var documents = _connectionPool.UseCommand(
                command => command.SetCommandText($@"
SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} {UseUpdateLock(useUpdateLock)} 
WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId}  {TypeInClause(acceptableTypeIds)}")
                                  .AddNVarcharParameter(Schema.Id, 500, idString)
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(1), reader.GetString(0))));
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
                _connectionPool.UseCommand(command =>
                {

                    command.SetCommandText($@"INSERT INTO {Schema.TableName}({Schema.Id}, {Schema.ValueTypeId}, {Schema.Value}, {Schema.Created}, {Schema.Updated}) VALUES(@{Schema.Id}, @{Schema.ValueTypeId}, @{Schema.Value}, @{Schema.Created}, @{Schema.Updated})")
                           .AddNVarcharParameter(Schema.Id, 500, row.Id)
                           .AddParameter(Schema.ValueTypeId, row.TypeId)
                           .AddDateTime2Parameter(Schema.Created, row.UpdateTime)
                           .AddDateTime2Parameter(Schema.Updated, row.UpdateTime)
                           .AddNVarcharMaxParameter(Schema.Value, row.SerializedDocument)
                           .ExecuteNonQuery();
                });
            }
            catch(SqlException exception)when(SqlExceptions.MsSql.IsPrimaryKeyViolation(exception))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
            }
        }

        public int Remove(string idString, IReadonlySetCEx<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionPool.UseCommand(
                command =>
                    command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .AddNVarcharParameter(Schema.Id, 500, idString)
                           .ExecuteNonQuery());
        }

        public IEnumerable<Guid> GetAllIds(IReadonlySetCEx<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionPool.UseCommand(
                command => command.SetCommandText($@"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                                  .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0)))); //bug: Huh, we store string but require them to be Guid!?
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadonlySetCEx<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionPool.UseCommand(
                command => command.SetCommandText($@"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                   AND {Schema.Id} IN('" + ids.Select(id => id.ToString()).Join("','") + "')")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadonlySetCEx<Guid> acceptableTypes)
        {
            EnsureInitialized();
            return _connectionPool.UseCommand(
                command => command.SetCommandText($@"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                                  .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
        }

        static string TypeInClause(IEnumerable<Guid> acceptableTypeIds) { return "IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n"; }

        static string UseUpdateLock(bool useUpdateLock) => useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

        readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
        void EnsureInitialized() => _monitor.Update(() =>
        {
            if(!_initialized)
            {
                _schemaManager.EnsureInitialized();
                _initialized = true;
            }
        });
    }
}
