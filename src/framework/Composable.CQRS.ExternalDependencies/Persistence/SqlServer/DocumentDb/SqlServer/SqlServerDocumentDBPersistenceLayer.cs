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
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText += "UPDATE Store SET Value = @Value, Updated = @Updated WHERE Id = @Id AND ValueTypeId = @TypeId";
                command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = writeRow.IdString});
                command.Parameters.Add(new SqlParameter("Updated", SqlDbType.DateTime2) {Value = writeRow.UpdateTime});
                command.Parameters.Add(new SqlParameter("TypeId", SqlDbType.UniqueIdentifier) {Value = writeRow.TypeIdGuid});

                command.Parameters.Add(new SqlParameter("Value", SqlDbType.NVarChar, -1) {Value = writeRow.SerializedDocument});
                command.ExecuteNonQuery();
            }
        }

        public bool TryGet(string idString, IReadOnlyList<Guid> getAcceptableTypes, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? document)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            var lockHint = useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";
            command.CommandText = $@"
SELECT Value, ValueTypeId FROM Store {lockHint} 
WHERE Id=@Id AND ValueTypeId
";

            command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = idString});

            Storage(command, getAcceptableTypes);

            using var reader = command.ExecuteReader();
            if(!reader.Read())
            {
                document = null;
                return false;
            }

            document = new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(1), reader.GetString(0));

            return true;
        }

        public void Add(string idString, Guid typeIdGuid, DateTime now, string serializedDocument)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;

            command.CommandText += @"INSERT INTO Store(Id, ValueTypeId, Value, Created, Updated) VALUES(@Id, @ValueTypeId, @Value, @Created, @Updated)";

            command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = idString});
            command.Parameters.Add(new SqlParameter("ValueTypeId", SqlDbType.UniqueIdentifier) {Value = typeIdGuid});
            command.Parameters.Add(new SqlParameter("Created", SqlDbType.DateTime2) {Value = now});
            command.Parameters.Add(new SqlParameter("Updated", SqlDbType.DateTime2) {Value = now});

            command.Parameters.Add(new SqlParameter("Value", SqlDbType.NVarChar, -1) {Value = serializedDocument});

            try
            {
                command.ExecuteNonQuery();
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
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText += "DELETE Store WHERE Id = @Id AND ValueTypeId ";
            command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = idString});

            Storage(command, acceptableTypeIds);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected;
        }

        public int RemoveAll(Guid typeIdGuid)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText += "DELETE Store WHERE ValueTypeId = @TypeId";
            command.Parameters.Add(new SqlParameter("TypeId", SqlDbType.UniqueIdentifier) {Value = typeIdGuid});

            return command.ExecuteNonQuery();
        }

        public IEnumerable<Guid> GetAllIds(IReadOnlyList<Guid> acceptableTypeIds)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @"SELECT Id FROM Store WHERE ValueTypeId ";

            Storage(loadCommand, acceptableTypeIds);

            //bug: Huh, we store string but require them to be Guid!?
            return loadCommand.ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0)));
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlyList<Guid> getAcceptableTypes)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @"SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId ";

            Storage(loadCommand, getAcceptableTypes);

            loadCommand.CommandText += " AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')";

            var storedList = loadCommand.ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1)));
            return storedList;
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadOnlyList<Guid> acceptableTypeIds)
        {
            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @" SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId ";

            Storage(loadCommand, acceptableTypeIds);

            var storedList = loadCommand.ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1)));
            return storedList;
        }

        static void Storage(SqlCommand command, IReadOnlyList<Guid> acceptableTypeIds)
        {
            command.CommandText += "IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n";
        }

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
