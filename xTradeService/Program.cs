using System.ServiceProcess;

namespace xTradeService
{
    static class Program
    {
        static void Main()
        {
            #if DEBUG
                System.Diagnostics.Debugger.Launch();
            #endif

            var servicesToRun = new ServiceBase[] 
                                              { 
                                                  new ServerService() 
                                              };
            ServiceBase.Run(servicesToRun);
        }
    }
}
