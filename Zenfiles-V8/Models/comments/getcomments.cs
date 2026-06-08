using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.comments
{
    public class getcomments
    {
        [Required]
        public int ObjectId { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int ObjectTypeId { get; set; }
    }
}
