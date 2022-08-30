using System.Net.Sockets;

namespace xTradeService.Core
{
    public class SocketPacket
    {
        public SocketPacket(Socket socket)
        {
            m_currentSocket = socket;
        }

        public bool AccessGranted { get; set; }

        public Socket m_currentSocket;

        public byte[] dataBuffer = new byte[1024];
    }
}