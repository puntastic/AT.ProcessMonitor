using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Diagnostics;
using System.IO;

namespace AT.ProcessMonitor
{
    class MonitoredProcess
    {
        public Process attachedProcess = null;

        public int? exitCode { get; protected set; }

        public string directory;
        public string fileName;
        public string processName;
        public string args;

        //are we starting the process for the first time? or simply restarting it?
        private bool _firstTimeStart = true;
        private bool _startImmediately = false;


        public bool? Active
        {
            get
            {
                //has never been attached to a process
                if (attachedProcess == null)
                {
                    return false;
                }

                //has been attached to a process that has exited
                try
                {
                    if (attachedProcess.HasExited)
                    {
                        return false;
                    }
                }
                //was attached to a process that no longer exists
                catch
                {
                    return false;
                }

                //a process is active
                return true;
            }
        }


        public void Create(string directory, string fileName, string processName, string args, bool startImmediatly)
        {
            this.directory = directory;
            this.fileName = fileName;
            this.processName = processName;
            this.args = args;

            _startImmediately = startImmediatly;
        }

        public void WaitForManualRestart()
        {
             _firstTimeStart = true;
            _startImmediately = false;
        }

        /// <summary>
        /// Start or attach to a process as specified in the configuration options, then wait for it to exit
        /// </summary>
        /// <param name="startIfNotFound">Weather to start a new process if a valid running process is not found</param>
        /// <returns>True if a process was found/started and it exited, false otherwise</returns>
        public bool StartOrAttachToProcessAndWait()
        {
            attachedProcess = null;
            bool NotMonitorStarted = false;

            //Find all processes by the configured process name, and attach to the first one it finds in the configured target directory
            List<Process> processes = new List<Process>(Process.GetProcessesByName(processName));
            foreach (Process targetProcess in processes)
            {
                //NOTE: a 32 bit process cannot access 64 bit application file paths
                if (Path.GetDirectoryName(targetProcess.Modules[0].FileName) + @"\" == directory) //directory has a "\"
                {
                    Console.WriteLine("\nAttaching to existing {0}", processName);
                    attachedProcess = targetProcess;

                    //process has been started at least once, so get ready to restart it as needed
                    NotMonitorStarted = true;
                    _firstTimeStart = false;
                }
            }

            //no valid running processes found, start a new one and attach to it
            if (attachedProcess == null && (_startImmediately || !_firstTimeStart))
            {
                Console.WriteLine("\n{0} isn't running: Starting...", processName);

                ProcessStartInfo startInfo = new ProcessStartInfo(directory + fileName, args);

                startInfo.WorkingDirectory = directory;
                attachedProcess = Process.Start(startInfo);
            }

            if (attachedProcess != null)
            {
                //a process created with start() holds on to a good amount of relavent information, including a valid handle 
                //that needs to be cleaned up
                attachedProcess.WaitForExit();
                if (NotMonitorStarted)
                {
                    exitCode = null;
                }
                else
                {
                    exitCode = attachedProcess.ExitCode;
                    attachedProcess.Close();
                }
                Console.WriteLine("\n{0} Has Exited.", processName);
                return true;
            }
            else
            {
                Console.WriteLine("\n{0} was not running and could not be started due to configuration; Waiting to attach to new process...", fileName);
                return false;
            }
        }

    }
}
