using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.Valuelist
{
    public class ValuelistPost
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        [Range(0.0000001, double.MaxValue, ErrorMessage = "Value must be greater than 0.")]
        public int UserID { get; set; }
        [Range(0.0000001, double.MaxValue, ErrorMessage = "Value must be greater than 0.")]
        [Required]
        public int ValuelistID { get; set; }
        [Required]
        public string? Name { get; set; }
    }
}
