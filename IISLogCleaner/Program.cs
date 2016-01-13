using System.ServiceProcess;

namespace IISLogCleaner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] servicesToRun = {
                new Service()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}