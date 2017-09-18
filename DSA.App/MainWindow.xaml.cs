using DSA.App.Properties;
using DSA.Lib;
using DSA.Lib.Data;
using DSA.Lib.Models;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DSA.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = Settings.Default;
            PasswordTextBox.DataContext = this;
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
        }

        private async void TestApiConnectivityClick(object sender, RoutedEventArgs e)
        {
            TestApiConnectivityResponse.Text = "Working...";
            TestApiConnectivityButton.IsEnabled = false;

            try
            {
                TestApiConnectivityResponse.Text = await Utility.GetUpdater().TestApiConnectivity();
            }
            catch (Exception ex)
            {
                TestApiConnectivityResponse.Text = ex.Message;
            }
            finally
            {
                TestApiConnectivityButton.IsEnabled = true;
            }
        }

        private async void TestDataConnectivityClick(object sender, RoutedEventArgs e)
        {
            TestDataConnectivityResponse.Text = "Working...";
            TestDataConnectivityButton.IsEnabled = false;

            try
            {
                TestDataConnectivityResponse.Text = await Utility.GetUpdater().TestDataConnectivity();
            }
            catch (Exception ex)
            {
                TestDataConnectivityResponse.Text = ex.Message;
            }
            finally
            {
                TestDataConnectivityButton.IsEnabled = true;
            }
        }

        private async void TestIncidentsQueryClick(object sender, RoutedEventArgs e)
        {
            TestIncidentsQueryResponse.Text = "Working...";
            TestIncidentsQueryButton.IsEnabled = false;

            try
            {
                var updater = Utility.GetUpdater();
                var response = await updater.TestIncidentsQuery();
                TestIncidentsQueryResponse.Text = $"Found {response.Item2} incident records since {updater.LastUpdateOn} - {response.Item1}";
            }
            catch (Exception ex)
            {
                TestIncidentsQueryResponse.Text = ex.Message;
            }
            finally
            {
                TestIncidentsQueryButton.IsEnabled = true;
            }
        }

        private async void TestUnitsQueryClick(object sender, RoutedEventArgs e)
        {
            TestUnitsQueryResponse.Text = "Working...";
            TestUnitsQueryButton.IsEnabled = false;

            try
            {
                var updater = Utility.GetUpdater();
                var response = await updater.TestUnitsQuery();
                TestUnitsQueryResponse.Text = $"Found {response.Item2} unit records since {updater.LastUpdateOn} - {response.Item1}";
            }
            catch (Exception ex)
            {
                TestUnitsQueryResponse.Text = ex.Message;
            }
            finally
            {
                TestUnitsQueryButton.IsEnabled = true;
            }
        }

        private void RunAllClick(object sender, RoutedEventArgs e)
        {
            RunAllResponse.Text = "Working...";
            RunAllButton.IsEnabled = false;

            try
            {
                var batches = Utility.GetUpdater().Run();
                var message = $"Successfully submitted {batches.Count()} batch(es): {string.Join(", ", batches.ToArray())}";
                LogClient.Log(message);
                RunAllResponse.Text = message;
            }
            catch (Exception ex)
            {
                RunAllResponse.Text = ex.Message;
            }
            finally
            {
                RunAllButton.IsEnabled = true;
            }
        }

        private void CreateTaskClick(object sender, RoutedEventArgs e)
        {
            CreateTaskResponse.Text = "Working...";
            CreateTaskButton.IsEnabled = false;
            var taskMessage = string.Empty;
            var taskError = string.Empty;

            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = $"schtasks.exe /Create /RU \"NT AUTHORITY\\SYSTEM\" /SC DAILY /TN \"{ Settings.Default.TaskName }\" /TR \"{Assembly.GetExecutingAssembly().Location} -s\" /RI {Settings.Default.RunInterval} /DU 24:00 /RL HIGHEST",
                        //Arguments = $"/Create /RU \"NT AUTHORITY\\SYSTEM\" /SC DAILY /TN \"{ Settings.Default.TaskName }\" /TR \"{Assembly.GetExecutingAssembly().Location} -s\" /RI {Settings.Default.RunInterval} /DU 24:00 /RL HIGHEST",
                        UseShellExecute = true,
                        //RedirectStandardOutput = true,
                        //CreateNoWindow = true,
                        //RedirectStandardError = true,
                        Verb = "runas"
                    }
                };

                proc.Start();
                //taskMessage = proc.StandardOutput.ReadToEnd();
                //taskError = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (!taskMessage.StartsWith("SUCCESS") || !string.IsNullOrWhiteSpace(taskError))
                    throw new Exception(taskMessage);

                CreateTaskResponse.Text = taskMessage;
            }
            catch (Exception ex)
            {
                CreateTaskResponse.Text = string.IsNullOrWhiteSpace(ex.Message) ? string.IsNullOrWhiteSpace(taskError) ? "unkown error" : taskError : ex.Message;
            }
            finally
            {
                CreateTaskButton.IsEnabled = true;
            }
        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            if (Tabs.SelectedIndex != Tabs.Items.Count - 1)
                Tabs.SelectedIndex++;

        }
        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            if (Tabs.SelectedIndex != 0)
                Tabs.SelectedIndex--;
        }
    }
}
