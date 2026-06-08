using System.ComponentModel.DataAnnotations;
using ZenFiles.Models;

namespace ZenFiles.Controllers
{
    public class CombinedMfilesCreate
    {
       
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        public int OldClassID { get; set; }
        [Required]
        public int OldObjectTypeID { get; set; }
        [Required]
        public int objectId { get; set; }
        public string Title { get; set; }
    }
  
}