using readingmetaconfigjson.MetaModals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace readingmetaconfigjson.metaServices
{
    public interface IMeta
    {
        List<behaveprop> behaveprops(string objecttypeid,string objectalias, string classalias, string classtypeid, List<workflowstatepropbehave> workflowstatepropbehaves, string vaultguid);
    }
}
