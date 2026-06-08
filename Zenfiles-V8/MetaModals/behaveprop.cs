using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace readingmetaconfigjson.MetaModals
{
    public class behaveprop
    {
        public bool IsHidden { get; set; }
        public bool IsRequired { get; set; }
        public int Priority { get; set; }
        public string Property { get; set; }
    }
}
