using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface IBackgroundService
    {
        int ModuleId { get; }
        TimeSpan Interval { get; }

        object Initialize();
        object DoWork(object state);
    }
}
