using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip;

namespace xTradeService.Core
{
    sealed class Server
    {
        private readonly int _mNumConnections;   
        readonly BufferManager _mBufferManager;  
        const int OpsToPreAlloc = 2;    
        Socket _listenSocket;

        private Thread _listenerThread;
        readonly IPEndPoint _localEndPoint;
       
        private readonly SocketAsyncEventArgsPool _mReadWritePool;
        private int _mTotalBytesRead;        
        private int _mTotalBytesWrite;
        private int _mNumConnectedSockets;      
        private readonly int _receiveBufferSize;

        private const int TinfoLength = 50;

        public ManualResetEvent ServerClose = new ManualResetEvent(false);

        readonly Semaphore _mMaxNumberAcceptedClients;

        public delegate void ServerEventHandler(object sender, ServerEventArgs e);

        public event ServerEventHandler ServerEvent;

        private void RaiseServerEvent(string msg, int type, long time, int numcon, int bytesRead, int bytesWrite)
        {
            if (ServerEvent != null)
                ServerEvent(this, new ServerEventArgs(msg, type, time, numcon, bytesRead, bytesWrite));
        }

        public Server(int numConnections, int receiveBufferSize, IPEndPoint localEndPoint)
        {
            _mTotalBytesRead = 0;
            _mNumConnectedSockets = 0;
            _mNumConnections = numConnections;

            _mBufferManager = new BufferManager(receiveBufferSize * numConnections * OpsToPreAlloc,
                receiveBufferSize);
      
            _mReadWritePool = new SocketAsyncEventArgsPool(numConnections);
            _mMaxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

            _receiveBufferSize = receiveBufferSize;
            _localEndPoint = localEndPoint;
        }

        public void Init()
        {
            _mBufferManager.InitBuffer();

            for (int i = 0; i < _mNumConnections; i++)
            {
                var readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += IoCompleted;
                readWriteEventArg.UserToken = new AsyncUserToken();

                _mBufferManager.SetBuffer(readWriteEventArg);

                _mReadWritePool.Push(readWriteEventArg);
            }
        }

        public void Start()
        {
            _listenerThread = new Thread(StartL) {IsBackground = true, Name = "Listener"};
            _listenerThread.Start();
        }

        public void StartL()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _listenSocket.Bind(_localEndPoint);
            }
            catch (Exception e)
            {
                string msg = string.Format("Error. Can not bind local adress: {0}", e.Message);
                Console.WriteLine(msg);
                RaiseServerEvent(msg, 0, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
            }
            
            _listenSocket.Listen(100);
            
            StartAccept(null);

            ServerClose.WaitOne();
        }

