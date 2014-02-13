using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Composable.CQRS.Testing
{
    public static class DatabaseHelpers
    {
        private static IEnumerable<string> CreateDropAllObjectsStatements(IDbCommand cmd)
        {
            cmd.CommandText = @"SELECT stmt FROM (SELECT CASE WHEN type = 'AF'                           THEN 'DROP AGGREGATE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type IN('C', 'F', 'UQ')               THEN 'ALTER TABLE ' + QUOTENAME(object_schema_name(parent_object_id)) + '.' + QUOTENAME(object_name(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name)
                                                              WHEN type IN('FN', 'FS', 'FT', 'IF', 'TF') THEN 'DROP FUNCTION ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type IN('P', 'PC')                    THEN 'DROP PROCEDURE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'R'                            THEN 'DROP RULE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'SN'                           THEN 'DROP SYNONYM ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'U'                            THEN 'DROP TABLE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type IN('TA', 'TR')                   THEN 'DROP TRIGGER ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'V'                            THEN 'DROP VIEW ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                         END stmt, type
                                                    FROM sys.objects) x
                                 WHERE stmt IS NOT NULL
                                 ORDER BY CASE WHEN type IN('C', 'F', 'UQ', 'TA', 'TR') THEN 1 ELSE 2 END";

            var result = new List<string>();
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    result.Add((string)rdr[0]);
                }
            }
            return result;
        }

        private static void ExecuteDropStatements(IEnumerable<string> stmts, IDbCommand cmd)
        {
            var list = stmts.ToList();
            Exception innerEx = null;
            for (;;)
            {
                bool didSomething = false;
                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        cmd.CommandText = list[i];
                        cmd.ExecuteNonQuery();
                        // drop was successful
                        list.RemoveAt(i);
                        i--; // Retry next entry
                        didSomething = true;
                    }
                    catch (SqlException ex)
                    {
                        if (i == 0)
                            innerEx = ex;   // This is the exception we will later raise from this method in case we, for one reason or another, can't drop all objects.
                    }
                }
                if (!didSomething)
                    break;
            }
            if (list.Count > 0)
                throw new Exception(string.Format("Could not execute statement '{0}'", list[0]), innerEx);
        }

        public static void DropAllObjects(this IDbConnection connection) {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                var stmts = CreateDropAllObjectsStatements(cmd);
                ExecuteDropStatements(stmts, cmd);
            }
        }
    }
}
