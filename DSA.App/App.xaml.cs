using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace DSA.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Contains("-s") || e.Args.Contains("--silent"))
            {
                try
                {
                    var task = Utility.GetUpdater().Run();
                    task.Wait();
                    using (var eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "Application";
                        eventLog.WriteEntry($"Successfully submitted batch: {task.Result.ToString()}", EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    using (var eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "Application";
                        eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
                    }
                    Shutdown(1);
                }
                Shutdown(0);
            }
            else
            {
                var window = new MainWindow();
                window.Show();
            }
        }
    }
}
