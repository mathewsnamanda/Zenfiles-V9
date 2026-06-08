using DocumentFormat.OpenXml.Spreadsheet;
using MFilesAPI;
using Microsoft.Extensions.Caching.Memory;
using Zenfiles.PermissionService;
using ZenFiles.Models;

namespace Zenfiles_V8.Services
{
    public class GetCacheObjects
    {
        private readonly IMemoryCache _cache;
        public GetCacheObjects(IMemoryCache cache)
        {
            _cache = cache;
        }
        public List<ObjTypeAdmin> GetObjectTypes(Vault vault)
        {
            return _cache.GetOrCreate($"GetObjectTypes_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                return new Lazy<List<ObjTypeAdmin>>(() =>
                {
                    var results = new List<ObjTypeAdmin>();
                    var mfilesObjects = vault.ObjectTypeOperations.GetObjectTypesAdmin();
                    foreach (ObjTypeAdmin objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<ObjectClass> ClassTypes(Vault vault)
        {
            return _cache.GetOrCreate($"GetClassTypes_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                return new Lazy<List<ObjectClass>>(() =>
                {
                    var results = new List<ObjectClass>();
                    var mfilesObjects = vault.ClassOperations.GetAllObjectClasses();
                    foreach (ObjectClass objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<ObjectClassAdmin> ClassAdmin(Vault vault)
        {
            return _cache.GetOrCreate($"ClassAdmin_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                return new Lazy<List<ObjectClassAdmin>>(() =>
                {
                    var results = new List<ObjectClassAdmin>();
                    var mfilesObjects = vault.ClassOperations.GetAllObjectClassesAdmin();
                    foreach (ObjectClassAdmin objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<PropertyDefAdmin> PropTypes(Vault vault)
        {
            return _cache.GetOrCreate($"GetPropsTypes_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // Use Lazy<T> to ensure only one thread populates the cache
                return new Lazy<List<PropertyDefAdmin>>(() =>
                {
                    var results = new List<PropertyDefAdmin>();
                    var mfilesObjects = vault.PropertyDefOperations.GetPropertyDefsAdmin();
                    foreach (PropertyDefAdmin objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<WorkflowAdmin> WorkflowTypes(Vault vault)
        {
            return _cache.GetOrCreate($"GetWorkflowTypes_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // Wrap in Lazy<T> so only one thread populates the cache
                return new Lazy<List<WorkflowAdmin>>(() =>
                {
                    var results = new List<WorkflowAdmin>();
                    var mfilesObjects = vault.WorkflowOperations.GetWorkflowsAdmin();
                    foreach (WorkflowAdmin objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<ClassGroup> ClassGroupTypes(Vault vault,int objectid)
        {
            return _cache.GetOrCreate($"GetClassGroupTypes_{vault.GetGUID()}_{objectid}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // Wrap in Lazy<T> so only one thread populates the cache
                return new Lazy<List<ClassGroup>>(() =>
                {
                    var results = new List<ClassGroup>();
                    var mfilesObjects = vault.ClassGroupOperations.GetClassGroups(objectid);
                    foreach (ClassGroup objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<UserGroup> UserGroups(Vault vault)
        {
            return _cache.GetOrCreate($"GetClassGroupTypes_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // Wrap in Lazy<T> so only one thread populates the cache
                return new Lazy<List<UserGroup>>(() =>
                {
                    var results = new List<UserGroup>();
                    var mfilesObjects = vault.UserGroupOperations.GetUserGroups();
                    foreach (UserGroup objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }
        public List<UserAccount> UserAccounts(Vault vault)
        {
            return _cache.GetOrCreate($"GetClassGroupTypes_{vault.GetGUID()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // Wrap in Lazy<T> so only one thread populates the cache
                return new Lazy<List<UserAccount>>(() =>
                {
                    var results = new List<UserAccount>();
                    var mfilesObjects = vault.UserOperations.GetUserAccounts();
                    foreach (UserAccount objTypeAdmin in mfilesObjects)
                    {
                        results.Add(objTypeAdmin);
                    }
                    return results;
                });
            }).Value;

        }

    }

}
