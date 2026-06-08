using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class prop5
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public List<prop>? propIds { get; set; }
        [Required]
        public int objectid { get; set; }
        [Required]
        public int classid { get; set; }
    }
    public class prop
    {
        public int propid { get; set; }
    }
}
