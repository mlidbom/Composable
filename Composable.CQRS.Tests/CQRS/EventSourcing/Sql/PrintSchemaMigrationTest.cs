using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Composable.CQRS.EventSourcing.SQLServer;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class PrintSchemaMigrationTest
    {
        [Test]
        public void PrintMigrationScript()
        {
            Console.WriteLine(new LegacyEventTableSchemaManager().ActualMigrationScript);
        }


        [Test]
        public void AlternateAlgorithm()
        {
            var before = new Decimal(long.MaxValue - 1);
            var after = new Decimal(long.MaxValue);

            Console.WriteLine(after);

            var diff = after - before;
            var eventsToInsert = 1000;

            var increment = diff / eventsToInsert;

            for(decimal readOrder = before + increment; readOrder < after; readOrder += increment)
            {
                Console.WriteLine(readOrder);
            }
        }



        [Test]
        public void AlternateAlgorithmRecursed()
        {
            var before = SqlDecimal.Parse("9223372036854775806.999999999999998");
            var after =  SqlDecimal.Parse("9223372036854775806.999999999999999");

            Console.WriteLine($"before = {before}\nafter  = {after}");
            Console.WriteLine("999999999999999989".Length);

            var eventsToInsert = 999;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert);


            foreach(var insertSortOrder in insertSortOrders)
            {
                Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} ");
            }
        }

        [Test]
        public void AlternateAlgorithmRecursedDeep()
        {
            var before = SqlDecimal.Parse("9223372036854775806.9999999999999998");
            var after = SqlDecimal.Parse("9223372036854775806.99999999999999999");

            Console.WriteLine($"before = {before}\nafter  = {after}");
            Console.WriteLine("999999999999999989".Length);

            var eventsToInsert = 99;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert);


            foreach (var insertSortOrder in insertSortOrders)
            {
                Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} ");
            }
        }

        [Test]
        public void AlternateAlgorithmRecursedMaxDepth()
        {
            var before = SqlDecimal.Parse("9223372036854775806.9999999999999999997");
            var after =  SqlDecimal.Parse("9223372036854775806.9999999999999999999");

            Console.WriteLine($"before = {before}:{before.Scale}\nafter  = {after}:{after.Scale}");
            Console.WriteLine("999999999999999989".Length);

            var eventsToInsert = 1;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert);


            foreach (var insertSortOrder in insertSortOrders)
            {
                Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} ");
            }
        }

        [Test]
        public void AlternateAlgorithmRecursedMaxDepthForced()
        {
            var before = SqlDecimal.Parse("9223372036854775806.99999999999999999");
            var after =  SqlDecimal.Parse("9223372036854775807.00000000000000000");

            Console.WriteLine($"before = {before}:{before.Scale}\nafter  = {after}:{after.Scale}");

            var eventsToInsert = 99;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert).ToList();


            foreach (var insertSortOrder in insertSortOrders)
            {
                Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} ");
            }
        }




        private IEnumerable<SqlDecimal> GetSortOrdersBetween(SqlDecimal before, SqlDecimal after, long eventsToInsert)
        {
            before = SqlDecimal.ConvertToPrecScale(before, 38, 19);//Unless we convert to the correct precision we will not be able to use the full resolution
            after = SqlDecimal.ConvertToPrecScale(after, 38, 19);
            var diff = after - before;
            var increment = diff / (eventsToInsert + 1);
            if(increment == 0)
            {
                throw new Exception("Resolution Failure 1");
            }
            var previousSortOrder = before;
            long insertedEvents = 0;

            for(var sortOrder = before + increment; sortOrder < after && insertedEvents < eventsToInsert; sortOrder += increment, insertedEvents++)
            {
                yield return sortOrder;
                if(sortOrder == previousSortOrder)
                {
                    throw new Exception("Resolution failure 2");
                }
                previousSortOrder = sortOrder;
            }

            if(insertedEvents != eventsToInsert)
            {
                throw new Exception($"Should have generated {eventsToInsert} values but generated {insertedEvents} values.");
            }
        }



        [Test]
        public void TestDecimalResolution()
        {
            var before = new Decimal(long.MaxValue -1);
            var after = new Decimal(long.MaxValue);

            Console.WriteLine(after);
            Console.WriteLine(before);
            Console.WriteLine(after.ToString().Length);

            long numberOfInsertedRows = 0;
            var middle = before;

            do
            {
                middle = (after + middle)/2;
                numberOfInsertedRows++;
                Console.WriteLine(middle);

            } while(middle != after);

            Console.WriteLine(numberOfInsertedRows);

            Console.WriteLine(999999999);
            Console.WriteLine(10e8-1);

            Console.WriteLine(new decimal(
                    0,
                    0,
                    0,
                    false,
                    8));
        }
    }


    class DecimalCtorIIIBByDemo
    {
        // Get the exception type name; remove the namespace prefix.
        public static string GetExceptionType(Exception ex)
        {
            string exceptionType = ex.GetType().ToString();
            return exceptionType.Substring(
                exceptionType.LastIndexOf('.') + 1);
        }

        // Create a decimal object and display its value.
        public static void CreateDecimal(int low, int mid, int high, bool isNeg, byte scale)
        {
            // Format the constructor for display.
            string ctor = String.Format("decimal( {0}, {1}, {2}, {3}, {4} )", low, mid, high, isNeg, scale);
            string valOrExc;

            try
            {
                // Construct the decimal value.
                decimal decimalNum = new decimal(low, mid, high, isNeg, scale);

                // Format and save the decimal value.
                valOrExc = decimalNum.ToString();
            }
            catch (Exception ex)
            {
                // Save the exception type if an exception was thrown.
                valOrExc = GetExceptionType(ex);
            }

            // Display the constructor and decimal value or exception.
            int ctorLen = 76 - valOrExc.Length;
            
            if(ctorLen > ctor.Length)// Display the data on one line if it will fit.
            {
                Console.WriteLine("{0}{1}", ctor.PadRight(ctorLen), valOrExc);
            }else// Otherwise, display the data on two lines.
            {
                Console.WriteLine("{0}", ctor);
                Console.WriteLine("{0,76}", valOrExc);
            }
        }

        [Test]
        public void DemoDecimal()
        {

            Console.WriteLine("This example of the decimal( int, int, int, bool, byte ) \nconstructor generates the following output.\n");
            Console.WriteLine("{0,-38}{1,38}", "Constructor", "Value or Exception"); 
            Console.WriteLine("{0,-38}{1,38}", "-----------", "------------------");

            // Construct decimal objects from the component fields.
            //CreateDecimal(0, 0, 0, false, 0);
            //CreateDecimal(0, 0, 0, false, 27);
            //CreateDecimal(0, 0, 0, true, 0);
            //CreateDecimal(1000000000, 0, 0, false, 0);
            //CreateDecimal(0, 1000000000, 0, false, 0);
            //CreateDecimal(0, 0, 1000000000, false, 0);
            //CreateDecimal(1000000000, 1000000000, 1000000000, false, 0);
            //CreateDecimal(-1, -1, -1, false, 0);
            //CreateDecimal(-1, -1, -1, true, 0);
            //CreateDecimal(-1, -1, -1, false, 15);
            //CreateDecimal(-1, -1, -1, false, 28);
            //CreateDecimal(-1, -1, -1, false, 29);
            //CreateDecimal(int.MaxValue, 0, 0, false, 18);
            //CreateDecimal(int.MaxValue, 0, 0, false, 28);
            //CreateDecimal(int.MaxValue, 0, 0, true, 28);

            //CreateDecimal(int.MaxValue, int.MaxValue, int.MaxValue, false, 18);
            //CreateDecimal(int.MaxValue, int.MaxValue, int.MaxValue, false, 28);
            //CreateDecimal(int.MaxValue, 0, int.MaxValue, false, 28);

            //CreateDecimal(int.MaxValue, 0, int.MaxValue, false, 28);

            Console.WriteLine("39614081247908796757769715711".Length);

            Console.WriteLine(long.MaxValue);
            Console.WriteLine(new decimal(long.MaxValue));

            for (byte scale = 0; scale <= 28; scale++)
            {
                Console.WriteLine( new decimal(
                    int.MaxValue,
                    int.MaxValue,
                    int.MaxValue,
                    false,
                    scale));
            }
        }
    }
}