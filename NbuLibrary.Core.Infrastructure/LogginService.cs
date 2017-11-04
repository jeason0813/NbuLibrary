using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Infrastructure
{
    public class LoggingService : ILoggingService
    {
        private const string LOGS_DIRECTORY_KEY = "LogsDirectory";

        private IDatabaseService _storageService;
        private long maxLogFileSize = 5000000; //5MB
        private string logDirPath = null;


        public LoggingService(IDatabaseService storageService)
        {
            _storageService = storageService;
            logDirPath = ConfigurationManager.AppSettings[LOGS_DIRECTORY_KEY] ?? Path.Combine(_storageService.GetRootPath(), "Logs\\");
        }

        public void WriteEvent(LogEventType type, string message, int indent = 0)
        {
            using (FileStream fs = getLogFile())
            {
                using (var writer = new StreamWriter(fs))
                {
                    var now = DateTime.Now;
                    writer.Write("===========[BeginLogEvent:{0} on {1}]===========\n", type.ToString(), now);
                    for (int i = 0; i < indent; i++)
                        writer.Write("\t");
                    writer.Write(message);
                    writer.Write("\n-----------[EndLogEvent:{0} on {1}]-----------\n", type.ToString(), now);
                }
            }
        }

        private FileStream getLogFile()
        {
            if (!Directory.Exists(logDirPath))
                Directory.CreateDirectory(logDirPath);


            FileStream logFile = null;
            foreach (var file in Directory.GetFiles(logDirPath))
            {
                if (file.Length < maxLogFileSize)
                    logFile = new FileStream(file, FileMode.Append);
            }

            if (logFile == null)
            {
                var now = DateTime.Now;
                logFile = File.Create(Path.Combine(logDirPath, string.Format("{0}{1}{2}{3}{4}{5}.log", now.Day, now.Month, now.Year, now.Hour, now.Minute, now.Second)));
            }
            return logFile;
        }
    }
}
