using pulling_object_permission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenfiles.Models.views
{
    public class ParentViews
    {
        public int ID { get; set; }
        public string? ViewName { get; set; }
        public UserPermission? userPermission { get; set; }
        public List<GroupLevel>? groupLevels { get; set; }
    }
    public class AllParentsViews
    {
        public List<ParentViews>? CommonViews { get; set; }
        public List<ParentViews>? MyViews { get; set; }

        public List<ParentViews>? OtherViews { get; set; }

    }
}
