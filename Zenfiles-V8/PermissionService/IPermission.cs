using MFilesAPI;
using pulling_object_permission;

namespace Zenfiles.PermissionService
{
    public interface IPermission
    {
        public UserPermission ObjectPermission(Vault vault, int UserID, int ObjectID);
        public UserPermission ClassPermission(Vault vault, int UserID, int ClassID);
        public UserPermission PropPermission(Vault vault, int UserID, int PropID);
        public UserPermission WorkflowPermission(Vault vault, int UserID, int WorkflowId);
        public UserPermission Valuelist(Vault vault, int UserID, int valuelist);
    }
}
