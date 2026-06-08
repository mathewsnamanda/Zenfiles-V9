using System.ComponentModel.DataAnnotations;

namespace Zenfiles.Models.Workflow
{
    public class SetObjectWorkflowstates
    {
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int ObjectTypeId { get; set; }
        [Required]
        public int ObjectId { get; set; }
        [Required]
        public int StateId {  get; set; }
        [Required]
        public int WorkflowId { get; set; }
        [Required]
        public int UserID { get; set; }
    }
}
