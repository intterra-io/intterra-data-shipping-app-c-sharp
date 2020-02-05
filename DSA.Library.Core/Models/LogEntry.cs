using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Core.Models { 
    public class LogEntry
    {
        [JsonProperty(PropertyName = "entry")]
        public LogEntryItem Entry { get; set; } = new LogEntryItem();

        public LogEntry(string message)
        {
            Entry.LogMessage = message;
        }

        public LogEntry(string message, string level)
        {
            Entry.LogMessage = message;
            Entry.LogLevel = level;
        }

        public LogEntry(string message, string level, string username)
        {
            Entry.LogMessage = message;
            Entry.LogLevel = level;
            Entry.Username = username;
        }
    }
    public class LogEntryItem
    {
        public string ApplicationName { get; set; } = "DSA";
        public string LogLevel { get; set; } = "INFO";
        public string StackTrace { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public string GroupName { get; set; }
        public string LogMessage { get; set; }
        public string Username { get; set; }
    }
}
