using DSS_Api.Context;
using Microsoft.EntityFrameworkCore;
using RecentFix.models;

namespace RecentFix.services
{
    public class MFilesObjectRepository : IMFilesObjectRepository
    {
        private readonly MFilesDbContext _context;
        public MFilesObjectRepository(MFilesDbContext context)
        {
            _context = context;
        }
        public async Task AddOrUpdateAsync(RecentModel entity)
        {
            var existing = await _context.Objects
                .FirstOrDefaultAsync(e =>
                    e.VaultGuid == entity.VaultGuid &&
                    e.Id == entity.Id &&
                    e.ClassID == entity.ClassID &&
                    e.ObjectID == entity.ObjectID &&
                    e.UserID == entity.UserID);

            if (existing != null)
            {
                // Update timestamp or other fields
                existing.TimeStamp = DateTime.UtcNow;
                _context.Objects.Update(existing);
            }
            else
            {
                await _context.Objects.AddAsync(entity);
            }

            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int counter)
        {
            var entity = await _context.Objects.FindAsync(counter);
            if (entity != null)
            {
                _context.Objects.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        public async Task CleanupExpiredAsync(int userId)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            var expired = _context.Objects
                .Where(o => o.UserID == userId && o.TimeStamp < cutoff);

            _context.Objects.RemoveRange(expired);
            await _context.SaveChangesAsync();
        }
        public async Task<List<RecentModel>> GetValidItemsAsync(string vaultGuid, int userId)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            var expired = _context.Objects
                .Where(o => o.UserID == userId && o.TimeStamp < cutoff);
            _context.Objects.RemoveRange(expired);
            await _context.SaveChangesAsync();

            return await _context.Objects
                .Where(o => o.VaultGuid == vaultGuid &&
                            o.UserID == userId &&
                            o.TimeStamp >= cutoff)
                .OrderByDescending(m => m.TimeStamp)
                .Take(20)
                .ToListAsync();
        }
    }

}
