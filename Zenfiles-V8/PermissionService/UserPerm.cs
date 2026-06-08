using ConsoleApp1;
using MFilesAPI;
using pulling_object_permission;
using System;
using System.Data;
using Zenfiles_V8.Services;

namespace Zenfiles.PermissionService
{
    public class UserPerm : IPermission
    {
        private readonly GetCacheObjects _cacheObjects;
        private readonly Gettingusersinusergroup _gettingusersinusergroup;

        public UserPerm(GetCacheObjects cacheObjects, Gettingusersinusergroup gettingusersinusergroup)
        {
            _cacheObjects = cacheObjects;
            _gettingusersinusergroup = gettingusersinusergroup;
        }
        public UserPermission ClassPermission(Vault vault, int UserID, int ClassID)
        {
            UserPermission userPermission = new UserPermission();
            var found = false;
            try
            {
                try
                {
                    var objType = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m=>m.ID==ClassID);
                    if (objType != null)
                    {
                        Console.WriteLine(ClassID);
                        AccessControlList acl = objType.ACLForObjects; // Display the ACL details
                        if (acl != null)
                            foreach (AccessControlEntry accessControlEntry1 in acl)
                            {
                                if (accessControlEntry1.IsGroup)
                                {
                                    try
                                    {


                                        var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                        foreach (var item2 in items)
                                        {
                                            if (item2 == UserID)
                                            {
                                                found = true;
                                            }
                                        }

                                    }
                                    catch (Exception ex)
                                    {

                                    }


                                }
                                else
                                {
                                    try
                                    {
                                        var username = vault.UserOperations.GetUserAccount(accessControlEntry1.UserOrGroupID);
                                        if (UserID == username.ID)
                                        {
                                            found = true;
                                        }
                                    }
                                    catch
                                    {

                                    }

                                }
                                if (found)
                                {
                                    AccessControlEntryContainer accessControlEntry = acl.CustomComponent.AccessControlEntries;
                                    // Example key (replace with actual key)
                                    AccessControlEntryKey aceKey = new AccessControlEntryKey();
                                    aceKey.SetUserOrGroupID(accessControlEntry1.UserOrGroupID, accessControlEntry1.IsGroup); // Example user/group ID
                                    AccessControlEntryData aceData = accessControlEntry.At(aceKey);
                                    if (aceData != null)
                                    {
                                        
                                            if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow"))|| (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "")))
                                            {
                                                userPermission.ReadPermission = true;
                                            }
                                            if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow"))|| (!userPermission.EditPermission && (aceData.EditPermission.ToString() == "")))
                                            {
                                                userPermission.EditPermission = true;
                                            }
                                            if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow"))|| (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "")))
                                            {

                                                userPermission.AttachObjectsPermission = true;
                                            }
                                            if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow"))|| (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "")))
                                            {
                                                userPermission.DeletePermission = true;
                                            }
                                       
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        else
                        {
                            userPermission.AttachObjectsPermission = true;
                            userPermission.EditPermission = true;
                            userPermission.ReadPermission = true;
                            userPermission.DeletePermission = false;
                        }

                    }
                }
                catch
                {
             
                }

            }
            catch
            {

            }
            if(!found)
            try
            {
                var username = vault.UserOperations.GetUserAccount(UserID);

                int result;

                if (!int.TryParse(username.VaultRoles.ToString(), out result))
                {
                    result = 0; // fallback if parsing fails
                }

                var roles = EnumDecryptor.Decrypt(result);
                bool foundperm = false;
                foreach (var role in roles)
                {
                    if (role.ToString() == "FullControl")
                    {
                        foundperm = true;
                    }
                }

                if (foundperm)
                {
                    if (!found)
                    {
                        userPermission.DeletePermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.AttachObjectsPermission = true;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return userPermission;
        }
        public UserPermission ObjectPermission(Vault vault, int UserID, int ObjectID)
        {
            UserPermission userPermission = new UserPermission();
            var found = false;
            try
            {
                var objType = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == ObjectID)?.ObjectType;
                if (objType != null)
                {
                    AccessControlList acl = objType.AccessControlList; // Display the ACL details
                    if(acl!= null)
                    foreach (AccessControlEntry accessControlEntry1 in acl)
                    {
                        if (accessControlEntry1.IsGroup)
                        {
                            try
                            {

                                var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                foreach (var item2 in items)
                                {
                                    if (item2 == UserID)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        else
                        {
                            try
                            {
                                var username = vault.UserOperations.GetUserAccount(accessControlEntry1.UserOrGroupID);
                                if (UserID == username.ID)
                                {
                                    found = true;
                                }
                            }
                            catch
                            {

                            }
                        }
                        if (found)
                        {
                            AccessControlEntryContainer accessControlEntry = acl.CustomComponent.AccessControlEntries;
                            // Example key (replace with actual key)
                            AccessControlEntryKey aceKey = new AccessControlEntryKey();
                            aceKey.SetUserOrGroupID(accessControlEntry1.UserOrGroupID, accessControlEntry1.IsGroup); // Example user/group ID
                            AccessControlEntryData aceData = accessControlEntry.At(aceKey);
                            if (aceData != null)
                            {
                                    if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "")))
                                    {
                                        userPermission.ReadPermission = true;
                                    }
                                    if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == "")))
                                    {
                                        userPermission.EditPermission = true;
                                    }
                                    if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "")))
                                    {

                                        userPermission.AttachObjectsPermission = true;
                                    }
                                    if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "")))
                                    {
                                        userPermission.DeletePermission = true;
                                    }

                                }

                            }
                    }
                    else
                    {
                        userPermission.AttachObjectsPermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.DeletePermission = false;
                    }
                }
            }
            catch
            {
              
            }

            if (!found)
            {
                try
                {
                    var username = vault.UserOperations.GetUserAccount(UserID);
                    int result;

                    if (!int.TryParse(username.VaultRoles.ToString(), out result))
                    {
                        result = 0; // fallback if parsing fails
                    }

                    var roles = EnumDecryptor.Decrypt(result);
                    bool foundperm = false;
                    foreach (var role in roles)
                    {
                        if (role.ToString() == "FullControl")
                        {
                            foundperm = true;
                        }
                    }

                    if (foundperm)
                    {
                        if (!found)
                        {
                            userPermission.DeletePermission = true;
                            userPermission.EditPermission = true;
                            userPermission.ReadPermission = true;
                            userPermission.AttachObjectsPermission = true;
                        }
                    }

                }
                catch
                {

                }
            }
            return userPermission;
        }
        public UserPermission PropPermission(Vault vault, int UserID, int PropID)
        {
            UserPermission userPermission = new UserPermission();
            var found = false;
            try
            {
                var objType = _cacheObjects.PropTypes(vault)?.FirstOrDefault(m=>m.PropertyDef.ID==PropID);
                if (objType != null)
                {
                    AccessControlList acl = objType.PropertyDef.AccessControlList; // Display the ACL details
                    if(acl!=null)
                    foreach (AccessControlEntry accessControlEntry1 in acl)
                    {
                        if (accessControlEntry1.IsGroup)
                        {
                            try
                            {

                                var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                foreach (var item2 in items)
                                {
                                    if (item2 == UserID)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                        else
                        {
                            try
                            {
                                var username = vault.UserOperations.GetUserAccount(accessControlEntry1.UserOrGroupID);
                                if (UserID == username.ID)
                                {
                                    found = true;
                                }
                            }
                            catch
                            {

                            }

                        }
                        if (found)
                        {
                            AccessControlEntryContainer accessControlEntry = acl.CustomComponent.AccessControlEntries;
                            // Example key (replace with actual key)
                            AccessControlEntryKey aceKey = new AccessControlEntryKey();
                            aceKey.SetUserOrGroupID(accessControlEntry1.UserOrGroupID, accessControlEntry1.IsGroup); // Example user/group ID
                            AccessControlEntryData aceData = accessControlEntry.At(aceKey);
                            if (aceData != null)
                            {
                                    if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "")))
                                    {
                                        userPermission.ReadPermission = true;
                                    }
                                    if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == "")))
                                    {
                                        userPermission.EditPermission = true;
                                    }
                                    if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "")))
                                    {

                                        userPermission.AttachObjectsPermission = true;
                                    }
                                    if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "")))
                                    {
                                        userPermission.DeletePermission = true;
                                    }

                                }
                                else
                            {

                            }
                        }
                    }
                    else
                    {
                        userPermission.AttachObjectsPermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.DeletePermission = false;
                    }
                }
            }
            catch
            {
              

            }
            if (!found)
                try
            {
                var username = vault.UserOperations.GetUserAccount(UserID);
                int result;

                if (!int.TryParse(username.VaultRoles.ToString(), out result))
                {
                    result = 0; // fallback if parsing fails
                }

                var roles = EnumDecryptor.Decrypt(result);
                bool foundperm = false;
                foreach (var role in roles)
                {
                    if (role.ToString() == "FullControl")
                    {
                        foundperm = true;
                    }
                }
                if (foundperm)
                {
                    if (!found)
                    {
                        userPermission.DeletePermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.AttachObjectsPermission = true;
                    }
                }

            }
            catch
            {

            }
            return userPermission;
        }
        public UserPermission WorkflowPermission(Vault vault, int UserID, int WorkflowId)
        {
            UserPermission userPermission = new UserPermission();
            var found = false;
            try
            {
                var objType = _cacheObjects.WorkflowTypes(vault)?.FirstOrDefault(m=>m.Workflow.ID==WorkflowId);
                if (objType != null)
                {
                    AccessControlList acl = objType.Permissions; // Display the ACL details
                    if(acl!=null)
                    foreach (AccessControlEntry accessControlEntry1 in acl)
                    {
                        if (accessControlEntry1.IsGroup)
                        {
                            try
                            {

                                var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                foreach (var item2 in items)
                                {
                                    if (item2 == UserID)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        else
                        {
                            try
                            {
                                var username = vault.UserOperations.GetUserAccount(accessControlEntry1.UserOrGroupID);
                                if (UserID == username.ID)
                                {
                                    found = true;
                                }
                            }
                            catch
                            {

                            }
                        }
                        if (found)
                        {
                            AccessControlEntryContainer accessControlEntry = acl.CustomComponent.AccessControlEntries;
                            // Example key (replace with actual key)
                            AccessControlEntryKey aceKey = new AccessControlEntryKey();
                            aceKey.SetUserOrGroupID(accessControlEntry1.UserOrGroupID, accessControlEntry1.IsGroup); // Example user/group ID
                            AccessControlEntryData aceData = accessControlEntry.At(aceKey);
                            if (aceData != null)
                            {
                                    if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "")))
                                    {
                                        userPermission.ReadPermission = true;
                                    }
                                    if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == "")))
                                    {
                                        userPermission.EditPermission = true;
                                    }
                                    if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "")))
                                    {

                                        userPermission.AttachObjectsPermission = true;
                                    }
                                    if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "")))
                                    {
                                        userPermission.DeletePermission = true;
                                    }

                                }
                                else
                            {

                            }
                        }
                    }
                    else
                    {
                        userPermission.AttachObjectsPermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.DeletePermission = false;
                    }
                }

            }
            catch
            {

            }
            if (!found)
                try
            {
                var username = vault.UserOperations.GetUserAccount(UserID);
                int result;

                if (!int.TryParse(username.VaultRoles.ToString(), out result))
                {
                    result = 0; // fallback if parsing fails
                }

                var roles = EnumDecryptor.Decrypt(result);
                bool foundperm = false;
                foreach (var role in roles)
                {
                    if (role.ToString() == "FullControl")
                    {
                        foundperm = true;
                    }
                }
                if (foundperm)
                {
                    if (!foundperm)
                    {
                        userPermission.DeletePermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.AttachObjectsPermission = true;
                    }
                }

            }
            catch
            {

            }
            return userPermission;
        }
        public UserPermission Valuelist(Vault vault, int UserID, int valuelist)
        {
            UserPermission userPermission = new UserPermission();
            var found = false;
            try
            {
                var objType = vault.ValueListOperations.GetValueList(valuelist);
                if (objType != null)
                {

                    AccessControlList acl = objType.AccessControlList; // Display the ACL details
                    if(acl!= null)
                    foreach (AccessControlEntry accessControlEntry1 in acl)
                    {
                        if (accessControlEntry1.IsGroup)
                        {
                            try
                            {

                                var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                foreach (var item2 in items)
                                {
                                    if (item2 == UserID)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        else
                        {
                            try
                            {
                                var username = vault.UserOperations.GetUserAccount(accessControlEntry1.UserOrGroupID);
                                if (UserID == username.ID)
                                {
                                    found = true;
                                }
                            }
                            catch
                            {

                            }
                        }
                        if (found)
                        {
                            AccessControlEntryContainer accessControlEntry = acl.CustomComponent.AccessControlEntries;
                            // Example key (replace with actual key)
                            AccessControlEntryKey aceKey = new AccessControlEntryKey();
                            aceKey.SetUserOrGroupID(accessControlEntry1.UserOrGroupID, accessControlEntry1.IsGroup); // Example user/group ID
                            AccessControlEntryData aceData = accessControlEntry.At(aceKey);
                            if (aceData != null)
                            {
                                    if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "")))
                                    {
                                        userPermission.ReadPermission = true;
                                    }
                                    if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == "")))
                                    {
                                        userPermission.EditPermission = true;
                                    }
                                    if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "")))
                                    {

                                        userPermission.AttachObjectsPermission = true;
                                    }
                                    if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "")))
                                    {
                                        userPermission.DeletePermission = true;
                                    }

                                }
                                else
                            {

                            }
                        }
                    }
                    else
                    {
                        userPermission.AttachObjectsPermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.DeletePermission = false;
                    }
                }
            }
            catch
            {
              

            }
            if (!found)
                try
            {
                var username = vault.UserOperations.GetUserAccount(UserID);
                int result;

                if (!int.TryParse(username.VaultRoles.ToString(), out result))
                {
                    result = 0; // fallback if parsing fails
                }

                var roles = EnumDecryptor.Decrypt(result);
                bool foundperm = false;
                foreach (var role in roles)
                {
                    if (role.ToString() == "FullControl")
                    {
                        foundperm = true;
                    }
                }
                if (foundperm)
                {
                    if (!found)
                    {
                        userPermission.DeletePermission = true;
                        userPermission.EditPermission = true;
                        userPermission.ReadPermission = true;
                        userPermission.AttachObjectsPermission = true;
                    }
                }

            }
            catch
            {

            }
            return userPermission;
        }
    }
}
