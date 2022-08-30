using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace xTradeService.Core
{
    class SocketAsyncEventArgsPool
    {
        public Stack<SocketAsyncEventArgs> m_pool { get; private set; }
        
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                Console.WriteLine("My ERROR - Push: Items added to a SocketAsyncEventArgsPool cannot be null");
            }

            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        public int Count
        {
            get { return m_pool.Count; }
        }

    }
}
