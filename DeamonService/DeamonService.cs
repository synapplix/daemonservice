using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Threading;
using System.IO;

namespace DeamonService
{
    public partial class DeamonService : ServiceBase
    {
        public DeamonService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (!File.Exists(ConfigurationSettings.AppSettings["StartupScript"]))
                throw new Exception("No file: " + ConfigurationSettings.AppSettings["StartupScript"]);

            new Thread(ExecuteCommand).Start();
        }

        private void ExecuteCommand()
        {
            String startupScript = ConfigurationSettings.AppSettings["StartupScript"];
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(startupScript);
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new Exception("Error while executing: " + startupScript);
        }

        protected override void OnStop()
        {
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(ConfigurationSettings.AppSettings["ShutdownScript"]);
            process.WaitForExit();
        }
    }
}
