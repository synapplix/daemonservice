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
        private string runningProcessOutput;
        private string runningProcessError;

        public DeamonService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SetWorkingDirectory();
            if (!File.Exists(getStringSetting("StartupScript")))
                throw new Exception("No file: " + getStringSetting("StartupScript"));

            new Thread(ExecuteCommand).Start();
        }

        private void SetWorkingDirectory()
        {
            String workingDirectory = getStringSetting("WorkingDirectory");
            if (string.IsNullOrEmpty(workingDirectory))
            {
                return;
            }

            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            Directory.SetCurrentDirectory(workingDirectory);
        }

        private void ExecuteCommand()
        {
            try
            {
                String startupScript = getStringSetting("StartupScript");
                String startupScriptArguments = getStringSetting("StartupScript.Arguments");
                this.EventLog.WriteEntry("Starting process: " + startupScript + " " + startupScriptArguments, EventLogEntryType.Information);
                Process runningProcess = new Process();
                // Redirect the output stream of the child process.
                runningProcess.StartInfo.UseShellExecute = false;
                runningProcess.StartInfo.RedirectStandardOutput = true;
                runningProcess.StartInfo.RedirectStandardError = true;
                runningProcess.StartInfo.FileName = startupScript;
                runningProcess.StartInfo.Arguments = startupScriptArguments;
                runningProcess.Start(); 

                runningProcessOutput = runningProcess.StandardOutput.ReadToEnd();
                runningProcessError = runningProcess.StandardError.ReadToEnd();
                
                runningProcess.WaitForExit();
                if (runningProcess.ExitCode != 0)
                {
                    throw new Exception("Error while executing: " + startupScript + "\r\nProcess exited with exit code: " + runningProcess.ExitCode);
                }
            }
            catch (Exception e) {
                //this.EventLog.WriteEntry(runningProcess.StandardOutput.ReadToEnd(), EventLogEntryType.Error);
                this.EventLog.WriteEntry(e.Message + ProcessOutput, EventLogEntryType.Error);
                throw (e);
            }

        }

        private string ProcessOutput {
            get {
                string output = "\r\nOutput:";
                if (!string.IsNullOrEmpty(runningProcessOutput))
                    output += "\r\n"+runningProcessOutput;
                
                output += "\r\nError:";
                if (!string.IsNullOrEmpty(runningProcessError))
                    output += "\r\n" + runningProcessError;

                return output;
            }
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
                {
                    ProcessUtility.KillTree(runningProcess.Id);
                }
            }
            finally {
                runningProcess = null;
                runningProcessOutput = null;
                runningProcessError = null;
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
