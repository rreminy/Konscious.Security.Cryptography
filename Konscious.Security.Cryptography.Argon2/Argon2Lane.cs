namespace Konscious.Security.Cryptography
{
    using System;
    using System.Diagnostics;

    internal class Argon2Lane
    {
        public Argon2Lane(int blockCount)
        {
            _memory = new Memory<ulong>(new ulong[128 * blockCount]);
            BlockCount = blockCount;
        }

        public Memory<ulong> this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < BlockCount);
                return _memory.Slice(128*index, 128);
            }
        }

        public int BlockCount { get; }

        private readonly Memory<ulong> _memory;
    }
}