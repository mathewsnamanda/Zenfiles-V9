using pulling_object_permission;
using System.ComponentModel.DataAnnotations;
using ZenFiles.Models;

namespace Zenfiles.Models.templates
{
    public class template
    {
            [Required]
            public int objectTypeID { get; set; }
            [Required]
            public int classID { get; set; }
            public List<Property1>? properties { get; set; }
            [Required]
            public string? VaultGuid { get; set; }
            [Required]
            public int ObjectID { get; set; }
            [Required]
            public int UserID { get; set; }

    }
}
