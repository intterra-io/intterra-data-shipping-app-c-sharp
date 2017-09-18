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
            var opts = new UpdaterOpts()
            {
                ApiKey = Settings.Default.ApiKey,
                ApiKeySecret = Settings.Default.ApiKeySecret,
                ConnectionString = Settings.Default.AnalyticsConnectionString,
                DataType = "analytics",
                DataUrl = Settings.Default.DataCenterRootUrl + Settings.Default.DataUrl,
                LastDatetimeUrl = Settings.Default.DataCenterRootUrl + Settings.Default.LastDatetimeUrl,
                TestUrl = Settings.Default.DataCenterRootUrl + Settings.Default.TestUrl,
                Limit = Settings.Default.Limit
            };

            opts.IncidentsQuery = opts.BuildQuery(
                Settings.Default.AnalyticsIncidentsSelect,
                Settings.Default.AnalyticsIncidentsFrom,
                Settings.Default.AnalyticsIncidentsWhere,
                Settings.Default.AnalyticsIncidentsOrderBy);
            opts.UnitsQuery = opts.BuildQuery(
                Settings.Default.AnalyticsUnitsSelect,
                Settings.Default.AnalyticsUnitsFrom,
                Settings.Default.AnalyticsUnitsWhere,
                Settings.Default.AnalyticsUnitsOrderBy);

            return new Updater(opts);
        }
    }
}
