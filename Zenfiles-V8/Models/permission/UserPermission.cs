using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pulling_object_permission
{
    public class UserPermission
    {
        public bool ReadPermission { get; set; } = false;
        public bool EditPermission { get; set; } = false;
        public bool AttachObjectsPermission { get; set; } = false;
        public bool DeletePermission { get; set; } = false;
        public bool IsClassDeleted { get; set; } = false;
    }
}
