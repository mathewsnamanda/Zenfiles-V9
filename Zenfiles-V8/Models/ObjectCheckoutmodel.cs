using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models
{
    public class ObjectCheckoutmodel
    {
        [Required]
        public int Objecttypeid { get; set; }
        [Required]
        [Range(0.0000001, double.MaxValue, ErrorMessage = "Value must be greater than 0.")]
        public int objectid { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        [Range(0.0000001, double.MaxValue, ErrorMessage = "Value must be greater than 0.")]
        public int UserID { get; set; }
    }
}
