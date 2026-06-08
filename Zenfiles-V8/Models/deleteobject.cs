using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models
{
    public class deleteobject
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int ObjectId { get; set; }
        [Required]
        public int ClassId { get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
