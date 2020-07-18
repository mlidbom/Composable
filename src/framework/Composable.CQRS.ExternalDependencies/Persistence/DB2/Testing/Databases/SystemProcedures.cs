using System;
using System.Linq;
using Composable.Logging;
using Composable.Persistence.DB2.SystemExtensions;
using IBM.Data.DB2.Core;
// ReSharper disable StringLiteralTypo

namespace Composable.Persistence.DB2.Testing.Databases
{
    class SystemProcedures
    {
        //Urgent: Unless we create new stored procedures, get rid of this file.
        const string ObjectDoesNotExist = "42704";
        public static void CreateProcedures(ComposableDB2ConnectionProvider connection)
        {
            //drop if they exist. For new we ignore errors

            void DropObjectIgnoreNonExistence(string dropStatement)
            {
                try
                {
                    connection.ExecuteNonQuery(dropStatement);
                }
                catch(DB2Exception e)when(e.Errors.Cast<DB2Error>().Any(error => error.SQLState == ObjectDoesNotExist))
                {}
            }

            //DropObjectIgnoreNonExistence(@"DROP PROCEDURE CDBPOOL_DROP_SCHEMA_STATEMENTS");
        }
    }
}
