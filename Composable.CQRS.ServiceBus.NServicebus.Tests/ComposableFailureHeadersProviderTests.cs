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
 @"Exception:Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.NestedException2
Message:NestedException2 exception message

   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel3() in ThisCodeFile.cs:line 85
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel2() in ThisCodeFile.cs:line 75
   ---End of inner stack trace---

Exception:Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.NestedException1
Message:NestedException1 exception message

   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel2() in ThisCodeFile.cs:line 81
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.ThrowNestedLevel1() in ThisCodeFile.cs:line 68
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.GenerateNestedException() in ThisCodeFile.cs:line 58
   ---End of inner stack trace---

Exception:Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.RootException
Message:Root exception message

   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.GenerateNestedException() in ThisCodeFile.cs:line 64
   at Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests.ComposableFailureHeadersProviderTests.FormatsNestedExceptionAsIntended() in ThisCodeFile.cs:line 39";

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