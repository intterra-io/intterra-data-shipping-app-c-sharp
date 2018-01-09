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
            UpdaterOpts opts = null;

            try
            {
                opts = JsonConvert.DeserializeAnonymousType(File.ReadAllText(GetSettingsPath()), opts);
            }
            catch (Exception ex)
            {
                LogClient.Log(ex.Message);

                // something went wrong - either this is first time or malformed object
                opts = new UpdaterOpts();
            }

            // ensure profiles are created as expected
            if (!opts.Profiles.ContainsKey("analytics"))
                opts.Profiles["analytics"] = GetDefaultAnalyticsProfile();

            if (!opts.Profiles.ContainsKey("sitstat"))
                opts.Profiles["sitstat"] = GetDefaultSitstatProfile();

            return opts;
        }

        public static UpdaterProfile GetDefaultAnalyticsProfile()
        {
            return new UpdaterProfile()
            {
                Name = "analytics",

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
                ConnectionString = "Data Source=ServerName,Initial Catalog=DatabaseName,User Id=userid,Password=password",
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

        public static UpdaterProfile GetDefaultSitstatProfile()
        {
            return new UpdaterProfile()
            {
                Name = "sitstat",

                RunInterval = 15,
                RunIntervalTimeUnit = "seconds",

                IncidentsSelect = "*",
                IncidentsFrom = "dbo.incident_summary",
                IncidentsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'",
                IncidentsOrderBy = "incident_datetime",

                UnitsSelect = "*",
                UnitsFrom = "dbo.unit_summary",
                UnitsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'",
                UnitsOrderBy = "last_updated_rms",

                Driver = "mssql",
                ConnectionString = @"Data Source=.\SQLEXPRESS,Initial Catalog=sandbox,Integrated Security=true",
                ApiKey = "",
                ApiKeySecret = "",
                Limit = -1,
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

            File.WriteAllText(GetSettingsPath(), JsonConvert.SerializeObject(opts, Formatting.Indented));
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), OrgName, AppName, FileName);
        }
    }
}
