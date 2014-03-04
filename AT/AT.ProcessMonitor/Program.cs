using System;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace AT.ProcessMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessMonitorConfig monitorConfig;/* = new ProcessMonitorConfig
            {
                                                * 
                UseGlobalEmailTimeout = false,
                GlobalEmailTimeout_Minutes = 0,
                Items = new ConfigItem[]
                {
                    new ConfigItem
                    {
                        startOptions = new StartOptions
                        {
                            StartImmediately = true,
                            ProcessName = "AT.AutoGitUpdater",
                            Filename = @"C:\\Users\\Administrator\\Desktop\\Debug\\AT.AutoGitUpdater.exe",
                            Args = ""
                        },
                         restartOptions = new RestartOptions
                         {
                             RestartOnlyIfLogFileIsPresent = true,
                             RestartDelayIfNotUsingLogFileCheck_Minutes = 0
                         },
                     exceptionLoggingOptions = new ExceptionLoggingOptions
                     {
                         TimeBetweenEmails_Minutes = 10,
                         ExceptionLogFileName = "ExceptionLog.txt"
                     }
                    }
                }
            };

            File.WriteAllText("ProcessMonitorConfig.json", JsonConvert.SerializeObject(monitorConfig));*/

            //ProcessMonitorConfig monitorConfig;
            using (StreamReader configReader = new StreamReader("ProcessMonitorConfig.json"))
                monitorConfig = JsonConvert.DeserializeObject<ProcessMonitorConfig>(configReader.ReadToEnd());

            Reporter reporter = new Reporter
            {
                UserGlobalEmailTimeout = monitorConfig.UseGlobalEmailTimeout,
                GlobalEmailTimeout_Minutes = monitorConfig.GlobalEmailTimeout_Minutes
            };
            TaskFactory monitorFactory = new TaskFactory();

            foreach (var currentItem in monitorConfig.Items)
            {
                Monitor nextMonitor = new Monitor();
                nextMonitor.Create(currentItem, reporter, new MonitoredProcess());

                //create and start a new task - which does not need to be held on to since the thread pool will still reference it
                monitorFactory.StartNew(() => nextMonitor.Start());

            }

            Process.GetCurrentProcess().WaitForExit(); //wait... forevveeeerrrrr
        }
    }
}
