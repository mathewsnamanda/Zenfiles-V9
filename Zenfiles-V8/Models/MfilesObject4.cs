using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class Property5
    {
        public int propId { get; set; }
        public string? title { get; set; }
    }
   
    public class MfilesObject4
    {
    
        public int objectID { get; set; }
        public int classID { get; set; }
        [Required]
        public List<Property5>? properties { get; set; }
      
    }
}
