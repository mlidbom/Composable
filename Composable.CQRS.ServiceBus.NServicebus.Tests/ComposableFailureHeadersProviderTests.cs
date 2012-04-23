using System;
using System.Diagnostics;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using NUnit.Framework;

namespace Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests
{
    [TestFixture]
    public class ComposableFailureHeadersProviderTests
    {
        private const string ExpectedStackTrace =
 @"RootException : Root exception message
   NestedException1 : NestedException1 exception message
      NestedException2 : NestedException2 exception message

NestedException2 : NestedException2 exception message
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel3() in ThisCodeFile.cs:line 81
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel2() in ThisCodeFile.cs:line 71
   ---End of inner stack trace---
NestedException1 : NestedException1 exception message
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel2() in ThisCodeFile.cs:line 77
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel1() in ThisCodeFile.cs:line 64
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.GenerateNestedException() in ThisCodeFile.cs:line 54
   ---End of inner stack trace---
RootException : Root exception message
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.GenerateNestedException() in ThisCodeFile.cs:line 60
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.FormatsNestedExceptionAsIntended() in ThisCodeFile.cs:line 35";

        [Test]
        public void FormatsNestedExceptionAsIntended()
        {
            var provider = new ComposableFailureHeadersProvider();
            try
            {
                GenerateNestedException();
            }
            catch (RootException e)
            {
                var headers = provider.GetExceptionHeaders(null, e);
                var stacktrace = headers[ComposableFailureHeadersProvider.ComposableStackTraceHeaderName];
                var sf = new StackTrace(0, true).GetFrame(0);

                string actualStackTrace = stacktrace.Replace(sf.GetFileName(), "ThisCodeFile.cs");
                Console.WriteLine(actualStackTrace);

                Assert.That(actualStackTrace, Is.EqualTo(ExpectedStackTrace));
            }
        }

        private void GenerateNestedException()
        {
            try
            {
                ThrowNestedLevel1();
            }
            catch (Exception e)
            {
                throw new RootException(e);
            }
        }

        private void ThrowNestedLevel1()
        {
            ThrowNestedLevel2();
        }

        private void ThrowNestedLevel2()
        {
            try
            {
                ThrowNestedLevel3();
            }
            catch (Exception e)
            {
                throw new NestedException1(e);
            }
        }

        private void ThrowNestedLevel3()
        {
            throw new NestedException2();
        }
    }

    public class RootException : Exception
    {
        public RootException(Exception inner)
            : base("Root exception message", inner)
        {

        }
    }

    public class NestedException1 : Exception
    {
        public NestedException1(Exception inner)
            : base("NestedException1 exception message", inner)
        {

        }
    }

    public class NestedException2 : Exception
    {
        public NestedException2()
            : base("NestedException2 exception message")
        {

        }
    }
}