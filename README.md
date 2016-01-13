# IISLogCleaner
A service for deleting IIS (or any) log files based on time or disk space thresholds.

Windows does not ship with any method to clean up old IIS logs. On a production server this can cause problems with disk space. There are some PowerShell and Task Scheduler methods out there for removing the old log files, but I felt they were incomplete and cleaning up IIS logs should be a Windows service. With that in mind I've put together this simple service to solve the problem. 

## Important Settings (App.config)
DaysToKeep - Instructs the service to look for logs older than the given value. (Default: 7)
CheckIntervalMinutes - How often the specified log folder should be checked. (Default: 15)
LowDiskThresholdMB - The minumum disk space below which the service will delete log files despite other settings. (Default: 1000)
RootLogSearchDirectory - Where to begin searching for log files. Being specific helps performance. (Default: %SystemDrive%\inetpub\logs)

## Assumptions
You're going to need to setup the service to run as an account with sufficient permissions to delete files out of the configured directory. The service also needs to be able to create Event Log sources.

The service currently assumes that it's looking for *.log files. That is the default for IIS.
