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
        static string ObjectDoesNotExist = "42704";
        public static void CreateProcedures(DB2ConnectionProvider connection)
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

            DropObjectIgnoreNonExistence(@"DROP FUNCTION X_DROP_LIST");
            DropObjectIgnoreNonExistence(@"DROP FUNCTION QUOTE_IDENTIFIER");
            DropObjectIgnoreNonExistence(@"DROP PROCEDURE DROP_SCHEMA");


            connection.ExecuteNonQuery(@"
CREATE FUNCTION QUOTE_IDENTIFIER(AIDENT VARCHAR(128))
    RETURNS VARCHAR(258)
    SPECIFIC QUOTE_IDENTIFIER1
    LANGUAGE SQL
    DETERMINISTIC
    NO EXTERNAL ACTION
    CONTAINS SQL
RETURN
    CASE
        WHEN AIDENT IS NULL THEN NULL
        WHEN
            TRANSLATE(SUBSTR(AIDENT, 1, 1),
                'XXXXXXXXXXXXXXXXXXXXXXXXXXXXX',
                'ABCDEFGHIJKLMNOPQRSTUVWXYZ#$@') ||
            TRANSLATE(SUBSTR(AIDENT, 2),
                'XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX',
                'ABCDEFGHIJKLMNOPQRSTUVWXYZ#$@_0123456789') =
            REPEAT('X', LENGTH(RTRIM(AIDENT)))
        THEN
            RTRIM(AIDENT)
        ELSE
            '""' || REPLACE(RTRIM(AIDENT), '""', '""""') || '""'
    END
");

            connection.ExecuteNonQuery(@"
-------------------------------------------------------------------------------
-- DROP SCHEMA UTILITY
-------------------------------------------------------------------------------
-- Copyright (c) 2005-2013 Dave Hughes <dave@waveform.org.uk>
--
-- Permission is hereby granted, free of charge, to any person obtaining a copy
-- of this software and associated documentation files (the ""Software""), to
-- deal in the Software without restriction, including without limitation the
-- rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
-- sell copies of the Software, and to permit persons to whom the Software is
-- furnished to do so, subject to the following conditions:
--
-- The above copyright notice and this permission notice shall be included in
-- all copies or substantial portions of the Software.
--
-- THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
-- IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
-- FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
-- AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
-- LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
-- FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
-- IN THE SOFTWARE.
-------------------------------------------------------------------------------


-- DROP_SCHEMA(ASCHEMA)
-------------------------------------------------------------------------------
-- DROP_SCHEMA is a utility procedure which drops all objects (tables, views,
-- triggers, sequences, aliases, etc.) in a schema and then drops the schema.
-- It is primarily intended to make destruction of user-owned schemas easier
-- (in the event that a user no longer requires access) but can also be used
-- to make writing upgrade scripts easier.
--
-- NOTE: this procedure is effectively redundant since DB2 9.5 which includes
-- the built-in procedure ADMIN_DROP_SCHEMA, albeit with a somewhat more
-- complicated calling convention.
-------------------------------------------------------------------------------

CREATE FUNCTION X_DROP_LIST(ASCHEMA VARCHAR(128))
    RETURNS TABLE(
        CREATE_TIME TIMESTAMP,
        DDL    VARCHAR(1000)
    )
    SPECIFIC X_DROP_LIST
    READS SQL DATA
    NOT DETERMINISTIC
    NO EXTERNAL ACTION
    LANGUAGE SQL
RETURN
    WITH DROP_LIST (CREATE_TIME, SCHEMA_NAME, DDL) AS (
        SELECT
            CREATE_TIME,
            TABSCHEMA AS SCHEMA_NAME,
            'DROP ' || CASE TYPE
                WHEN 'A' THEN 'ALIAS'
                WHEN 'H' THEN 'TABLE'
                WHEN 'N' THEN 'NICKNAME'
                WHEN 'S' THEN 'TABLE'
                WHEN 'T' THEN 'TABLE'
                WHEN 'U' THEN 'TABLE'
                WHEN 'V' THEN 'VIEW'
                WHEN 'W' THEN 'VIEW'
            END || ' ' || QUOTE_IDENTIFIER(TABSCHEMA) || '.' || QUOTE_IDENTIFIER(TABNAME) AS DDL
        FROM SYSCAT.TABLES
        UNION
        SELECT
            CREATE_TIME,
            TRIGSCHEMA AS SCHEMA_NAME,
            'DROP TRIGGER ' || QUOTE_IDENTIFIER(TRIGSCHEMA) || '.' || QUOTE_IDENTIFIER(TRIGNAME) AS DDL
        FROM SYSCAT.TRIGGERS
        UNION
        SELECT
            CREATE_TIME,
            ROUTINESCHEMA AS SCHEMA_NAME,
            'DROP ' || CASE ROUTINETYPE
                WHEN 'F' THEN 'SPECIFIC FUNCTION'
                WHEN 'M' THEN 'SPECIFIC METHOD'
                WHEN 'P' THEN 'SPECIFIC PROCEDURE'
            END || ' ' || QUOTE_IDENTIFIER(ROUTINESCHEMA) || '.' || QUOTE_IDENTIFIER(SPECIFICNAME) AS DDL
        FROM SYSCAT.ROUTINES
        UNION
        SELECT
            CREATE_TIME,
            TYPESCHEMA AS SCHEMA_NAME,
            'DROP TYPE ' || QUOTE_IDENTIFIER(TYPESCHEMA) || '.' || QUOTE_IDENTIFIER(TYPENAME) AS DDL
        FROM SYSCAT.DATATYPES
        UNION
        SELECT
            CREATE_TIME,
            SEQSCHEMA AS SCHEMA_NAME,
            'DROP SEQUENCE ' || QUOTE_IDENTIFIER(SEQSCHEMA) || '.' || QUOTE_IDENTIFIER(SEQNAME) AS DDL
        FROM SYSCAT.SEQUENCES
        WHERE SEQTYPE <> 'I'
        UNION
        SELECT
            CREATE_TIME,
            SCHEMANAME AS SCHEMA_NAME,
            'DROP SCHEMA ' || QUOTE_IDENTIFIER(SCHEMANAME) || ' RESTRICT' AS DDL
        FROM SYSCAT.SCHEMATA
    )
    SELECT CREATE_TIME, DDL
    FROM DROP_LIST
    WHERE SCHEMA_NAME = ASCHEMA
");

            connection.ExecuteNonQuery(@"
CREATE PROCEDURE DROP_SCHEMA(ASCHEMA VARCHAR(128))
    SPECIFIC DROP_SCHEMA1
    MODIFIES SQL DATA
    NOT DETERMINISTIC
    NO EXTERNAL ACTION
    LANGUAGE SQL
BEGIN ATOMIC
    FOR D AS
        SELECT DDL
        FROM TABLE(X_DROP_LIST(ASCHEMA))
        ORDER BY CREATE_TIME DESC
    DO
        EXECUTE IMMEDIATE D.DDL;
    END FOR;
END"
);
        }
    }
}
