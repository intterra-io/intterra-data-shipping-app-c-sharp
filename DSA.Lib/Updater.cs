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
            var unitsCsv = GetUnits();
            var incidentCsv = GetIncidents();
            var units = Encoding.UTF8.GetBytes(unitsCsv);
            var incidents = Encoding.UTF8.GetBytes(incidentCsv);

            MultipartFormDataContent form = new MultipartFormDataContent();

            form.Add(new StringContent(Opts.DataType), "\"type\"");
            form.Add(new ByteArrayContent(incidents, 0, incidents.Length), "\"incidents\"", "incidents.csv");
            form.Add(new ByteArrayContent(units, 0, units.Length), "\"units\"", "units.csv");

            var resultString = await Http.Post(Opts.DataUrl, form, GetAuthHeader());
            var result = JObject.Parse(resultString);
            return Guid.Parse(result["transactionId"]?.ToString());
        }

        private string GetIncidents()
        {
            return new IncidentResponseClient(Opts.ConnectionString, Opts.UnitsQuery.Replace("{{LIMIT}}", Opts.Limit.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdateOn.ToString())).GetCsv();
        }

        private string GetUnits()
        {
            return new IncidentResponseClient(Opts.ConnectionString, Opts.UnitsQuery.Replace("{{LIMIT}}", Opts.Limit.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdateOn.ToString())).GetCsv();
        }

        private AuthenticationHeaderValue GetAuthHeader()
        {
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Opts.ApiKey}:{Opts.ApiKeySecret}")));
        }

        public async Task<string> TestApiConnectivity()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = GetAuthHeader();
                var response = await httpClient.PostAsync(Opts.TestUrl, new StringContent("{\"message\":\"test\"}", Encoding.UTF8, "application/json"));
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public async Task<string> TestDataConnectivity()
        {
            var result = await Task.Run(() =>
            {
                return new IncidentResponseClient(Opts.ConnectionString, "Select 'Success!' as message;").GetJson();
            });
            return result;
        }

        public async Task<string> TestIncidentsQuery()
        {
            var result = await Task.Run(() =>
            {
                return GetIncidents();
            });
            return result;
        }

        public async Task<string> TestUnitsQuery()
        {
            var result = await Task.Run(() =>
            {
                return GetUnits();
            });
            return result;
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
