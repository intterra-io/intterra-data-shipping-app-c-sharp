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

namespace DSA.Lib.Data
{
    public class Updater
    {
        public UpdaterProfile Profile { get; set; }
        public HashHistory History { get; set; }
        public DateTime? LastUpdateOn { get; private set; }

        private IEnumerable<HashHistory> HashHistories = new List<HashHistory>();

        public Updater(UpdaterProfile profile)
        {
            Profile = profile;
            History = SettingsClient.GetHashHistory(Profile.Id);
        }

        public UpdaterResponse Run()
        {
            // Init 
            LastUpdateOn = GetLastUpdatedOn();

            // Get data
            // TODO: should be factored into a class method
            foreach (var query in Profile.Queries)
            {
                query.Data = GetSqlData(query);
            }

            // Send data
            var response = SendData();

            // Save hashes
            SettingsClient.SaveHashes(Profile.Id, HashHistories);

            // Return
            return response;
        }

        private byte[][] GetHashes(DataTable table)
        {
            using (var hasher = new SHA256Managed())
            {
                return table.Rows.Cast<DataRow>().Select(x => hasher.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x.ItemArray)))).ToArray();
            }
        }

        private void RemoveDuplicates(DataTable table, byte[][] tableHashes, byte[][] sentHashes)
        {
            if (tableHashes == null || sentHashes == null)
                return; // there's nothing to comare

            for (var i = table.Rows.Count - 1; i >= 0; i--)
            {
                if (sentHashes.Any(x => x.SafeEquals(tableHashes[i])))
                {
                    table.Rows.RemoveAt(i);
                }
            }
        }

        private UpdaterResponse SendData()
        {
            // Init hash history
            var incidentHashes = GetHashes(incidents);
            var unitHashes = GetHashes(units);
            NewHistory.AppendIcidentHashes(incidentHashes);
            NewHistory.AppendUnitHashes(unitHashes);

            // Strip duplicate data based on hash history
            if (!Profile.AllowDuplication)
            {
                RemoveDuplicates(incidents, incidentHashes, History.Incidents);
                RemoveDuplicates(units, unitHashes, History.Units);
            }

            // Build response object 
            var response = new UpdaterResponse() {
                SentIncidents = incidents.Rows.Count,
                IgnoredIncidents = incidentHashes.Count() - incidents.Rows.Count,
                SentUnits = units.Rows.Count,
                IgnoredUnits = unitHashes.Count() - units.Rows.Count
            };

            // Return if there is no data to send
            if (incidents.Rows.Count == 0 && units.Rows.Count == 0)
            {
                return response;
            }

            // Send data
            var incidentsBytes = Encoding.UTF8.GetBytes(incidents.toCsv());
            var unitsBytes = Encoding.UTF8.GetBytes(units.toCsv());

            MultipartFormDataContent form = new MultipartFormDataContent();

            form.Add(new StringContent(Profile.Type), "\"type\"");

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

            // Parse response and return
            Guid guid;
            var jsonResponse = JObject.Parse(content);
            if (Guid.TryParse(jsonResponse["transactionId"]?.ToString(), out guid))
            {
                response.TransactionId = guid;
            }

            return response;
        }

        private DataTable GetSqlData(Query query)
        {
            return GetSqlClient().GetData(UpdaterProfile.GetQuery(query, LastUpdateOn));
        }

        private string GetFileData(Query query)
        {
            throw new NotImplementedException("Must be implemented for Image Trend integration");
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
                var responseStr = response.Content.ReadAsStringAsync().Result;

                var jsonResult = JObject.Parse(responseStr);
                return JsonConvert.SerializeObject(jsonResult, Formatting.Indented);
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

        //public async Task<Tuple<string, int>> TestIncidentsQuery()
        //{
        //    var result = await Task.Run(() =>
        //    {
        //        LastUpdateOn = GetLastUpdatedOn();
        //        var data = GetIncidents();
        //        return new Tuple<string, int>(data.toJson(Formatting.Indented), data.Rows.Count);
        //    });
        //    return result;
        //}

        //public async Task<Tuple<string, int>> TestUnitsQuery()
        //{
        //    var result = await Task.Run(() =>
        //    {
        //        LastUpdateOn = GetLastUpdatedOn();
        //        var data = GetUnits();
        //        return new Tuple<string, int>(data.toJson(Formatting.Indented), data.Rows.Count);
        //    });
        //    return result;
        //}

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
