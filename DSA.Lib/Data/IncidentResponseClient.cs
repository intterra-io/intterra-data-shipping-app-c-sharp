using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DSA.Lib.Data
{
    public class IncidentResponseClient : SqlServerClient
    {
        public string Query { get; private set; }

        public IncidentResponseClient(string connectionString, string query)
            : base(connectionString)
        {
            Query = query;
        }

        public string Get()
        {
            return GetJson(Query);
        }
    }
}
