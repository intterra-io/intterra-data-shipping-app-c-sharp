using DSA.Lib;
using DSA.Lib.Data;
using DSA.Lib.Models;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

namespace DSA.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public UpdaterOpts Opts { get; set; } = SettingsClient.Get();

        const string TaskName = "Intterra Data Shipping App";
        const string TaskDescription = "Reads incident response data from RMS data sources as configured and send to Intterra's secure API";

        public MainWindow()
        {
            InitializeComponent();

            DataContext = Opts;

            if (IsAdministrator())
            {
                Title = $"{Title} (Administrator)";
            }
        }

        private async void TestApiConnectivityClick(object sender, RoutedEventArgs e)
        {
            TestApiConnectivityResponse.Text = "Working...";
            TestApiConnectivityButton.IsEnabled = false;

            try
            {
                TestApiConnectivityResponse.Text = await new Updater(Opts).TestApiConnectivity();
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
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Opts.Save();

            base.OnClosing(e);
        }

        private async void TestDataConnectivityClick(object sender, RoutedEventArgs e)
        {
            TestDataConnectivityResponse.Text = "Working...";
            TestDataConnectivityButton.IsEnabled = false;

            try
            {
                TestDataConnectivityResponse.Text = await new Updater(Opts).TestDataConnectivity();
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
                var updater = new Updater(Opts);
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
                var updater = new Updater(Opts);
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
                var batches = new Updater(Opts).Run();
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
            if (!IsAdministrator())
            {
                var dialogResult = MessageBox.Show("This function requires Adminitrator privileges.\n\nRestart in Admin mode?", "Administrator Required", MessageBoxButton.OKCancel);
                if (dialogResult == MessageBoxResult.OK)
                {
                    var proc = new ProcessStartInfo();
                    proc.UseShellExecute = true;
                    proc.FileName = Assembly.GetExecutingAssembly().Location;
                    proc.Verb = "runas";

                    try
                    {
                        Process.Start(proc);
                        App.Current.Shutdown(0);
                    }
                    catch
                    {
                        // Swallow
                    }
                }
                else
                {
                    return;
                }
            }

            CreateTaskResponse.Text = "Working...";
            CreateTaskButton.IsEnabled = false;
            var taskMessage = string.Empty;
            var taskError = string.Empty;

            try
            {
                using (TaskService ts = new TaskService())
                {

                    TaskDefinition td = ts.NewTask();
                    td.Settings.Enabled = true;
                    td.RegistrationInfo.Description = TaskDescription;
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                    td.Principal.UserId = "NT AUTHORITY\\SYSTEM";
                    td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    td.Settings.Hidden = true;

                    var dt = new DailyTrigger();
                    dt.DaysInterval = 1;
                    dt.Repetition.Duration = TimeSpan.FromDays(1);
                    dt.Repetition.Interval = TimeSpan.FromMinutes(Opts.RunInterval);
                    td.Triggers.Add(dt);

                    var action = new ExecAction(Assembly.GetExecutingAssembly().Location, "-s");
                    td.Actions.Add(action);

                    var newTask = ts.RootFolder.RegisterTaskDefinition(TaskName, td, TaskCreation.CreateOrUpdate, null);

                    CreateTaskResponse.Text = $"Schedule successfully created: '{newTask.Name}'";
                }
            }
            catch (Exception ex)
            {
                CreateTaskResponse.Text = string.IsNullOrWhiteSpace(ex.Message) ? "Unkown error" : ex.Message;
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

        private bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
