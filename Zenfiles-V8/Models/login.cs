using System.ComponentModel.DataAnnotations;

namespace TestAuthentication.models
{
    public class login
    {
     
        public string? Domain { get; set; }
        [Required]
        public string? Username { get; set; }
        [Required]
        public string? Password { get; set; }
    }
}
