// ReSharper disable InconsistentNaming
namespace Composable.Tests.TestFrameworkExploration
{
    static class Class_context
    {
        public static readonly string Name = nameof(Class_context);
        public static readonly string before_all = $"{Name}:{nameof(before_all)}";
        public static readonly string before_each = $"{Name}:{nameof(before_each)}";
        public static readonly string after_each = $"{Name}:{nameof(after_each)}";
        public static readonly string after_all = $"{Name}:{nameof(after_all)}";
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
        public static readonly string Name = nameof(Outer_context);
        public static readonly string before = $"{Name}:before";
        public static readonly string beforeAll = $"{Name}:{nameof(beforeAll)}";
        public static readonly string after = $"{Name}:{nameof(after)}";
        public static readonly string afterAll = $"{Name}:{nameof(afterAll)}";
        public static readonly string It1 = $"{Name}:{nameof(It1)}";
        public static readonly string It2 = $"{Name}:{nameof(It2)}";
    }
}