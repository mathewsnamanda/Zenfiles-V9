using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class UpdateFile
    {
        [Required]
        public int objectid { get; set; }
        [Required]
        public int ClassId { get; set; }
        [Required]
        public int fileid { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        public int versionID { get; set; } 
    }
}
