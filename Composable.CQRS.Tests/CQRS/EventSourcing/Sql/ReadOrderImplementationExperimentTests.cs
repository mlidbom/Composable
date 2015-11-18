using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class ReadOrderImplementationExperimentTests
    {
        [Test]
        public void PrintMigrationScript()
        {
            Console.WriteLine(new LegacyEventTableSchemaManager().ActualMigrationScript);
        }

        [Test]
        public void PrintCreateReadOrdersSql()
        {
            Console.WriteLine(MicrosoftSqlServerEventStore.EnsurePersistedMigrationsHaveConsistentReadOrdersAndEffectiveVersionsSql);
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

            insertSortOrders.ForEach(insertSortOrder => Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
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

            insertSortOrders.ForEach(insertSortOrder => Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
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

            insertSortOrders.ForEach(insertSortOrder => Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
        }

        [Test]
        public void AlternateAlgorithmRecursedMaxDepthForced()
        {
            var before = SqlDecimal.Parse("9223372036854775806.99999999999999999");
            var after =  SqlDecimal.Parse("9223372036854775807.00000000000000000");

            Console.WriteLine($"before = {before}:{before.Scale}\nafter  = {after}:{after.Scale}");

            var eventsToInsert = 99;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert).ToList();


            insertSortOrders.ForEach(insertSortOrder => Console.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
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
    }    
}