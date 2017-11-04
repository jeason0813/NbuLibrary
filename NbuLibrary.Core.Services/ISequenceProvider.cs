using NbuLibrary.Core.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface ISequenceProvider
    {
        object GetNext(SequencePropertyModel pm);
    }
}
