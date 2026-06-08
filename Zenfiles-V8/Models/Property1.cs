using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class Property1
    {
        [Required]
        public string? value { get; set; }        
        public string? DisplayValue { get; set; }
        [Required]
        public int propId { get; set; }
        [Required]
        public string? propertytype { get; set; }
        public string? PropGuid { get; set; }
        public string? Alias { get; set; }
        public string? PropertyName { get; set; }
    }
    public class MfilesCreate
    {
        [Required]
        public int objectID { get; set; }
        [Required]
        public int classID { get; set; }
        public List<Property1>? properties { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        public string? UploadId { get; set; }
        [Required]
        public int UserID { get; set; }
    }
  
}
