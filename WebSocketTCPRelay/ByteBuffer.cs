using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketTCPRelay
{
    class ByteBuffer
    {
        private object lockObj;

        private byte[] backingStore;

        public int Length { get; private set; }

        public int InitialSize { get; set; }
        public int GrowSize { get; set; }

        public ByteBuffer(int initialSize = 2048, int growSize = 1024)
        {
            lockObj = new object();
            Length = 0;
            InitialSize = initialSize;
            GrowSize = growSize;
            backingStore = new byte[initialSize];
        }

        private bool NeedsGrowth(int additionalBytes)
        {
            return additionalBytes + Length > backingStore.Length;
        }

        private void Enlarge(int additionalBytes)
        {
            // TODO: This should factor in the remaining space in the current backingStore
            int growIncrements = (additionalBytes / GrowSize) + 1;

            byte[] newStore = new byte[backingStore.Length + (growIncrements * GrowSize)];
            Array.Copy(backingStore, newStore, Length);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int length)
        {
            lock (lockObj)
            {
                if (NeedsGrowth(buffer.Length))
                {
                    Enlarge(buffer.Length);
                }

                Array.Copy(buffer, offset, backingStore, Length, length);
                Length += length;
            }
        }

        public void Clear(bool shrinkToInitialSize = false)
        {
            lock (lockObj)
            {
                Length = 0;

                if (shrinkToInitialSize == true)
                {
                    backingStore = new byte[InitialSize];
                }
            }
        }

        public byte[] GetBufferBytesCopy()
        {
            lock (lockObj)
            {
                byte[] result = new byte[Length];
                Array.Copy(backingStore, result, Length);
                return result;
            }
        }
    }
}
