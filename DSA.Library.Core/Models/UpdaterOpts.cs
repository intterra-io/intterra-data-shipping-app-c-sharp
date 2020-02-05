using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DSA.Lib.Core.Models
{
    public class UpdaterOpts
    {
        public Guid CurrentProfileId { get; set; }
        public string AppVersion { get; set; }
        public IReadOnlyList<string> DbDrivers { get; } = new List<string>(new[] { "mssql", "odbc" }).AsReadOnly();
        public IReadOnlyList<string> ProfileTypes { get; } = new List<string>(new[] { "analytics", "sitstat", "custom" }).AsReadOnly();
        public IReadOnlyList<string> DataSourceTypes { get; } = new List<string>(new[] { "database" }).AsReadOnly(); // TODO: add type "file" for Image trend integration
        public ObservableCollection<UpdaterProfile> Profiles { get; } = new ObservableCollection<UpdaterProfile>();
        public string LogUrl { get; set; }
        public bool RemoteLogging { get; set; } = true;

        [JsonIgnore]
        public string SavedOn { get; set; } = "";
        [JsonIgnore]
        public UpdaterProfile CurrentProfile { get; set; }
        [JsonIgnore]
        public bool CurrentProfileNotNull { get; set; } = false;

        public UpdaterOpts()
        {
            if (string.IsNullOrWhiteSpace(LogUrl))
            {
#if DEBUG
                LogUrl = "https://portal-dev.intterragroup.com/api/logs/create";
#else
                LogUrl = "https://portal.intterragroup.com/api/logs/create";
#endif
            }
        }

        public void Validate()
        {
            // make sure we don't have any duplicate profiles
            var dups = Profiles.GroupBy(x => x.Id).Where(group => group.Count() > 1);
            if (dups.Count() > 0)
            {
                throw new Exception($"Duplicate profile names: {string.Join(",", dups)}");
            }

            // make sure we don't have any duplicate query definitions within profiles
            foreach (var profile in Profiles)
            {
                foreach (var query in profile.Queries)
                {
                    var count = profile.Queries.Count(x => x.DataName == query.DataName);
                    if (count > 1)
                    {
                        throw new Exception($"Profile \"{profile.Name}\" invalid due to duplicate queries named \"{query.DataName}\"");
                    }
                }
            }

            // make sure we custom profiles all have paths
            foreach (var profile in Profiles.Where(x => x.Type == "custom"))
            {
                var queriesWithmissingPaths = profile.Queries.Where(x => string.IsNullOrWhiteSpace(x.Path));
                if (queriesWithmissingPaths.Count() > 0)
                {
                    throw new Exception($"Profile \"{profile.Name}\" invalid due to missing path definitions in the following queries: {string.Join(", " , queriesWithmissingPaths.Select(x => $"\"{x.DataName}\""))}");
                }
            }
        }
    }

    public class UpdaterProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Type { get; set; }
        public string LastDatetimeUrl { get; set; }
        public string DataUrl { get; set; }
        public string TestUrl { get; set; }

        public int RunInterval { get; set; }
        public string RunIntervalTimeUnit { get; set; }
        [JsonIgnore]
        public DateTime RunStartTime { get; set; } = DateTime.Now;


        public string DataSourceType { get; set; }
        public string Driver { get; set; }
        public string ConnectionString { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeySecret { get; set; }
        public string Agency { get; set; }
        public bool AllowDuplication { get; set; } = false;
        public ObservableCollection<Query> Queries { get; set; } = new ObservableCollection<Query>();
        [Obsolete("This property is now stored in the 'Queries' data structure")]
        public string IncidentsQuery { get; set; }
        [Obsolete("This property is now stored in the 'Queries' data structure")]
        public string UnitsQuery { get; set; }

        public static string GetQuery(Query query, DateTime? lastUpdatedOn)
        {
            return ReplaceQueryPlaceholders(query.CommandText, lastUpdatedOn);
        }

        private static string ReplaceQueryPlaceholders(string query, DateTime? lastUpdatedOn)
        {
            if (lastUpdatedOn != null)
            {
                query = query.Replace("{{LASTUPDATEDDATETIME}}", ((DateTime)lastUpdatedOn).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            }

            return query;
        }

        public bool UsesLastUpdatedDatetime()
        {
            return Queries
                .Where(x => !string.IsNullOrWhiteSpace(x.CommandText)) // filter out empty queries
                .Any(x => x.CommandText.Contains("{{LASTUPDATEDDATETIME}}"));
        }
    }

    public class Query
    {
        public string ProfileType { get; set;}
        public string DataName { get; set; } = "(new query)";
        public string CommandText { get; set; } = "";
        public string Path { get; set; } = "";
        [JsonIgnore]
        public DataTable Data { get; set; }
        [JsonIgnore]
        public byte[][] Hashes { get; set; }
    }

    public class UpdaterResponse
    {
        public IEnumerable<SingleResponse> Results { get; set; }
        public Guid TransactionId { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder($"Batch uuid: {(TransactionId != Guid.Empty ? TransactionId.ToString() : "N/A")}\n\n");

            foreach (var item in Results)
            {
                sb.Append($"{item.Name}: Sent {item.SentCount} records\n");
                sb.Append($"{item.Name}: Ignored {item.IgnoredCount} records\n\n");
            }

            return sb.ToString();
        }
    }

    public class SingleResponse
    {
        public string Name { get; set; }
        public int SentCount { get; set; }
        public int IgnoredCount { get; set; }
    }
}
