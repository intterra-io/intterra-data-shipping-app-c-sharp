using DSA.Lib;
using DSA.Lib.Data;
using System;
using System.Linq;

namespace DSA.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            LogClient.Init();

            if (args.Contains("-r") || args.Contains("--run"))
            {
                try
                {
                    var opts = SettingsClient.Get();

                    var profileIx = Array.FindIndex(args, x => x == "-p" || x == "--profile");
                    if (profileIx == -1 || profileIx + 1 > args.Length - 1)
                    {
                        throw new Exception("Profile must be specified");
                    }

                    var profileName = args[profileIx + 1];
                    var profile = opts.Profiles[profileName];
                    if (profileIx == -1 || profileIx + 1 > args.Length - 1)
                    {
                        throw new Exception($"Profile not found: {profileName}");
                    }

                    var batches = new Updater(profile).Run();
                    LogClient.Log($"Successfully submitted {batches.Count()} batch(es): {string.Join(", ", batches.ToArray())}");
                }
                catch (Exception ex)
                {
                    LogClient.Log(ex.Message, "ERROR");
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
