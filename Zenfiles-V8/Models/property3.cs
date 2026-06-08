using pulling_object_permission;
using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class property3
    {
        [Required]
        public string? title { get; set; }
        public int propId { get; set; }
        [Required]
        public string? propertytype { get; set; }
        public Boolean IsRequired { get; set; }
        public Boolean IsHidden { get; set; }
        public Boolean IsAutomatic { get; set; }
        public UserPermission? userPermission{ get; set; }
        public bool AllowAdding { get; set; } = false;
        public bool objectTypeVL { get; set; } = false;
        public int TypeID { get; set; }
        public string alias { get; set; }

    }
    public class propreqs
    {
        public int propId { get; set; }
        public bool IsRequired { get; set; }
    }
}
