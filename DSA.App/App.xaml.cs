using DSA.Lib;
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
                    var opts = SettingsClient.Get();

                    var profileIx = Array.FindIndex(e.Args, x => x == "-p" || x == "--profile");
                    if (profileIx == -1 || profileIx + 1 > e.Args.Length - 1)
                    {
                        throw new Exception("Profile must be specified");
                    }

                    var profileName = e.Args[profileIx + 1];
                    var profile = opts.Profiles[profileName];
                    if (profileIx == -1 || profileIx + 1 > e.Args.Length - 1)
                    {
                        throw new Exception($"Profile not found: {profileName}");
                    }

                    var batches = new Updater(profile).Run();
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

    public static class Constants
    {
        public const string OrgName = "Intterra";
        public const string AppName = "DSA";
    }
}
