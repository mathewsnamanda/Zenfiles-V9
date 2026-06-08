
using Microsoft.EntityFrameworkCore;
using RecentFix.models;

namespace DSS_Api.Context
{
    public class MFilesDbContext : DbContext
    {
        public MFilesDbContext(DbContextOptions<MFilesDbContext> options) : base(options) { }

        public DbSet<RecentModel> Objects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecentModel>(entity =>
            {
                entity.HasKey(e => e.Counter);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DisplayID).HasMaxLength(50);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.ObjectTypeName).HasMaxLength(100);
                entity.Property(e => e.ClassTypeName).HasMaxLength(100);
                entity.Property(e => e.VaultGuid).HasMaxLength(100);

                // Default timestamp in Firebird
                entity.Property(e => e.TimeStamp)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Composite unique index to avoid duplicates
                entity.HasIndex(e => new { e.UserID, e.VaultGuid, e.Id, e.ObjectID, e.ClassID })
                      .IsUnique();
            });
        }
    }

}
