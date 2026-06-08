using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class property4
    {
        [Required]
        public string? title { get; set; }
        public bool IsRequired { get; set; }
        [Required]
        public string? propertytype { get; set; }
    }
  
    public class MfilesObject1
    {
        [Required]
        public string? objectName { get; set; }
        public string? ClassName { get; set; }
        [Required]
        public List<property4>? properties { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
    }
}
