using DSA.Lib;
using DSA.Lib.Data;
using DSA.Lib.Models;
using System;
using System.Linq;

namespace DSA.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("-r") || args.Contains("--run"))
            {
                UpdaterOpts opts = null;
                try
                {
                    opts = SettingsClient.Get();
                    LogClient.Init();

                    var profileIx = Array.FindIndex(args, x => x == "-p" || x == "--profile");
                    if (profileIx == -1 || profileIx + 1 > args.Length - 1)
                    {
                        throw new Exception("Profile must be specified");
                    }

                    var profileName = args[profileIx + 1];
                    var profile = opts.Profiles.FirstOrDefault(x => x.Name == profileName);
                    if (profile == null)
                    {
                        throw new Exception($"Profile not found: {profileName}");
                    }

                    // hit it!
                    var batches = new Updater(profile).Run();

                    // log healthy activity locally
                    var entry = new LogEntry($"Successfully submitted {batches.Count()} batch(es): {string.Join(", ", batches.ToArray())}");
                    LogClient.Log(entry, opts.RemoteLogging, opts.LogUrl); 

                    // heartbeat - lub dub
                    if (new DateTime().Minute % 10 == 0)
                    {
                        entry.LogMessage = $"Periodic heartbeat: {entry.LogMessage}";
                        LogClient.Log(entry, opts.RemoteLogging, opts.LogUrl);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Log(new LogEntry(ex.Message, "ERROR")); // log locally

                    // try to log to intterra
                    if (opts != null)
                    {
                        LogClient.Log(new LogEntry(ex.Message, "ERROR"), opts.RemoteLogging, opts.LogUrl); // log to intterra
                    }

                    System.Console.WriteLine($"UNKNOWN ERROR:\n\n{ex.Message}");
                    Environment.Exit(1);
                }
                Environment.Exit(0);
            }
            else
            {
                System.Console.WriteLine($"FLAGS:\n\n");
                System.Console.WriteLine($"Flags\t\tDescription");
                System.Console.WriteLine($"--run or -r\t\tSpecifies that the data should be transferred");
                System.Console.WriteLine($"--profile or -p\t\tSpecifies profile (required with -r)");
            }
        }
    }
}
