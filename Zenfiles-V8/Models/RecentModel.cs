using System.ComponentModel.DataAnnotations;

namespace RecentFix.models
{
    public class RecentModel
    {
        [Key]
        public int Counter { get; set; }   // PK

        [MaxLength(50)]
        public string? DisplayID { get; set; }

        public int Id { get; set; }        // renamed from 'id'

        [MaxLength(200)]
        public string? Title { get; set; }
        [MaxLength(100)]
        public string? ObjectTypeName { get; set; }
        public int ObjectID { get; set; }
        public int ClassID { get; set; }

        [MaxLength(100)]
        public string? ClassTypeName { get; set; }
        public int UserID { get; set; }
        [MaxLength(100)]
        public string? VaultGuid { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
