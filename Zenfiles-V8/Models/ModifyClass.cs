using System.ComponentModel.DataAnnotations;
using ZenFiles.Models;

namespace Zenfiles_V7.Models
{
    public class ModifyClass
    {
        [Required]
        public int objectID { get; set; }
        [Required]
        public int ObjectTypeID { get; set; }
        [Required]
        public int OldClassID { get; set; }
        [Required]
        public int NewClassID { get; set; }
        public List<Property1>? properties { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
