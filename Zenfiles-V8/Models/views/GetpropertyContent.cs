using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.views
{
    public class GetpropertyContent
    {
        [Required]
        public int ViewId { get; set; }
        [Required]
        public int UserID { get; set; } 

        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public List<properties> properties { get; set; } = new List<properties>();

    }
    public class properties
    {

        [Required]
        public string? propId { get; set; }
        [Required]
        public string? PropDatatype { get; set; }
    }
}
