using System.Net.Sockets;

namespace xTradeService.Core
{
    class AsyncUserToken
    {
        Socket _mSocket;

        public AsyncUserToken() : this(null) { }

        public AsyncUserToken(Socket socket)
        {
            _mSocket = socket;
        }

        public Socket Socket
        {
            get { return _mSocket; }
            set { _mSocket = value; }
        }

    }
}
