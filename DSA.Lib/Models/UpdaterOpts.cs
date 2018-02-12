using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DSA.Lib.Models
{
    public class UpdaterOpts
    {
        public string CurrentProfileName { get; set; } = "analytics";
        [JsonIgnore]
        public UpdaterProfile CurrentProfile { get; set; }
        [JsonIgnore]
        public bool CurrentProfileNotNull { get; set; } = false;
        public IReadOnlyList<string> DbDrivers { get; } = new List<string>(new[] { "mssql", "odbc" }).AsReadOnly();
        public IReadOnlyList<string> ProfileTypes { get; } = new List<string>(new[] { "analytics", "sitstat" }).AsReadOnly();
        public ObservableCollection<UpdaterProfile> Profiles { get; } = new ObservableCollection<UpdaterProfile>();
        public string LogUrl { get; set; }
        public bool RemoteLogging { get; set; } = true;

        public UpdaterOpts()
        {
            if (string.IsNullOrWhiteSpace(LogUrl))
            {
#if DEBUG
                LogUrl = "https://portal-dev.intterragroup.com/api/logs/create";
#else
                LogUrl = "https://portal.intterragroup.com/api/logs/create"
#endif
            }
        }
    }

    public class UpdaterProfile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string LastDatetimeUrl { get; set; }
        public string DataUrl { get; set; }
        public string TestUrl { get; set; }

        public int RunInterval { get; set; }
        public string RunIntervalTimeUnit { get; set; }
        [JsonIgnore]
        public DateTime RunStartTime { get; set; } = DateTime.Now;

        public string Driver { get; set; }
        public string ConnectionString { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeySecret { get; set; }
        public int Limit { get; set; }
        public string Agency { get; set; }

        public string IncidentsSelect { get; set; }
        public string IncidentsFrom { get; set; }
        public string IncidentsWhere { get; set; }
        public string IncidentsOrderBy { get; set; }

        public string UnitsSelect { get; set; }
        public string UnitsFrom { get; set; }
        public string UnitsWhere { get; set; }
        public string UnitsOrderBy { get; set; }

        public string GetIncidentsQuery(DateTime? lastUpdatedOn, int page = 0)
        {
            return ReplacePlaceQueryPlaceholders(BuildQuery(IncidentsSelect, IncidentsFrom, IncidentsWhere, IncidentsOrderBy), lastUpdatedOn, page);
        }

        public string GetUnitsQuery(DateTime? lastUpdatedOn, int page = 0)
        {
            return ReplacePlaceQueryPlaceholders(BuildQuery(UnitsSelect, UnitsFrom, UnitsWhere, UnitsOrderBy), lastUpdatedOn, page);
        }

        private string ReplacePlaceQueryPlaceholders(string query, DateTime? lastUpdatedOn, int page)
        {
            query = query.Replace("{{PAGE}}", page.ToString());

            if (lastUpdatedOn != null)
            {
                query = query.Replace("{{LASTUPDATEDDATETIME}}", ((DateTime)lastUpdatedOn).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            }

            return query;
        }

        private string BuildQuery(string select, string from, string where, string orderBy)
        {
            //return $"SELECT {select}, COUNT(*) OVER() as total FROM {from} WHERE {where} ORDER BY {orderBy} OFFSET {{{{PAGE}}}} ROWS FETCH NEXT {Limit} ROWS ONLY";
            where = string.IsNullOrWhiteSpace(where) ? "1=1" : where;

            if (Driver == "mssql")
            {
                return $"SELECT * FROM ( SELECT {select}, ROW_NUMBER() OVER (ORDER BY {orderBy}) AS seqence, COUNT(*) OVER () as total FROM {from} WHERE {where} ) as x WHERE seqence BETWEEN {{{{PAGE}}}} AND {{{{PAGE}}}} + {Limit}";
            }
            else
            {
                return $"SELECT {select} FROM {from} WHERE {where}";
            }
        }

        public bool UsesLastUpdatedDatetime()
        {
            return (new[] { IncidentsWhere, IncidentsOrderBy, UnitsWhere, UnitsOrderBy }).Any(x => x.Contains("{{LASTUPDATEDDATETIME}}"));
        }
    }
}
