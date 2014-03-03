using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;

using System.Net;
using System.Net.Mail;

namespace AT.ProcessMonitor
{
    class Monitor
    {
        public MonitoredProcess montProcess;
        public Reporter reporterRef;

        //are we starting the process for the first time? or simply restarting it?
        protected bool firstTimeStart = true;
        protected bool WaitForManualRestart = false; //false to prevent the process from being started by the monitor,

        public void Start()
        {
            Stopwatch restartTimer = new Stopwatch();
            int timeToWaitBeforeRestart_Milliseconds = 0;

            while (true)
            {
                //sleep for -at least- one second
                Thread.Sleep(1000);// sleep, inneficient but perfect for this situation...

                //waiting for timeout?
                if (restartTimer.IsRunning && restartTimer.ElapsedMilliseconds < timeToWaitBeforeRestart_Milliseconds)
                {
                    Console.WriteLine("{0}: Waiting {1} min and {2} seconds before restarting",
                        montProcess.config.startOptions.ProcessName,
                        (timeToWaitBeforeRestart_Milliseconds - restartTimer.ElapsedMilliseconds) / 60000,          //minutes to wait for start
                       ((timeToWaitBeforeRestart_Milliseconds - restartTimer.ElapsedMilliseconds) % 60000) / 1000);  //seconds to wait for start
                    continue;
                }



                try
                {
                    if (!montProcess.StartOrAttachToProcessAndWait((montProcess.config.startOptions.StartImmediately || !firstTimeStart) && !WaitForManualRestart))
                    {
                        continue;
                    }
                }
                catch (FileNotFoundException ex)
                {
                    reporterRef.SendException(ex.Message, montProcess.config.startOptions.ProcessName);
                }
                if (restartTimer.IsRunning)
                {
                    restartTimer.Reset();
                }




                //process has been started at least once, so get ready to restart it as needed
                WaitForManualRestart = false;
                firstTimeStart = false;

                bool exceptionLogFound;
                reporterRef.ReportOnExit
                (
                    out exceptionLogFound,
                    montProcess.config.exceptionLoggingOptions.TimeBetweenEmails_Minutes,
                    montProcess.emailTimer, montProcess.config.Directory + montProcess.config.startOptions.Filename,
                    montProcess.config.startOptions.ProcessName, montProcess.exitCode,
                    montProcess.config.Directory + montProcess.config.exceptionLoggingOptions.ExceptionLogFileName
                );

                //not using log file restart, so start the timer and wait around (while still looping) for it to lapse
                if (!montProcess.config.restartOptions.RestartOnlyIfLogFileIsPresent)
                {
                    restartTimer.Start();
                    timeToWaitBeforeRestart_Milliseconds = montProcess.config.restartOptions.RestartDelayIfNotUsingLogFileCheck_Minutes * 60 * 1000;
                }
                //no log found, and we are using log file restart, so wait for the program to start up again before performing any actions
                else if (!exceptionLogFound && montProcess.config.restartOptions.RestartOnlyIfLogFileIsPresent)
                {
                    WaitForManualRestart = true;
                }
                //else: restart emmediatly as the log file is was both found, and we are using it
            }
        }
    }
}

