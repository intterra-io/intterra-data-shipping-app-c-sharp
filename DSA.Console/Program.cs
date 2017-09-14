using DSA.Lib;
using DSA.Lib.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace DSA.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataCenterRootUrl = ConfigurationManager.AppSettings["dataCenterRootUrl"];

            var opts = new UpdaterOpts()
            {
                ApiKey = ConfigurationManager.AppSettings["apiKey"],
                ApiKeySecret = ConfigurationManager.AppSettings["apiKeySecret"],
                DataUrl = $"{dataCenterRootUrl}{ConfigurationManager.AppSettings["dataUrl"]}",
                LastDatetimeUrl = $"{dataCenterRootUrl}{ConfigurationManager.AppSettings["lastDatetimeUrl"]}"
            };

            var dataType = string.Empty;
            var analyticsConnectionString = ConfigurationManager.AppSettings["analyticsConnectionString"];
            var incidentsQueryTemplate = ConfigurationManager.AppSettings["analyticsIncidentsQuery"];
            var unitsQueryTemplate = ConfigurationManager.AppSettings["analyticsUnitsQuery"];
            if (!string.IsNullOrWhiteSpace(analyticsConnectionString) && 
                !string.IsNullOrWhiteSpace(incidentsQueryTemplate) && 
                !string.IsNullOrWhiteSpace(unitsQueryTemplate))
            {
                dataType = "analytics";
            } 
            else
            {
                throw new Exception("Missing one or more required configurations: 'analyticsConnectionString', 'analyticsIncidentsQuery', or 'analyticsUnitsQuery'");
            }

            opts.DataType = dataType;
            opts.IncidentsQuery = incidentsQueryTemplate;
            opts.UnitsQuery = unitsQueryTemplate;
            opts.ConnectionString = analyticsConnectionString;

            var updater = new Updater(opts);
            var response = updater.Run();
            response.Wait();
            System.Console.WriteLine($"Done. TransactionId: {response.Result}");
        }
    }
}
