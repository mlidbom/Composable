using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.NewtonSoft;
using Composable.System;
using Composable.System.Linq;
using Newtonsoft.Json;
using log4net;
using Composable.System.Reflection;
using Composable.System.Collections.Collections;

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerObjectStore : IObjectStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerObjectStore));

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
                                                                   {

                                                                       TypeNameHandling = TypeNameHandling.Auto,
                                                                       ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                       ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                   };

        internal readonly SqlServerDocumentDb _store;
        internal readonly SqlServerDocumentDbConfig _config;

        private readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();
        private const int UniqueConstraintViolationErrorNumber = 2627;

        private static readonly ISet<Type> KnownTypes = new HashSet<Type>();

        private static readonly object LockObject = new object();

        public SqlServerObjectStore(SqlServerDocumentDb store)
        {
            _store = store;
            _config = _store.Config;

            EnsureInitialized(_store.ConnectionString);
        }


        public bool TryGet<TValue>(object key, out TValue value)
        {
            EnsureTypeRegistered(typeof(TValue));
            value = default(TValue);

            object found;
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    string lockHint = DocumentDbSession.UseUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";
                    command.CommandText = @"
SELECT Value, ValueType FROM Store {0} INNER JOIN
ValueType ON Store.ValueTypeId = ValueType.Id
WHERE Store.Id=@Id AND ValueTypeId 
".FormatWith(lockHint);
                    command.Parameters.Add(new SqlParameter("Id", key.ToString()));

                    AddTypeCriteria(command, typeof(TValue));

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }
                        var stringValue = reader.GetString(0);
                        found = JsonConvert.DeserializeObject(stringValue, reader.GetString(1).AsType(), _jsonSettings);
                        _persistentValues.GetOrAddDefault(found.GetType())[key.ToString()] = stringValue;
                    }
                }
            }

            value = (TValue)found;
            return true;
        }

        public void Add<T>(object id, T value)
        {
            EnsureTypeRegistered(value.GetType());
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;

                    command.CommandText += @"
DECLARE @ValueTypeId int

IF NOT EXISTS(SELECT Id FROM ValueType WHERE ValueType = @ValueType)
	BEGIN
		INSERT INTO ValueType(ValueType)Values(@ValueType)
		SET @ValueTypeId = @@IDENTITY
	END
ELSE
	BEGIN
		SELECT @ValueTypeId = Id FROM ValueType WHERE ValueType = @ValueType
	END
	
INSERT INTO Store(Id, ValueTypeId, Value) VALUES(@Id, @ValueTypeId, @Value)";

                    command.Parameters.Add(new SqlParameter("Id", id.ToString()));
                    command.Parameters.Add(new SqlParameter("ValueType", value.GetType().FullName));

                    var stringValue = JsonConvert.SerializeObject(value, _config.JSonFormatting, _jsonSettings);
                    command.Parameters.Add(new SqlParameter("Value", stringValue));

                    _persistentValues.GetOrAddDefault(value.GetType())[id.ToString()] = stringValue;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        if (e.Number == UniqueConstraintViolationErrorNumber)
                        {
                            throw new AttemptToSaveAlreadyPersistedValueException(id, value);
                        }
                        throw;
                    }
                }
            }
        }

        public bool Remove<T>(object id)
        {
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Store WHERE Id = @Id AND ValueTypeId ";
                    command.Parameters.Add(new SqlParameter("Id", id.ToString()));

                    AddTypeCriteria(command, typeof(T));

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 1)
                    {
                        throw new TooManyItemsDeletedException();
                    }
                    return rowsAffected > 0;
                }
            }
        }

        public void Update(IEnumerable<KeyValuePair<string, object>> values)
        {
            values = values.ToList();
            using (var connection = OpenSession())
            {
                foreach (var entry in values)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        var stringValue = JsonConvert.SerializeObject(entry.Value, _config.JSonFormatting, _jsonSettings);

                        string oldValue;
                        var needsUpdate = !_persistentValues.GetOrAddDefault(entry.Value.GetType()).TryGetValue(entry.Key, out oldValue) || stringValue != oldValue;
                        if (needsUpdate)
                        {
                            _persistentValues.GetOrAddDefault(entry.Value.GetType())[entry.Key] = stringValue;
                            command.CommandText += "UPDATE Store SET Value = @Value WHERE Id = @Id AND ValueTypeId \n";
                            command.Parameters.Add(new SqlParameter("Id", entry.Key));

                            AddTypeCriteria(command, entry.Value.GetType());

                            command.Parameters.Add(new SqlParameter("Value", stringValue));
                        }
                        if (!command.CommandText.IsNullOrWhiteSpace())
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }


        IEnumerable<KeyValuePair<Guid, T>> IObjectStore.GetAll<T>()
        {
            if (KnownTypes.None(t => typeof(T).IsAssignableFrom(t)))
            {
                yield break;
            }

            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = @"
SELECT Store.Id, Value, ValueType 
FROM Store INNER JOIN
ValueType ON Store.ValueTypeId = ValueType.Id
WHERE ValueTypeId ";

                    AddTypeCriteria(loadCommand, typeof(T));

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return
                                new KeyValuePair<Guid, T>(Guid.Parse(reader.GetString(0)),
                                (T)JsonConvert.DeserializeObject(reader.GetString(1), reader.GetString(2).AsType(), _jsonSettings));
                        }
                    }
                }
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        private SqlConnection OpenSession()
        {
            return OpenSession(_store.ConnectionString);
        }

        private static SqlConnection OpenSession(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private void EnsureTypeRegistered(Type type)
        {
            lock (LockObject)
            {
                if (!KnownTypes.Contains(type))
                {
                    KnownTypes.Add(type);
                }
            }
        }

        private void AddTypeCriteria(SqlCommand command, Type type)
        {
            lock (LockObject)
            {
                var acceptableTypeNames = KnownTypes.Where(type.IsAssignableFrom).Select(t => t.FullName).ToArray();
                if (acceptableTypeNames.None())
                {
                    throw new Exception("Type: {0} is not among the known types".FormatWith(type.FullName));
                }

                command.CommandText += "IN(Select Id from ValueType where ValueType IN( '" + acceptableTypeNames.Join("','") + "'))\n";
            }
        }

        private static void EnsureInitialized(string connectionString)
        {
            lock (LockObject)
            {
                if (!VerifiedTables.Contains(connectionString))
                {
                    using (var connection = OpenSession(connectionString))
                    {
                        using (var checkForValueTypeCommand = connection.CreateCommand())
                        {
                            checkForValueTypeCommand.CommandText = "select count(*) from sys.tables where name = 'ValueType'";
                            var valueTypeExists = (int)checkForValueTypeCommand.ExecuteScalar();
                            if (valueTypeExists == 0)
                            {
                                using (var createValueTypeCommand = connection.CreateCommand())
                                {
                                    createValueTypeCommand.CommandText = @"
CREATE TABLE [dbo].[ValueType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ValueType] [varchar](500) NOT NULL,
 CONSTRAINT [PK_ValueType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";
                                    createValueTypeCommand.ExecuteNonQuery();
                                }
                                //Check if Store Exists without dbo.ValueType then we need to modify the Store table
                                using (var checkForStoreCommand = connection.CreateCommand())
                                {
                                    checkForStoreCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                                    var exists = (int)checkForStoreCommand.ExecuteScalar();
                                    if (exists > 0)
                                    {
                                        using(var alterStoreCommand = connection.CreateCommand())
                                        {
                                            alterStoreCommand.CommandTimeout = 60*30; //30 Minutes ;-)
                                            alterStoreCommand.CommandText = @"
--Create Keys
INSERT INTO ValueType(ValueType) 
SELECT DISTINCT ValueType FROM Store;
--Add column
ALTER TABLE Store
ADD ValueTypeId int;
--Insert Keys
UPDATE Store
SET ValueTypeId = (SELECT Id FROM ValueType WHERE ValueType = Store.ValueType)
--Add Constraints
ALTER TABLE [dbo].[Store]
ALTER COLUMN [ValueTypeId] INT NOT NULL

ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_ValueType_Store] FOREIGN KEY([ValueTypeId])
REFERENCES [dbo].[ValueType] ([Id])

ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_ValueType_Store]
--Change PK
ALTER TABLE [dbo].[Store]
DROP CONSTRAINT PK_Store

DROP INDEX IX_ValueType
ON [dbo].[Store]

ALTER TABLE [dbo].[Store] 
ADD CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[ValueTypeId] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
--Drop column
ALTER TABLE [dbo].[Store]
DROP COLUMN ValueType
";
                                            alterStoreCommand.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }
                        using (var checkForStoreCommand = connection.CreateCommand())
                        {
                            checkForStoreCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                            var exists = (int)checkForStoreCommand.ExecuteScalar();
                            if (exists == 0)
                            {
                                using (var createStoreCommand = connection.CreateCommand())
                                {

                                    createStoreCommand.CommandText =
                                        @"
CREATE TABLE [dbo].[Store](
	[Id] [nvarchar](500) NOT NULL,
	[ValueTypeId] [int] NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[ValueTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_ValueType_Store] FOREIGN KEY([ValueTypeId])
REFERENCES [dbo].[ValueType] ([Id])

ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_ValueType_Store]

";
                                    createStoreCommand.ExecuteNonQuery();
                                }
                            }
                            VerifiedTables.Add(connectionString);
                        }
                        using (var findTypesCommand = connection.CreateCommand())
                        {
                            findTypesCommand.CommandText = "SELECT DISTINCT ValueType FROM ValueType";
                            using (var reader = findTypesCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    KnownTypes.Add(reader.GetString(0).AsType());
                                }
                            }
                        }
                    }
                }
            }
        }



        private static readonly HashSet<String> VerifiedTables = new HashSet<String>();

        public static void PurgeDb(string connectionString)
        {
            lock (LockObject)
            {
                using (var connection = OpenSession(connectionString))
                {
                    using (var dropCommand = connection.CreateCommand())
                    {
                        dropCommand.CommandText =
                            @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store];
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ValueType]') AND type in (N'U'))
DROP TABLE [dbo].[ValueType];
";

                        dropCommand.ExecuteNonQuery();

                        VerifiedTables.Remove(connectionString);
                        EnsureInitialized(connectionString);
                    }
                }
            }
        }
    }
}