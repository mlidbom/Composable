using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Logging;
using Composable.System.Linq;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class ReadOrderImplementationExperimentTests
    {
        [Test]
        public void AlternateAlgorithmRecursed()
        {
            var before = SqlDecimal.Parse("9223372036854775806.999999999999998");
            var after =  SqlDecimal.Parse("9223372036854775806.999999999999999");

            SafeConsole.WriteLine($"before = {before}\nafter  = {after}");
            SafeConsole.WriteLine("999999999999999989".Length.ToString());

            var eventsToInsert = 999;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert);

            insertSortOrders.ForEach(insertSortOrder => SafeConsole.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
        }

        [Test]
        public void AlternateAlgorithmRecursedDeep()
        {
            var before = SqlDecimal.Parse("9223372036854775806.9999999999999998");
            var after = SqlDecimal.Parse("9223372036854775806.99999999999999999");

            SafeConsole.WriteLine($"before = {before}\nafter  = {after}");
            SafeConsole.WriteLine("999999999999999989".Length.ToString());

            var eventsToInsert = 99;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert);

            insertSortOrders.ForEach(insertSortOrder => SafeConsole.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
        }

        [Test]
        public void AlternateAlgorithmRecursedMaxDepth()
        {
            var before = SqlDecimal.Parse("9223372036854775806.9999999999999999997");
            var after =  SqlDecimal.Parse("9223372036854775806.9999999999999999999");

            SafeConsole.WriteLine($"before = {before}:{before.Scale}\nafter  = {after}:{after.Scale}");
            SafeConsole.WriteLine("999999999999999989".Length.ToString());

            var eventsToInsert = 1;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert);

            insertSortOrders.ForEach(insertSortOrder => SafeConsole.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
        }

        [Test]
        public void AlternateAlgorithmRecursedMaxDepthForced()
        {
            var before = SqlDecimal.Parse("9223372036854775806.99999999999999999");
            var after =  SqlDecimal.Parse("9223372036854775807.00000000000000000");

            SafeConsole.WriteLine($"before = {before}:{before.Scale}\nafter  = {after}:{after.Scale}");

            var eventsToInsert = 99;

            var insertSortOrders = GetSortOrdersBetween(before, after, eventsToInsert).ToList();


            insertSortOrders.ForEach(insertSortOrder => SafeConsole.WriteLine($"         {insertSortOrder} scale: {insertSortOrder.Scale} "));
        }

        static IEnumerable<SqlDecimal> GetSortOrdersBetween(SqlDecimal before, SqlDecimal after, long eventsToInsert)
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