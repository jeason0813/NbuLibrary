using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public enum QueryStatus
    {
        New = 0,
        InProcess = 1,
        Completed = 2,
        Canceled = 3,
        Rejected = 4
    }
}
