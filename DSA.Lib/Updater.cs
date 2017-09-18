using DSA.Lib.Data;
using DSA.Lib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
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
        public DateTime LastUpdateOn { get; private set; }

        public Updater(UpdaterOpts opts)
        {
            Opts = opts;
            LastUpdateOn = GetLastUpdatedOn();
        }

        public IEnumerable<Guid> Run()
        {
            var transactionIds = new List<Guid>();

            var incidents = GetIncidents();
            var units = GetUnits();

            var incidentsTotal = 0;
            if (incidents.Rows.Count > 0) incidentsTotal = int.Parse(incidents.Rows[0]["total"].ToString());

            var unitsTotal = 0;
            if (units.Rows.Count > 0) unitsTotal = int.Parse(units.Rows[0]["total"].ToString());

            for (var i = 0; i < (int)Math.Ceiling((double)Math.Max(incidentsTotal, unitsTotal) / (double)Opts.Limit); i++)
            {
                if (i > 0)
                {
                    incidents = GetIncidents(i * Opts.Limit);
                    units = GetUnits(i * Opts.Limit);
                }

                incidents.Columns.Remove("total");
                units.Columns.Remove("total");

                var incidentsBytes = Encoding.UTF8.GetBytes(incidents.toCsv());
                var unitsBytes = Encoding.UTF8.GetBytes(units.toCsv());

                MultipartFormDataContent form = new MultipartFormDataContent();

                form.Add(new StringContent(Opts.DataType), "\"type\"");
                form.Add(new ByteArrayContent(incidentsBytes, 0, incidentsBytes.Length), "\"incidents\"", "incidents.csv");
                form.Add(new ByteArrayContent(unitsBytes, 0, unitsBytes.Length), "\"units\"", "units.csv");

                var httpResponse = Http.Post(Opts.DataUrl, form, GetAuthHeader());
                var readContentTask = httpResponse.Content.ReadAsStringAsync();
                readContentTask.Wait();
                var content = readContentTask.Result;

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"{httpResponse.StatusCode} - {content}");
                }

                var result = JObject.Parse(content);
                transactionIds.Add(Guid.Parse(result["transactionId"]?.ToString()));
            }

            return transactionIds;
        }

        private DataTable GetIncidents(int offset = 0)
        {
            return new SqlServerClient(Opts.ConnectionString).GetData(ReplacePlaceQueryPlaceholders(Opts.IncidentsQuery, offset));
        }

        private DataTable GetUnits(int offset = 0)
        {
            return new SqlServerClient(Opts.ConnectionString).GetData(ReplacePlaceQueryPlaceholders(Opts.UnitsQuery, offset));
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
                return new SqlServerClient(Opts.ConnectionString).GetData("Select 'Success!' as message;").toJson(Formatting.Indented);
            });
            return result;
        }

        public async Task<Tuple<string, int>> TestIncidentsQuery()
        {
            var result = await Task.Run(() =>
            {
                var data = GetIncidents();
                return new Tuple<string, int>(data.toJson(Formatting.Indented), data.Rows.Count);
            });
            return result;
        }

        public async Task<Tuple<string, int>> TestUnitsQuery()
        {
            var result = await Task.Run(() =>
            {
                var data = GetUnits();
                return new Tuple<string, int>(data.toJson(Formatting.Indented), data.Rows.Count);
            });
            return result;
        }

        private string ReplacePlaceQueryPlaceholders(string query, int page)
        {
            return query.Replace("{{PAGE}}", page.ToString()).Replace("{{LASTUPDATEDDATETIME}}", LastUpdateOn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
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