        public void StopServer()
        {
            ServerClose.Set();
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += AcceptEventArgCompleted;
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            _mMaxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        void AcceptEventArgCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                Interlocked.Increment(ref _mNumConnectedSockets);
                
                string msg = string.Format("Client connection accepted. There are {0} clients connected to the server", _mNumConnectedSockets);
                Console.WriteLine(msg);
                RaiseServerEvent(msg, 7, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);

                SocketAsyncEventArgs readEventArgs = _mReadWritePool.Pop();

                ((AsyncUserToken) readEventArgs.UserToken).Socket = e.AcceptSocket;

                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);


                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs);
                }

                StartAccept(e);
            }
            catch (Exception exc)
            {
                string msg = string.Format("ProcessAccept error: {0}", exc.Message);
                Console.WriteLine(msg);
                RaiseServerEvent(msg, 0, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
            }
        }

        void IoCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    string msg = string.Format("My ERROR - IO_Completed: The last operation completed on the socket was not a receive or send");
                    Console.WriteLine(msg);
                    RaiseServerEvent(msg, 0, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
                    break;
            }       
    
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref _mTotalBytesRead, e.BytesTransferred);

                string str = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);

                string msg = string.Format("Incoming msg from {0}", token.Socket.RemoteEndPoint);
                Console.WriteLine(msg);
                
                RaiseServerEvent(msg, 6, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);

                Console.WriteLine("The server has read a total of {0} bytes", _mTotalBytesRead);

                int endIndx = str.IndexOf("<EOF>", StringComparison.Ordinal);

                if (endIndx > -1)
                {
                    var start = DateTime.Now;
                    try
                    {
                        str = str.Substring(0, endIndx);
                        string[] values = str.Split("\t".ToCharArray(), StringSplitOptions.None);

                        var comparer = StringComparer.OrdinalIgnoreCase;

                        if (0 == comparer.Compare(values[0], "xTradeMobility"))
                        {
                            var ps = new PassClass();

                            bool logIn = false;

                            string[] rcvname = values[1].Split("=".ToCharArray(), StringSplitOptions.None);
                            string[] rcvpass = values[2].Split("=".ToCharArray(), StringSplitOptions.None);
                            string[] typecmd = values[3].Split("=".ToCharArray(), StringSplitOptions.None);
                            
                            string tmphash = Md5HashClass.GetMd5Hash(rcvpass[1]);

                            foreach (var uinf in ps.GetUserList())
                            {
                                if ((0 == comparer.Compare(uinf.UssName, rcvname[1])) && (0 == comparer.Compare(tmphash, uinf.UssPHash)))
                                {
                                    logIn = true;
                                    
                                    int ussid = uinf.UssID;

                                    if (uinf.AllowLogin)
                                    {
                                        msg = string.Format("Access accept for user: {0}", rcvname[1]);
                                        Console.WriteLine(msg);
                                        RaiseServerEvent(msg, 5, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
                                        
                                        if (0 == comparer.Compare(typecmd[1], "GetData"))
                                        {
                                            var data = new GetDataMClass();

                                            MemoryStream prodims = data.GetProdData();

                                            MemoryStream reqims = null;
                                            MemoryStream reqimclosed = null;
                                            MemoryStream reqimarr = null;

                                            data.GetReqData(uinf.UssID, ref reqims, ref reqimclosed, ref reqimarr);

                                            MemoryStream reqinf = data.GetFullInf();

                                            byte[] xfileData;

                                            using (prodims)
                                            using (reqims)
                                            using (reqimclosed)
                                            using (reqimarr)
                                            {
                                                prodims.Position = 0;

                                                using (var oms = new MemoryStream())
                                                {
                                                    using (var s = new ZipOutputStream(oms))
                                                    {
                                                        s.SetLevel(9);

                                                        var buffer = new byte[4096];

                                                        var entry = new ZipEntry("Tovars.xml")
                                                                        {DateTime = DateTime.Now};

                                                        s.PutNextEntry(entry);

                                                        prodims.Position = 0;

                                                        int sourceBytes;
                                                        do
                                                        {
                                                            sourceBytes = prodims.Read(buffer, 0, buffer.Length);
                                                            s.Write(buffer, 0, sourceBytes);
                                                        } while (sourceBytes > 0);

                                                        entry = new ZipEntry("Requests.xml")
                                                                    {DateTime = DateTime.Now};

                                                        s.PutNextEntry(entry);

                                                        reqims.Position = 0;

                                                        do
                                                        {
                                                            sourceBytes = reqims.Read(buffer, 0, buffer.Length);
                                                            s.Write(buffer, 0, sourceBytes);
                                                        } while (sourceBytes > 0);

                                                        entry = new ZipEntry("RequestsClosed.xml") { DateTime = DateTime.Now };

                                                        s.PutNextEntry(entry);

                                                        reqimclosed.Position = 0;

                                                        do
                                                        {
                                                            sourceBytes = reqimclosed.Read(buffer, 0, buffer.Length);
                                                            s.Write(buffer, 0, sourceBytes);
                                                        } while (sourceBytes > 0);

                                                        entry = new ZipEntry("RequestsArr.xml") { DateTime = DateTime.Now };

                                                        s.PutNextEntry(entry);

                                                        reqimarr.Position = 0;

                                                        do
                                                        {
                                                            sourceBytes = reqimarr.Read(buffer, 0, buffer.Length);
                                                            s.Write(buffer, 0, sourceBytes);
                                                        } while (sourceBytes > 0);

                                                        entry = new ZipEntry("Inf.xml") {DateTime = DateTime.Now};

                                                        s.PutNextEntry(entry);

                                                        reqinf.Position = 0;

                                                        do
                                                        {
                                                            sourceBytes = reqinf.Read(buffer, 0, buffer.Length);
                                                            s.Write(buffer, 0, sourceBytes);
                                                        } while (sourceBytes > 0);

                                                        s.Finish();

                                                        oms.Position = 0;

                                                        xfileData = new byte[oms.Length];
                                                        oms.Read(xfileData, 0, xfileData.Length);

                                                        Console.WriteLine("Compressed from {0} to {1} bytes.",
                                                                            prodims.Length + reqims.Length,
                                                                            xfileData.Length);
                                                        s.Close();
                                                    }
                                                }
                                            }

                                            string cmd = string.Format("cmd=getdatafile");

                                            byte[] tpdata = Encoding.UTF8.GetBytes(cmd.PadRight(TinfoLength));

                                            byte[] fSize = BitConverter.GetBytes(xfileData.Length + TinfoLength);
                                            var clientData = new byte[TinfoLength + 4 + xfileData.Length];
                                                
                                            fSize.CopyTo(clientData, 0);
                                            tpdata.CopyTo(clientData, 4);
                                            xfileData.CopyTo(clientData, TinfoLength + 4);

                                            e.SetBuffer(clientData, 0, clientData.Length);

                                            bool willRaiseEvent = token.Socket.SendAsync(e);

                                            Interlocked.Add(ref _mTotalBytesWrite, e.Count);
                                            Console.WriteLine("The server has write a total of {0} bytes",
                                                                _mTotalBytesWrite);

                                            if (!willRaiseEvent)
                                            {
                                                ProcessSend(e);
                                            }
                                        }

                                        if (0 == comparer.Compare(typecmd[1].Substring(0, 7), "SendRec"))
                                        {

                                            string[] rcvcnts = typecmd[1].Split(":".ToCharArray(), StringSplitOptions.None);

                                            int rcvcnt = int.Parse(rcvcnts[1]);

                                            string cmd;

                                            if ((e.BytesTransferred - (endIndx + 5)) == rcvcnt)
                                            {
                                                using (var rms = new MemoryStream())
                                                {
                                                    rms.Write(e.Buffer, e.Offset + endIndx + 5, e.BytesTransferred - endIndx - 5);

                                                    rms.Flush();

                                                    rms.Position = 0;

                                                    var serializer = new XmlSerializer(typeof(ReqClass));

                                                    ReqClass reqData;


                                                    using (var reader = XmlReader.Create(rms))
                                                    {
                                                        reqData = (ReqClass)serializer.Deserialize(reader);
                                                    }

                                                    Console.WriteLine(string.Format("Order is accepted ({0})", reqData.DateCreation));

                                                    rms.Close();

                                                    reqData.UserID = ussid;

                                                    cmd = string.Format(reqData.Insert() ? "cmd=recvreq:OrderIsAccepted#{0}" : "cmd=recvreq:OrderNotAccepted#{0}", reqData.DateCreation);
                                                } 
                                            }
                                            else cmd = string.Format("cmd=recvreq:DataIsCorrupted");

                                            byte[] tpdata = Encoding.UTF8.GetBytes(cmd.PadRight(TinfoLength));

                                            byte[] fSize = BitConverter.GetBytes(TinfoLength);
                                            var clientData = new byte[TinfoLength + 4];

                                            fSize.CopyTo(clientData, 0);
                                            tpdata.CopyTo(clientData, 4);

                                            e.SetBuffer(clientData, 0, clientData.Length);

                                            bool willRaiseEvent = token.Socket.SendAsync(e);

                                            if (!willRaiseEvent)
                                            {
                                                ProcessSend(e);
                                            }

                                            Interlocked.Add(ref _mTotalBytesWrite, clientData.Length);
                                        }
                                    }
                                    else
                                    {
                                        msg = string.Format("Access denied for user: {0}", rcvname[1]);
                                        Console.WriteLine(msg);
                                        RaiseServerEvent(msg, 4, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
                                    }

                                    break;
                                }
                            }
                            if (!logIn) CloseClientSocket(e);
                        }
                        else
                        {
                            CloseClientSocket(e);
                        }

                        DateTime finish = DateTime.Now;

                        TimeSpan delta = finish - start;
                        long ticks = delta.Ticks;

                        long tm = ticks/10000;
                        msg = string.Format("Answer time: {0} ms", tm);

                        Console.WriteLine(msg);
                        RaiseServerEvent(msg, 3, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
                     }
                    catch (Exception exc)
                    {
                        CloseClientSocket(e);

                        msg = string.Format("ProcessReceive error: {0}", exc.Message);
                        Console.WriteLine(msg);
                        RaiseServerEvent(msg, 0, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var token = (AsyncUserToken)e.UserToken;

                bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            var nbuff = new byte[_receiveBufferSize];
            e.SetBuffer(nbuff, 0, nbuff.Length);

            var token = e.UserToken as AsyncUserToken;

            string msg;
            try
            {
                if (token != null)
                {
                    token.Socket.Shutdown(SocketShutdown.Send);
                }
            }
            catch (Exception exc)
            {
                msg = string.Format("CloseClientSocket error: {0}", exc.Message);
                Console.WriteLine(msg);
                RaiseServerEvent(msg, 0, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);
            }

            if (token != null) token.Socket.Close();

            Interlocked.Decrement(ref _mNumConnectedSockets);
            _mMaxNumberAcceptedClients.Release();

            msg = string.Format(
                "A client has been disconnected from the server. There are {0} clients connected to the server",
                _mNumConnectedSockets);

            Console.WriteLine(msg);
            RaiseServerEvent(msg, 2, 0, _mNumConnectedSockets, _mTotalBytesRead, _mTotalBytesWrite);

            _mReadWritePool.Push(e);
        }

    }
}
