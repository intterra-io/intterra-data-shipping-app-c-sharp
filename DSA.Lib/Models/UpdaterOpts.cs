using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DSA.Lib.Models
{
    public class UpdaterOpts
    {
        public string LastDatetimeUrl { get; set; }
        public string DataUrl { get; set; }
        public string TestUrl { get; set; }

        public int RunInterval { get; set; }

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

        public UpdaterOpts()
        {
#if DEBUG
            LastDatetimeUrl = "http://localhost:8000/v1/data/get-last-datetime";
            DataUrl = "http://localhost:8000/v1/data/add";
            TestUrl = "http://localhost:8000/v1/keys/test";

            RunInterval = 60;

            IncidentsSelect = "*";
            IncidentsFrom = "dbo.incident_summary";
            IncidentsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'";
            IncidentsOrderBy = "incident_datetime";

            UnitsSelect = "*";
            UnitsFrom = "dbo.unit_summary";
            UnitsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'";
            UnitsOrderBy = "last_updated_rms";

            ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sandbox;Integrated Security=true";
            ApiKey = "";
            ApiKeySecret = "";
            Limit = 50;
#else
            LastDatetimeUrl = "https://dc.intterragroup.com/v1/data/get-last-datetime";
            DataUrl = "https://dc.intterragroup.com/v1/data/add";
            TestUrl = "https://dc.intterragroup.com/v1/keys/test";

            RunInterval = 60;

            IncidentsSelect = "*";
            IncidentsFrom = "dbo.incidents";
            IncidentsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'";
            IncidentsOrderBy = "incident_datetime";

            UnitsSelect = "*";
            UnitsFrom = "dbo.units";
            UnitsWhere = "last_updated_rms > '{{LASTUPDATEDDATETIME}}'";
            UnitsOrderBy = "last_updated_rms";

            ConnectionString = "Data Source=ServerName;Initial Catalog=DatabaseName;User Id=userid;Password=password";
            ApiKey = "";
            ApiKeySecret = "";
            Limit = 10000;
#endif
        }


        public string GetIncidentsQuery(DateTime lastUpdatedOn, int page = 0)
        {
            return ReplacePlaceQueryPlaceholders(BuildQuery(IncidentsSelect, IncidentsFrom, IncidentsWhere, IncidentsOrderBy), lastUpdatedOn, page);
        }

        public string GetUnitsQuery(DateTime lastUpdatedOn, int page = 0)
        {
            return ReplacePlaceQueryPlaceholders(BuildQuery(UnitsSelect, UnitsFrom, UnitsWhere, UnitsOrderBy), lastUpdatedOn, page);
        }

        private string ReplacePlaceQueryPlaceholders(string query, DateTime lastUpdatedOn, int page)
        {
            return query.Replace("{{PAGE}}", page.ToString()).Replace("{{LASTUPDATEDDATETIME}}", lastUpdatedOn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        private string BuildQuery(string select, string from, string where, string orderBy)
        {
            //return $"SELECT {select}, COUNT(*) OVER() as total FROM {from} WHERE {where} ORDER BY {orderBy} OFFSET {{{{PAGE}}}} ROWS FETCH NEXT {Limit} ROWS ONLY";
            where = string.IsNullOrWhiteSpace(where) ? "1=1" : where;
            return $"SELECT * FROM ( SELECT {select}, ROW_NUMBER() OVER (ORDER BY {orderBy}) AS seqence, COUNT(*) OVER () as total FROM {from} WHERE {where} ) as x WHERE seqence BETWEEN {{{{PAGE}}}} AND {{{{PAGE}}}} + {Limit}";

        }
    }
}
