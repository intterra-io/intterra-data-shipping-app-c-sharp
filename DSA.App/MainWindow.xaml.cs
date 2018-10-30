﻿using DSA.Lib;
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
using System.Windows;
using System.Windows.Controls;

namespace DSA.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public UpdaterOpts Opts { get; set; } = SettingsClient.GetSettings();

        const string TaskName = "Intterra Data Shipping App";
        const string TaskDescription = "Reads incident response data from CAD, AVL, and/or RMS data sources as configured and sends to Intterra's secure API";
        const string GithubUrl = "https://github.com/intterra/intterra-data-shipping-app-c-sharp";

        public MainWindow()
        {
            InitializeComponent();

            DataContext = Opts;

            if (Opts.CurrentProfileId != Guid.Empty)
            {
                Opts.CurrentProfile = Opts.Profiles.FirstOrDefault(x => x.Id == Opts.CurrentProfileId);
            }

            if (Opts.CurrentProfile == null)
            {
                Opts.CurrentProfile = Opts.Profiles.FirstOrDefault();
            }

            Opts.CurrentProfileNotNull = Opts.CurrentProfile != null;

            SetTitle();
            VersionLabel.Content = Assembly.GetEntryAssembly().GetName().Version;
            DsaAppWindow.ContentRendered += DsaAppWindow_ContentRendered;
        }

        private void DsaAppWindow_ContentRendered(object sender, EventArgs e)
        {
            // jump to and create schedule 
            if (Environment.GetCommandLineArgs().Contains("--schedule"))
            {
                Tabs.SelectedIndex = Tabs.Items.Count - 1;
                CreateTask();
            }
        }

        private void SetTitle()
        {
            var newTitle = $"{TaskName}";
            newTitle += Opts.CurrentProfile != null ? $" [{Opts.CurrentProfile.Name}]" : "";
            newTitle += IsAdministrator() ? " (Administrator)" : "";
            Title = newTitle;
        }

        private async void TestApiConnectivityClick(object sender, RoutedEventArgs e)
        {
            TestApiConnectivityResponse.Text = "Working...";
            TestApiConnectivityButton.IsEnabled = false;

            try
            {
                TestApiConnectivityResponse.Text = await new Updater(Opts.CurrentProfile).TestApiConnectivity();
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
            if (SettingsClient.HasChanges(Opts))
            {
                var previouslySavedOn = Opts.SavedOn;
                var dialogResult = MessageBox.Show("There are unsaved changes, would you like to save?", "Save Changes", MessageBoxButton.YesNoCancel);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    Save();

                    // Cancel closing event if save failed
                    if (previouslySavedOn == Opts.SavedOn)
                    {
                        e.Cancel = true;
                    }
                }
                else if (dialogResult == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    base.OnClosing(e);
                }
                else
                {
                    base.OnClosing(e);
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }

        private async void TestDataConnectivityClick(object sender, RoutedEventArgs e)
        {
            TestDataConnectivityResponse.Text = "Working...";
            TestDataConnectivityButton.IsEnabled = false;

            try
            {
                TestDataConnectivityResponse.Text = await new Updater(Opts.CurrentProfile).TestDataConnectivity();
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

        //private async void TestQueryClick(object sender, RoutedEventArgs e)
        //{
        //    TestIncidentsQueryResponse.Text = "Working...";
        //    TestQueryButton.IsEnabled = false;

        //    try
        //    {
        //        var updater = new Updater(Opts.CurrentProfile);
        //        var response = await updater.TestIncidentsQuery();
        //        TestIncidentsQueryResponse.Text = $"Last modified date from API: { (updater.LastUpdateOn != null ? updater.LastUpdateOn.ToString() : "N/A") }\n\nFound {response.Item2} incident records: {response.Item1}";
        //    }
        //    catch (Exception ex)
        //    {
        //        TestIncidentsQueryResponse.Text = ex.Message;
        //    }
        //    finally
        //    {
        //        TestIncidentsQueryButton.IsEnabled = true;
        //    }
        //}

        //private async void TestUnitsQueryClick(object sender, RoutedEventArgs e)
        //{
        //    TestUnitsQueryResponse.Text = "Working...";
        //    TestUnitsQueryButton.IsEnabled = false;

        //    try
        //    {
        //        var updater = new Updater(Opts.CurrentProfile);
        //        var response = await updater.TestUnitsQuery();
        //        TestUnitsQueryResponse.Text = $"Last modified date from API: { (updater.LastUpdateOn != null ? updater.LastUpdateOn.ToString() : "N/A") }\n\nFound {response.Item2} unit records: {response.Item1}";
        //    }
        //    catch (Exception ex)
        //    {
        //        TestUnitsQueryResponse.Text = ex.Message;
        //    }
        //    finally
        //    {
        //        TestUnitsQueryButton.IsEnabled = true;
        //    }
        //}

        private void RunAllClick(object sender, RoutedEventArgs e)
        {
            RunAllResponse.Text = "Working...";
            RunAllButton.IsEnabled = false;

            try
            {
                var response = new Updater(Opts.CurrentProfile).Run();
                var message = response.ToString();
                LogClient.Log(new LogEntry(message, "INFO", Opts.CurrentProfile.ApiKey), Opts.RemoteLogging, Opts.LogUrl);
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
                    // we can assume a user wants to save their settings
                    var previouslySavedOn = Opts.SavedOn;
                    Save();
                    if (previouslySavedOn == Opts.SavedOn)
                    {
                        //save failed
                        return;
                    }

                    var proc = new ProcessStartInfo();
                    proc.UseShellExecute = true;
                    proc.FileName = Assembly.GetExecutingAssembly().Location;
                    proc.Arguments = "--schedule";
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

            CreateTask();
        }

        private void CreateTask()
        {
            CreateTaskResponse.Text = "Working...";
            CreateTaskButton.IsEnabled = false;
            var taskMessage = string.Empty;
            var taskError = string.Empty;

            try
            {
                if (Opts.CurrentProfile.RunIntervalTimeUnit == "seconds" && 60 % Opts.CurrentProfile.RunInterval != 0)
                {
                    throw new Exception("Run interval must divide evenly into 60");
                }

                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.Settings.Enabled = true;
                    td.RegistrationInfo.Description = TaskDescription;
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                    td.Principal.UserId = "NT AUTHORITY\\SYSTEM";
                    td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    td.Settings.Hidden = true;

                    switch (Opts.CurrentProfile.RunIntervalTimeUnit)
                    {
                        case "seconds":
                            {
                                if (Opts.CurrentProfile.RunInterval > 0)
                                {
                                    for (int i = 0; i < 60 / Opts.CurrentProfile.RunInterval; i++)
                                    {
                                        var dt = new DailyTrigger();
                                        dt.DaysInterval = 1;
                                        dt.StartBoundary = Opts.CurrentProfile.RunStartTime + TimeSpan.FromSeconds(Opts.CurrentProfile.RunInterval * i);

                                        dt.Repetition.Duration = TimeSpan.FromDays(1);
                                        dt.Repetition.Interval = TimeSpan.FromMinutes(1);

                                        td.Triggers.Add(dt);
                                    }
                                }
                            }
                            break;
                        case "minutes":
                            {
                                var dt = new DailyTrigger();
                                dt.DaysInterval = 1;
                                dt.StartBoundary = Opts.CurrentProfile.RunStartTime;

                                if (Opts.CurrentProfile.RunInterval > 0)
                                {
                                    dt.Repetition.Duration = TimeSpan.FromDays(1);
                                    dt.Repetition.Interval = TimeSpan.FromMinutes(Opts.CurrentProfile.RunInterval);
                                }

                                td.Triggers.Add(dt);
                            }
                            break;
                        case "hours":
                            {
                                var dt = new DailyTrigger();
                                dt.DaysInterval = 1;
                                dt.StartBoundary = Opts.CurrentProfile.RunStartTime;

                                if (Opts.CurrentProfile.RunInterval > 0)
                                {
                                    dt.Repetition.Duration = TimeSpan.FromDays(1);
                                    dt.Repetition.Interval = TimeSpan.FromHours(Opts.CurrentProfile.RunInterval);
                                }

                                td.Triggers.Add(dt);
                            }
                            break;
                    }

                    var action = new ExecAction(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DSA.exe"), $"-r -p {Opts.CurrentProfile.Id}");
                    td.Actions.Add(action);

                    var newTask = ts.RootFolder.RegisterTaskDefinition($"{TaskName} ({Opts.CurrentProfile.Name})", td, TaskCreation.CreateOrUpdate, null);

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

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            Save();
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

        private void ProfilesListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update profile binding
            Opts.CurrentProfileNotNull = Opts.CurrentProfile != null;

            // Reset response texboxes
            TestApiConnectivityResponse.Clear();
            TestDataConnectivityResponse.Clear();
            TestQueryResponse.Clear();
            RunAllResponse.Clear();
            CreateTaskResponse.Clear();

            // Update title
            SetTitle();
        }

        private void Save()
        {
            try
            {
                Opts.Save();
                Opts.SavedOn = DateTime.Now.ToString();
                SavedOnLabel.Content = Opts.SavedOn;
                SaveOnLabelPrefix.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Problem saving settings:\n\n{ex.Message}", "Whoops!", MessageBoxButton.OK);
            }
        }

        private bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = SettingsClient.GetDefaultProfile();
            newProfile.Name = "(New Profile)";
            Opts.Profiles.Add(newProfile);
            Opts.CurrentProfile = Opts.Profiles.FirstOrDefault(x => x.Name == newProfile.Name);
            Opts.CurrentProfileNotNull = Opts.CurrentProfile != null;
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this, $"Are you sure you want to delete \"{Opts.CurrentProfile.Name}\"?", "Are you sure?", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

            if (result == MessageBoxResult.OK)
            {
                Opts.Profiles.Remove(Opts.CurrentProfile);
                Opts.CurrentProfile = Opts.Profiles.FirstOrDefault();
                Opts.CurrentProfileNotNull = Opts.CurrentProfile != null;
            }
        }

        private void ViewOnGithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(GithubUrl);
        }
    }
}
