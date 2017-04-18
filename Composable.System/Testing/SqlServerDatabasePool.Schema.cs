using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Testing
{
    static class DatabaseExtensions
    {
        internal static string Name(this Database @this) => $"{SqlServerDatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
    }

    [Serializable]
    public class Database
    {
        internal int Id { get; }
        internal bool IsReserved { get; set; }
        public DateTime ReservationDate { get; set; }


        public Database() { }
        internal Database(int id) => Id = id;
        internal Database(SqlServerDatabasePool pool, string name) : this(IdFromName(name)) { }

        static int IdFromName(string name)
        {
            var nameIndex = name.Replace(SqlServerDatabasePool.PoolDatabaseNamePrefix, "");
            return int.Parse(nameIndex);
        }
    }

    sealed partial class SqlServerDatabasePool
    {
        void CreateDatabase(string databaseName)
        {
            var createDatabaseCommand = $@"CREATE DATABASE [{databaseName}]";
            if(!DatabaseRootFolderOverride.IsNullOrWhiteSpace())
            {
                createDatabaseCommand += $@"
ON      ( NAME = {databaseName}_data, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.mdf') 
LOG ON  ( NAME = {databaseName}_log, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.ldf');";
            }
            _masterConnection.ExecuteNonQuery(createDatabaseCommand);

            _masterConnection.ExecuteNonQuery($"ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;");
            //SafeConsole.WriteLine($"Created: {databaseName}");
        }

        void DropAllAndStartOver(SharedState machineWide)
        {
            var dbsToDrop = ListPoolDatabases();

            foreach (var db in dbsToDrop)
            {
                var dropCommand = $"drop database [{db.Name()}]";
                try
                {
                    _masterConnection.ExecuteNonQuery(dropCommand);
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            }
            machineWide.Databases = new List<Database>();
            1.Through(30).ForEach(_ => InsertDatabase(machineWide));
        }

        List<Database> ListPoolDatabases()
        {
            var databases = new List<string>();
            _masterConnection.UseCommand(
                action: command =>
                        {
                            command.CommandText = "select name from sysdatabases";
                            using(var reader = command.ExecuteReader())
                            {
                                while(reader.Read())
                                {
                                    var dbName = reader.GetString(i: 0);
                                    if(dbName.StartsWith(PoolDatabaseNamePrefix))
                                        databases.Add(dbName);
                                }
                            }
                        });

            return databases.Select(name => new Database(this, name))
                            .ToList();
        }
    }
}