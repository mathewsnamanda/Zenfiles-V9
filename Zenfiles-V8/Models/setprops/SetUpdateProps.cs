using System.ComponentModel.DataAnnotations;
using ZenFiles.Models;

namespace Zenfiles.Models.setprops
{
    public class SetUpdateProps
    {
        [Required]
        public int objectid { get; set; }
        [Required]
        public int classid { get; set; }
        [Required]
        public int NewClassId { get; set; }
        [Required]
        public List<updateprop>? props { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
