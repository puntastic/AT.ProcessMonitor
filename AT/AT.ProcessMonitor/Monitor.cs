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

        private MonitoredProcess _montProcess;
        private Reporter _reporterRef;

        private int _restartDelayIfNotUsingLogFileCheck_Minutes;
        private bool _restartOnlyIfLogFileIsPresent;
        private bool _notifyOfStartFailure = true;

        public int _timeBetweenEmails_Minutes;
        public string _exceptionLogFileName;

        private Stopwatch _emailTimer = new Stopwatch(); //one stopwatch, no need to re-inizialize, ever.
        private Stopwatch _emailTimeout = new Stopwatch();

        public void Create(ConfigItem config, Reporter reporter, MonitoredProcess montProcess)
        {
            _restartDelayIfNotUsingLogFileCheck_Minutes = config.restartOptions.RestartDelayIfNotUsingLogFileCheck_Minutes;
            _restartOnlyIfLogFileIsPresent = config.restartOptions.RestartOnlyIfLogFileIsPresent;

            _timeBetweenEmails_Minutes = config.exceptionLoggingOptions.TimeBetweenEmails_Minutes;
            _exceptionLogFileName = config.exceptionLoggingOptions.ExceptionLogFileName;

            _reporterRef = reporter;

            _montProcess = montProcess;

            _montProcess.Create(config.Directory, config.startOptions.FileName, config.startOptions.ProcessName,
                config.startOptions.Args, config.startOptions.StartImmediately);
        }

        public void Start()
        {
            Stopwatch restartTimer = new Stopwatch();
            int timeToWaitBeforeRestart_Milliseconds = 0;
            bool exceptionLogFound;

            while (true)
            {
                //sleep for -at least- one second
                Thread.Sleep(1000);// sleep, inneficient but perfect for this situation...

                //waiting for timeout?
                if (restartTimer.IsRunning && restartTimer.ElapsedMilliseconds < timeToWaitBeforeRestart_Milliseconds)
                {
                    Console.WriteLine("{0}: Waiting {1} min and {2} seconds before restarting",
                        _montProcess.processName,
                        (timeToWaitBeforeRestart_Milliseconds - restartTimer.ElapsedMilliseconds) / 60000,          //minutes to wait for start
                       ((timeToWaitBeforeRestart_Milliseconds - restartTimer.ElapsedMilliseconds) % 60000) / 1000);  //seconds to wait for start
                    continue;
                }

                try
                {
                    if (!_montProcess.StartOrAttachToProcessAndWait())
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not start {0}; {1}\n", _montProcess.processName, ex.Message);
                    _reporterRef.SendReport(out exceptionLogFound, _timeBetweenEmails_Minutes, _emailTimer, "", "ProcessMonitor", null, null);

                }
                if (restartTimer.IsRunning)
                {
                    restartTimer.Reset();
                }

                _reporterRef.SendReport
                (
                    out exceptionLogFound,
                    _timeBetweenEmails_Minutes,
                    _emailTimer, _montProcess.directory + _montProcess.fileName,
                    _montProcess.processName, _montProcess.exitCode,
                    _montProcess.directory + _exceptionLogFileName
                );

                //not using log file restart, so start the timer and wait around (while still looping) for it to lapse
                if (!_restartOnlyIfLogFileIsPresent)
                {
                    restartTimer.Start();
                    timeToWaitBeforeRestart_Milliseconds = _restartDelayIfNotUsingLogFileCheck_Minutes * 60 * 1000;
                }
                //no log found, and we are using log file restart, so wait for the program to start up again before performing any actions
                else if (!exceptionLogFound && _restartOnlyIfLogFileIsPresent)
                {
                    _montProcess.WaitForManualRestart();
                }
                //else: restart emmediatly as the log file is was both found, and we are using it
            }
        }
    }
}

