using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Testing
{
    static class DatabaseExtensions
    {
        internal static string Name(this SqlServerDatabasePool.Database @this) => $"{SqlServerDatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this SqlServerDatabasePool.Database @this, SqlServerDatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    sealed partial class SqlServerDatabasePool
    {
        [Serializable]
        internal class Database : IBinarySerializeMySelf
        {
            internal int Id { get; private set; }
            internal bool IsReserved { get; private set; }
            public DateTime ReservationDate { get; private set; }
            internal string ReservationName { get; private set; }

            internal Database() { }
            internal Database(int id) => Id = id;
            internal Database(string name) : this(IdFromName(name)) { }

            static int IdFromName(string name)
            {
                var nameIndex = name.Replace(PoolDatabaseNamePrefix, "");
                return int.Parse(nameIndex);
            }

            internal void Release()
            {
                Contract.Assert.That(IsReserved, "IsReserved");
                IsReserved = false;
                ReservationDate = DateTime.MaxValue;
                ReservationName = string.Empty;
            }

            internal void Reserve(string reservationName)
            {
                Contract.Assert.That(!IsReserved, "!IsReserved");
                IsReserved = true;
                ReservationName = reservationName;
                ReservationDate = DateTime.UtcNow;
            }

            public void Deserialize(BinaryReader reader)
            {
                Id = reader.ReadInt32();
                IsReserved = reader.ReadBoolean();
                ReservationDate = DateTime.FromBinary(reader.ReadInt64());
                ReservationName = reader.ReadString();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Id);
                writer.Write(IsReserved);
                writer.Write(ReservationDate.ToBinary());
                writer.Write(ReservationName);
            }
        }

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

        void RebootPoolIfNotAlreadyRebooted()
        {
            _log.Warning("Rebooting if required");
            TransactionScopeCe.SupressAmbient(
                () =>
                    _machineWideState.Update(
                        machineWide =>
                        {
                            lock(RebootedMasterConnections)
                            {
                                if(!RebootedMasterConnections.Contains(_masterConnectionString))
                                {
                                    RebootPool(machineWide);
                                } else
                                {
                                    _log.Warning("Skipped rebooting");
                                }
                            }
                        }));
        }

        void RebootPool(SharedState machineWide)
        {
            lock(RebootedMasterConnections)
            {
                _log.Warning("Rebooting database pool");
                _reservedDatabases.Values.ForEach(db => db.Release());

                var dbsToDrop = ListPoolDatabases();

                _log.Warning("Dropping databases");
                foreach(var db in dbsToDrop)
                {
                    var dropCommand = $"drop database [{db.Name()}]";
                    _log.Info(dropCommand);
                    _masterConnection.ExecuteNonQuery(dropCommand);
                }

                machineWide.Reset();

                _log.Warning("Creating new databases");

                //1.Through(30)
                // .ForEach(_ => InsertDatabase(machineWide));

                RebootedMasterConnections.Add(_masterConnectionString);
            }
        }

        IReadOnlyList<Database> ListPoolDatabases()
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

            return databases.Select(name => new Database(name))
                            .ToList();
        }
    }
}