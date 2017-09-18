using DSA.Lib.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
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
            LogClient.Init();

            if (e.Args.Contains("-s") || e.Args.Contains("--silent"))
            {
                try
                {
                    var batches = Utility.GetUpdater().Run();
                    LogClient.Log($"Successfully submitted {batches.Count()} batch(es): {string.Join(", ", batches.ToArray())}");
                }
                catch (Exception ex)
                {
                    LogClient.Log(ex.Message, "ERROR");
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
