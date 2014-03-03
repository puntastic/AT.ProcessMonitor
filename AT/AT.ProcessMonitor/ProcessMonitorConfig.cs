using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AT.ProcessMonitor
{
    public class ProcessMonitorConfig
    {
        public bool UseGlobalEmailTimeout;                   //tired of emails as individual processes fail? set this to true WARNING: if two seperate processes throw, they will use the same timeout
        public int GlobalEmailTimeout_Minutes;
        public ConfigItem[] Items;
    }

    public class ConfigItem 
    {

        private string _directory;
        public string Directory //path to look for when searching for both the .exe and the process, logfilename etc.
        {
            //get/set allows this variable to json (de)serialize just fine
            get
            {
                return _directory.TrimEnd(new char[] { '\\' }) + "\\"; //it does not matter if \\ is added -to the end- or not
            }     
            set
            {
                _directory = value;
            }
        }

        public StartOptions startOptions;
        public RestartOptions restartOptions;
        public ExceptionLoggingOptions exceptionLoggingOptions;
    }

    public class StartOptions
    {
        public bool StartImmediately;

        public string ProcessName;
        public string Filename;                         
        public string Args;
    }

    public class RestartOptions
    {
        public bool RestartOnlyIfLogFileIsPresent;      // When True, monitor does not try to restart the target application if there is no Exception Log file present
        public int RestartDelayIfNotUsingLogFileCheck_Minutes;  // Time to wait for the target application to restart itself or be restarted by an external agent if not using the Exception log file method

    }

    public class ExceptionLoggingOptions
    {
        public int TimeBetweenEmails_Minutes;
        public string ExceptionLogFileName;             // includes entire directory path
    }
}
