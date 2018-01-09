using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DSA.Lib.Data
{
    public abstract class SqlClient
    {
        public abstract DataTable GetData(string query);
    }

    public class SqlServerClient : SqlClient
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

        public override DataTable GetData(string query)
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

                    return table;
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

    public class OdbcClient : SqlClient
    {
        private string ConnectionString;
        
        public OdbcClient(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public override DataTable GetData(string query)
        {
            var list = new List<JObject>();
            using (var conn = GetConnection())
            {
                using (var cmd = new OdbcCommand
                {
                    CommandTimeout = 300,
                    Connection = conn,
                    CommandType = CommandType.Text,
                    CommandText = query
                })
                {
                    var table = new DataTable();
                    using (var da = new OdbcDataAdapter(cmd))
                        da.Fill(table);

                    return table;
                }
            }
        }

        private OdbcConnection GetConnection()
        {
            var output = new OdbcConnection(ConnectionString);
            if (output.State != ConnectionState.Open)
            {
                output.Open();
            }
            return output;
        }
    }

    public static class DataExtensions
    {
        public static string toCsv(this DataTable datatable, char seperator = '\t')
        {
            var sb = new StringBuilder();
            for (int i = 0; i < datatable.Columns.Count; i++)
            {
                sb.Append(datatable.Columns[i]);
                if (i < datatable.Columns.Count - 1)
                    sb.Append(seperator);
            }
            sb.AppendLine();
            foreach (DataRow dr in datatable.Rows)
            {
                for (int i = 0; i < datatable.Columns.Count; i++)
                {
                    sb.Append(dr[i].ToString().Replace("\t", "    "));

                    if (i < datatable.Columns.Count - 1)
                        sb.Append(seperator);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
        public static string toJson(this DataTable datatable, Formatting formatting = Formatting.None, char seperator = '\t')
        {
            return JsonConvert.SerializeObject(datatable, formatting);
        }
    }
}
