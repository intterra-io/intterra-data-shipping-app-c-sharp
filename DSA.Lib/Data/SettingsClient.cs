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
        const string SettingsFileName = "settings.json";
        const string HashHistoryFileName = "hash.json";

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

        public static UpdaterOpts GetSettings()
        {
            UpdaterOpts opts = null;

            try
            {
                opts = JsonConvert.DeserializeAnonymousType(File.ReadAllText(GetSettingsFilePath()), opts);
            }
            catch (Exception ex)
            {
                // something went wrong - either this is first time or malformed object
                opts = new UpdaterOpts();
            }

            // ensure profiles are created as expected
            if (opts.Profiles.Count == 0)
                opts.Profiles.Add(GetDefaultProfile());

            return opts;
        }

        public static HashHistory GetHashHistory()
        {
            HashHistory opts = null;

            try
            {
                opts = JsonConvert.DeserializeAnonymousType(File.ReadAllText(GetHistoryFilePath()), opts);
            }
            catch (Exception ex)
            {
                // something went wrong - either this is first time or malformed object
                opts = new HashHistory();
            }

            return opts;
        }

        public static bool HasChanges(UpdaterOpts inMemSettings)
        {
            var persistentSettings = GetSettings();

            return JsonConvert.SerializeObject(persistentSettings) != JsonConvert.SerializeObject(inMemSettings);
        }

        public static UpdaterProfile GetDefaultProfile()
        {
            return new UpdaterProfile()
            {
                Name = "Default Profile",
                Type = "analytics",
                RunInterval = -1,
                RunIntervalTimeUnit = "hours",

                IncidentsSelect = "*",
                IncidentsFrom = "dbo.incidents",
                IncidentsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'",
                IncidentsOrderBy = "incident_datetime",

                UnitsSelect = "*",
                UnitsFrom = "dbo.units",
                UnitsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'",
                UnitsOrderBy = "last_updated_rms",

                Driver = "mssql",
                ConnectionString = "Data Source=my_server;Initial Catalog=my_db;User Id=my_uid; Password=my_password",
                ApiKey = "",
                ApiKeySecret = "",
                Limit = 500000,
#if DEBUG
                LastDatetimeUrl = "http://localhost:8000/v1/data/get-last-datetime",
                DataUrl = "http://localhost:8000/v1/data/add",
                TestUrl = "http://localhost:8000/v1/keys/test",
#else
                LastDatetimeUrl = "https://dc.intterragroup.com/v1/data/get-last-datetime",
                DataUrl = "https://dc.intterragroup.com/v1/data/add",
                TestUrl = "https://dc.intterragroup.com/v1/keys/test",
#endif
            };
        }

        public static void Save(this UpdaterOpts opts)
        {
            // make sure we don't have any duplicate profiles
            var dups = opts.Profiles.GroupBy(x => x.Name).Where(group => group.Count() > 1);
            if (dups.Count() > 0)
            {
                throw new Exception($"Duplicate profile names: {string.Join(",", dups)}");
            }

            // Set current profile name (so we can load it on next startup)
            if (opts.CurrentProfile != null)
            {
                opts.CurrentProfileName = opts.CurrentProfile.Name;
            }

            // Hit it!
            File.WriteAllText(GetSettingsFilePath(), JsonConvert.SerializeObject(opts, Formatting.Indented));
        }

        public static void SaveHashes(HashHistory opts)
        {
            // Hit it!
            File.WriteAllText(GetHistoryFilePath(), JsonConvert.SerializeObject(opts, Formatting.None));
        }

        private static string GetHistoryFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, HashHistoryFileName);
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, SettingsFileName);
        }
    }
}
