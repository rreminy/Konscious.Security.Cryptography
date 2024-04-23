namespace Konscious.Security.Cryptography
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    // this could be refactored to support either endianness
    // but I only need little endian for this
    internal class LittleEndianActiveStream : Stream
    {
        public LittleEndianActiveStream(byte[] buffer = null)
        {
            _buffer = buffer;
            _bufferAvailable = _buffer?.Length ?? 0;
        }

        public void Expose(short data)
        {
            _bufferSetupActions.Enqueue(() => BufferShort((ushort)data));
        }

        public void Expose(ushort data)
        {
            _bufferSetupActions.Enqueue(() => BufferShort(data));
        }

        public void Expose(int data)
        {
            _bufferSetupActions.Enqueue(() => BufferInt((uint)data));
        }

        public void Expose(uint data)
        {
            _bufferSetupActions.Enqueue(() => BufferInt(data));
        }

        public void Expose(byte data)
        {
            _bufferSetupActions.Enqueue(() => BufferByte(data));
        }

        public void Expose(byte[] data)
        {
            if (data != null)
            {
                _bufferSetupActions.Enqueue(() => BufferArray(data, 0, data.Length));
            }
        }

        public void Expose(Memory<ulong> mem)
        {
            _bufferSetupActions.Enqueue(() => BufferSpan(mem.Span));
        }

        public void Expose(Stream subStream)
        {
            if (subStream != null)
            {
                _bufferSetupActions.Enqueue(() => BufferSubStream(subStream));
            }
        }

        public void ClearBuffer()
        {
            Array.Clear(_buffer);
            _bufferAvailable = 0;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int available = _bufferAvailable - _bufferOffset;
                if (available == 0)
                {
                    if (_bufferSetupActions.TryDequeue(out var action)) action();
                    else return totalRead;

                    // we are safe to assume that offset becomes 0 after that call
                    available = _bufferAvailable;
                }

                // if we only need to read part of available - reduce that
                available = Math.Min(available, count - totalRead);
                Array.Copy(_buffer, _bufferOffset, buffer, offset, available);

                _bufferOffset += available;
                offset += available;
                totalRead += available;
            }

            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("LittleEndianActiveStream is non-seekable");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("LittleEndianActiveStream is an actual Stream that doesn't support length");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _bufferSetupActions.Enqueue(() => BufferArray(buffer, offset, count));
        }

        private void BufferSubStream(Stream stream)
        {
            ReserveBuffer(1024);
            var result = stream.Read(_buffer, 0, 1024);
            if (result == 1024)
            {
                _bufferSetupActions.Enqueue(() => BufferSubStream(stream));
            }
            else
            {
                stream.Dispose();
            }

            _bufferAvailable = result;
        }

        private void BufferByte(byte value)
        {
            ReserveBuffer(1);
            _buffer[0] = value;
        }

        private void BufferArray(byte[] value, int offset, int length)
        {
            ReserveBuffer(value.Length);
            Array.Copy(value, offset, _buffer, 0, length);
        }

        private void BufferSpan(ReadOnlySpan<ulong> span)
        {
            var cast = MemoryMarshal.Cast<ulong, byte>(span);
            ReserveBuffer(cast.Length);
            cast.CopyTo(_buffer.AsSpan(0, cast.Length));
        }

        private void BufferShort(ushort value)
        {
            ReserveBuffer(sizeof(ushort));
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
        }

        private void BufferInt(uint value)
        {
            ReserveBuffer(sizeof(uint));
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
        }

        private void ReserveBuffer(int size)
        {
            if (_buffer == null)
            {
                _buffer = new byte[size];
            }
            else if (_buffer.Length < size)
            {
                Array.Resize(ref _buffer, size);
            }

            _bufferOffset = 0;
            _bufferAvailable = size;
        }

        private readonly Queue<Action> _bufferSetupActions = new Queue<Action>();

        private byte[] _buffer;
        private int _bufferOffset;
        private int _bufferAvailable;

        public override bool CanRead
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanSeek
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}