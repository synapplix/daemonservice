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
        private StreamPipe outPipe;
        private StreamPipe errPipe;
        private StreamWriter outStream;
        private StreamWriter errStream;

        public DeamonService()
        {
            InitializeComponent();
        }

        public void Run(string[] args)
        {
            OnStart(args);
        }

        public void StopService()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                PerpareLogFiles();

                Log(EventLogEntryType.Information, "Loading configuration");
                String startupScript = StartupScript;

                new Thread(ExecuteStartupScript).Start();
            }
            catch (Exception e)
            {
                Log(EventLogEntryType.Information, e.Message);
                throw (e);
            }
        }

        private void PerpareLogFiles()
        {
            outStream = new StreamWriter(OutLogFile, true, Encoding.UTF8);
            errStream = new StreamWriter(ErrorLogFile, true, Encoding.UTF8);
        }

        private string StartupScript
        {
            get
            {
                string workingDirectory = WorkingDirectory;
                String startupScript = GetStringSetting("StartupScript");
                if (string.IsNullOrEmpty(startupScript))
                    throw new Exception("No StartupScript defined!");

                if (!File.Exists(startupScript))
                {
                    if (!File.Exists(workingDirectory + "/" + startupScript))
                    {
                        throw new Exception(startupScript + " does not exist!");
                    }
                    startupScript = workingDirectory + "/" + startupScript;
                }

                return startupScript;
            }
        }

        private string WorkingDirectory
        {
            get{
                string workingDirectory = GetStringSetting("WorkingDirectory");
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    return null;
                }

                if (!Directory.Exists(workingDirectory))
                {
                    throw new Exception("Working directory doesn't exist: " + workingDirectory);
                }

                return workingDirectory;
            }
        }

        private void Log(EventLogEntryType e, string s){
            outStream.WriteLine(DateTime.Now + " - " + e + " - " + s);
            outStream.Flush();
            this.EventLog.WriteEntry(s, e);
        }

        private void PipeOutput(Process process)
        {
            outPipe = new StreamPipe(process.StandardOutput.BaseStream, outStream.BaseStream);
            errPipe = new StreamPipe(process.StandardError.BaseStream, errStream.BaseStream);
        }

        private void ExecuteStartupScript()
        {
            try
            {
                string workingDirectory = WorkingDirectory;
                String startupScript = StartupScript;
                String startupScriptArguments = GetStringSetting("StartupScript.Arguments");

                Log(EventLogEntryType.Information, "Starting process: " + startupScript + " " + startupScriptArguments);

                runningProcess = new Process();
                // Redirect the output stream of the child process.
                runningProcess.StartInfo.UseShellExecute = false;
                runningProcess.StartInfo.RedirectStandardOutput = true;
                runningProcess.StartInfo.RedirectStandardError = true;
                runningProcess.StartInfo.FileName = startupScript;
                runningProcess.StartInfo.Arguments = startupScriptArguments;

                if (null != workingDirectory)
                {
                    runningProcess.StartInfo.WorkingDirectory = workingDirectory;
                    Log(EventLogEntryType.Information, "Executing " + startupScript + " in working directory " + workingDirectory);
                }else
                    Log(EventLogEntryType.Information, "Executing " + startupScript);

                
                runningProcess.Start();

                PipeOutput(runningProcess);

                if (null != runningProcess)
                    runningProcess.WaitForExit();
                if (null != runningProcess && runningProcess.ExitCode != 0)
                {
                    throw new Exception("Error while executing: " + startupScript + "\r\nProcess exited with exit code: " + runningProcess.ExitCode);
                }
            }
            catch (Exception e) {
                Log(EventLogEntryType.Error, e.Message + "See log files for more details! Logs (" + OutLogFile + ", " + ErrorLogFile + ")");
                throw (e);
            }

        }

        protected override void OnStop()
        {
            try
            {
                string workingDirectory = WorkingDirectory;
                String shutdownScript = GetStringSetting("ShutdownScript");

                if (!string.IsNullOrEmpty(shutdownScript))
                {
                    String shutdownScriptArguments = GetStringSetting("ShutdownScript.Arguments");
                    try
                    {
                        if (!File.Exists(shutdownScript))
                        {
                            if (!File.Exists(workingDirectory + "/" + shutdownScript))
                            {
                                throw new Exception(shutdownScript + " does not exist!");
                            }
                            shutdownScript = workingDirectory + "/" + shutdownScript;
                        }

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
                            Log(EventLogEntryType.Information, "Executing " + shutdownScript + " in working directory " + workingDirectory);
                        }
                        else
                            Log(EventLogEntryType.Information, "Executing " + shutdownScript);

                        process.Start();

                        PipeOutput(runningProcess);

                        process.WaitForExit();

                        int afterExitDelay = AfterExitDelay;
                        if (afterExitDelay > 0) {
                            int delay = AfterExitDelay * 1000;
                            while (delay > 0 &&  null != runningProcess && !runningProcess.HasExited)
                            {
                                delay -= 10;
                                Thread.Sleep(10);
                            }
                        }
                    }
                    catch (Exception e) {
                        Log(EventLogEntryType.Error, e.Message);
                    }
                }else
                    Log(EventLogEntryType.Information, "No shutdown script defined! Killing process!");


                if (null != runningProcess && !runningProcess.HasExited) 
                {
                    ProcessUtility.KillTree(runningProcess.Id);
                }
            }
            catch (Exception e)
            {
                Log(EventLogEntryType.Error, e.Message);
                throw (e);
            }
            finally
            {
                CloseSafely(outPipe);
                CloseSafely(errPipe);
                
                outPipe = null;
                errPipe = null;
                outStream = null;
                errStream = null;
            }
        }

        private int AfterExitDelay
        {
            get
            {
                String afterExitDelayString = GetStringSetting("ShutdownScript.AfterExitDelay");
                int afterExitDelay;
                if (!int.TryParse(afterExitDelayString, out afterExitDelay))
                    afterExitDelay = 3;

                return afterExitDelay < 0 ? 0 : afterExitDelay;
            }
        }

        private string OutLogFile
        {
            get
            {
                return GetFile(GetStringSetting("Logging.OutLogFileName", LogsDirectory + "\\out.log"));
            }
        }

        private string ErrorLogFile
        {
            get
            {
                return GetFile(GetStringSetting("Logging.OutLogFileName", LogsDirectory + "\\error.log"));
            }
        }

        private string GetFile(string file)
        {
            string outFile = file;
            if (!IsAbsolutePath(outFile))
            {
                outFile = WorkingDirectory + "\\" + outFile;
            }

            DirectoryInfo dir = new FileInfo(outFile).Directory;
            if (!dir.Exists)
            {
                Directory.CreateDirectory(dir.FullName);
            }

            return outFile;
        }

        private string LogsDirectory
        {
            get
            {
                string logsDirectory = GetFile(GetStringSetting("Logging.LogsDirectory", "Logs"));
                
                if (!Directory.Exists(logsDirectory))
                {
                    Directory.CreateDirectory(logsDirectory);
                }

                return logsDirectory;
            }
        }

        private bool IsAbsolutePath(string path)
        {
            if(null==path)
                return false;

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if(path.ToLower().StartsWith(drive.RootDirectory.Name.ToLower()))
                    return true;
            }
            
            return false;
        }

        private string GetStringSetting(string name)
        {
            return GetStringSetting(name, null);
        }

        private string GetStringSetting(string name, string defaultValue)
        {
            try
            {
                string value = ConfigurationSettings.AppSettings[name];
                return value ?? defaultValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }


        private void CloseSafely(StreamPipe pipe)
        {
            try
            {
                pipe.Close();
            }
            catch { }
        }
    }
}
