using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class UpdateFile1
    {

        [Required]
        public int objectid { get; set; }
        [Required]
        public int ClassId { get; set; }
        [Required]
        public int fileid { get; set; }
        [Required]
        public int versionID { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public string? SignerEmail { get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
