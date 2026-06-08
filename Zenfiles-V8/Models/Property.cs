using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class Property
    {
        [Required]
        public string? title { get; set; }
        public bool IsRequired { get; set; }
        public int propId { get; set; }
        [Required]
        public string? propertytype { get; set; }
    }
    public class MfilesObject
    {
        [Required]
        public string? objectName { get; set; }
        public int objectID { get; set; }
        public string? ClassName { get; set; }
        public int classID { get; set; }
        [Required]
        public List<Property>? properties { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
    }
}
