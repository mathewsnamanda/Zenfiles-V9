using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class Reports
    {
        [Required]
        public int objectID { get; set; }
        [Required]
        public int objectTypeID { get; set; }
        [Required]
        public int classID { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
      
    }
  
}
