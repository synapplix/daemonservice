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
            try
            {
                string workingDirectory = WorkingDirectory;
                String startupScript = getStringSetting("StartupScript");
                if (string.IsNullOrEmpty(startupScript))
                    throw new Exception("No StartupScript defined!");

                if (!File.Exists(startupScript))
                    throw new Exception(startupScript + " does not exist!");

                new Thread(ExecuteStartupScript).Start();
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                throw (e);
            }
        }

        private string WorkingDirectory
        {
            get{
                string workingDirectory = getStringSetting("WorkingDirectory");
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    return null;
                }

                if (!Directory.Exists(workingDirectory))
                {
                    throw new Exception("Working directory doesn't exist: " + workingDirectory);
                }

                return workingDirectory;
                //Directory.SetCurrentDirectory(workingDirectory);
            }
        }

        private void ExecuteStartupScript()
        {
            try
            {
                string workingDirectory = WorkingDirectory;
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
                if (null != workingDirectory)
                {
                    runningProcess.StartInfo.WorkingDirectory = workingDirectory;
                    this.EventLog.WriteEntry("Executing "+startupScript +" in working directory "+workingDirectory, EventLogEntryType.Information);
                }else
                    this.EventLog.WriteEntry("Executing " + startupScript , EventLogEntryType.Information);

                
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
                string workingDirectory = WorkingDirectory;
                String shutdownScript = getStringSetting("ShutdownScript");

                if (!string.IsNullOrEmpty(shutdownScript))
                {
                    String shutdownScriptArguments = getStringSetting("ShutdownScript.Arguments");
                    try
                    {
                        if (!File.Exists(shutdownScript))
                            throw new Exception(shutdownScript + " does not exist!");

                        Process process = new Process();
                        // Redirect the output stream of the child process.
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.FileName = shutdownScript;
                        process.StartInfo.Arguments = shutdownScriptArguments;
                        if (null != workingDirectory)
                        {
                            process.StartInfo.WorkingDirectory = workingDirectory;
                            this.EventLog.WriteEntry("Executing " + shutdownScript + " in working directory " + workingDirectory, EventLogEntryType.Information);
                        }
                        else
                            this.EventLog.WriteEntry("Executing " + shutdownScript, EventLogEntryType.Information);

                        process.Start();

                        process.WaitForExit();

                        int afterExitDelay = AfterExitDelay;
                        if (afterExitDelay > 0)
                            Thread.Sleep(AfterExitDelay);
                    }
                    catch (Exception e) {
                        this.EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                    }
                }else
                    this.EventLog.WriteEntry("No shutdown script defined! Killing process!", EventLogEntryType.Information);


                if (null != runningProcess && !runningProcess.HasExited) 
                {
                    ProcessUtility.KillTree(runningProcess.Id);
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                throw (e);
            }
            finally
            {
                runningProcess = null;
                runningProcessOutput = null;
                runningProcessError = null;
            }
        }

        private int AfterExitDelay
        {
            get
            {
                String afterExitDelayString = getStringSetting("ShutdownScript.AfterExitDelay");
                int afterExitDelay;
                if (!int.TryParse(afterExitDelayString, out afterExitDelay))
                    afterExitDelay = 3;

                return afterExitDelay < 0 ? 0 : afterExitDelay;
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
