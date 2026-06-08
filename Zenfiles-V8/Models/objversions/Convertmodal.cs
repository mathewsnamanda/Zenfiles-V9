using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.objversions
{
    public class Convertmodal
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int ObjectId { get; set; }
        [Required]
        public int ClassId { get; set; }
        [Required]
        public int fileID { get; set; }
        [Required]
        public bool OverWriteOriginal { get; set; } = false;
        [Required]
        public bool SeparateFile { get; set; } = false;
        [Required]
        public int UserID { get; set; }
    }
}