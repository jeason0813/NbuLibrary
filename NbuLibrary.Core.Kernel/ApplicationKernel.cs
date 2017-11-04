using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;

namespace NbuLibrary.Core.Kernel
{
    public class ApplicationKernel
    {
        private static IKernel _kernel;
        public static IKernel Current
        {
            get
            {
                if (_kernel == null)
                {
                    _kernel = new StandardKernel();

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asmbl => asmbl.FullName.StartsWith("NbuLibrary."));

                    _kernel.Load(assemblies);
                }

                return _kernel;
            }
        }
    }
}
