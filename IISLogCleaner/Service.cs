using System;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace IISLogCleaner
{
    public partial class Service : ServiceBase
    {
        //The root directory to start searching for logs
        private static string _rootLogSearchDirectory = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\logs");
        //The number of days (since last write) that a log can be stale)
        private int _logDaysToKeep = 7;
        //The check interval in minutes
        private static int _intervalInMinutes = 15;
        //If the disk space is below this threshold (in MB) then start deleting the oldest logs first regardless of last write time
        private static long _lowDiskThreshold = 1000;

        private Timer _workTimer;
        private static EventLog _eventLog = new EventLog();
        
        public Service()
        {
            if (EventLog.SourceExists("IIS Log Cleaner"))
            {
                EventLog.CreateEventSource("IISLogCleaner", "IIS Log Cleaner");
            }
            _eventLog.Source = "IISLogCleaner";

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Debug point
            //Thread.Sleep(30000);

            TimerCallback tcb = DoWork;
            _workTimer = new Timer(tcb, new object(), _intervalInMinutes*60*1000, _intervalInMinutes*60*1000);
            CheckForTimerChange();

            base.OnStart(args);
            _eventLog.WriteEntry("IIS Log Cleaner Started", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _workTimer.Dispose();
            _eventLog.WriteEntry("IIS Log Cleaner Stopped", EventLogEntryType.Information);
            base.OnStop();
        }

        /// <summary>
        /// Searches the provided directory for log files that meet the config criteria
        /// </summary>
        /// <param name="state">The state provided by the timer callback</param>
        private void DoWork(object state)
        {
            CheckForDirectoryChange();
            CheckForLogDaysChange();
            CheckForLowDiskThresholdChange();
            
            if (Directory.Exists(_rootLogSearchDirectory))
            {
                foreach (string path in Directory.GetFileSystemEntries(_rootLogSearchDirectory, "*.log", SearchOption.AllDirectories).OrderBy(File.GetLastAccessTimeUtc))
                {
                    if (File.Exists(path) && (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(_logDaysToKeep*-1) || LowDiskThresholdCrossed()))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch
                        {
                            //For some reason we can't delete this. Let's leave it alone 
                            _eventLog.WriteEntry("Error deleting log file: " + path, EventLogEntryType.Error);
                        }
                    }
                }  
            }

            CheckForTimerChange();
        }

        /// <summary>
        /// Check the config file for a timer settings change
        /// </summary>
        private void CheckForTimerChange()
        {
            int tmpMinutes;
            try
            {
                tmpMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["CheckIntervalMinutes"]);
            }
            catch
            {
                tmpMinutes = 15;
            }

            if (tmpMinutes != _intervalInMinutes)
            {
                _intervalInMinutes = tmpMinutes;
                _workTimer.Change(_intervalInMinutes*60*1000, _intervalInMinutes*60*1000);
            }
        }

        /// <summary>
        /// Check the config file for a days threshold change
        /// </summary>
        private void CheckForLogDaysChange()
        {
            try
            {
                _logDaysToKeep = Convert.ToInt32(ConfigurationManager.AppSettings["DaysToKeep"]);
            }
            catch
            {
                _logDaysToKeep = 7;
            }
        }

        /// <summary>
        /// Check the config file for a low disk threshold change
        /// </summary>
        private void CheckForLowDiskThresholdChange()
        {
            try
            {
                _lowDiskThreshold = Convert.ToInt32(ConfigurationManager.AppSettings["LowDiskThresholdMB"]);
            }
            catch
            {
                _lowDiskThreshold = 1000;
            }
        }

        /// <summary>
        /// Check the config file for a directory config change
        /// </summary>
        private void CheckForDirectoryChange()
        {
            try
            {
                _rootLogSearchDirectory = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["RootLogSearchDirectory"]);
            }
            catch
            {
                _rootLogSearchDirectory = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\logs");
            }
        }

        /// <summary>
        /// Check the log disk for available space exceeding the threshold
        /// </summary>
        /// <returns></returns>
        private bool LowDiskThresholdCrossed()
        {
            DriveInfo[] disks = DriveInfo.GetDrives();
            long diskSpaceInMB =
                disks
                    .First(x => _rootLogSearchDirectory.Contains(x.Name))
                    .AvailableFreeSpace / 1024 / 1024;

            return diskSpaceInMB < _lowDiskThreshold;
        }
    }
}
