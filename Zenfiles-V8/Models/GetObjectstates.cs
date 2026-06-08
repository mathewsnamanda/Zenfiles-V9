using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models
{
    public class GetObjectstates
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int ObjectTypeId { get; set; }
        [Required]
        public int ObjectId { get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
