using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using NSpec.NUnit;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
namespace Composable.Tests.TestFrameworkExploration.NSpec
{
    [UsedImplicitly] public class Describe_NSpec : nspec
    {
        readonly CallTracker Calls = new CallTracker();
        public void before_all() => Calls.CurrentIs(null)
                                         .Push(Class_context.before_all);

        public void before_each() => Calls.CurrentIs("")
                                          .Push(Class_context.before_each);

        public void after_each() => Calls.CurrentIs("")
                                         .Push(Class_context.after_each);

        public void outer_context()
        {
            beforeAll = () => Calls.CurrentIs(Class_context.before_all)
                                   .Push(Outer_context.beforeAll);

            before = () => Calls.CurrentIs(Class_context.before_each)
                                .Push(Outer_context.before);

            after = () => Calls.CurrentIs("")
                               .Push(Outer_context.after);

            afterAll = () => Calls.CurrentIs(Inner_context.afterAll)
                                  .Push(Outer_context.afterAll);

            it[Outer_context.It1] = () => Calls.CurrentIs(Outer_context.before)
                                               .Push(Outer_context.It1);

            it[Outer_context.It2] = () => Calls.CurrentIs(Outer_context.before)
                                               .Push(Outer_context.It2);

            context[Inner_context.Name] = () =>
                                          {
                                              beforeAll = () => Calls.CurrentIs("")
                                                                     .Push(Inner_context.beforeAll);

                                              before = () => Calls.CurrentIs(Outer_context.before)
                                                                  .Push(Inner_context.before);

                                              after = () => Calls.CurrentIs(Inner_context.It1, Inner_context.It2)
                                                                 .Push(Inner_context.after);

                                              afterAll = () => Calls.CurrentIs("")
                                                                    .Push(Inner_context.afterAll);

                                              it[Inner_context.It1] = () => Calls.CurrentIs(Inner_context.before)
                                                                                 .Push(Inner_context.It1);

                                              it[Inner_context.It1] = () => Calls.CurrentIs(Inner_context.before)
                                                                                 .Push(Inner_context.It2);
                                          };
        }

        class CallTracker
        {
            Stack<string> calls;

            public CallTracker CurrentIs(params string[] current)
            {
                if(current == null)
                {
                    calls.Should().BeNull();
                    calls = new Stack<string>();
                    return this;
                }

                if(current.Length == 1 && current.Single() == "")
                    return this;

                calls.Peek().Should().BeOneOf(current);
                return this;
            }

            public void Push(string push) { calls.Push(push); }

            public string Current => calls.Peek();
        }

        static class Class_context
        {
            static readonly string Name = nameof(Class_context);
            public static readonly string before_all = $"{Name}:{nameof(before_all)}";
            public static readonly string before_each = $"{Name}:{nameof(before_each)}";
            public static readonly string after_each = $"{Name}:{nameof(after_each)}";
        }

        static class Inner_context
        {
            public static readonly string Name = nameof(Inner_context);
            public static readonly string before = $"{Name}:before";
            public static readonly string beforeAll = $"{Name}:{nameof(beforeAll)}";
            public static readonly string after = $"{Name}:{nameof(after)}";
            public static readonly string afterAll = $"{Name}:{nameof(afterAll)}";
            public static readonly string It1 = $"{Name}:{nameof(It1)}";
            public static readonly string It2 = $"{Name}:{nameof(It2)}";
        }

        static class Outer_context
        {
            static readonly string ClassName = nameof(Outer_context);
            public static readonly string before = $"{ClassName}:before";
            public static readonly string beforeAll = $"{ClassName}:{nameof(beforeAll)}";
            public static readonly string after = $"{ClassName}:{nameof(after)}";
            public static readonly string afterAll = $"{ClassName}:{nameof(afterAll)}";
            public static readonly string It1 = $"{ClassName}:{nameof(It1)}";
            public static readonly string It2 = $"{ClassName}:{nameof(It2)}";
        }
    }
}
