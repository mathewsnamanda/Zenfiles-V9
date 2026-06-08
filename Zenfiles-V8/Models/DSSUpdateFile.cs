using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class DSSUpdateFile
    {

        [Required]
        public int objectid { get; set; }
        [Required]
        public int fileid { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        public string? FileGuid { get; set; }
    }
}
