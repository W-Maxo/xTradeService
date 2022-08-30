using System;
using System.Globalization;

namespace xTradeService.Core
{
    public class ServerEventArgs
    {
        public ServerEventArgs(string msg, int type, long time, int numcon, int bytesRead, int bytesWrite)
        {
            Msg = msg;
            Type = type;
            Time = time;
            Numcon = numcon;
            BytesRead = bytesRead;
            BytesWrite = bytesWrite;

            Dt = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}{7}",
                Msg, Type, Time, Numcon, BytesRead, BytesWrite, Dt.ToString(new CultureInfo("en-US", false)), "<EOF>");
        }

        public string Msg { get; private set; }
        public int Type { get; private set; }
        public long Time { get; private set; }
        public int Numcon { get; private set; }
        public int BytesRead { get; private set; }
        public int BytesWrite { get; private set; }
        public DateTime Dt { get; private set; }
    }
}