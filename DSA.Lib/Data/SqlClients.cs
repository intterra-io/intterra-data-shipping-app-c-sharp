using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DSA.Lib.Data
{
    public abstract class SqlServerClient
    {
        private string DbName;
        private string ServerName;
        private string Username;
        private string Password;
        private string ConnectionString;

        public SqlServerClient(string dbName, string serverName, string username, string password)
        {
            DbName = dbName;
            ServerName = serverName;
            Username = username;
            Password = password;
        }

        public SqlServerClient(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected string GetJson(string query)
        {
            var list = new List<JObject>();
            using (var conn = GetConnection())
            {
                using (var cmd = new SqlCommand
                {
                    CommandTimeout = 300,
                    Connection = conn,
                    CommandType = CommandType.Text,
                    CommandText = query
                })
                {
                    var table = new DataTable();
                    using (var da = new SqlDataAdapter(cmd))
                        da.Fill(table);

                    return JsonConvert.SerializeObject(table, Formatting.None);
                }
            }
        }

        private SqlConnection GetConnection()
        {
            var output = new SqlConnection(ConnectionString ?? $"Data Source={ServerName};initial catalog={DbName}SitStat;user id={Username};password={Password}");
            if (output.State != ConnectionState.Open)
            {
                output.Open();
            }
            return output;
        }
    }

}
