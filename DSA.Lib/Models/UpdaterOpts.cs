using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSA.Lib.Models
{
    public class UpdaterOpts
    {
        public string IncidentsQuery { get; set; }
        public string UnitsQuery { get; set; }
        public string ConnectionString { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeySecret { get; set; }
        public string LastDatetimeUrl { get; set; }
        public string DataUrl { get; set; }
        public string TestUrl { get; set; }
        public string DataType { get; set; }
        public int Limit { get; set; }

        public UpdaterOpts()
        {
            Limit = 100000;
        }

        public string BuildQuery(string select, string from, string where, string orderBy )
        {
            return $"SELECT {select}, COUNT(*) OVER() as total FROM {from} WHERE {where} ORDER BY {orderBy} OFFSET {{{{PAGE}}}} ROWS FETCH NEXT {Limit} ROWS ONLY";
        }
    }
}
