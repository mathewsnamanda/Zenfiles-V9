using System.ComponentModel.DataAnnotations;

namespace Zenfiles_V8.Models
{
    public class WorkflowAgeingReport
    {
        [Required]
        public string? VaultGuid { get; set; }
        public int WorkflowId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
