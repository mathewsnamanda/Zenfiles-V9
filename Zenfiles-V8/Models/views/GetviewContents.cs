using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.views
{
    public class GetviewContents
    {

        [Required]
        public int ViewId { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int UserID { get; set; }

    }
}
