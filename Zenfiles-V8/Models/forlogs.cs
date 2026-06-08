using pulling_object_permission;

namespace Zenfiles.Models
{
    public class forlogs
    {
        public string VaultGuid { get; set; }
        public int id { get; set; }
        public string Title { get; set; }
        public string? ObjectTypeName { get; set; }
        public int ObjectID { get; set; }
        public int ClassID { get; set; }
        public UserPermission userPermission { get; set; }
        public string ClassTypeName { get; set; }
        public int VersionId { get; set; }

        public string DisplayID { get; set; }
        public bool IsSingleFile { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastModifiedUtc { get; set; }
    }
}
