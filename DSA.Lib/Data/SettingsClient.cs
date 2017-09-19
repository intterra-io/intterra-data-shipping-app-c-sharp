using DSA.Lib.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Data
{
    public static class SettingsClient
    {
        const string OrgName = "Intterra";
        const string AppName = "DSA";
        const string FileName = "settings.json";

        public static void Init()
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
            }
            catch (Exception)
            {
                // Swallow
            }
        }

        public static UpdaterOpts Get()
        {
            var opts = new UpdaterOpts();

            if (File.Exists(GetSettingsPath()))
            {
                return JsonConvert.DeserializeAnonymousType(File.ReadAllText(GetSettingsPath()), opts);
            } else
            {
                return opts;
            }
        }

        public static void Save(this UpdaterOpts opts)
        {

            File.WriteAllText(GetSettingsPath(), JsonConvert.SerializeObject(opts, Formatting.Indented));
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, FileName);
        }
    }
}
