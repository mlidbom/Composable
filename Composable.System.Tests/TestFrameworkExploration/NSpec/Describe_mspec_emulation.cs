using JetBrains.Annotations;
using Machine.Specifications;
using NSpec.NUnit;
// ReSharper disable UnusedMember.Global

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace Composable.Tests.TestFrameworkExploration.NSpec
{
    [UsedImplicitly] public class Describe_mspec_emulation : nspec
    {
        CallTracker Current;

        void before_all()
        {
            Current = new CallTracker();
            Current.Is(null)
                   .Push(Class_context.before_all);
        }


        void after_all() => Current.Is(Outer_context.afterAll)
                                         .Push(Class_context.after_all)
                                         .PrintLog();

        public void outer_context()
        {
            beforeAll = () => Current.Is(Class_context.before_all)
                                               .Push(Outer_context.beforeAll);

            afterAll = () => Current.Is(Inner_context.afterAll, Outer_context.beforeAll, Outer_context.It2)
                                            .Push(Outer_context.afterAll);

            it[Outer_context.It1] = () => Current.Is(Outer_context.beforeAll)
                                                .Push(Outer_context.It1);

            it[Outer_context.It2] = () => Current.Is(Outer_context.It1)
                                                .Push(Outer_context.It2);

            context[Inner_context.Name] =
                () =>
                {
                    beforeAll = () => Current.Is(Outer_context.It2)//With mspec it would have been: Outer_context.beforeAll
                                                       .Push(Inner_context.beforeAll);

                    afterAll = () => Current.Is(Inner_context.It2)
                                                    .Push(Inner_context.afterAll);

                    it[Inner_context.It1] = () => Current.Is(Inner_context.beforeAll)
                                                         .Push(Inner_context.It1);

                    it[Inner_context.It2] = () => Current.Is(Inner_context.It1)
                                                         .Push(Inner_context.It2);
                };
        }
    }
}
