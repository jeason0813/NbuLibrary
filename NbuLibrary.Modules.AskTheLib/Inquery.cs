using NbuLibrary.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Modules.AskTheLib
{
    public class Inquery
    {
        public const string EntityType = "Inquery";

        public string Question { get; set; }

        public DateTime ReplyBefore { get; set; }

        public ReplyMethods ReplyMethod { get; set; }

        public QueryStatus Status { get; set; }
    }
}
