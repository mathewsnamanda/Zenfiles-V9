using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Zenfiles.Models
{
    public class otherfile
    {
        [Required]
        public int ObjectId { get; set; }
        [Required]

        public string? VaultGuid { get; set; }
        [Required]

        public int fileID { get; set; }
        [Required]

        public int ClassId { get; set; }
    }
}
