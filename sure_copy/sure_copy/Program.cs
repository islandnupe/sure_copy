using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace sure_copy
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //compiler directive, run as a normal windows service when not in debug mode 
#if (!DEBUG)
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new SC_Service() 
            };
            ServiceBase.Run(ServicesToRun);
#else
            //compiler directive, when in debug mode, call our special Debug OnStart_Debug
            SC_Service service = new SC_Service();
            service.m_boolServiceRunning = true;
            string[] values = { "", "" };
            service.OnStart_Debug(values);
#endif
        }
    }
}
