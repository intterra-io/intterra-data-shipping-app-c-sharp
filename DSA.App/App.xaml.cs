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
            var window = new MainWindow();
            window.Show();
        }
    }

    public static class Constants
    {
        public const string OrgName = "Intterra";
        public const string AppName = "DSA";
    }
}
