using DSA.Lib.Data;
using DSA.Lib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace DSA.Lib
{
    public class Updater
    {
        public UpdaterOpts Opts { get; set; }
        public DateTime lastUpdateOn { get; private set; }

        public Updater(UpdaterOpts opts)
        {
            Opts = opts;
            lastUpdateOn = GetLastUpdatedOn();
        }

        public Guid Run()
        {
            var unitsJson = new IncidentResponseClient(Opts.ConnectionString, Opts.UnitsQuery.Replace("{{LIMIT}}", Opts.Limit.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdateOn.ToString())).Get();
            var incidentJson = new IncidentResponseClient(Opts.ConnectionString, Opts.UnitsQuery.Replace("{{LIMIT}}", Opts.Limit.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdateOn.ToString())).Get();

            var result = JObject.Parse(Http.Post(Opts.DataUrl, new NameValueCollection()
            {
                { "type", Opts.DataType },
                { "apiKey", Opts.ApiKey },
                { "apiKeySecret", Opts.ApiKeySecret },
                { "incidents", incidentJson },
                { "units", unitsJson }
            }));

            return Guid.Parse(result["transactionId"]?.ToString());
        }

        private DateTime GetLastUpdatedOn()
        {
            var result = JObject.Parse(Http.Post(Opts.LastDatetimeUrl, new NameValueCollection()
            {
                { "type", Opts.DataType },
                { "apiKey", Opts.ApiKey },
                { "apiKeySecret", Opts.ApiKeySecret }
            }));
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(result["tz"]?.ToString());
            DateTime utcTime = new DateTime();

            if (result["timestamp"]?.ToString() != null)
            {
                utcTime = DateTime.Parse(result["timestamp"]?.ToString());
            }

            return tz == null ? utcTime : TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
    }
}
