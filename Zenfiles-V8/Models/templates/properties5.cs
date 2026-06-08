using pulling_object_permission;
using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.templates
{
    public class properties5
    {
        [Required]
        public string? title { get; set; }
        public int propId { get; set; }
        [Required]
        public string? propertytype { get; set; }
        public Boolean IsRequired { get; set; }
        public Boolean IsHidden { get; set; }
        public Boolean IsAutomatic { get; set; }
        public string? Value { get; set; }
        public UserPermission? userPermission { get; set; }
        public string PropGuid { get; set; }
        public string Alias { get; set; }

    }
}
