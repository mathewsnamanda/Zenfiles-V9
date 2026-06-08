using System.ComponentModel.DataAnnotations;

namespace Zenfiles_V8.Models.search
{
    public class Searchparams
    {
        [Required]
        public string? VaultGuid { get; set; }
        public string? SearchPhrase { get; set; }
        public int UserID { get; set; }
        public Objecttype? Objecttype { get; set; }
        public Objecttype? Classtype { get; set; }
    }
    public class Objecttype
    {
        public string? searchcondition { get; set; }
        public int value { get; set; }
    }
}
