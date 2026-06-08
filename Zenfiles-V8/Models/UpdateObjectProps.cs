using pulling_object_permission;
using System.ComponentModel.DataAnnotations;

namespace ZenFiles.Models
{
    public class UpdateObjectProps
    {
        [Required]
        public int objectid { get; set; }
        [Required]
        public int Objectypeid { get; set; }
        [Required]
        public int classid { get; set; }
        [Required]
        public List<updateprop>? props { get; set; }
        [Required]
        public string? VaultGuid { get; set; }
        [Required]
        public int UserID { get; set; }
    }
    public class updateprop
    {
        public int id { get; set; }
        public string? Value { get; set; }
        public string? Datatype { get; set; }
    }
    public class updateprop1
    {
        public int id { get; set; }
        public object Value { get; set; }
        public string? Datatype { get; set; }
        public string? DisplayValue { get; set; }
        public string? PropName { get; set; }
        public bool IsRequired { get; set; }
        public bool IsHidden { get; set; }
        public bool IsAutomatic { get; set; }
        public bool AllowAdding { get; set; } = false;
        public bool objectTypeVL { get; set; }= false;
        public int TypeID { get; set; }
        public UserPermission? userPermission { get; set; }
        public string Alias { get; set; }
        public string PropGuid { get; set; }
    }
}
