using System;
using System.Net;
using System.ServiceProcess;
using System.Diagnostics;
using xTradeService.Core;

namespace xTradeService
{
    public partial class ServerService : ServiceBase
    {
        static ManageClass _manage;
        readonly IPEndPoint _localEndPointManage;
        Server _server;

        public ServerService()
        {
            InitializeComponent();

            int numConnections;
            int receiveSize;
            IPEndPoint localEndPoint;
            
            try
            {
                numConnections = Properties.Settings.Default.numConnections;
                receiveSize = Properties.Settings.Default.receiveSize;
                int port = Properties.Settings.Default.Port;

                int portManage = Properties.Settings.Default.PortManage;

                if (numConnections <= 0)
                {
                    throw new ArgumentException("The number of connections specified must be greater than 0");
                }
                if (receiveSize <= 0)
                {
                    throw new ArgumentException("The receive size specified must be greater than 0");
                }
                if (port <= 0)
                {
                    throw new ArgumentException("The port specified must be greater than 0");
                }

                if (portManage <= 0)
                {
                    throw new ArgumentException("(Manage) The port specified must be greater than 0");
                }

                localEndPoint = new IPEndPoint(IPAddress.Any, port);
                _localEndPointManage = new IPEndPoint(IPAddress.Any, portManage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            _manage = new ManageClass(_localEndPointManage);
            _manage.StartEvent += delegate(object sender, StartEventArgs args)
            {
                if (args.Startres)
                {
                    _server = new Server(numConnections, receiveSize, localEndPoint);
                    _server.ServerEvent += ServerOnServerEvent;
                    _server.Init();
                    _server.Start();
                }
            };
        }

        protected override void OnStart(string[] args)
        {
            _manage.Start();
            AddLog("Start service", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _server.StopServer();
            AddLog("Stop service", EventLogEntryType.Information);
        }

        private void ServerOnServerEvent(object sender, ServerEventArgs serverEventArgs)
        {
            _manage.SendEvent(serverEventArgs);

            if (0 == serverEventArgs.Type)
            {
                AddLog(serverEventArgs.Msg, EventLogEntryType.Error);
            }
        }

        public void AddLog(string log, EventLogEntryType ee)
        {
            try
            {
                if (!EventLog.SourceExists(ServiceName))
                {
                    EventLog.CreateEventSource(ServiceName, ServiceName);
                }

                eventLog1.Source = ServiceName;

                eventLog1.WriteEntry(log, ee);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
