namespace xTradeService.Core
{
    public class StartEventArgs
    {
        public StartEventArgs(bool res) { Startres = res; }
        public bool Startres { get; private set; }
    }
}