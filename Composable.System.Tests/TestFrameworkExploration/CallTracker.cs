using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;

namespace Composable.Tests.TestFrameworkExploration
{
    class CallTracker
    {
        Stack<string> calls;
        StringBuilder log = new StringBuilder();
        string Indent(string call)
        {
            if (call.StartsWith(Class_context.Name))
                return "";

            if (call.StartsWith(Outer_context.Name))
                return call.StartsWith($"{Outer_context.Name}:It") ? "      " : "   ";

            if (call.StartsWith(Inner_context.Name))
                return call.StartsWith($"{Inner_context.Name}:It") ? "         " : "      ";

            throw new Exception("Unrecognized context");
        }

        public CallTracker Is(params string[] current)
        {
            if (current == null)
            {
                calls.Should().BeNull();
                calls = new Stack<string>();
                return this;
            }

            if (current.Length == 1 && current.Single() == "")
                return this;

            calls.Peek().Should().BeOneOf(current);
            return this;
        }

        public CallTracker Push(string push)
        {
            if (calls.Any() && Current.Contains("after") && !push.Contains("after"))
            {
                log.AppendLine();
            }
            log.AppendLine($"{Indent(push)}{push}");
            calls.Push(push);
            return this;
        }

        public string Current => calls.Peek();

        public void PrintLog()
        {
            Console.WriteLine();
            Console.WriteLine("################ Complete call log  #################");
            Console.WriteLine();
            Console.WriteLine(log.ToString());
        }
    }
}