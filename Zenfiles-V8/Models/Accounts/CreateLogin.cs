using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.login
{
    public class CreateLogin
    {
     
        public string? FullName { get; set; }
        public int UserID { get; set; }
        public string? Password { get; set; }
        [EmailAddress]
        public string? EmailAddress { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        public Boolean IsAdmin { get; set; }
    }
}
