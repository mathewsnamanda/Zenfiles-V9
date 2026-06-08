using pulling_object_permission;

namespace ZenFiles.Models
{
    public class ObjectClassp
    {
        public int classId { get; set; }
        public string? ClassName { get; set; }
        public UserPermission? userPermission { get; set; }
    }
}
