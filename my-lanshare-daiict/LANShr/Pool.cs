using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LANShr
{
    public class Pool
    {
        private int[] pool;
        private int top;
        public Pool(int size)
        {
            pool = new int[size];
            top = size;
            for (int i = 0; i < size; i++) { pool[i] = size - i - 1; }
        }
        public void Push(int index)
        {
            pool[top] = index;
            top++;
        }
        public int Pop()
        {
            top--;
            return pool[top];
        }
    }
}
