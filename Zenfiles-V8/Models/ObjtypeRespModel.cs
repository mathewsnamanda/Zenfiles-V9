using pulling_object_permission;

namespace ZenFiles.Models
{
    public class ObjtypeRespModel
    {
        public int objectid { get; set; }
        public string? namesingular { get; set; }
        public string? nameplural { get; set; }
        public UserPermission? userPermission { get; set; }
        public bool External { get; set; }
    }
}
