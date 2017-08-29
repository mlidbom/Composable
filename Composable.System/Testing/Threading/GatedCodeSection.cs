using System;
using Composable.System;

namespace Composable.Testing.Threading
{
    interface IGatedCodeSection
    {
        IThreadGate EntranceGate { get; }
        IThreadGate ExitGate { get; }
        IDisposable Enter();
    }

    static class GatedCodeSectionExtensions
    {
        public static IGatedCodeSection LetOneThreadEnter(this IGatedCodeSection @this)
        {
            @this.EntranceGate.LetOneThreadPass();
            return @this;
        }

        public static IGatedCodeSection Open(this IGatedCodeSection @this)
        {
            @this.EntranceGate.Open();
            @this.ExitGate.Open();
            return @this;
        }

        public static IGatedCodeSection LetOneThreadPass(this IGatedCodeSection @this)
        {
            @this.EntranceGate.LetOneThreadPass();
            @this.ExitGate.LetOneThreadPass();
            return @this;
        }
    }

    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    class GatedCodeSection : IGatedCodeSection
    {
        public IThreadGate EntranceGate { get; }
        public IThreadGate ExitGate { get; }

        public static IGatedCodeSection WithTimeout(TimeSpan timeout) => new GatedCodeSection(timeout);

        GatedCodeSection(TimeSpan timeout)
        {
            EntranceGate = ThreadGate.WithTimeout(timeout);
            ExitGate = ThreadGate.WithTimeout(timeout);
        }

        public IDisposable Enter()
        {
            EntranceGate.Pass();
            return Disposable.Create(() => ExitGate.Pass());
        }
    }
}
