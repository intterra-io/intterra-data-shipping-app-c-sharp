using DSA.Lib.Data;
using DSA.Lib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib
{
    public class Updater
    {
        public UpdaterOpts Opts { get; set; }
        public DateTime lastUpdateOn { get; private set; }

        private const int MaxLimit = 10000;

        public Updater(UpdaterOpts opts)
        {
            Opts = opts;
            lastUpdateOn = GetLastUpdatedOn();
            Opts.Limit = Opts.Limit > MaxLimit ? MaxLimit : Opts.Limit;
        }

        public async Task<Guid> Run()
        {
            try
            {
                var unitsCsv = new IncidentResponseClient(Opts.ConnectionString, Opts.UnitsQuery.Replace("{{LIMIT}}", Opts.Limit.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdateOn.ToString())).GetCsv();
                var incidentCsv = new IncidentResponseClient(Opts.ConnectionString, Opts.UnitsQuery.Replace("{{LIMIT}}", Opts.Limit.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdateOn.ToString())).GetCsv();
                var units = Encoding.UTF8.GetBytes(unitsCsv);
                var incidents = Encoding.UTF8.GetBytes(incidentCsv);

                MultipartFormDataContent form = new MultipartFormDataContent();

                form.Add(new StringContent(Opts.DataType), "type");
                form.Add(new ByteArrayContent(incidents, 0, incidents.Length), "incidents", "incidents.csv");
                form.Add(new ByteArrayContent(units, 0, units.Length), "units", "units.csv");

                var resultString = await Http.Post(Opts.DataUrl, form, new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Opts.ApiKey}:{Opts.ApiKeySecret}"))));
                var result = JObject.Parse(resultString);
                return Guid.Parse(result["transactionId"]?.ToString());
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private DateTime GetLastUpdatedOn()
        {
            var result = JObject.Parse(Http.Post(Opts.LastDatetimeUrl, new NameValueCollection()
            {
                { "type", Opts.DataType },
                { "apiKey", Opts.ApiKey },
                { "apiKeySecret", Opts.ApiKeySecret }
            }));
            TimeZoneInfo tz = TimezoneConverter.PosixToTimezone(result["tz"]?.ToString());
            DateTime utcTime = new DateTime();

            if (result["timestamp"]?.ToString() != null)
            {
                utcTime = DateTime.Parse(result["timestamp"]?.ToString());
            }

            return tz == null ? utcTime : TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
    }
}
