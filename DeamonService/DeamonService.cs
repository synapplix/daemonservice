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
        private System.Diagnostics.Process runningProcess;

        public DeamonService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (!File.Exists(getStringSetting("StartupScript")))
                throw new Exception("No file: " + getStringSetting("StartupScript"));

            new Thread(ExecuteCommand).Start();
        }

        private void ExecuteCommand()
        {
            String startupScript = getStringSetting("StartupScript");
            String startupScriptArguments = getStringSetting("StartupScript.Arguments");
            runningProcess = System.Diagnostics.Process.Start(startupScript, startupScriptArguments);
            runningProcess.WaitForExit();
            if (runningProcess.ExitCode != 0)
                throw new Exception("Error while executing: " + startupScript);
        }

        protected override void OnStop()
        {
            try
            {
                String shutdownScript = getStringSetting("ShutdownScript");
                if (!string.IsNullOrEmpty(shutdownScript))
                {
                    String shutdownScriptArguments = getStringSetting("ShutdownScript.Arguments");
                    System.Diagnostics.Process process = System.Diagnostics.Process.Start(shutdownScript, shutdownScriptArguments);
                    process.WaitForExit();
                    return;
                }

                if (null != runningProcess && !runningProcess.HasExited)
                    runningProcess.Kill();
            }
            finally {
                runningProcess = null;
            }
        }

        private string getStringSetting(string name)
        {
            try
            {
                return ConfigurationSettings.AppSettings[name];
            }
            catch (Exception) {
                return null;
            }
        }
    }
}
