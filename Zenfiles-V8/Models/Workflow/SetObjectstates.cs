using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.Workflow
{
    public class SetObjectstates
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int ObjectTypeId { get; set; }
        [Required]
        public int ObjectId { get; set; }
        [Required]
        public int NextStateId {  get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
