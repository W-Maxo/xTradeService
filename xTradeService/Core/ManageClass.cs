using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace xTradeService.Core
{
    sealed class ManageClass
    {
        private ServerEventArgs _sea;

        private AsyncCallback _pfnWorkerCallBack ;
		private  Socket _mMainSocket;

        readonly IPEndPoint _localEndPointManage;

        private readonly List<SocketPacket> _mWorkerSocketList;
		
        public delegate void StartEventHandler(object sender, StartEventArgs e);

        public event StartEventHandler StartEvent;

        private void RaiseStartEvent(bool res)
        {
            if (StartEvent != null)
                StartEvent(this, new StartEventArgs(res));
        }

        public ManageClass(IPEndPoint localEndPointManage)
        {
            _mWorkerSocketList = new List<SocketPacket>();

            _localEndPointManage = localEndPointManage;
        }

		public void Start()
		{
			try
			{
				_mMainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_mMainSocket.Bind(_localEndPointManage);
				_mMainSocket.Listen(100);
				_mMainSocket.BeginAccept(OnClientConnect, null);

                RaiseStartEvent(true);
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message);
			}
		}

        private void OnClientConnect(IAsyncResult asyn)
		{
			try
			{
				Socket workerSocket = _mMainSocket.EndAccept (asyn);

                var theSocPkt = new SocketPacket(workerSocket);
                _mWorkerSocketList.Add(theSocPkt);

                if (null == _sea) _sea=new ServerEventArgs("", 0, 0, 0, 0, 0);

                string msg = new ServerEventArgs("Успешное подключение к серверу", 8, _sea.Time, _sea.Numcon, _sea.BytesRead, _sea.BytesWrite).ToString();
                SendMsgToClient(msg, theSocPkt);

                Console.WriteLine("(Manage) Client connected: " + _mWorkerSocketList.Count);

                WaitForData(theSocPkt);
							
				_mMainSocket.BeginAccept(OnClientConnect, null);
			}
			catch(ObjectDisposedException)
			{
				System.Diagnostics.Debugger.Log(0,"1","\n OnClientConnection: Socket has been closed\n");
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message);
			}
		}

        private void WaitForData(SocketPacket soc)
		{
			try
			{
				if  ( _pfnWorkerCallBack == null )
				{		
					_pfnWorkerCallBack = OnDataReceived;
				}

                soc.m_currentSocket.BeginReceive(soc.dataBuffer, 0,
                    soc.dataBuffer.Length,
					SocketFlags.None,
					_pfnWorkerCallBack,
                    soc);
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message );
			}
		}

        private void OnDataReceived(IAsyncResult asyn)
		{
			var socketData = (SocketPacket)asyn.AsyncState;
			try
			{
				int iRx  = socketData.m_currentSocket.EndReceive (asyn);

			    if (!socketData.AccessGranted)
                {
                    string str = Encoding.UTF8.GetString(socketData.dataBuffer, 0, iRx);

                    string[] data = str.Split("\t".ToCharArray(), StringSplitOptions.None);

                    if (2 == data.Length)
                    {
                        string tmphash = Md5HashClass.GetMd5Hash(data[1]);

                        var comparer = StringComparer.OrdinalIgnoreCase;

                        if ((0 == comparer.Compare(Properties.Settings.Default.AdmName, data[0])) &&
                            (0 == comparer.Compare(tmphash, Properties.Settings.Default.AdmPassHash)))
                        {
                            socketData.AccessGranted = true;

                            if (null == _sea) _sea = new ServerEventArgs("", 0, 0, 0, 0, 0);

                            string msg = new ServerEventArgs("Добро пожаловать", 5, _sea.Time, _sea.Numcon, _sea.BytesRead, _sea.BytesWrite).ToString();
                            SendMsgToClient(msg, socketData);
                        }
                        else
                        {
                            FailedLogin(socketData);
                            
                        }
                    }
                    else
                    {
                        FailedLogin(socketData);
                    }
                }
	
				WaitForData(socketData);

			}
			catch (ObjectDisposedException )
			{
				System.Diagnostics.Debugger.Log(0,"1","\nOnDataReceived: Socket has been closed\n");
			}
			catch(SocketException se)
			{
				if(se.ErrorCode == 10054) 
				{
                    Console.WriteLine("(Manage) Client disconnected");
                    _mWorkerSocketList.Remove(socketData);
                    Console.WriteLine("(Manage) Client connected: " + _mWorkerSocketList.Count);
				}
				else
				{
					Console.WriteLine(se.Message );
				}
			}
		}

        public void FailedLogin(SocketPacket socketData)
        {
            string msg = new ServerEventArgs("Неверное имя или пароль!", 9, 0, 0, 0, 0).ToString();
            SendMsgToClient(msg, socketData);

            socketData.m_currentSocket.Shutdown(SocketShutdown.Send);
            socketData.m_currentSocket.Close();

            _mWorkerSocketList.Remove(socketData);

            Console.WriteLine("(Manage) Client connected: " + _mWorkerSocketList.Count);
        }

		public void SendEvent(ServerEventArgs sea)
		{
			try
			{
			    _sea = sea;

			    string msg = sea.ToString();

				byte[] byData = Encoding.UTF8.GetBytes(msg);

                foreach (SocketPacket sp in _mWorkerSocketList.Where(sp => sp.m_currentSocket != null && sp.AccessGranted).Where(sp => sp.m_currentSocket.Connected))
                {
                    sp.m_currentSocket.Send(byData);
                }
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message);
			}
		}

        public void CloseSockets()
        {
            if(_mMainSocket != null)
			{
				_mMainSocket.Close();
			}

            foreach (SocketPacket workerSocket in _mWorkerSocketList.Where(workerSocket => workerSocket.m_currentSocket != null))
            {
                workerSocket.m_currentSocket.Close();
            }
        }

        void SendMsgToClient(string msg, SocketPacket workerSocket)
        {
            byte[] byData = Encoding.UTF8.GetBytes(msg);

            if (workerSocket.m_currentSocket != null) workerSocket.m_currentSocket.Send(byData);
        }
    }
}
