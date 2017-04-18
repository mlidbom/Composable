using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        [Serializable]
        class SharedState
        {
            internal void Release(string name)
            {
                Get(name).IsReserved = false;
            }

            internal bool TryReserve(out Database reserved)
            {
                reserved = Databases.FirstOrDefault(db => db.IsReserved == false);
                if(reserved == null)
                {
                    return false;
                }

                reserved.IsReserved = true;
                reserved.ReservationDate = DateTime.UtcNow;
                return true;
            }

            internal Database Insert(SqlServerDatabasePool pool)
            {
                var newId = Databases.Any() ? Databases.Max(db => db.Id) + 1 : 1;
                var database = new Database(newId);
                Databases.Add(database);
                return database;
            }

            internal IReadOnlyList<Database> DbsWithOldLocks() => Databases.Where(db => db.ReservationDate < DateTime.UtcNow - 10.Minutes())
                                                                  .ToList();

            Database Get(string name) => Databases.Single(db => db.Name() == name);

            internal List<Database> Databases { get; set; }
        }
    }
}
