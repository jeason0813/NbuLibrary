using NbuLibrary.Core.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface IDomainChangeListener
    {
        void BeforeSave(EntityModel entityModel);
        void AfterSave(EntityModel entityModel);
    }
}
