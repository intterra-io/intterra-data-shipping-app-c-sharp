using DSA.App.Properties;
using DSA.Lib;
using DSA.Lib.Models;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                TestIncidentsQueryResponse.Text = await Utility.GetUpdater().TestIncidentsQuery();
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
                TestUnitsQueryResponse.Text = await Utility.GetUpdater().TestUnitsQuery();
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

        private async void RunAllClick(object sender, RoutedEventArgs e)
        {
            RunAllResponse.Text = "Working...";
            RunAllButton.IsEnabled = false;

            try
            {
                var batchUuid = await Utility.GetUpdater().Run();
                RunAllResponse.Text = batchUuid.ToString();
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
            if (string.IsNullOrWhiteSpace(PasswordTextBox.Password))
            {
                CreateTaskResponse.Text = "Password is required";
                return;
            }

            CreateTaskResponse.Text = "Working...";
            CreateTaskButton.IsEnabled = false;

            try
            {
                // Get the service on the local machine
                using (TaskService ts = new TaskService())
                {
                    // Create a new task definition and assign properties
                    TaskDefinition td = ts.NewTask();
                    td.Settings.Enabled = false;
                    td.RegistrationInfo.Description = Settings.Default.TaskDescription;
                    td.Principal.RunLevel = TaskRunLevel.LUA;
                    td.Principal.UserId = WindowsIdentity.GetCurrent().Name;
                    //td.Principal.LogonType = TaskLogonType.Password;
                    td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    td.Settings.Hidden = true;

                    // Create a trigger that will fire the task at this time every other day
                    var dt = new DailyTrigger();
                    dt.DaysInterval = 1;
                    dt.Repetition.Duration = TimeSpan.FromDays(1);
                    dt.Repetition.Interval = TimeSpan.FromMinutes(Settings.Default.RunInterval);

                    // Create an action that will launch Notepad whenever the trigger fires
                    td.Actions.Add(new ExecAction(Assembly.GetExecutingAssembly().Location, "-s"));

                    // Register the task in the root folder
                    var newTask = ts.RootFolder.RegisterTaskDefinition(Settings.Default.TaskName, td);

                    var taskMessage = string.Empty;
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = $"/change /TN \"{Settings.Default.TaskName}\" /RU {WindowsIdentity.GetCurrent().Name} /RP {PasswordTextBox.Password}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    proc.Start();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        taskMessage += proc.StandardOutput.ReadLine();
                        // do something with line
                    }

                    if (!taskMessage.StartsWith("SUCCESS"))
                    {
                        throw new Exception(taskMessage);
                    }

                    CreateTaskResponse.Text = $"Schedule successfully created: '{newTask.Name}'";
                    PasswordTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                CreateTaskResponse.Text = ex.Message;
            }
            finally
            {
                CreateTaskButton.IsEnabled = true;
            }
        }
    }
}
