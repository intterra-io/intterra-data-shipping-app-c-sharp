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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib
{
    public class Updater
    {
        public UpdaterProfile Profile { get; set; }
        public HashHistory History { get; set; }
        public DateTime? LastUpdateOn { get; private set; }

        private HashHistory NewHistory = new HashHistory();

        public Updater(UpdaterProfile profile)
        {
            Profile = profile;
            History = SettingsClient.GetHashHistory();
        }

        public IEnumerable<Guid> Run()
        {
            LastUpdateOn = GetLastUpdatedOn();
            var transactionIds = new List<Guid>();

            LastUpdateOn = GetLastUpdatedOn();
            var incidents = GetIncidents();
            var units = GetUnits();
            var supportsMultipleBatches = Profile.Driver == "mssql";

            if (supportsMultipleBatches)
            {
                var incidentsTotal = 0;
                if (incidents.Rows.Count > 0) incidentsTotal = int.Parse(incidents.Rows[0]["total"].ToString());

                var unitsTotal = 0;
                if (units.Rows.Count > 0) unitsTotal = int.Parse(units.Rows[0]["total"].ToString());

                for (var i = 0; i < (int)Math.Ceiling((double)Math.Max(incidentsTotal, unitsTotal) / (double)Profile.Limit); i++)
                {
                    if (i > 0)
                    {
                        incidents = GetIncidents(i * Profile.Limit);
                        units = GetUnits(i * Profile.Limit);
                    }

                    incidents.Columns.Remove("total");
                    units.Columns.Remove("total");

                    var result = SendData(incidents, units);
                    transactionIds.Add(Guid.Parse(result["transactionId"]?.ToString()));
                }
            }
            else
            {
                var result = SendData(incidents, units);
                transactionIds.Add(Guid.Parse(result["transactionId"]?.ToString()));
            }

            SettingsClient.SaveHashes(NewHistory);

            return transactionIds;
        }

        private byte[][] GetHashes(DataTable table)
        {
            using (var hasher = new SHA256Managed())
            {
                return table.Rows.Cast<DataRow>().Select(x => hasher.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x)))).ToArray();
            }
        }

        private void RemoveDuplicates(DataTable table, byte[][] tableHashes, byte[][] sentHashes)
        {
            for (var i = table.Rows.Count - 1; i >= 0; i--)
            {
                if (sentHashes.Any(x => x == tableHashes[i]))
                {
                    table.Rows.RemoveAt(i);
                }
            }
        }

        private JObject SendData(DataTable incidents, DataTable units)
        {
            var incidentHashes = GetHashes(incidents);
            var unitHashes = GetHashes(units);
            NewHistory.AppendIcidentHashes(incidentHashes);
            NewHistory.AppendUnitHashes(unitHashes);

            if (!Profile.AllowDuplication)
            {
                RemoveDuplicates(incidents, incidentHashes, History.Incidents);
                RemoveDuplicates(units, unitHashes, History.Units);
            }

            if (incidents.Rows.Count == 0 && units.Rows.Count == 0)
            {
                var response = new JObject();
                response.Add("message", "No data to send");
                return response;
            }

            var incidentsBytes = Encoding.UTF8.GetBytes(incidents.toCsv());
            var unitsBytes = Encoding.UTF8.GetBytes(units.toCsv());

            MultipartFormDataContent form = new MultipartFormDataContent();

            form.Add(new StringContent(Profile.Name), "\"type\"");

            if (!string.IsNullOrWhiteSpace(Profile.Agency))
                form.Add(new StringContent(Profile.Agency), "\"agency\""); // Add agency if populated

            form.Add(new ByteArrayContent(incidentsBytes, 0, incidentsBytes.Length), "\"incidents\"", "incidents.csv");
            form.Add(new ByteArrayContent(unitsBytes, 0, unitsBytes.Length), "\"units\"", "units.csv");

            var httpResponse = Http.Post(Profile.DataUrl, form, GetAuthHeader());
            var readContentTask = httpResponse.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"{httpResponse.StatusCode} - {content}");
            }

            return JObject.Parse(content);
        }

        private DataTable GetIncidents(int offset = 0)
        {
            return GetSqlClient().GetData(Profile.GetIncidentsQuery(LastUpdateOn, offset));
        }

        private DataTable GetUnits(int offset = 0)
        {
            return GetSqlClient().GetData(Profile.GetUnitsQuery(LastUpdateOn, offset));
        }

        private AuthenticationHeaderValue GetAuthHeader()
        {
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Profile.ApiKey}:{Profile.ApiKeySecret}")));
        }

        private SqlClient GetSqlClient()
        {
            switch (Profile.Driver)
            {
                case "mssql":
                    return new SqlServerClient(Profile.ConnectionString);
                case "odbc":
                    return new OdbcClient(Profile.ConnectionString);
                default:
                    throw new Exception("No valid driver selected");
            }
        }

        public async Task<string> TestApiConnectivity()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = GetAuthHeader();
                var response = await httpClient.PostAsync(Profile.TestUrl, new StringContent("{\"message\":\"test\"}", Encoding.UTF8, "application/json"));
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public async Task<string> TestDataConnectivity()
        {
            var result = await Task.Run(() =>
            {
                return GetSqlClient().GetData("Select 'Success!' as message;").toJson(Formatting.Indented);
            });
            return result;
        }

        public async Task<Tuple<string, int>> TestIncidentsQuery()
        {
            var result = await Task.Run(() =>
            {
                LastUpdateOn = GetLastUpdatedOn();
                var data = GetIncidents();
                return new Tuple<string, int>(data.toJson(Formatting.Indented), data.Rows.Count);
            });
            return result;
        }

        public async Task<Tuple<string, int>> TestUnitsQuery()
        {
            var result = await Task.Run(() =>
            {
                LastUpdateOn = GetLastUpdatedOn();
                var data = GetUnits();
                return new Tuple<string, int>(data.toJson(Formatting.Indented), data.Rows.Count);
            });
            return result;
        }

        private DateTime? GetLastUpdatedOn()
        {
            // Return null if the queries don't use lastupdated time
            if (!Profile.UsesLastUpdatedDatetime())
                return null;

            var result = JObject.Parse(Http.Post(Profile.LastDatetimeUrl, new NameValueCollection()
            {
                { "type", Profile.Type },
                { "apiKey", Profile.ApiKey },
                { "apiKeySecret", Profile.ApiKeySecret }
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
