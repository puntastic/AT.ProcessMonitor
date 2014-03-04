using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Timers;
namespace AT.ProcessMonitor
{
    /// <summary>
    /// Attempts to capture and report on process termination stuffs
    /// </summary>
    class Reporter
    {
        public bool UserGlobalEmailTimeout;
        public int GlobalEmailTimeout_Minutes;

        protected Stopwatch _globalEmailTimer = new Stopwatch();

        private object _threadLock = new object();

        /// <summary>
        /// Email a pre-configured email with a number of details about a processes' exit state
        /// </summary>
        /// <param name="exceptionLogFound">weather or not the specified exception log was found</param>
        /// <param name="emailTimeout_Minutes">timeout to wait before email, ignored if global timeout is set</param>
        /// <param name="targetStopwatchRef">stopwatch to use for the email timeout, ignored if global timeout is set</param>
        /// <param name="processFileNameAndDirectory">directory for the process that we are reporting on</param>
        /// <param name="processName">process name that we are reporting on</param>
        /// <param name="exitCode">The programs exit code, if any, or null for none</param>
        /// <param name="logFile">log file to look for, or null if none</param>
        public void SendReport(out bool exceptionLogFound, int emailTimeout_Minutes, Stopwatch targetStopwatchRef, string processFileNameAndDirectory, string processName, int? exitCode, string logFile)
        {
            if (logFile != null && File.Exists(logFile))
            {
                // SendException(File.ReadAllText(logFile), processName);
                exceptionLogFound = true;
            }
            else
            {
                exceptionLogFound = false;
            }
            
            //References ftw
            if (UserGlobalEmailTimeout)
            {
                targetStopwatchRef = _globalEmailTimer;
                emailTimeout_Minutes = GlobalEmailTimeout_Minutes;
            }
            //else// emailTimeout = no need to set - already the value we want

            //critical section since multiple thread can access the same reporter object
            lock (_threadLock)
            {
                int emailTimeout_MS = emailTimeout_Minutes * 60 * 1000;
                if (targetStopwatchRef.IsRunning && targetStopwatchRef.ElapsedMilliseconds < emailTimeout_MS)
                {
                    Console.WriteLine("Exception raised but waiting {0} m {1} s to send new email...",
                        (emailTimeout_MS - targetStopwatchRef.ElapsedMilliseconds) / 60000,  // minutes
                        ((emailTimeout_MS - targetStopwatchRef.ElapsedMilliseconds) % 60000) / 1000); // seconds

                    return;
                }

                targetStopwatchRef.Restart();
            }

                string preMessage = "FileName:" + processFileNameAndDirectory + "\n\n";
                Console.WriteLine("Exception raised: Sending Email.");

                if (exceptionLogFound)
                {
                    SendException(File.ReadAllText(logFile), processName);
                }
                //no exit code recorded
                else if (exitCode != null)
                {
                    SendException(preMessage + "No log file, Exit Code: " + exitCode, processName);
                }
                else
                {
                     SendException(File.ReadAllText(preMessage +
                      "No log file, Could not capture exit code (invalid process handle), did the monitor not start the process?"),
                       processName);
                }
       
        }



        public void SendException(string message, string processName, params string[] extraTags)
        {

            //Console.WriteLine(message); //Potentially confusing as it can be mistaken for exceptions within the monitor if displayed

            WebClient webClient = new WebClient();
            string instanceID = "Unkown Instance";
            string availabilityZone = "Unknown Availability Zone";
            //mail encode the message
            message = message.Replace("\n", "<br/>");

            try
            {
                instanceID = webClient.DownloadString("http://169.254.169.254/latest/meta-data/instance-id");
                availabilityZone = webClient.DownloadString("http://169.254.169.254/latest/meta-data/placement/availability-zone");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            StringBuilder tags = new StringBuilder();
            foreach(string currentTag in extraTags)
            {
                tags.Append("[" + currentTag + "]");
            }
            //TODO: Change email target
            EmailHelper.SendGmail(tags.ToString() + "[" + processName + "] on [" + instanceID + "] at [" + availabilityZone + "] caused an exception",
                message, true, new MailAddress[] { new MailAddress("example@gmail.com", "example user") }, null, null, null);


        }
    }
}
