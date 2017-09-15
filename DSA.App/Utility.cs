using DSA.App.Properties;
using DSA.Lib;
using DSA.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.App
{
    public class Utility
    {
        public static Updater GetUpdater()
        {
            return new Updater(new UpdaterOpts()
            {
                ApiKey = Settings.Default.ApiKey,
                ApiKeySecret = Settings.Default.ApiKeySecret,
                ConnectionString = Settings.Default.AnalyticsConnectionString,
                DataType = "analytics",
                DataUrl = Settings.Default.DataCenterRootUrl + Settings.Default.DataUrl,
                IncidentsQuery = Settings.Default.AnalyticsIncidentsQuery,
                UnitsQuery = Settings.Default.AnalyticsUnitsQuery,
                LastDatetimeUrl = Settings.Default.DataCenterRootUrl + Settings.Default.LastDatetimeUrl,
                TestUrl = Settings.Default.DataCenterRootUrl + Settings.Default.TestUrl,
                Limit = Settings.Default.Limit
            });
        }
    }
}
