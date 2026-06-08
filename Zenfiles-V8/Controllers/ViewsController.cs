using ConsoleApp1;
using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using pulling_object_permission;
using RecentFix.services;
using System.Security;
using System.Security.Cryptography;
using System.Xml.Linq;
using Zenfiles.Models;
using Zenfiles.Models.comments;
using Zenfiles.Models.objversions;
using Zenfiles.Models.views;
using Zenfiles.PermissionService;
using ZenFiles.Models;
using Zenfiles_V8.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZenFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    public class ViewsController : ControllerBase
    {
        private readonly IMFilesObjectRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly Zenfiles.PermissionService.IPermission _permission;
        private readonly Gettingusersinusergroup  _gettingusersinusergroup;
        private readonly GetCacheObjects _cacheObjects;

        public ViewsController(IConfiguration Configuration, 
            Zenfiles.PermissionService.IPermission permission,
            IMFilesObjectRepository repository,
            Gettingusersinusergroup gettingusersinusergroup,
            GetCacheObjects getCacheObjects)
        {
            _permission = permission;
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _repository = repository;
            _gettingusersinusergroup = gettingusersinusergroup;
            _cacheObjects = getCacheObjects;

        }
        // GET: api/<ViewsController>
        [HttpGet("GetViews/{VaultGuid}/{UserID}")]
        public IActionResult GetViews(string VaultGuid, int UserID)
        {

            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            // Instantiate an MFilesServerApplication object.
            // https://developer.m-files.com/APIs/COM-API/Reference/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port
                );
            try
            {
                var vault = mfServerApplication.LogInToVault(VaultGuid);
                var views = vault.ViewOperations.GetViewsAdmin(true, UserID);
                List<ParentViews> CommonViews = new List<ParentViews>();
                List<ParentViews> MyViews = new List<ParentViews>();

                List<ParentViews> OtherViews = new List<ParentViews>();
                AllParentsViews allParentsViews = new AllParentsViews();
                foreach (MFilesAPI.View viewp in views)
                {
                    try
                    {
                        UserPermission userPermission = new UserPermission();

                        if (!viewp.HasParent)
                        {

                            if (viewp.ID >= 100)
                            {
                                if (viewp.Common)
                                {
                                    int usergroupsd = 0;

                                    #region setting permission
                                    {
                                        var perm = viewp.AccessControlList;
                                       
                                        {
                                            try
                                            {
                                                AccessControlList acl = perm; // Display the ACL details
                                                foreach (AccessControlEntry accessControlEntry1 in perm)
                                                {
                                                    var found = false;

                                                    if (accessControlEntry1.IsGroup)
                                                    {
                                                        try
                                                        {
                                                          
                                                            var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                            if (itemsks.Any(m => m.Equals(UserID)))
                                                            {
                                                                found = true;
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
                                                            if (userPermission != null)
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
                                                        else
                                                        {

                                                        }
                                                    }
                                                }
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    }
                                    #endregion
                                    if (!userPermission.ReadPermission)
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
                                               
                                                    userPermission.DeletePermission = true;
                                                    userPermission.EditPermission = true;
                                                    userPermission.ReadPermission = true;
                                                    userPermission.AttachObjectsPermission = true;
                                                


                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                        }
                                    }
                                  

                                    if (viewp.SearchConditions.Count > 0)
                                    {
                                        int id = 1;
                                        List<GroupLevel> groupLevels = new List<GroupLevel>();
                                        foreach (ExpressionEx expressionEx in viewp.Levels)
                                        {
                                            if (expressionEx.Expression.Type == MFExpressionType.MFExpressionTypeTypedValue)
                                            {
                                                var valuelistname = vault.ValueListOperations.GetValueList(expressionEx.Expression.DataTypedValueValueList);
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = valuelistname.NameSingular });

                                            }
                                            else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeStatusValue)
                                            {
                                                if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectType"))
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Object Type" });

                                                }
                                                else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectID"))
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID" });

                                                }
                                                else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("MFStatusTypeObjectVersionID"))
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Version" });

                                                }
                                                else
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = expressionEx.Expression.DataStatusValueType.ToString() });

                                                }

                                            }
                                            else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeObjectIDSegment)
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID Segment" });

                                            }
                                            else
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = expressionEx.Expression.DataPropertyValueDataFunction.ToString(), mfilesproperty = expressionEx.Expression.DataPropertyValuePropertyDef });

                                            }
                                            id += 1;
                                        }
                                        if(userPermission.ReadPermission)
                                        CommonViews.Add(new ParentViews { ID = viewp.ID, ViewName = viewp.Name, groupLevels = groupLevels, userPermission = new UserPermission { EditPermission = true, ReadPermission = true, AttachObjectsPermission = true } });

                                    }
                                }
                                else
                                {
                                    #region setting permission
                                    {
                                        var perm = viewp.AccessControlList;

                                        {
                                            try
                                            {
                                                AccessControlList acl = perm; // Display the ACL details
                                                foreach (AccessControlEntry accessControlEntry1 in perm)
                                                {
                                                    var found = false;

                                                    if (accessControlEntry1.IsGroup)
                                                    {
                                                        try
                                                        {
                                                           
                                                            var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                            if (itemsks.Any(m => m.Equals(UserID)))
                                                            {
                                                                found = true;
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
                                                            if (userPermission != null)
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
                                                        else
                                                        {

                                                        }
                                                    }
                                                }
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    }
                                    #endregion
                                    if (!userPermission.ReadPermission)
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

                                                userPermission.DeletePermission = true;
                                                userPermission.EditPermission = true;
                                                userPermission.ReadPermission = true;
                                                userPermission.AttachObjectsPermission = true;



                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                        }
                                    }
                                    if (viewp.SearchConditions.Count > 0)
                                    {
                                        int id = 1;
                                        List<GroupLevel> groupLevels = new List<GroupLevel>();
                                        foreach (ExpressionEx expressionEx in viewp.Levels)
                                        {
                                            if (expressionEx.Expression.Type == MFExpressionType.MFExpressionTypeTypedValue)
                                            {
                                                var valuelistname = vault.ValueListOperations.GetValueList(expressionEx.Expression.DataTypedValueValueList);
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = valuelistname.NameSingular });

                                            }
                                            else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeStatusValue)
                                            {
                                                if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectType"))
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Object Type" });

                                                }
                                                else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectID"))
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID" });

                                                }
                                                else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("MFStatusTypeObjectVersionID"))
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Version" });

                                                }
                                                else
                                                {
                                                    groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = expressionEx.Expression.DataStatusValueType.ToString() });

                                                }
                                            }
                                            else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeObjectIDSegment)
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID Segment" });

                                            }
                                            else
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = expressionEx.Expression.DataPropertyValueDataFunction.ToString(), mfilesproperty = expressionEx.Expression.DataPropertyValuePropertyDef });

                                            }
                                            id += 1;
                                        }
                                        if (userPermission.ReadPermission)
                                        MyViews.Add(new ParentViews { ID = viewp.ID, ViewName = viewp.Name, groupLevels = groupLevels });

                                    }
                                }


                            }
                            else
                            {
                                if (viewp.SearchConditions.Count > 0)
                                {
                                    int id = 1;
                                    List<GroupLevel> groupLevels = new List<GroupLevel>();
                                    foreach (ExpressionEx expressionEx in viewp.Levels)
                                    {

                                        if (expressionEx.Expression.Type == MFExpressionType.MFExpressionTypeTypedValue)
                                        {
                                            var valuelistname = vault.ValueListOperations.GetValueList(expressionEx.Expression.DataTypedValueValueList);
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = valuelistname.NameSingular });

                                        }
                                        else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeStatusValue)
                                        {
                                            if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectType"))
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Object Type" });

                                            }
                                            else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectID"))
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID" });

                                            }
                                            else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("MFStatusTypeObjectVersionID"))
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Version" });

                                            }
                                            else
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = expressionEx.Expression.DataStatusValueType.ToString() });

                                            }
                                        }
                                        else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeObjectIDSegment)
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID Segment" });

                                        }
                                        else
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = expressionEx.Expression.DataPropertyValueDataFunction.ToString(), mfilesproperty = expressionEx.Expression.DataPropertyValuePropertyDef });
                                        }
                                        id += 1;
                                    }
                                    OtherViews.Add(new ParentViews { ID = viewp.ID, ViewName = viewp.Name, groupLevels = groupLevels });

                                }

                            }

                        }
                    }
                    catch
                    {

                    }
                  

                }
                allParentsViews.CommonViews = CommonViews;
                allParentsViews.OtherViews = OtherViews;
                allParentsViews.MyViews = MyViews;
                if(allParentsViews.CommonViews.Count==0&& allParentsViews.OtherViews.Count == 0&& allParentsViews.MyViews.Count==0)
                {
                    return NotFound("No vies in the vault");
                }
                else
                {
                    return Ok(allParentsViews);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // GET api/<ViewsController>/5
        [HttpGet("GetViewObjects/{VaultGuid}/{viewid}/{ObjectIds}/{UserID}")]
        public IActionResult Get(string VaultGuid,int viewid,string ObjectIds, int UserID)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            // Instantiate an MFilesServerApplication object.
            // https://developer.m-files.com/APIs/COM-API/Reference/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port
                );
            try
            {
                var vault = mfServerApplication.LogInToVault(VaultGuid);
              
                List<int> ints1 = new List<int>();
                    var totalobjects = ObjectIds.Split(',');
                    foreach (var i in totalobjects)
                    {
                        int numberp;
                        bool isParsablep = int.TryParse(i, out numberp);


                        if (isParsablep)
                        {
                            ints1.Add(numberp);
                        }
                    }

                // Create our search conditions.
                try
                {
                    var viewp = vault.ViewOperations.GetView(viewid);
                    List<Objectsearchresponse> Response = new List<Objectsearchresponse>();

                    if (viewp.SearchConditions.Count > 0)
                    {
                        var searchConditions = new SearchConditions();
                        foreach (SearchCondition searchCondition in viewp.SearchConditions)
                        {
                            searchConditions.Add(-1, searchCondition);
                        }
                        var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                                             viewp.SearchFlags, SortResults: false);

                        if (searchResults.Count > 0)
                        {
                            try
                            {
                                foreach (ObjectVersion objectVersion in searchResults)
                                {
                                    var classname = "";

                                    var propfordisplay = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);
                                    foreach (PropertyValueForDisplay propertyValueForDisplay in propfordisplay)
                                    {
                                        if (propertyValueForDisplay.PropertyDef == 100)
                                        {
                                            classname = propertyValueForDisplay.PropertyValue.Value.DisplayValue;
                                        }
                                    }
                                    var objecttypeid = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);

                                    if (ints1.Any(m => m.Equals(objectVersion.ObjVer.Type)))
                                    {

                                        UserPermission userPermission = new UserPermission();

                                        #region setting permission
                                        {
                                            var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);

                                            if (perm.CustomACL)
                                            {
                                                userPermission = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                                                if ((!userPermission.ReadPermission && (userPermission.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (userPermission.ReadPermission.ToString() == "")))
                                                {
                                                    userPermission.ReadPermission = true;
                                                }
                                                if ((!userPermission.EditPermission && (userPermission.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (userPermission.EditPermission.ToString() == "")))
                                                {
                                                    userPermission.EditPermission = true;
                                                }
                                                if ((!userPermission.AttachObjectsPermission && (userPermission.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (userPermission.AttachObjectsPermission.ToString() == "")))
                                                {
                                                    userPermission.AttachObjectsPermission = true;
                                                }
                                                if ((!userPermission.DeletePermission && (userPermission.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (userPermission.DeletePermission.ToString() == "")))
                                                {
                                                    userPermission.DeletePermission = true;
                                                }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                                    foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                                    {
                                                        var found = false;

                                                        if (accessControlEntry1.IsGroup)
                                                        {
                                                            try
                                                            {
                                                               
                                                                var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                                if (itemsks.Any(m => m.Equals(UserID)))
                                                                {
                                                                    found = true;
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
                                                                if (UserID == accessControlEntry1.UserOrGroupID)
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
                                                                if (userPermission != null)
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
                                                            else
                                                            {

                                                            }
                                                        }
                                                    }
                                                }
                                                catch
                                                {

                                                }
                                            }
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
                                                    userPermission.DeletePermission = true;
                                                    userPermission.EditPermission = true;
                                                    userPermission.ReadPermission = true;
                                                    userPermission.AttachObjectsPermission = true;



                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.Message);
                                            }
                                        }
                                        #endregion



                                        if (userPermission.EditPermission)
                                        {
                                            var objID = new MFilesAPI.ObjID();
                                            objID.SetIDs(
                                                ObjType: objectVersion.ObjVer.Type,
                                                ID: objectVersion.ObjVer.ID);
                                            var checkoutid = -1;

                                            var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                                            var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                                            if (!Directory.Exists(path))
                                                Directory.CreateDirectory(path);
                                            string[] files = Directory.GetFiles(path, VaultGuid + "_" + objectVersion.ObjVer.Type.ToString() + "_" + objectVersion.ObjVer.ID.ToString() + "*");

                                            foreach (string file in files)
                                            {
                                                string fileName = Path.GetFileName(file);

                                                checkoutid = Convert.ToInt16(file.Split(".")[0].Trim().Substring(file.Split(".")[0].Trim().LastIndexOf("_") + 1));
                                            }
                                            if (checkout && checkoutid < 0)
                                            {
                                                checkoutid = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 23).TypedValue.GetLookupID();
                                            }
                                            string fileextension = "";
                                            int fileid = 0;
                                            if (objectVersion.SingleFile)
                                            {
                                                fileextension = objectVersion.Files[1].Extension;
                                                fileid = objectVersion.Files[1].ID;
                                            }
                                            var st = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                                            Response.Add(new Objectsearchresponse { VersionId = objectVersion.ObjVer.Version, ClassTypeName = classname, ObjectTypeName = objecttypeid.NameSingular, userPermission = userPermission, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, CreatedUtc = objectVersion.CreatedUtc, LastModifiedUtc = objectVersion.LastModifiedUtc, DisplayID = objectVersion.DisplayID, IsSingleFile = objectVersion.SingleFile, IsCheckedOut = checkout, checkoutuserid = checkoutid, FileExtension=fileextension, HasRelationship= st.Count>0, FileId=fileid, checkoutusername = objectVersion.CheckedOutToUserName });

                                        }

                                    }

                                }

                                return Ok(Response);
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                    }
                    if (Response.Count == 0)
                    {
                        return NotFound("No objects in that view");
                    }
                    else
                    {
                        return Ok(Response);
                    }
                }
                catch
                {
                    Views views = vault.ViewOperations.GetViewsAdmin(false, UserID);
                    foreach (MFilesAPI.View v in views)
                    {
                        if (v.ID == viewid) 
                        {
                            var viewp = v;
                            List<Objectsearchresponse> Response = new List<Objectsearchresponse>();

                            if (viewp.SearchConditions.Count > 0)
                            {
                                var searchConditions = new SearchConditions();
                                foreach (SearchCondition searchCondition in viewp.SearchConditions)
                                {
                                    searchConditions.Add(-1, searchCondition);
                                }
                                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                                                     viewp.SearchFlags, SortResults: false);

                                if (searchResults.Count > 0)
                                {
                                    try
                                    {
                                        foreach (ObjectVersion objectVersion in searchResults)
                                        {
                                            var classname = "";

                                            var propfordisplay = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);
                                            foreach (PropertyValueForDisplay propertyValueForDisplay in propfordisplay)
                                            {
                                                if (propertyValueForDisplay.PropertyDef == 100)
                                                {
                                                    classname = propertyValueForDisplay.PropertyValue.Value.DisplayValue;
                                                }
                                            }
                                            var objecttypeid = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);

                                            if (ints1.Any(m => m.Equals(objectVersion.ObjVer.Type)))
                                            {

                                                UserPermission userPermission = new UserPermission();

                                                #region setting permission
                                                {
                                                    var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);

                                                    if (perm.CustomACL)
                                                    {
                                                        userPermission = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                                                        if (userPermission.ReadPermission)
                                                        {
                                                            userPermission = _permission.ClassPermission(vault, UserID, objectVersion.Class);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                                            foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                                            {
                                                                var found = false;

                                                                if (accessControlEntry1.IsGroup)
                                                                {
                                                                    try
                                                                    {
                                                                        var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                                        if (itemsks.Any(m => m.Equals(UserID)))
                                                                        {
                                                                            found = true;
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
                                                                        if (UserID == accessControlEntry1.UserOrGroupID)
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
                                                                        if (userPermission != null)
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
                                                                    else
                                                                    {

                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {

                                                        }
                                                    }
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
                                                            userPermission.DeletePermission = true;
                                                            userPermission.EditPermission = true;
                                                            userPermission.ReadPermission = true;
                                                            userPermission.AttachObjectsPermission = true;



                                                        }

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex.Message);
                                                    }
                                                }
                                                #endregion



                                                if (userPermission.EditPermission)
                                                {
                                                    var objID = new MFilesAPI.ObjID();
                                                    objID.SetIDs(
                                                        ObjType: objectVersion.ObjVer.Type,
                                                        ID: objectVersion.ObjVer.ID);
                                                    var checkoutid = -1;

                                                    var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                                                    var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                                                    if (!Directory.Exists(path))
                                                        Directory.CreateDirectory(path);
                                                    string[] files = Directory.GetFiles(path, VaultGuid + "_" + objectVersion.ObjVer.Type.ToString() + "_" + objectVersion.ObjVer.ID.ToString() + "*");

                                                    foreach (string file in files)
                                                    {
                                                        string fileName = Path.GetFileName(file);

                                                        checkoutid = Convert.ToInt16(file.Split(".")[0].Trim().Substring(file.Split(".")[0].Trim().LastIndexOf("_") + 1));
                                                    }
                                                    if (checkout && checkoutid < 0)
                                                    {
                                                        checkoutid = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 23).TypedValue.GetLookupID();
                                                    }
                                                    string fileextension = "";
                                                    int fileid = 0;
                                                    if (objectVersion.SingleFile)
                                                    {
                                                        fileextension = objectVersion.Files[1].Extension;
                                                        fileid = objectVersion.Files[1].ID;
                                                    }
                                                    var st = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                                                    Response.Add(new Objectsearchresponse { VersionId = objectVersion.ObjVer.Version, ClassTypeName = classname, ObjectTypeName = objecttypeid.NameSingular, userPermission = userPermission, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, CreatedUtc = objectVersion.CreatedUtc, LastModifiedUtc = objectVersion.LastModifiedUtc, DisplayID = objectVersion.DisplayID, IsSingleFile = objectVersion.SingleFile, IsCheckedOut = checkout, checkoutuserid = checkoutid, FileExtension=fileextension, HasRelationship=st.Count>0, FileId=fileid, checkoutusername = objectVersion.CheckedOutToUserName });

                                                }

                                            }

                                        }

                                        return Ok(Response);
                                    }
                                    catch (Exception ex)
                                    {

                                    }

                                }
                            }
                            if (Response.Count == 0)
                            {
                                return NotFound("No objects in that view");
                            }
                            else
                            {
                                return Ok(Response);
                            }
                            
                        }
                        
                    }
                    return NotFound();
                }

                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetObjectsInView")]
        public IActionResult GetObjectsInView([FromQuery] GetviewContents getviewContents)
        {
            if (getviewContents.ViewId == 0)
            {
                return NotFound("Could not find a view with that ID");
            }

            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            // Instantiate an MFilesServerApplication object.
            // https://developer.m-files.com/APIs/COM-API/Reference/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port
                );
            try
            {

                    var vault = mfServerApplication.LogInToVault(getviewContents.VaultGuid);
                    List<Getview> getviews = new List<Getview>();
                    #region searching for a view content
                    {
                        var viewp = vault.ViewOperations.GetView(getviewContents.ViewId);

                        FolderDefs folderDefs = new FolderDefs();
                        var folderDef = new FolderDef();
                        folderDef.SetView(getviewContents.ViewId);
                        folderDefs.Add(-1, folderDef);
                        var mfilesSearchResult = vault.ViewOperations.GetFolderContents(folderDefs);
                        int ids = folderDefs.Count;
                        int spp = 1;
                        bool valuepermcheck = false;
                        List<int> ints = new List<int>();
                        List<string> strings = new List<string>();
                        bool showprop = false;
                        bool isclassprop = false;
                        foreach (ExpressionEx expression in viewp.Levels)
                        {
                            if (spp == ids)
                            {
                                var s = 0;
                                try
                                {
                                    s = expression.Expression.DataPropertyValuePropertyDef;
                                    showprop = _permission.PropPermission(vault, getviewContents.UserID, s).ReadPermission;

                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        s = expression.Expression.DataTypedValueValueList;
                                        if (s == 1)
                                        {
                                            isclassprop = true;
                                            var classes = vault.ClassOperations.GetAllObjectClasses();
                                            foreach (ObjectClass objectClass in classes)
                                            {
                                                //  UserPermission userPermission = new UserPermission();
                                                var found = false;
                                                if (_permission.ClassPermission(vault, getviewContents.UserID, objectClass.ID).ReadPermission)
                                                {
                                                    ints.Add(objectClass.ID);
                                                }

                                                try
                                                {
                                                    UserPermission userPermission = new UserPermission();
                                                    var username = vault.UserOperations.GetUserAccount(getviewContents.UserID);

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
                                                catch (Exception exet)
                                                {

                                                }

                                            }
                                        }
                                        else if (s == 7)
                                        {
                                            var workflows = vault.WorkflowOperations.GetWorkflowsAdmin();
                                            foreach (WorkflowAdmin workflow in workflows)
                                            {
                                                if (_permission.WorkflowPermission(vault, getviewContents.UserID, workflow.Workflow.ID).ReadPermission)
                                                {
                                                    ints.Add(workflow.Workflow.ID);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            valuepermcheck = true;
                                            if (_permission.Valuelist(vault, getviewContents.UserID, s).ReadPermission)
                                            {
                                                ints.Add(s);
                                            }
                                        }
                                    }
                                    catch (Exception x)
                                    {

                                    }

                                }
                            }
                            strings.Add(expression.NULLFolderName ?? "");
                            spp += 1;
                        }
                        var items = strings.Count();
                        foreach (FolderContentItem item in mfilesSearchResult)
                        {
                            if (item.FolderContentItemType.ToString().Trim() == "MFFolderContentItemTypeViewFolder")
                            {
                                int id = 1;
                                List<GroupLevel> groupLevels = new List<GroupLevel>();
                                foreach (ExpressionEx expressionEx in item.View.Levels)
                                {
                                    if (expressionEx.Expression.Type == MFExpressionType.MFExpressionTypeTypedValue)
                                    {
                                        var valuelistname = vault.ValueListOperations.GetValueList(expressionEx.Expression.DataTypedValueValueList);
                                        groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = valuelistname.NameSingular });

                                    }
                                    else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeStatusValue)
                                    {
                                        if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectType"))
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Object Type" });

                                        }
                                        else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectID"))
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID" });

                                        }
                                        else if (expressionEx.Expression.DataStatusValueType.ToString().Contains("MFStatusTypeObjectVersionID"))
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Version" });

                                        }
                                        else
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = expressionEx.Expression.DataStatusValueType.ToString() });

                                        }

                                    }
                                    else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeObjectIDSegment)
                                    {
                                        groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID Segment" });

                                    }
                                    else
                                    {
                                        groupLevels.Add(new GroupLevel { id = id, mfilesfunction = expressionEx.Expression.DataPropertyValueDataFunction.ToString(), mfilesproperty = expressionEx.Expression.DataPropertyValuePropertyDef });

                                    }
                                    id += 1;
                                }


                                string title = item.View.Name;
                                if (string.IsNullOrEmpty(item.View.Name))
                                {
                                    if (item.View.ViewLocation.Overlapping)
                                    {
                                        title = item.View.ViewLocation.OverlappedFolder.DisplayValue;

                                    }

                                }
                                UserPermission userPermission = new UserPermission();
                                var found1 = false;
                                #region setting permission
                                {
                                    try
                                    {
                                        AccessControlList acl = item.View.AccessControlList; // Display the ACL details
                                        foreach (AccessControlEntry accessControlEntry1 in item.View.AccessControlList)
                                        {
                                            var found = false;

                                            if (accessControlEntry1.IsGroup)
                                            {
                                                try
                                                {
                                                   
                                                    var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                    if (itemsks.Any(m => m.Equals(getviewContents.UserID)))
                                                    {
                                                        found = true;
                                                        found1 = true;
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
                                                    if (getviewContents.UserID == username.ID)
                                                    {
                                                        found = true;
                                                        found1 = true;
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
                                                    if (userPermission != null)
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
                                                else
                                                {

                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                                #endregion
                                try
                                {
                                    var username = vault.UserOperations.GetUserAccount(getviewContents.UserID);

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
                                        if (!found1)
                                        {
                                            userPermission.DeletePermission = true;
                                            userPermission.EditPermission = true;
                                            userPermission.ReadPermission = true;
                                            userPermission.AttachObjectsPermission = true;
                                        }


                                    }

                                }
                                catch (Exception exet)
                                {

                                }
                                if (userPermission.ReadPermission)
                                {
                                    getviews.Add(new Getview { id = item.View.ID, ObjectTypeId = -1, Title = title, Type = "MFFolderContentItemTypeViewFolder", ClassId = -1, PropDatatype = "", propId = "", ViewId = -1, groupLevels = groupLevels });

                                }
                            }
                            else if (item.FolderContentItemType.ToString().Trim() == "MFFolderContentItemTypeObjectVersion")
                            {
                                var objectid = vault.ClassOperations.GetObjectClass(item.ObjectVersion.Class);
                                var objecttypeid = vault.ObjectTypeOperations.GetObjectType(item.ObjectVersion.ObjVer.Type);
                                var objID = new MFilesAPI.ObjID();
                                UserPermission userPermission = new UserPermission();

                                #region setting permission
                                {
                                    var perm = vault.ObjectOperations.GetObjectPermissions(item.ObjectVersion.ObjVer);
                                    if (perm.CustomACL)
                                    {
                                        userPermission = _permission.ObjectPermission(vault, getviewContents.UserID, item.ObjectVersion.ObjVer.Type);
                                        if (userPermission.ReadPermission)
                                        {
                                            userPermission = _permission.ClassPermission(vault, getviewContents.UserID, item.ObjectVersion.Class);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                            foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                            {
                                                var found = false;

                                                if (accessControlEntry1.IsGroup)
                                                {
                                                    try
                                                    {
                                                      
                                                        var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                        if (itemsks.Any(m => m.Equals(getviewContents.UserID)))
                                                        {
                                                            found = true;
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
                                                        if (getviewContents.UserID == username.ID)
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
                                                        if (userPermission != null)
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
                                                    else
                                                    {

                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {

                                        }
                                    }
                                    try
                                    {
                                        AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                        foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                        {
                                            var found = false;

                                            if (accessControlEntry1.IsGroup)
                                            {
                                                try
                                                {
                                                  
                                                    var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                    if (itemsks.Any(m => m.Equals(getviewContents.UserID)))
                                                    {
                                                        found = true;
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
                                                    if (getviewContents.UserID == username.ID)
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
                                                    if (userPermission != null)
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
                                                else
                                                {

                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                                #endregion

                                objID.SetIDs(
                                    ObjType: item.ObjectVersion.ObjVer.Type,
                                    ID: item.ObjectVersion.ObjVer.ID);
                                var checkoutid = -1;

                                var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                                var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                                if (!Directory.Exists(path))
                                    Directory.CreateDirectory(path);
                                string[] files = Directory.GetFiles(path, getviewContents.VaultGuid + "_" + item.ObjectVersion.ObjVer.Type.ToString() + "_" + item.ObjectVersion.ObjVer.ID.ToString() + "*");

                                foreach (string file in files)
                                {
                                    string fileName = Path.GetFileName(file);

                                    checkoutid = Convert.ToInt16(file.Split(".")[0].Trim().Substring(file.Split(".")[0].Trim().LastIndexOf("_") + 1));
                                }
                                if (checkout && checkoutid < 0)
                                {
                                    checkoutid = vault.ObjectPropertyOperations.GetProperty(item.ObjectVersion.ObjVer, 23).TypedValue.GetLookupID();
                                }
                                string fileextension = "";
                                int fileid = 0;
                                if (item.ObjectVersion.SingleFile)
                                {
                                    fileextension = item.ObjectVersion.Files[1].Extension;
                                    fileid = item.ObjectVersion.Files[1].ID;
                                }
                            var st = vault.ObjectOperations.GetRelationshipsEx(item.ObjectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                            getviews.Add(new Getview { id = item.ObjectVersion.ObjVer.ID, ObjectTypeId = item.ObjectVersion.OriginalObjID.Type, Title = item.ObjectVersion.Title, Type = "MFFolderContentItemTypeObjectVersion", ClassId = item.ObjectVersion.Class, propId = "", PropDatatype = "", ViewId = -1, userPermission = userPermission, ClassTypeName = objectid.Name, ObjectTypeName = objecttypeid.NameSingular, VersionId = item.ObjectVersion.ObjVer.Version, DisplayID = item.ObjectVersion.DisplayID, CreatedUtc = item.ObjectVersion.CreatedUtc, LastModifiedUtc = item.ObjectVersion.LastModifiedUtc, IsSingleFile = item.ObjectVersion.SingleFile, IsCheckedOut = checkout, checkoutuserid = checkoutid, FileExtension=fileextension, HasRelationship=st.Count>0, FileId=fileid, checkoutusername =item.ObjectVersion.CheckedOutToUserName });

                            }
                            else if (item.FolderContentItemType.ToString().Trim() == "MFFolderContentItemTypePropertyFolder")
                            {

                                string id = "";
                                if (item.PropertyFolder.DataType == MFDataType.MFDatatypeInteger)
                                {
                                    if (showprop)
                                    {
                                        int idd = item.PropertyFolder.Value;
                                        id = idd.ToString();
                                    }
                                }
                                else if (item.PropertyFolder.DataType == MFDataType.MFDatatypeText)
                                {
                                    if (showprop)
                                    {
                                        id = (item.PropertyFolder.Value) ?? strings[spp - 1];
                                    }
                                    else if (strings.Count() > 0)
                                    {
                                        id = (item.PropertyFolder.Value) ?? strings[spp - 1];
                                    }
                                }
                                else if (item.PropertyFolder.DataType == MFDataType.MFDatatypeLookup)
                                {
                                    int idd = (item.PropertyFolder.GetLookupID());
                                    var t = ints;
                                    if (ints.Any(m => m.Equals(idd)) | valuepermcheck)
                                    {
                                        id = idd.ToString();
                                    }
                                    else if (showprop && !ints.Any(m => m.Equals(idd)))
                                    {
                                        id = idd.ToString();
                                    }
                                    else if (isclassprop)
                                    {
                                        try
                                        {
                                            var classp = vault.ClassOperations.GetObjectClass(idd);
                                        }
                                        catch
                                        {
                                            id = idd.ToString();
                                        }

                                    }
                                }
                                else if (item.PropertyFolder.DataType == MFDataType.MFDatatypeDate)
                                {
                                    if (showprop)
                                    {
                                        id = Convert.ToString(item.PropertyFolder.Value) ?? strings[spp - 1];
                                    }
                                }
                                if (!string.IsNullOrEmpty(id))
                                {
                                    if (string.IsNullOrEmpty(item.PropertyFolder.DisplayValue))
                                    {
                                        getviews.Add(new Getview { id = -1, ObjectTypeId = -1, ClassId = -1, Title = strings[ids - 1], Type = "MFFolderContentItemTypePropertyFolder", propId = id, PropDatatype = item.PropertyFolder.DataType.ToString(), ViewId = getviewContents.ViewId });

                                    }
                                    else
                                    {
                                        getviews.Add(new Getview { id = -1, ObjectTypeId = -1, ClassId = -1, Title = item.PropertyFolder.DisplayValue ?? strings[spp - 1], Type = "MFFolderContentItemTypePropertyFolder", propId = id, PropDatatype = item.PropertyFolder.DataType.ToString(), ViewId = getviewContents.ViewId });

                                    }
                                }

                            }
                        }
                    }
                    #endregion

                    if (getviews.Count > 0)
                    {
                        return Ok(getviews);
                    }
                    else
                    {
                        return NotFound("Could not find any items in that view");
                    }
                
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
           
        }
        [HttpPost("GetViewPropObjects")]
        public IActionResult GetViewPropObjects([FromBody] GetpropertyContent getviewContents)
        {
            if (getviewContents.ViewId <= 0)
            {
                return NotFound("Could not find a view with that ID");
            }
            if(getviewContents.properties.Count<=0)
            {
                return NotFound("Could not find a view with that ID");
            }
           

            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            // Instantiate an MFilesServerApplication object.
            // https://developer.m-files.com/APIs/COM-API/Reference/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port
                );
            try
            {
                    var vault = mfServerApplication.LogInToVault(getviewContents.VaultGuid);
                    List<Getview> getviews = new List<Getview>();
                    #region searching for a view content
                    {
                        TypedValue typedValue = new TypedValue();

                        FolderDefs folderDefspd = new FolderDefs();
                        {
                            var folderDef = new FolderDef();
                            folderDef.SetView(getviewContents.ViewId);

                            folderDefspd.Add(-1, folderDef);
                        }
                        foreach (var t in getviewContents.properties)
                        {

                            if (t.PropDatatype == MFDataType.MFDatatypeText.ToString())
                            {
                                if (string.IsNullOrEmpty(t.propId))
                                {
                                    typedValue.SetValueToNULL(MFDataType.MFDatatypeText);
                                }
                                else
                                {
                                    typedValue.SetValue(MFDataType.MFDatatypeText, t.propId);
                                }
                                var folderDef = new FolderDef();
                                folderDef.SetPropertyFolder(typedValue);

                                folderDefspd.Add(-1, folderDef);
                            }
                            else if (t.PropDatatype == MFDataType.MFDatatypeInteger.ToString())
                            {
                                if (int.Parse(t.propId) == 0)
                                {
                                    typedValue.SetValueToNULL(MFDataType.MFDatatypeInteger);
                                }
                                else
                                {
                                    typedValue.SetValue(MFDataType.MFDatatypeInteger, int.Parse(t.propId));

                                }
                                var folderDef = new FolderDef();
                                folderDef.SetPropertyFolder(typedValue);

                                folderDefspd.Add(-1, folderDef);
                            }
                            else if (t.PropDatatype == MFDataType.MFDatatypeLookup.ToString())
                            {
                                var id = int.Parse(t.propId);
                                if (id == 0)
                                {
                                    typedValue.SetValueToNULL(MFDataType.MFDatatypeLookup);
                                }
                                else
                                {
                                    typedValue.SetValue(MFDataType.MFDatatypeLookup, id);
                                }

                                var folderDef = new FolderDef();
                                folderDef.SetPropertyFolder(typedValue);

                                folderDefspd.Add(-1, folderDef);
                            }
                            else if (t.PropDatatype == MFDataType.MFDatatypeFloating.ToString())
                            {
                                if (int.Parse(t.propId) == 0)
                                {
                                    typedValue.SetValueToNULL(MFDataType.MFDatatypeFloating);

                                }
                                else
                                {
                                    typedValue.SetValue(MFDataType.MFDatatypeFloating, int.Parse(t.propId));

                                }
                                var folderDef = new FolderDef();
                                folderDef.SetPropertyFolder(typedValue);

                                folderDefspd.Add(-1, folderDef);
                            }
                            else if (t.PropDatatype == MFDataType.MFDatatypeDate.ToString())
                            {
                                if (string.IsNullOrEmpty(t.propId))
                                {
                                    typedValue.SetValueToNULL(MFDataType.MFDatatypeDate);
                                }
                                else
                                {
                                    typedValue.SetValue(MFDataType.MFDatatypeDate, DateTime.Parse(t.propId));
                                }
                                var folderDef = new FolderDef();
                                folderDef.SetPropertyFolder(typedValue);

                                folderDefspd.Add(-1, folderDef);
                            }
                        }

                        #region searching for a view content
                        {
                            var viewp = vault.ViewOperations.GetView(getviewContents.ViewId);

                            var mfilesSearchResult = vault.ViewOperations.GetFolderContents(folderDefspd);
                            int ids = folderDefspd.Count;
                            int spp = 1;
                            bool check = false;
                            List<int> ints = new List<int>();
                            bool showprop = false;
                            foreach (ExpressionEx expression in viewp.Levels)
                            {
                                if (spp == ids)
                                {
                                    var s = 0;
                                    try
                                    {
                                        s = expression.Expression.DataPropertyValuePropertyDef;
                                        showprop = _permission.PropPermission(vault, getviewContents.UserID, s).ReadPermission;

                                    }
                                    catch (Exception ex)
                                    {
                                        s = expression.Expression.DataTypedValueValueList;
                                        if (s == 1)
                                        {
                                            check = true;
                                            var classes = vault.ClassOperations.GetAllObjectClasses();
                                            foreach (ObjectClass objectClass in classes)
                                            {
                                                UserPermission userPermission = new UserPermission();
                                                try
                                                {
                                                    AccessControlList acl = objectClass.AccessControlList; // Display the ACL details
                                                    foreach (AccessControlEntry accessControlEntry1 in objectClass.AccessControlList)
                                                    {
                                                        var found = false;

                                                        if (accessControlEntry1.IsGroup)
                                                        {
                                                            try
                                                            {
                                                              
                                                                var itemsks = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                                if (itemsks.Any(m => m.Equals(getviewContents.UserID)))
                                                                {
                                                                    found = true;
                                                                }
                                                            }
                                                            catch (Exception)
                                                            {

                                                            }


                                                        }
                                                        else
                                                        {
                                                            try
                                                            {
                                                                var username = vault.UserOperations.GetUserAccount(accessControlEntry1.UserOrGroupID);
                                                                if (getviewContents.UserID == username.ID)
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
                                                                if (userPermission != null)
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
                                                            else
                                                            {

                                                            }
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    userPermission.DeletePermission = false;
                                                    userPermission.EditPermission = false;
                                                    userPermission.AttachObjectsPermission = false;
                                                    userPermission.ReadPermission = true;
                                                    userPermission.IsClassDeleted = true;
                                                }

                                                try
                                                {
                                                    var username = vault.UserOperations.GetUserAccount(getviewContents.UserID);

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

                                                        userPermission.DeletePermission = true;
                                                        userPermission.EditPermission = true;
                                                        userPermission.ReadPermission = true;
                                                        userPermission.AttachObjectsPermission = true;


                                                    }

                                                }
                                                catch (Exception)
                                                {

                                                }



                                                if (userPermission.ReadPermission)
                                                {
                                                    ints.Add(objectClass.ID);
                                                }
                                            }
                                        }
                                        else if (s == 7)
                                        {
                                            check = true;
                                            var workflows = vault.WorkflowOperations.GetWorkflowsAdmin();
                                            foreach (WorkflowAdmin workflow in workflows)
                                            {
                                                if (_permission.WorkflowPermission(vault, getviewContents.UserID, workflow.Workflow.ID).ReadPermission)
                                                {
                                                    ints.Add(workflow.Workflow.ID);
                                                }
                                            }
                                        }

                                    }
                                }
                                spp += 1;
                            }

                            foreach (FolderContentItem item in mfilesSearchResult)
                            {
                                if (item.FolderContentItemType.ToString().Trim() == "MFFolderContentItemTypeViewFolder")
                                {
                                    int id = 1;
                                    List<GroupLevel> groupLevels = new List<GroupLevel>();
                                    foreach (ExpressionEx expressionEx in item.View.Levels)
                                    {
                                        if (expressionEx.Expression.Type == MFExpressionType.MFExpressionTypeTypedValue)
                                        {
                                            var valuelistname = vault.ValueListOperations.GetValueList(expressionEx.Expression.DataTypedValueValueList);
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = valuelistname.NameSingular });

                                        }
                                        if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeStatusValue)
                                        {
                                            if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectType"))
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Object Type" });

                                            }
                                            if (expressionEx.Expression.DataStatusValueType.ToString().Contains("ObjectID"))
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID" });

                                            }
                                            if (expressionEx.Expression.DataStatusValueType.ToString().Contains("MFStatusTypeObjectVersionID"))
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "Version" });

                                            }
                                            else
                                            {
                                                groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = expressionEx.Expression.DataStatusValueType.ToString() });

                                            }

                                        }
                                        else if (expressionEx.Expression.Type == MFilesAPI.MFExpressionType.MFExpressionTypeObjectIDSegment)
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = "", mfilesproperty = -1, PropertyName = "ID Segment" });

                                        }
                                        else
                                        {
                                            groupLevels.Add(new GroupLevel { id = id, mfilesfunction = expressionEx.Expression.DataPropertyValueDataFunction.ToString(), mfilesproperty = expressionEx.Expression.DataPropertyValuePropertyDef });

                                        }
                                        id += 1;
                                    }

                                    string title = item.View.Name;
                                    if (string.IsNullOrEmpty(item.View.Name))
                                    {
                                        if (item.View.ViewLocation.Overlapping)
                                        {
                                            title = item.View.ViewLocation.OverlappedFolder.DisplayValue;

                                        }

                                    }
                                    UserPermission userPermission = new UserPermission();
                                    var found = false;


                                    try
                                    {
                                        var username = vault.UserOperations.GetUserAccount(getviewContents.UserID);

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
                                    catch (Exception exet)
                                    {

                                    }
                                    if (userPermission.ReadPermission)
                                    {
                                        getviews.Add(new Getview { id = item.View.ID, ObjectTypeId = -1, Title = title, Type = "MFFolderContentItemTypeViewFolder", ClassId = -1, PropDatatype = "", propId = "", ViewId = -1, groupLevels = groupLevels });

                                    }
                                }
                                else if (item.FolderContentItemType.ToString().Trim() == "MFFolderContentItemTypeObjectVersion")
                                {
                                    var perm = _permission.ObjectPermission(vault, getviewContents.UserID, item.ObjectVersion.ObjVer.Type);
                                    var objectid = vault.ClassOperations.GetObjectClass(item.ObjectVersion.Class);
                                    var objecttypeid = vault.ObjectTypeOperations.GetObjectType(item.ObjectVersion.ObjVer.Type);
                                    if (perm.EditPermission)
                                    {
                                        perm = _permission.ClassPermission(vault, getviewContents.UserID, item.ObjectVersion.Class);

                                    }
                                    var objID = new MFilesAPI.ObjID();
                                    objID.SetIDs(
                                        ObjType: item.ObjectVersion.ObjVer.Type,
                                        ID: item.ObjectVersion.ObjVer.ID);
                                    Int64 checkoutid = -1;

                                    var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                                    var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                                    if (!Directory.Exists(path))
                                        Directory.CreateDirectory(path);
                                    string[] files = Directory.GetFiles(path, getviewContents.VaultGuid + "_" + item.ObjectVersion.ObjVer.Type.ToString() + "_" + item.ObjectVersion.ObjVer.ID.ToString() + "*");

                                    foreach (string file in files)
                                    {
                                        string fileName = Path.GetFileName(file);

                                        checkoutid = Convert.ToInt64(file.Split(".")[0].Trim().Substring(file.Split(".")[0].Trim().LastIndexOf("_") + 1));
                                    }
                                    if (checkout && checkoutid < 0)
                                    {
                                        checkoutid = vault.ObjectPropertyOperations.GetProperty(item.ObjectVersion.ObjVer, 23).TypedValue.GetLookupID();
                                    }
                                    string fileextension = "";
                                    int fileid = 0;
                                    if (item.ObjectVersion.SingleFile)
                                    {
                                        fileextension = item.ObjectVersion.Files[1].Extension;
                                        fileid = item.ObjectVersion.Files[1].ID;
                                    }
                                    var st = vault.ObjectOperations.GetRelationshipsEx(item.ObjectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                                    getviews.Add(new Getview { id = item.ObjectVersion.ObjVer.ID, ObjectTypeId = item.ObjectVersion.ObjVer.Type, Title = item.ObjectVersion.Title, Type = "MFFolderContentItemTypeObjectVersion", ClassId = item.ObjectVersion.Class, propId = "", PropDatatype = "", ViewId = -1, userPermission = perm, ClassTypeName = objectid.Name, ObjectTypeName = objecttypeid.NameSingular, VersionId = item.ObjectVersion.ObjVer.Version, DisplayID = item.ObjectVersion.DisplayID, CreatedUtc = item.ObjectVersion.CreatedUtc, LastModifiedUtc = item.ObjectVersion.LastModifiedUtc, IsSingleFile = item.ObjectVersion.SingleFile, IsCheckedOut = checkout, checkoutuserid = checkoutid, FileExtension = fileextension, HasRelationship=st.Count>0, FileId=fileid, checkoutusername =item.ObjectVersion.CheckedOutToUserName });

                                }
                                else if (item.FolderContentItemType.ToString().Trim() == "MFFolderContentItemTypePropertyFolder")
                                {

                                    string id = "";
                                    if (item.PropertyFolder.DataType == MFDataType.MFDatatypeInteger)
                                    {
                                        if (showprop)
                                        {
                                            int idd = item.PropertyFolder.Value;
                                            id = idd.ToString();
                                        }
                                    }
                                    else if (item.PropertyFolder.DataType == MFDataType.MFDatatypeText)
                                    {
                                        if (showprop)
                                        {
                                            id = (item.PropertyFolder.Value);
                                        }
                                    }
                                    else if (item.PropertyFolder.DataType == MFDataType.MFDatatypeLookup)
                                    {
                                        if (check)
                                        {
                                            int idd = (item.PropertyFolder.GetLookupID());
                                            if (ints.Any(m => m.Equals(idd)))
                                            {
                                                id = idd.ToString();
                                            }
                                        }
                                        else
                                        {
                                            int idd = (item.PropertyFolder.GetLookupID());
                                            id = idd.ToString();
                                        }

                                    }
                                    else if (item.PropertyFolder.DataType == MFDataType.MFDatatypeDate)
                                    {
                                        if (showprop)
                                        {
                                            id = Convert.ToString(item.PropertyFolder.Value);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(id))
                                        getviews.Add(new Getview { id = -1, ObjectTypeId = -1, ClassId = -1, Title = item.PropertyFolder.DisplayValue, Type = "MFFolderContentItemTypePropertyFolder", propId = id, PropDatatype = item.PropertyFolder.DataType.ToString(), ViewId = getviewContents.ViewId });

                                }
                            }
                        }
                        #endregion


                    }
                    #endregion

                    if (getviews.Count > 0)
                    {
                        return Ok(getviews);
                    }
                    else
                    {
                        return NotFound("Could not find any items in that view");
                    }
              
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetRecent/{VaultGuid}/{UserID}")]
        public async Task<IActionResult> GetRecentAsync(string VaultGuid, int UserID)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            // Instantiate an MFilesServerApplication object.
            // https://developer.m-files.com/APIs/COM-API/Reference/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port
                );
            try
            {
                var vault = mfServerApplication.LogInToVault(VaultGuid);
                var t = await _repository.GetValidItemsAsync(VaultGuid, UserID);
                List<Objectsearchresponse> responses = new List<Objectsearchresponse>();
                List<Objectsearchresponse> responses1 = new List<Objectsearchresponse>();
                List<Objectsearchresponse> responses2 = new List<Objectsearchresponse>();

                if (t.Count() > 0)
                {
                    HashSet<string> uniqueNumbers = new HashSet<string>();

                    foreach (var log in t)
                    {
                        if (log.VaultGuid == VaultGuid)
                        {
                            uniqueNumbers.Add(log.ClassID.ToString() + "-" + log.Id + "-" + log.ObjectID);
                        }
                    }
                    foreach (var st in uniqueNumbers)
                    {
                        var stt = st;
                        var classid = "";
                        if (stt.StartsWith("-"))
                        {
                            stt = stt.Substring(1);
                            classid = "-" + stt.Split("-")[0];
                        }
                        else
                        {
                            classid = stt.Split("-")[0];

                        }

                        var objectid = stt.Split("-")[1];
                      
                        foreach (var log in t)
                        {
                            if (log.VaultGuid == VaultGuid)
                            {
                                
                                responses.Add(new Objectsearchresponse
                                {
                                    ClassID = log.ClassID,
                                    ClassTypeName =log.ClassTypeName,
                                     id = log.Id,
                                    ObjectID = log.ObjectID,
                                    ObjectTypeName = log.ObjectTypeName,
                                    Title = log.Title,
                                    DisplayID = log.DisplayID,
                                   
                                });
                            }
                        }
                        if (!string.IsNullOrEmpty(classid) && !string.IsNullOrEmpty(objectid))
                        {
                            var item = responses.FirstOrDefault(m => m.ClassID == Convert.ToInt64(classid) && m.id == Convert.ToInt64(objectid));
                            if (item != null)
                            {
                                responses1.Add(item);
                            }
                        }

                    }
                    var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json");
                    if (System.IO.File.Exists(filepath))
                    {
                        var jsonString = await System.IO.File.ReadAllTextAsync(filepath);
                        var deletedobjectlists = JsonConvert.DeserializeObject<deletedobjectlist>(jsonString);
                        foreach (var item in responses1)
                        {
                            var found = deletedobjectlists.Objectlists.Any(m => m.ObjectID == item.ObjectID && m.ClassID == item.ClassID);
                            if (!found)
                            {
                                #region searching if object exists
                                {
                                    // Create our search conditions.
                                    var searchConditions = new SearchConditions();

                                    // Add an object type filter.
                                    {
                                        // Create the condition.
                                        var condition = new SearchCondition();

                                        // Set the expression.
                                        condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);

                                        // Set the condition.
                                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                        // Set the value.
                                        condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                                            item.ObjectID);

                                        // Add the condition to the collection.
                                        searchConditions.Add(-1, condition);
                                    }

                                    // Add a "not deleted" filter.
                                    {
                                        // Create the condition.
                                        var condition = new SearchCondition();

                                        // Set the expression.
                                        condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);

                                        // Set the condition.
                                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                        // Set the value.
                                        condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, false);

                                        // Add the condition to the collection.
                                        searchConditions.Add(-1, condition);
                                    }
                                    //filter with id
                                    {
                                        // Create the condition.
                                        var condition = new SearchCondition();

                                        // Set the expression.
                                        condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                                        // Set the condition type.
                                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                        // Set the value (this excludes all objects with ID 478 - in all object types!).
                                        condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, item.id);
                                        searchConditions.Add(-1, condition);
                                    }
                                    // Add a class filter.
                                    {
                                        // Create the condition.
                                        var condition = new SearchCondition();

                                        // Set the expression.
                                        condition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass,
                                            MFParentChildBehavior.MFParentChildBehaviorNone);

                                        // Set the condition.
                                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                        // Set the value.
                                        condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                                            item.ClassID);

                                        // Add the condition to the collection.
                                        searchConditions.Add(-1, condition);
                                    }

                                    // Execute the search.
                                    var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                                        MFSearchFlags.MFSearchFlagNone, SortResults: false);
                                    foreach (ObjectVersion objectVersion in searchResults)
                                    {

                                        UserPermission userPermission = new UserPermission();

                                        #region setting permission
                                        {
                                            var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);

                                            if (perm.CustomACL)
                                            {
                                                try
                                                {
                                                    userPermission = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                                                    if (userPermission.ReadPermission)
                                                    {
                                                        userPermission = _permission.ClassPermission(vault, UserID, objectVersion.Class);
                                                    }
                                                }
                                                catch
                                                {

                                                }
                                                if (userPermission.EditPermission && userPermission.AttachObjectsPermission)
                                                {
                                                    userPermission.DeletePermission = true;
                                                }

                                            }
                                            else
                                            {
                                                try
                                                {
                                                    AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                                    foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                                    {
                                                        found = false;

                                                        if (accessControlEntry1.IsGroup)
                                                        {
                                                            try
                                                            {

                                                                var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                                if (items.Any(m => m.Equals(UserID)))
                                                                {
                                                                    found = true;
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
                                                                if (userPermission != null)
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
                                                            else
                                                            {

                                                            }
                                                        }
                                                    }
                                                }
                                                catch
                                                {

                                                }
                                            }
                                            try
                                            {
                                                var username = _cacheObjects.UserAccounts(vault)?.FirstOrDefault(m => m.ID == UserID);
                                                if (username != null)
                                                {
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
                                        }
                                        #endregion
                                        var tff = item;
                                        tff.LastModifiedUtc = objectVersion.LastModifiedUtc;
                                        tff.Title = objectVersion.Title;
                                        tff.CreatedUtc = objectVersion.CreatedUtc;
                                        tff.VersionId = objectVersion.ObjVer.Version;
                                        tff.IsSingleFile = objectVersion.SingleFile;
                                        tff.IsCheckedOut = objectVersion.CheckedOutTo>0;
                                        tff.checkoutuserid = objectVersion.CheckedOutTo;
                                        tff.checkoutusername = objectVersion.CheckedOutToUserName;
                                        tff.IsDeleted = objectVersion.Deleted;                                       
                                        tff.userPermission = userPermission;
                                        string fileextension = "";
                                        int fileid = 0;
                                        if (objectVersion.SingleFile)
                                        {
                                            fileextension = objectVersion.Files[1].Extension;
                                            fileid = objectVersion.Files[1].ID;
                                        }
                                        tff.FileExtension = fileextension;
                                        tff.HasRelationship = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true).Count>0;
                                        tff.FileId = fileid;
                                        responses2.Add(tff);
                                    }

                                }
                                #endregion


                            }

                        }
                    }
                    responses2.Reverse();
                    return Ok(responses2);
                }
                return NotFound();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("GetAssigned/{VaultGuid}/{UserID}")]
        public IActionResult GetAssigned(string VaultGuid, int UserID)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            // Instantiate an MFilesServerApplication object.
            // https://developer.m-files.com/APIs/COM-API/Reference/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port
                );
            try
            {
                var vault = mfServerApplication.LogInToVault(VaultGuid);
              
                List<int> ints = new List<int>();
                List<int> ints1 = new List<int>();

                // Create our search conditions.
                var searchConditions = new SearchConditions();


                // Add a "not deleted" filter.
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);

                    // Set the condition.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, false);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                //filter last modified by me
                {

                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeMultiSelectLookup,
                        UserID);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }
                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            UserPermission userPermission = new UserPermission();
                            #region setting permission
                            {
                                var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);

                                if (perm.CustomACL)
                                {
                                    try
                                    {
                                        userPermission = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                                        if (userPermission.ReadPermission)
                                        {
                                            userPermission = _permission.ClassPermission(vault, UserID, objectVersion.Class);
                                        }
                                    }
                                    catch
                                    {

                                    }
                                    if (userPermission.EditPermission && userPermission.AttachObjectsPermission)
                                    {
                                        userPermission.DeletePermission = true;
                                    }

                                }
                                else
                                {
                                    try
                                    {
                                        AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                        foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                        {
                                            var found = false;

                                            if (accessControlEntry1.IsGroup)
                                            {
                                                try
                                                {

                                                    var items = _gettingusersinusergroup.GetUsersFromGroup(vault, accessControlEntry1.UserOrGroupID);
                                                    if (items.Any(m => m.Equals(UserID)))
                                                    {
                                                        found = true;
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
                                                    if (userPermission != null)
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
                                                else
                                                {

                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                                try
                                {
                                    var username = _cacheObjects.UserAccounts(vault)?.FirstOrDefault(m => m.ID == UserID);
                                    if (username != null)
                                    {
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
                            }
                            #endregion
                            var classname = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer,100).TypedValue.DisplayValue;
                            
                            var objecttypeid = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                            var fileextension = "";
                            int fileid = 0;
                            if (objectVersion.SingleFile)
                            {
                                fileextension = objectVersion.Files[1].Extension;
                                fileid = objectVersion.Files[1].ID;
                            }
                            var st = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                            Response.Add(new Objectsearchresponse { ClassTypeName = classname, VersionId = objectVersion.ObjVer.Version, ObjectTypeName = objecttypeid.NameSingular, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = userPermission, CreatedUtc = objectVersion.CreatedUtc, LastModifiedUtc = objectVersion.LastModifiedUtc, DisplayID = objectVersion.DisplayID, IsSingleFile = objectVersion.SingleFile, IsCheckedOut = objectVersion.CheckedOutTo > 0, checkoutuserid = objectVersion.CheckedOutTo, FileExtension=fileextension, HasRelationship= st.Count>0, FileId=fileid, checkoutusername = objectVersion.CheckedOutToUserName });

                        }
                        return Ok(Response);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }

                }
                else
                {
                    return NotFound("Could not find object containing that search phrase");
                }


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
     
    }
}
