using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NServiceBus.Faults;
using NServiceBus.Unicast.Transport;
using Composable.System;

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public class ComposableFailureHeadersProvider : IProvideFailureHeaders
    {
        public const string ComposableExceptionHeaderName = "Composable.Exception";
        public const string ComposableStackTraceHeaderName = "Composable.StackTrace";

        public IDictionary<string, string> GetExceptionHeaders(TransportMessage message, Exception e)
        {
            return new Dictionary<string, string>
                      {
                          { ComposableExceptionHeaderName , CreateExceptionNodes(e).ToString() },
                          { ComposableStackTraceHeaderName , CreateStackTrace(e)}
                      };
        }

        private string CreateStackTrace(Exception exception)
        {
            string indent = "";
            var stackTrace = new StringBuilder();
            var exceptions = GetNestedExceptionsList(exception).ToList();
            foreach (var currentException in exceptions)
            {
                stackTrace.AppendFormat("{0}{1} : {2}{3}", indent, currentException.GetType().Name, currentException.Message, Environment.NewLine);
                indent += "   ";
            }
            stackTrace.AppendLine();

            var combinedStacks = exceptions.AsEnumerable().Reverse()
                .Select(currentException => "{1} : {2}{0}{3}".FormatWith(Environment.NewLine, currentException.GetType().Name, currentException.Message, currentException.StackTrace))
                .Join("{0}   ---End of inner stack trace---{0}".FormatWith(Environment.NewLine));

            stackTrace.Append(combinedStacks);

            return stackTrace.ToString();

        }

        private XElement CreateExceptionNodes(Exception exception)
        {
            return new XElement("Exceptions",
                                GetNestedExceptionsList(exception)
                                    .Select(CreateExceptionNode).ToList()
                );
        }

        private static XElement CreateExceptionNode(Exception e)
        {
            return new XElement("Exception",
                                new XElement("Type", e.GetType()),
                                new XElement("HelpLink", e.HelpLink),
                                new XElement("Message", e.Message),
                                new XElement("Source", e.Source),
                                new XElement("StackTrace", e.StackTrace),
                                new XElement("TargetSite", e.TargetSite)
                );
        }

        private IEnumerable<Exception> GetNestedExceptionsList(Exception exception)
        {
            yield return exception;
            while (exception.InnerException != null)
            {
                yield return exception.InnerException;
                exception = exception.InnerException;
            }
        }
    }
}