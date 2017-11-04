using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public enum LogEventType
    {
        Info = 0,
        Warning = 1,
        Error = 2

    }
    public interface ILoggingService
    {
        void WriteEvent(LogEventType type, string message, int indent = 0);
    }
}
