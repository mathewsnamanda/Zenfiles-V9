using pulling_object_permission;

namespace ZenFiles.Models
{
    public class Objectsearchresponse
    {
        public string? DisplayID { get; set; }
        public int id { get; set; }
        public string? Title { get; set; }
        public string? ObjectTypeName { get; set; }
        public int ObjectID { get; set; }
        public int ClassID { get; set; }
        public UserPermission? userPermission { get; set; }
        public string? ClassTypeName { get; set; }
        public int VersionId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public bool IsSingleFile { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsCheckedOut { get; set; } = false;
        public int checkoutuserid { get; set; } = -1;
        public string? checkoutusername { get; set; } 
        public bool HasRelationship { get; set; }
        public string? FileExtension { get; set; }
        public int FileId { get; set; }
    }
}
