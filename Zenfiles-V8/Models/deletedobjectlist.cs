namespace Zenfiles.Models
{
    public class deletedobjectlist
    {
        public List<objectlist> Objectlists { get; set; } = new List<objectlist>();
    }
    public class objectlist
    {
        public int UserID { get; set; }
        public int ObjectID { get; set; }
        public int ObjectTypeID { get; set; }
        public int ClassID { get; set; }
        public string VaultGuid { get; set; }
    }
}
