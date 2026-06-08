using Microsoft.EntityFrameworkCore;
using RecentFix.models;

namespace RecentFix.services
{
    public interface IMFilesObjectRepository
    {
        Task AddOrUpdateAsync(RecentModel entity);

        Task<List<RecentModel>> GetValidItemsAsync(string vaultGuid, int Userid);
        
        Task DeleteAsync(int counter);

        Task CleanupExpiredAsync(int userId);
    }

}
