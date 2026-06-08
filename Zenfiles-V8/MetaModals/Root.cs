using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class MetadataConfig
    {
        public List<Rule> Rules { get; set; }
    }

    public class Rule
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public Filter Filter { get; set; }
        public Behavior Behavior { get; set; }
        public List<Rule> SubRules { get; set; }
    }

    public class Filter
    {
        public List<string> Class { get; set; }
        public List<string> ObjectType { get; set; }
        public List<PropertyFilter> Properties { get; set; }
    }

    public class PropertyFilter
    {
        public string Property { get; set; }
        public string Operator { get; set; }
        public bool? Boolean { get; set; }
        public List<MSLUItem> MSLU { get; set; }
        public SSLU SSLU { get; set; }
    }

    public class MSLUItem
    {
        public SSLUItem Item { get; set; }
    }

    public class SSLU
    {
        public SSLUItem Item { get; set; }
    }

    public class SSLUItem
    {
        public string State { get; set; }
        public string workflow { get; set; }
        public string id { get; set; }
        public string valueListItem { get; set; }
    }

    public class Behavior
    {
        public List<PropertyBehavior> Properties { get; set; }
    }

    public class PropertyBehavior
    {
        public string Property { get; set; }
        public bool? IsRequired { get; set; }
        public bool? IsHidden { get; set; }
        public bool? IsReadOnly { get; set; }
        public bool? Contr { get; set; } // example extra field
        public int Priority { get; set; }
    }
}
