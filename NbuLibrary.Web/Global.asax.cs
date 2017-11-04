using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject;
using NbuLibrary.Core.Kernel;
using NbuLibrary.Core.ModuleEngine;
using System.IO;
using System.Xml;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Service.tmp;
using System.Xml.Serialization;
using System.Net.Http.Formatting;
using Newtonsoft.Json;
using NbuLibrary.Core.Services.tmp;
using System.Threading;
using System.Globalization;

namespace NbuLibrary.Web
{
    //TODO: move from here!!!

    public class BackgroundWorker
    {
        public delegate object BackgroundServiceDoWork(object state);

        private BackgroundServiceDoWork _doWork;
        private TimeSpan _interval;

        private static Thread _thread = null;
        private static object _lock = new object();
        private object _initialState;


        public BackgroundWorker(BackgroundServiceDoWork doWork, TimeSpan interval, object initialState)
        {
            _doWork = doWork;
            _interval = interval;
            _initialState = initialState;
        }

        public void Start()
        {
            if (_thread == null)
            {
                lock (_lock)
                {
                    if (_thread == null)
                    {
                        _thread = new Thread(new ThreadStart(Loop));
                        _thread.Start();
                    }
                }
            }
        }


        public void Loop()
        {
            try
            {
                object state = _initialState;
                while (true)
                {
                    state = _doWork(state);
                    Thread.Sleep(_interval);
                }
            }
            catch (ThreadAbortException)
            {
                lock (_lock)
                {
                    _thread = null;
                }
            }
        }
    }

    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        private IKernel _kernel;
        public IKernel Kernel
        {
            get
            {
                if (_kernel == null)
                {
                    _kernel = new StandardKernel();
                    _kernel.Load("NbuLibrary.Core.*.dll");
                    _kernel.Load("NbuLibrary.Modules.*.dll");
                }
                return _kernel;
            }
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            ControllerBuilder.Current.SetControllerFactory(new NinjectControllerFactory(Kernel));
            GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependancyResolver(Kernel);


            var ready = GetReadyModules();
            foreach (var svc in Kernel.GetAll<IBackgroundService>())
            {
                if (ready.Contains(svc.ModuleId))
                {
                    var state = svc.Initialize();
                    BackgroundWorker worker = new BackgroundWorker(svc.DoWork, svc.Interval, state);
                    worker.Start();
                }
            }
        }

        private HashSet<int> GetReadyModules()
        {
            var filepath = Server.MapPath("~/modules.config");
            XmlDocument installed = new XmlDocument();
            if (System.IO.File.Exists(filepath))
            {
                installed.Load(filepath);
            }
            else
            {
                installed.AppendChild(installed.CreateXmlDeclaration("1.0", "UTF-8", "yes"));
                installed.AppendChild(installed.CreateElement("InstalledModules"));
            }

            HashSet<int> hs = new HashSet<int>();
            foreach (var m in Kernel.GetAll<IModule>())
            {
                var node = installed.SelectSingleNode(string.Format("/InstalledModules/Module[@id={0}]", m.Id));
                if (node == null)
                    continue;

                decimal ver = 0.0m;
                if (decimal.TryParse(node.Attributes["ver"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out ver) && ver == m.Version)
                {
                    hs.Add(m.Id);
                }
            }

            return hs;
        }
    }

    public class NinjectDependancyResolver : System.Web.Http.Dependencies.IDependencyResolver
    {
        private IKernel _kernel;

        public NinjectDependancyResolver(IKernel kernel)
        {
            _kernel = kernel;
        }

        public System.Web.Http.Dependencies.IDependencyScope BeginScope()
        {
            return this;
        }

        public object GetService(Type serviceType)
        {
            return _kernel.TryGet(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return _kernel.GetAll(serviceType);
            }
            catch (Exception)
            {
                return new object[] { };
            }
        }

        public void Dispose()
        {
        }
    }

    public class NinjectControllerFactory : DefaultControllerFactory
    {
        private IKernel _kernel;
        public NinjectControllerFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType != null)
                return (IController)_kernel.Get(controllerType);
            else
                return null;
        }
    }

}