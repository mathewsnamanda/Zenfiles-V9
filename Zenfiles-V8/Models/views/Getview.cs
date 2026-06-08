using pulling_object_permission;

namespace Zenfiles.Models.views
{
    public class Getview
    {
        public string? Type { get; set; }
        public int id { get; set; }
        public string? Title { get; set; }
        public int ObjectTypeId { get; set; }
        public int ClassId { get; set; }
        public string? propId { get; set; }
        public string? PropDatatype { get; set; }
        public int ViewId { get; set; }
        public List<GroupLevel>? groupLevels { get; set; }
        public UserPermission? userPermission { get; set; }
        public string? ClassTypeName { get; set; }
        public int VersionId { get; set; }
        public string? ObjectTypeName { get; set; }
        public string? DisplayID { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public bool IsSingleFile { get; set; }
        public bool IsCheckedOut { get; set; }
        public Int64 checkoutuserid { get; set; } = -1;
        public bool HasRelationship { get; set; }
        public string? FileExtension { get; set; }
        public int FileId { get; set; }
        public string? checkoutusername { get; set; }
    }
}
