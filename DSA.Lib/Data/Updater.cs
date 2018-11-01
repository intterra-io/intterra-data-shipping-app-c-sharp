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
        public IEnumerable<HashHistory> HashHistories { get; set; }
        public DateTime? LastUpdateOn { get; private set; }

        public Updater(UpdaterProfile profile)
        {
            Profile = profile;
            HashHistories = SettingsClient.GetHashHistory(Profile.Id);
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
            foreach (var query in Profile.Queries)
            {
                query.Hashes = GetHashes(query.Data);
            }

            // Strip duplicate data based on hash history
            if (!Profile.AllowDuplication)
            {
                foreach (var x in Profile.Queries)
                {
                    var history = HashHistories.FirstOrDefault(y => y.Name == x.DataName);
                    RemoveDuplicates(x.Data, x.Hashes, history?.HashData);
                }
            }

            // Build response object 
            var response = new UpdaterResponse() {
                Results = Profile.Queries.Select(x => new SingleResponse() {
                    Name = x.DataName,
                    SentCount = x.Data.Rows.Count,
                    IgnoredCount = x.Hashes.Count() - x.Data.Rows.Count
                })
            };

            // Return if there is no data to send
            var rowsToSend = 0;
            foreach (var x in Profile.Queries)
                rowsToSend += x.Data.Rows.Count;
            if (rowsToSend == 0)
                return response;

            // Send data
            MultipartFormDataContent form = new MultipartFormDataContent();

            form.Add(new StringContent(Profile.Type), "\"type\"");

            if (!string.IsNullOrWhiteSpace(Profile.Agency))
                form.Add(new StringContent(Profile.Agency), "\"agency\""); // Add agency if populated

            foreach (var x in Profile.Queries)
            {
                var bytes = Encoding.UTF8.GetBytes(x.Data.toCsv());
                form.Add(new ByteArrayContent(bytes, 0, bytes.Length), $"\"{(Profile.Type == "custom" ? "custom_" : "")}{x.DataName}\"", $"{x.DataName}.csv");
            }

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

        public async Task<Tuple<string, int>> TestQuery(Query query)
        {
            var result = await Task.Run(() =>
            {
                LastUpdateOn = GetLastUpdatedOn();
                var data = GetSqlData(query);
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
