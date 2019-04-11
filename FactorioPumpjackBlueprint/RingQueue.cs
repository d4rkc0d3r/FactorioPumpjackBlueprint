using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint
{
    class RingQueue
    {
        public int Count { get; private set; }
        private int index;
        private int[] buffer;
        private int andConst;

        public RingQueue(int bufferSize)
        {
            bufferSize = 1 << (int)Math.Ceiling(Math.Log(bufferSize, 2));
            andConst = bufferSize - 1;
            Count = 0;
            index = 0;
            buffer = new int[bufferSize];
        }

        public void Enqueue(int value)
        {
            buffer[(index + Count) & andConst] = value;
            Count = Count + 1;
        }

        public int Dequeue()
        {
            int value = buffer[index];
            index = (index + 1) & andConst;
            Count = Count - 1;
            return value;
        }

        public void Clear()
        {
            Count = 0;
        }
    }
}
