using NbuLibrary.Core.Services;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Reporting
{
    public class Bindings : NinjectModule
    {
        public override void Load()
        {
            Bind<IReportingService>().To<ReportingServiceImpl>();
        }
    }
}
