using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace readingmetaconfigjson.MetaModals
{
    public class MetaModal
    {
        public List<probehavior> propbehavior { get; set; }
    }

    public class probehavior
    {
        public bool IsHidden { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public int Priority { get; set; } = 1000;
        public string? Property { get; set; } = "";
    }
}
