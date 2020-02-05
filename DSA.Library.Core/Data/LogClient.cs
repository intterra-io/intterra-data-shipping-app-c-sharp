using DSA.Lib.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Core.Data
{
    public static class LogClient
    {
        const string OrgName = "Intterra";
        const string AppName = "DSA";

        public static void Init(bool remoteLogging = false)
        {
            if (!remoteLogging)
            {
                try
                {
                    if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName)))
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName));
                    }
                    if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName)))
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName));
                    }

                    // Rotate logs
                    var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName);

                    foreach (var item in Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly)
                        .Select(x => new FileInfo(x))
                        .Where(x => x.CreationTime < (DateTime.Now.AddDays(-14))))
                    {
                        try
                        {
                            item.Delete();
                        }
                        catch (Exception)
                        {
                            // Swallow
                        }
                    }
                }
                catch (Exception)
                {
                    // Swallow
                }
            }
        }

        public static void Log(LogEntry entry, bool remoteLogging = false, string remoteLoggingUrl = null)
        {
            try
            {
                if (!remoteLogging)
                {
                    File.AppendAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, GetLogFileName()), new[] { $"{DateTime.Now}\t{entry.Entry.LogLevel}\t{entry.Entry.LogMessage}" });
                }
                else
                {
                    var httpResponse = Http.PostJson(remoteLoggingUrl, entry);
                    var readContentTask = httpResponse.Content.ReadAsStringAsync();
                    readContentTask.Wait();
                    var content = readContentTask.Result;
                }
            }
            catch (Exception)
            {
                // Swallow - nothing we can do at this point.
            }

        }

        private static string GetLogFileName(bool remoteLogging = false)
        {
            return $"{DateTime.Now.ToString("yyyyMMdd")}.log";
        }
    }
}
