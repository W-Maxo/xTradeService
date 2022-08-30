using System.Collections.Generic;
using System.Net.Sockets;

namespace xTradeService.Core
{
    class BufferManager
    {
        int m_numBytes;               
        byte[] _mBuffer;               
        Stack<int> m_freeIndexPool;    
        int _mCurrentIndex;
        int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            _mCurrentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        public void InitBuffer()
        {
            _mBuffer = new byte[m_numBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {

            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_mBuffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < _mCurrentIndex)
                {
                    return false;
                }
                args.SetBuffer(_mBuffer, _mCurrentIndex, m_bufferSize);
                _mCurrentIndex += m_bufferSize;
            }
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
