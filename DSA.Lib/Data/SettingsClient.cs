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
        const string HashHistoryFileSuffix = ".hash.json";

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
            catch (Exception)
            {
                // something went wrong - either this is first time or malformed object
                opts = new UpdaterOpts();
            }

            // ensure profiles are created as expected
            if (opts.Profiles.Count == 0)
                opts.Profiles.Add(GetDefaultProfile());

            return opts;
        }

        public static IEnumerable<HashHistory> GetHashHistory(Guid profileId)
        {
            IEnumerable<HashHistory> history = null;

            try
            {
                history = JsonConvert.DeserializeAnonymousType(File.ReadAllText(GetHistoryFilePath(profileId)), history);
            }
            catch (Exception)
            {
                // something went wrong - either this is first time or malformed object
                history = new List<HashHistory>();
            }

            return history;
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
                Id = Guid.NewGuid(),
                Name = "Default Profile",
                Type = "analytics",
                RunInterval = -1,
                RunIntervalTimeUnit = "hours",
                Queries = new List<Query>() { new Query() { CommandText = "", DataName = "" } },
                DataSourceType = "sql",
                Driver = "mssql",
                ConnectionString = "",
                ApiKey = "",
                ApiKeySecret = "",
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
            var dups = opts.Profiles.GroupBy(x => x.Id).Where(group => group.Count() > 1);
            if (dups.Count() > 0)
            {
                throw new Exception($"Duplicate profile names: {string.Join(",", dups)}");
            }

            // Set current profile name (so we can load it on next startup)
            if (opts.CurrentProfile != null)
            {
                opts.CurrentProfileId = opts.CurrentProfile.Id;
            }

            // Hit it!
            File.WriteAllText(GetSettingsFilePath(), JsonConvert.SerializeObject(opts, Formatting.Indented));
        }

        public static void SaveHashes(Guid profileId, IEnumerable<HashHistory> opts)
        {
            // Hit it!
            File.WriteAllText(GetHistoryFilePath(profileId), JsonConvert.SerializeObject(opts, Formatting.None));
        }

        private static string GetHistoryFilePath(Guid profileId)
        {
            var idStr = profileId.ToString().ToLower();
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, $"{idStr}{HashHistoryFileSuffix}" );
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, SettingsFileName);
        }
    }
}
