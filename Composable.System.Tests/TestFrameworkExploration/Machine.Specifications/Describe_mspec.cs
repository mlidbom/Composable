using Machine.Specifications;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
namespace Composable.Tests.TestFrameworkExploration.Machine.Specifications
{
    public class Describe_mspec
    {
        static CallTracker Current;

        Establish before_all = () =>
                               {
                                   Current = new CallTracker();
                                   Current.Is(null)
                                          .Push(Class_context.before_all);
                               };

        //Establish before_each = () => Current.Is(Outer_context.beforeAll,
        //                                         Class_context.after_each,
        //                                         Inner_context.beforeAll)
        //                                     .Push(Class_context.before_each);

        //Cleanup after_each = () => Current.Is(Outer_context.after)
        //                                  .Push(Class_context.after_each);

        Cleanup after_all = () => Current.Is(Outer_context.afterAll)
                                         .Push(Class_context.after_all)
                                         .PrintLog();

        class outer_context
        {
            Establish beforeAll = () => Current.Is(Class_context.before_all)
                                               .Push(Outer_context.beforeAll);

            //Establish before = () => Current.Is(Class_context.before_each)
            //                                .Push(Outer_context.before);

            //Cleanup after = () => Current.Is("")
            //                             .Push(Outer_context.after);

            Cleanup afterAll = () => Current.Is(Inner_context.afterAll, Outer_context.beforeAll, Outer_context.It2)
                                            .Push(Outer_context.afterAll);

            It Outer_context_It1 = () => Current.Is(Outer_context.beforeAll)
                                                .Push(Outer_context.It1);

            It Outer_context_It2 = () => Current.Is(Outer_context.It1)
                                                .Push(Outer_context.It2);

            class InnerContext
            {
                Establish beforeAll = () => Current.Is(Outer_context.beforeAll)
                                                   .Push(Inner_context.beforeAll);

                //Establish before = () => Current.Is(Outer_context.before)
                //                                .Push(Inner_context.before);

                //Cleanup after = () => Current.Is(Inner_context.It1,
                //                                 Inner_context.It2)
                //                             .Push(Inner_context.after);

                Cleanup afterAll = () => Current.Is(Inner_context.It2)
                                                .Push(Inner_context.afterAll);

                It Inner_context_It1 = () => Current.Is(Inner_context.beforeAll)
                                                    .Push(Inner_context.It1);

                It Inner_context_It2 = () => Current.Is(Inner_context.It1)
                                                    .Push(Inner_context.It2);
            }
        }
    }
}
