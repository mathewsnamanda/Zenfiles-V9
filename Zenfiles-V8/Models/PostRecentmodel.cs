using System.ComponentModel.DataAnnotations;

namespace RecentFix.models
{
    public class PostRecentmodel
    {
      public string? DisplayID { get; set; }
      public int Id { get; set; }        // renamed from 'id'
      public string? Title { get; set; }
    
        public string? ObjectTypeName { get; set; }
        public int ObjectID { get; set; }
        public int ClassID { get; set; }
         
        public string? ClassTypeName { get; set; }
        public int UserID { get; set; }

        public string? VaultGuid { get; set; }
    }
}
