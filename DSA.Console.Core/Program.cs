using DSA.Lib.Core.Data;
using DSA.Lib.Core.Models;
using System;
using System.Linq;

namespace DSA.Console.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("-r") || args.Contains("--run"))
            {
                UpdaterOpts opts = null;
                UpdaterProfile profile = null;
                try
                {
                    opts = SettingsClient.GetSettings();
                    LogClient.Init();

                    var profileIx = Array.FindIndex(args, x => x == "-p" || x == "--profile");
                    if (profileIx == -1 || profileIx + 1 > args.Length - 1)
                    {
                        throw new Exception("Profile must be specified");
                    }

                    var profileIdStr = args[profileIx + 1];
                    Guid profileId;

                    if (!Guid.TryParse(profileIdStr, out profileId))
                    {
                        throw new Exception($"Malformed Id: {profileIdStr}");
                    }

                    profile = opts.Profiles.FirstOrDefault(x => x.Id == profileId);
                    if (profile == null)
                    {
                        throw new Exception($"Profile not found: {profileId}");
                    }

                    // hit it!
                    var response = new Updater(profile).Run();

                    // Log to console
                    System.Console.WriteLine(response.ToString());

                    // log healthy activity locally
                    var entry = new LogEntry(response.ToString(), "INFO", profile.ApiKey);
                    LogClient.Log(entry);

                    // heartbeat - lub dub
                    if (DateTime.Now.Minute % 10 == 0)
                    {
                        entry.Entry.LogMessage = $"Periodic heartbeat: {entry.Entry.LogMessage}";
                        LogClient.Log(entry, opts.RemoteLogging, opts.LogUrl);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Log(new LogEntry(ex.Message, "ERROR")); // log locally

                    // try to log to intterra
                    if (opts != null)
                    {
                        LogClient.Log(new LogEntry(ex.Message, "ERROR", profile?.ApiKey), opts.RemoteLogging, opts.LogUrl); // log to intterra
                    }

                    System.Console.WriteLine($"ERROR: {ex.Message}");
                    Environment.Exit(1);
                }
                Environment.Exit(0);
            }
            else
            {

                System.Console.WriteLine($"Welcome to the Intterra");
                System.Console.WriteLine($"________    _________   _____   ");
                System.Console.WriteLine($"\\______ \\  /   _____/  /  _  \\  ");
                System.Console.WriteLine($" |    |  \\ \\_____  \\  /  /_\\  \\ ");
                System.Console.WriteLine($" |    `   \\/        \\/    |    \\");
                System.Console.WriteLine($"/_______  /_______  /\\____|__  /");
                System.Console.WriteLine($"        \\/        \\/         \\/ ");
                System.Console.WriteLine($"Data Shipping App\n");

                System.Console.WriteLine($"FLAGS:\n\n");
                System.Console.WriteLine($"Flags\t\tDescription");
                System.Console.WriteLine($"--run or -r\t\tSpecifies that the data should be transferred");
                System.Console.WriteLine($"--profile or -p\t\tSpecifies profile (required with -r)");
            }
        }
    }
}
