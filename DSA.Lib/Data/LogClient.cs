using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Data
{
    public static class LogClient
    {

        const string OrgName = "Intterra";
        const string AppName = "DSA";

        public static void Init()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OrgName)))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OrgName));
                }
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OrgName, AppName)))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OrgName, AppName));
                }

                // Rotate logs
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OrgName, AppName);

                foreach (var item in Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly)
                    .Select(x => new FileInfo(x))
                    .Where(x => x.CreationTime < (DateTime.Now.AddDays(-14)))
                    .Skip(14))
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

        private static string GetLogFileName()
        {
            return $"{DateTime.Now.ToString("yyyyMMdd")}.log";
        }

        public static void Log(string message, string level = "INFO")
        {
            try
            {
                File.AppendAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OrgName, AppName, GetLogFileName()), new[] { $"{DateTime.Now}\t{level}\t{message}" });
            }
            catch (Exception)
            {
                // Swallow - nothing we can do at this point.
            }
        }
    }
}
