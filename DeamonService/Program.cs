using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using DeamonService.UI;

namespace DeamonService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args != null && args.Length == 1 && args[0].Length > 1
                && (args[0][0] == '-' || args[0][0] == '/'))
            {
                switch (args[0].Substring(1).ToLower())
                {
                    default:
                        break;
                    case "install":
                    case "i":
                        SelfInstaller.InstallMe();
                        break;
                    case "uninstall":
                    case "u":
                        SelfInstaller.UninstallMe();
                        break;
                    case "console":
                    case "c":
                        Run();
                        break;
                }
            }
            else
                Launch();
        }       
        
        static void Launch()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new DeamonService() 
			};
            ServiceBase.Run(ServicesToRun);
        }

        static void Run()
        {
            DeamonService deamonService = new DeamonService();
            DebugConsole debugConsole = new DebugConsole(deamonService);
            debugConsole.ShowDialog();
        }
    }
}
