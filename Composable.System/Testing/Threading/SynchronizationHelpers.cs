using System;

namespace Composable.Testing.Threading
{
    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    class GatedCodeBlock
    {
        public ThreadGate EntranceGate { get; }
        public ThreadGate ExitGate { get; }

        readonly Action _codeBlock;

        GatedCodeBlock(Action codeBlock, ThreadGate entranceGate, ThreadGate exitGate)
        {
            EntranceGate = entranceGate;
            ExitGate = exitGate;
            _codeBlock = codeBlock;
        }
    }
}
