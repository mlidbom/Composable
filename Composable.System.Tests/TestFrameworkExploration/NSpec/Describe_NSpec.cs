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
        readonly CallTracker Current = new CallTracker();
        public void before_all() => Current.Is(null)
                                           .Push(Class_context.before_all);

        public void before_each() => Current.Is(Outer_context.beforeAll,
                                                Class_context.after_each,
                                                Inner_context.beforeAll)
                                            .Push(Class_context.before_each);

        public void after_each() => Current.Is(Outer_context.after)
                                           .Push(Class_context.after_each);

        public void after_all() => Current.Is(Outer_context.afterAll)
                                          .Push(Class_context.after_all)
                                          .PrintLog();

        public void outer_context()
        {
            beforeAll = () => Current.Is(Class_context.before_all)
                                     .Push(Outer_context.beforeAll);

            before = () => Current.Is(Class_context.before_each)
                                  .Push(Outer_context.before);

            after = () => Current.Is("")
                                 .Push(Outer_context.after);

            afterAll = () => Current.Is(Inner_context.afterAll)
                                    .Push(Outer_context.afterAll);

            it[Outer_context.It1] = () => Current.Is(Outer_context.before)
                                                 .Push(Outer_context.It1);

            it[Outer_context.It2] = () => Current.Is(Outer_context.before)
                                                 .Push(Outer_context.It2);

            context[Inner_context.Name] =
                () =>
                {
                    beforeAll = () => Current.Is(Class_context.after_each)
                                             .Push(Inner_context.beforeAll);

                    before = () => Current.Is(Outer_context.before)
                                          .Push(Inner_context.before);

                    after = () => Current.Is(Inner_context.It1,
                                             Inner_context.It2)
                                         .Push(Inner_context.after);

                    afterAll = () => Current.Is(Class_context.after_each)
                                            .Push(Inner_context.afterAll);

                    it[Inner_context.It1] = () => Current.Is(Inner_context.before)
                                                         .Push(Inner_context.It1);

                    it[Inner_context.It1] = () => Current.Is(Inner_context.before)
                                                         .Push(Inner_context.It2);
                };
        }
    }
}
