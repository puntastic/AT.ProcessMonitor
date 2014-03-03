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
        public ConfigItem config = null;
        public Stopwatch emailTimer { get { return _emailTimeout; } } //one stopwatch, no need to re-inizialize, ever.
        public int? exitCode { get; protected set; }


        protected Stopwatch _emailTimeout = new Stopwatch();
        protected bool NotifyOfNotStarted = false; //boolean flag to ensure that user is only notified of the program not being started once


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

        /// <summary>
        /// Start or attach to a process as specified in the configuration options, then wait for it to exit
        /// </summary>
        /// <param name="startIfNotFound">Weather to start a new process if a valid running process is not found</param>
        /// <returns>True if a process was found/started and it exited, false otherwise</returns>
        public bool StartOrAttachToProcessAndWait(bool startIfNotFound)
        {
            attachedProcess = null;
            bool NotMonitorStarted = false;

            //Find all processes by the configured process name, and attach to the first one it finds in the configured target directory
            List<Process> processes = new List<Process>(Process.GetProcessesByName(config.startOptions.ProcessName));
            foreach (Process targetProcess in processes)
            {
                //NOTE: a 32 bit process cannot access 64 bit application file paths
                if (Path.GetDirectoryName(targetProcess.Modules[0].FileName) == config.Directory)
                {
                    Console.WriteLine("\nAttaching to existing {0}", config.startOptions.ProcessName);
                    Console.WriteLine("At {0}", config.startOptions.Filename);
                    attachedProcess = targetProcess;
                    NotMonitorStarted = true;
                }
            }

            //no valid running processes found, start a new one and attach to it
            if (attachedProcess == null && startIfNotFound)
            {
                Console.WriteLine("\n{0} isn't running: Starting...", config.startOptions.ProcessName);

                ProcessStartInfo startInfo = new ProcessStartInfo(config.Directory + config.startOptions.Filename, config.startOptions.Args);

                startInfo.WorkingDirectory = config.Directory;
                attachedProcess = Process.Start(startInfo);
            }

            if (attachedProcess != null)
            {
                NotifyOfNotStarted = true;

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
                Console.WriteLine("\n{0} Has Exited.", config.startOptions.ProcessName);
                return true;
            }
            else
            {
                if (NotifyOfNotStarted)
                {
                    Console.WriteLine("\n{0} was not running and could not be started due to configuration; Waiting to attach to new process.", config.Directory + config.startOptions.Filename);

                    NotifyOfNotStarted = false;
                }
                return false;
            }
        }

    }
}
