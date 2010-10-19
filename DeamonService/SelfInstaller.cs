using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Configuration.Install;

namespace DeamonService
{
    public static class  SelfInstaller
    {
        private static readonly string exePath = Assembly.GetExecutingAssembly().Location;
        
        public static bool InstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(
                    new string[] { exePath });
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool UninstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(
                    new string[] { "/u", exePath });
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
