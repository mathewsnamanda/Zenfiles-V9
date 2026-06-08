using ConsoleApp1;
using DocumentFormat.OpenXml.Spreadsheet;
using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using pulling_object_permission;
using readingmetaconfigjson.metaServices;
using System;
using System.Security;
using Zenfiles.Models;
using Zenfiles.Models.comments;
using Zenfiles.Models.objversions;
using Zenfiles.PermissionService;
using ZenFiles.Controllers;
using ZenFiles.Models;
using Zenfiles_V8.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectDeletionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Zenfiles.PermissionService.IPermission _permission;
        private readonly GetCacheObjects _cacheObjects;
        private readonly Gettingusersinusergroup  _gettingusersinusergroup;

        public ObjectDeletionController(IConfiguration Configuration, GetCacheObjects cacheObjects, 
            Zenfiles.PermissionService.IPermission permission, Gettingusersinusergroup gettingusersinusergroup
             )
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _permission = permission;
            _cacheObjects = cacheObjects;
            _gettingusersinusergroup = gettingusersinusergroup;

        }
        // GET: api/<CommentsController>
        [HttpGet("GetDeletedObject/{VaultGuid}/{UserID}")]
        public async Task<IActionResult> GetDeletedObject(string VaultGuid, int UserID)
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
                List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
                deletedobjectlist deletedobjectlists = new deletedobjectlist();
                try
                {
                    var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json");
                    if (System.IO.File.Exists(filepath))
                    {
                        var jsonString = await System.IO.File.ReadAllTextAsync(filepath);
                        deletedobjectlists = JsonConvert.DeserializeObject<deletedobjectlist>(jsonString);
                        foreach(var item in deletedobjectlists.Objectlists.Where(m => m.VaultGuid == VaultGuid && m.UserID == UserID))
                        {
                            int id = 500;
                            while (id != 0)
                            {
                                // Create our search conditions.
                                var searchConditions = new SearchConditions();
                                {
                                    // Create the condition.
                                    var condition = new SearchCondition();

                                    // Set the expression.
                                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);

                                    // Set the condition.
                                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                    // Set the value.
                                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                                        item.ObjectTypeID);

                                    // Add the condition to the collection.
                                    searchConditions.Add(-1, condition);
                                }
                                //object id
                                {
                                    // Create the condition.
                                    var condition = new SearchCondition();

                                    // Set the expression.
                                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                                    // Set the condition type.
                                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, item.ObjectID);
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
                                // Add a "not deleted" filter.
                                {
                                    // Create the condition.
                                    var condition = new SearchCondition();

                                    // Set the expression.
                                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);

                                    // Set the condition.
                                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                                    // Set the value.
                                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                                    // Add the condition to the collection.
                                    searchConditions.Add(-1, condition);
                                }

                                // Execute the search.
                                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                                    MFSearchFlags.MFSearchFlagNone, SortResults: false, MaxResultCount: id);
                                if (searchResults.MoreResults)
                                {
                                    id += 500;
                                }
                                else
                                {
                                    id = 0;
                                    if (searchResults.Count > 0)
                                    {
                                        try
                                        {
                                            foreach (ObjectVersion objectVersion in searchResults)
                                            {
                                                var objecttype = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                                var classname = "";

                                                var propfordisplay = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);
                                                foreach (PropertyValueForDisplay propertyValueForDisplay in propfordisplay)
                                                {
                                                    if (propertyValueForDisplay.PropertyDef == 100)
                                                    {
                                                        classname = propertyValueForDisplay.PropertyValue.Value.DisplayValue;
                                                    }
                                                }
                                                Response.Add(new Objectsearchresponse { DisplayID = objectVersion.DisplayID, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = new UserPermission { AttachObjectsPermission=true, DeletePermission=true, EditPermission=true, ReadPermission=true}, ClassTypeName = classname, ObjectTypeName = objecttype.NameSingular, VersionId = objectVersion.ObjVer.Version, CreatedUtc = objectVersion.CreatedUtc, LastModifiedUtc = objectVersion.LastModifiedUtc, IsSingleFile = objectVersion.SingleFile });

                                            }


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

                            }
                        }
                        if(Response.Count > 0)
                        {
                            return Ok(Response);
                        }
                        else
                        {
                            return NotFound("No files have been deleted");
                        }
                    }
                    else
                    {
                        return NotFound("No files have been deleted");
                    }
                }
                catch
                {
                    return BadRequest("Internal server error please try again letter");
                }

              
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetAllDeletedObjects/{VaultGuid}")]
        public async Task<IActionResult> GetAllDeletedObjects(string VaultGuid)
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
                int id = 500;
                while (id != 0)
                {
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
                        condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                        // Add the condition to the collection.
                        searchConditions.Add(-1, condition);
                    }


                    // Execute the search.
                    var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                        MFSearchFlags.MFSearchFlagNone, SortResults: false, MaxResultCount: id);
                    if (searchResults.MoreResults)
                    {
                        id += 500;
                    }

                    else if (!searchResults.MoreResults)
                    {
                        id = 0;
                        try
                        {
                            deletedobjectlist deletedobjectlistp = new deletedobjectlist();
                            List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
                            deletedobjectlist deletedobjectlistsd = new deletedobjectlist();
                            foreach (ObjectVersion objectVersion in searchResults)
                            {
                                deletedobjectlistp.Objectlists.Add(new objectlist { ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.ID, ObjectTypeID = objectVersion.ObjVer.Type, UserID = 0 });
                                UserPermission userPermission = new UserPermission();
                                userPermission.DeletePermission = true;
                                userPermission.EditPermission = true;
                                userPermission.AttachObjectsPermission = true;
                                userPermission.ReadPermission = true;
                                Response.Add(new Objectsearchresponse { DisplayID = objectVersion.DisplayID, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = userPermission, LastModifiedUtc = objectVersion.LastModifiedUtc, CreatedUtc = objectVersion.CreatedUtc, IsSingleFile = objectVersion.SingleFile });
                            }


                            try
                            {
                                deletedobjectlist deletedobjectlists = new deletedobjectlist();
                                var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json");
                                if (System.IO.File.Exists(filepath))
                                {
                                    var jsonString = await System.IO.File.ReadAllTextAsync(filepath);
                                    deletedobjectlists = JsonConvert.DeserializeObject<deletedobjectlist>(jsonString);

                                }

                                foreach (var t in deletedobjectlists.Objectlists)
                                {
                                    var found = deletedobjectlistp.Objectlists.FirstOrDefault(m => m.ObjectTypeID == t.ObjectTypeID && m.ClassID == t.ClassID && m.ObjectID == t.ObjectID);
                                    if (found == null)
                                    {
                                        deletedobjectlistsd.Objectlists.Add(t);
                                    }
                                }
                                foreach (var t in deletedobjectlistsd.Objectlists)
                                {
                                    deletedobjectlists.Objectlists.RemoveAll(m => m.ObjectTypeID == t.ObjectTypeID && m.ClassID == t.ClassID && m.ObjectID == t.ObjectID);
                                }

                                JsonSerializer serializer = new JsonSerializer();
                                serializer.Converters.Add(new JavaScriptDateTimeConverter());
                                serializer.NullValueHandling = NullValueHandling.Ignore;

                                using (StreamWriter sw = new StreamWriter(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json")))
                                using (JsonWriter writer = new JsonTextWriter(sw))
                                {
                                    serializer.Serialize(writer, deletedobjectlists);
                                }
                            }
                            catch
                            {

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
                return BadRequest("Internal server error. try again");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // POST api/<CommentsController>
        [HttpPost("DeleteObject")]
        public async Task<IActionResult> DeleteObjectAsync([FromBody] deleteobject deleteobject)
        {
            if (deleteobject.ObjectId == 0)
            {
                return NotFound("Could not find the object");
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
                var vault = mfServerApplication.LogInToVault(deleteobject.VaultGuid);

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
                //add object id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, deleteobject.ObjectId);

                    searchConditions.Add(-1, condition);
                }
                //filter with class
                {
                    // Create the "maximum" search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property.
                    searchCondition.Expression.SetPropertyValueExpression(
                        100, // This is our date property ID
                        PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We only want documents that are before 1st February 2017.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, deleteobject.ClassId);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
                }
                // Execute the search.

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);

                if (searchResults.Count > 1)
                {
                    return BadRequest("More than two objects were found");
                }
                foreach (ObjectVersion objectVersion in searchResults)
                {
                    UserPermission userPermission = new UserPermission();


                    #region setting permission
                    {
                        var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);
                        if (perm.CustomACL)
                        {
                            userPermission = _permission.ObjectPermission(vault, deleteobject.UserID, objectVersion.ObjVer.Type);
                            if (userPermission.ReadPermission)
                            {
                                userPermission = _permission.ClassPermission(vault, deleteobject.UserID, objectVersion.Class);
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
                                            if (items.Any(m => m.Equals(deleteobject.UserID)))
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
                                            if (deleteobject. UserID == username.ID)
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
                                                if (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow"))
                                                {
                                                    userPermission.ReadPermission = true;
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
                    try
                    {
                        var username = vault.UserOperations.GetUserAccount(deleteobject.UserID);

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
                    if (userPermission != null)
                    if (!userPermission.DeletePermission)
                    {
                        return Unauthorized("You are not allowed to delete this object");
                    }

                    try
                    {

                        // We want to alter the document with ID 249.
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.ObjVer.Type,
                            ID: objectVersion.ObjVer.ID);
                        vault.ObjectOperations.DeleteObject(objID);

                        deletedobjectlist deletedobjectlists = new deletedobjectlist();
                        try
                        {
                            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json");
                            if (System.IO.File.Exists(filepath))
                            {
                                var jsonString = await System.IO.File.ReadAllTextAsync(filepath);
                                deletedobjectlists = JsonConvert.DeserializeObject<deletedobjectlist>(jsonString);

                            }

                            
                            deletedobjectlists.Objectlists.Add(new objectlist { ClassID=objectVersion.Class, ObjectID=objectVersion.ObjVer.ID, ObjectTypeID=objectVersion.ObjVer.Type, UserID=deleteobject.UserID, VaultGuid = deleteobject.VaultGuid});
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Converters.Add(new JavaScriptDateTimeConverter());
                            serializer.NullValueHandling = NullValueHandling.Ignore;


                            using (StreamWriter sw = new StreamWriter(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json")))
                            using (JsonWriter writer = new JsonTextWriter(sw))
                            {
                                serializer.Serialize(writer, deletedobjectlists);
                                // {"ExpiryDate":new Date(1230375600000),"Price":0}
                            }
                        }
                        catch
                        {

                        }

                        

                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }

                }
                return Ok("Successfully deleted");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("UnDeleteObject")]
        public async Task<IActionResult> UnDeleteObjectAsync([FromBody] deleteobject deleteobject)
        {
            if (deleteobject.ObjectId == 0)
            {
                return NotFound("Could not find the object");
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
                var vault = mfServerApplication.LogInToVault(deleteobject.VaultGuid);

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
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }
                //add object id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, deleteobject.ObjectId);

                    searchConditions.Add(-1, condition);
                }
                //filter with class
                {
                    // Create the "maximum" search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property.
                    searchCondition.Expression.SetPropertyValueExpression(
                        100, // This is our date property ID
                        PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We only want documents that are before 1st February 2017.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, deleteobject.ClassId);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
                }
                // Execute the search.

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);

                if (searchResults.Count > 1)
                {
                    return BadRequest("More than two objects were found");
                }
                foreach (ObjectVersion objectVersion in searchResults)
                {
                    UserPermission userPermission = new UserPermission();


                    #region setting permission
                    {
                        var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);
                        if (perm.CustomACL)
                        {
                            userPermission = _permission.ObjectPermission(vault, deleteobject.UserID, objectVersion.ObjVer.Type);
                            if (userPermission.ReadPermission)
                            {
                                userPermission = _permission.ClassPermission(vault, deleteobject.UserID, objectVersion.Class);
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
                                            if (items.Any(m => m.Equals(deleteobject.UserID)))
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
                                            if (deleteobject.UserID == username.ID)
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
                                                if (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow"))
                                                {
                                                    userPermission.ReadPermission = true;
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
                    try
                    {
                        var username = vault.UserOperations.GetUserAccount(deleteobject.UserID);

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
                    if (userPermission != null)
                        if (!userPermission.DeletePermission)
                        {
                            return Unauthorized("You are not allowed to delete this object");
                        }

                    try
                    {

                        // We want to alter the document with ID 249.
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.ObjVer.Type,
                            ID: objectVersion.ObjVer.ID);
                        vault.ObjectOperations.UndeleteObject(objID);
                        deletedobjectlist deletedobjectlists = new deletedobjectlist();
                        try
                        {
                            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json");
                            if (System.IO.File.Exists(filepath))
                            {
                                var jsonString = await System.IO.File.ReadAllTextAsync(filepath);
                                deletedobjectlists = JsonConvert.DeserializeObject<deletedobjectlist>(jsonString);

                            }

                            deletedobjectlists.Objectlists.RemoveAll(m=>m.ClassID == objectVersion.Class &&m.ObjectID == objectVersion.ObjVer.ID&& m.ObjectTypeID == objectVersion.ObjVer.Type&& m.UserID == deleteobject.UserID );
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Converters.Add(new JavaScriptDateTimeConverter());
                            serializer.NullValueHandling = NullValueHandling.Ignore;

                            using (StreamWriter sw = new StreamWriter(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json")))
                            using (JsonWriter writer = new JsonTextWriter(sw))
                            {
                                serializer.Serialize(writer, deletedobjectlists);
                                // {"ExpiryDate":new Date(1230375600000),"Price":0}
                            }
                        }
                        catch
                        {

                        }



                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }

                }
                return Ok("Successfully undeleted");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("AdminUnDeleteObject")]
        public async Task<IActionResult> AdminUnDeleteObject([FromBody] adminundelete deleteobject)
        {
            if (deleteobject.ObjectId == 0)
            {
                return NotFound("Could not find the object");
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
                var vault = mfServerApplication.LogInToVault(deleteobject.VaultGuid);

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
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }
                //add object id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, deleteobject.ObjectId);

                    searchConditions.Add(-1, condition);
                }
                //filter with class
                {
                    // Create the "maximum" search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property.
                    searchCondition.Expression.SetPropertyValueExpression(
                        100, // This is our date property ID
                        PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We only want documents that are before 1st February 2017.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, deleteobject.ClassId);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
                }
                // Execute the search.

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);

                if (searchResults.Count > 1)
                {
                    return BadRequest("More than two objects were found");
                }
                foreach (ObjectVersion objectVersion in searchResults)
                {
                  
                        // We want to alter the document with ID 249.
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.ObjVer.Type,
                            ID: objectVersion.ObjVer.ID);
                        vault.ObjectOperations.UndeleteObject(objID);
                        deletedobjectlist deletedobjectlists = new deletedobjectlist();
                        try
                        {
                            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json");
                            if (System.IO.File.Exists(filepath))
                            {
                                var jsonString = await System.IO.File.ReadAllTextAsync(filepath);
                                deletedobjectlists = JsonConvert.DeserializeObject<deletedobjectlist>(jsonString);

                            }

                            deletedobjectlists.Objectlists.RemoveAll(m => m.ClassID == objectVersion.Class && m.ObjectID == objectVersion.ObjVer.ID && m.ObjectTypeID == objectVersion.ObjVer.Type);
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Converters.Add(new JavaScriptDateTimeConverter());
                            serializer.NullValueHandling = NullValueHandling.Ignore;

                            using (StreamWriter sw = new StreamWriter(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "deletedObjects", "deleted.json")))
                            using (JsonWriter writer = new JsonTextWriter(sw))
                            {
                                serializer.Serialize(writer, deletedobjectlists);
                                // {"ExpiryDate":new Date(1230375600000),"Price":0}
                            }
                        }
                        catch
                        {

                        }

                }
                return Ok("Successfully deleted");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetDeletedObjectViewProps/{VaultGuid}/{ObjectId}/{ClassId}/{UserID}")]
        public IActionResult GetObjectViewProps(int ObjectId, string VaultGuid, int ClassId, int UserID)
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
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }
                //add search with internal id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, ObjectId);
                    searchConditions.Add(-1, condition);
                }
                //filter with class
                {
                    // Create the "maximum" search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property.
                    searchCondition.Expression.SetPropertyValueExpression(
                        100, // This is our date property ID
                        PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We only want documents that are before 1st February 2017.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, ClassId);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        List<updateprop1> props = new List<updateprop1>();
                        List<updateprop1> props1 = new List<updateprop1>();
                        List<property3> hiddenlist = new List<property3>();

                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            try
                            {
                                var classty = vault.ClassOperations.GetObjectClass(objectVersion.Class);
                                var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                            }
                            catch
                            {

                            }

                            List<readingmetaconfigjson.MetaModals.workflowstatepropbehave> workflowstatepropbehave = new List<readingmetaconfigjson.MetaModals.workflowstatepropbehave>();

                            try
                            {
                                var workflow = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 38);
                                var workflowstate = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 39);
                                foreach (Lookup lookup in workflow.Value.GetValueAsLookups())
                                {
                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "38", workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias = "" });
                                }
                                foreach (Lookup lookup in workflowstate.Value.GetValueAsLookups())
                                {
                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "39", workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias = "" });

                                }
                            }
                            catch (Exception ex)
                            {
                                workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "38", workflowstateguid = "", workflowstateid = "", workflowstatealias = "" });
                                workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "39", workflowstateguid = "", workflowstateid = "", workflowstatealias = "" });
                            }
                            var propspd = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);

                            foreach (PropertyValueForDisplay properties in propspd)
                            {
                                if (vault.ClassOperations.GetObjectClass(objectVersion.Class).NamePropertyDef != 0)
                                {
                                    if (properties.PropertyDef > 0)
                                    {

                                        var property = vault.PropertyDefOperations.GetPropertyDef(properties.PropertyDef);
                                        var perm = _permission.PropPermission(vault, UserID, properties.PropertyDef);

                                        if (property.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                        {
                                            var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                            if (prop == null)
                                            {
                                                if (properties.DataType == MFDataType.MFDatatypeLookup | properties.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                                                {
                                                    List<lookupvalues> values = new List<lookupvalues>();
                                                    var valuesd = properties.PropertyValue.Value.GetValueAsLookups();
                                                    foreach (Lookup lookup in valuesd)
                                                    {
                                                        values.Add(new lookupvalues { ID = lookup.DisplayID, Title = lookup.DisplayValue });
                                                    }
                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = false, IsAutomatic = false, userPermission = perm });

                                                }
                                                else
                                                {
                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = false, userPermission = perm });

                                                }

                                            }
                                        }
                                        else
                                        {
                                            var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                            if (prop == null)
                                            {
                                                if (properties.DataType == MFDataType.MFDatatypeLookup | properties.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                                                {
                                                    List<lookupvalues> values = new List<lookupvalues>();
                                                    var valuesd = properties.PropertyValue.Value.GetValueAsLookups();
                                                    foreach (Lookup lookup in valuesd)
                                                    {
                                                        values.Add(new lookupvalues { ID = lookup.DisplayID, Title = lookup.DisplayValue });
                                                    }
                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = false, IsAutomatic = true, userPermission = perm });

                                                }
                                                else
                                                {
                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = true, userPermission = perm });

                                                }

                                            }
                                        }
                                        if (properties.DataType == MFDataType.MFDatatypeLookup)
                                        {
                                            var workflow = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, properties.PropertyDef);
                                            var itemr = workflow.Value.GetValueAsLookups();
                                            if (itemr.Count > 0)
                                            {
                                                foreach (Lookup lookup in itemr)
                                                {
                                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias = "" });
                                                }
                                            }
                                            else
                                            {
                                                workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = "", workflowstateid = "", workflowstatealias = "" });

                                            }

                                        }
                                    }

                                }
                                else
                                {
                                    var property = vault.PropertyDefOperations.GetPropertyDefAdmin(properties.PropertyDef);
                                    var perm = _permission.PropPermission(vault, UserID, properties.PropertyDef);
                                    
                                    if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                    {
                                        var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                        if (prop == null)
                                        {
                                            if (properties.DataType == MFDataType.MFDatatypeLookup | properties.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                                            {
                                                List<lookupvalues> values = new List<lookupvalues>();
                                                var valuesd = properties.PropertyValue.Value.GetValueAsLookups();
                                                foreach (Lookup lookup in valuesd)
                                                {
                                                    values.Add(new lookupvalues { ID = lookup.DisplayID, Title = lookup.DisplayValue });
                                                }
                                                props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = false, IsAutomatic = false, userPermission = perm });

                                            }
                                            else
                                            {
                                                props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = false, userPermission = perm });

                                            }

                                        }
                                    }
                                    else
                                    {
                                        var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                        if (prop == null)
                                        {
                                            if (properties.DataType == MFDataType.MFDatatypeLookup | properties.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                                            {
                                                List<lookupvalues> values = new List<lookupvalues>();
                                                var valuesd = properties.PropertyValue.Value.GetValueAsLookups();
                                                foreach (Lookup lookup in valuesd)
                                                {
                                                    values.Add(new lookupvalues { ID = lookup.DisplayID, Title = lookup.DisplayValue });
                                                }
                                                props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = false, IsAutomatic = true, userPermission = perm });

                                            }
                                            else
                                            {
                                                props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = true, IsAutomatic = false, userPermission = perm });

                                            }

                                        }
                                    }
                                    if (properties.DataType == MFDataType.MFDatatypeLookup)
                                    {
                                        var workflow = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, properties.PropertyDef);
                                        var itemr = workflow.Value.GetValueAsLookups();
                                        if (itemr.Count > 0)
                                        {
                                            foreach (Lookup lookup in itemr)
                                            {
                                                workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias = property.SemanticAliases.Value });
                                            }
                                        }
                                        else
                                        {
                                            workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = "", workflowstateid = "", workflowstatealias = property.SemanticAliases.Value });

                                        }

                                    }
                                }
                            }

                            var classsp = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m => m.ID == objectVersion.Class);
                           
                            var tpp = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == classsp.ObjectType);
                           

                            IMeta meta = new MetaImplement();
                            if(classsp != null&& tpp!=null)
                            {
                                var items = meta.behaveprops(objectVersion.ObjVer.Type.ToString(),tpp.SemanticAliases.Value, vault.ClassOperations.GetObjectClassAdmin(objectVersion.Class).SemanticAliases.Value, objectVersion.Class.ToString(), workflowstatepropbehave, VaultGuid);

                                var itemss = items.Where(m => m.IsHidden).OrderBy(m => m.Property);

                                foreach (var t in itemss)
                                {
                                    props.RemoveAll(m => m.id.ToString().Trim() == t.Property);
                                }
                                var isrequired = items.Where(m => m.IsRequired).OrderBy(m => m.Property);
                                foreach (var r in isrequired)
                                {
                                    var tt = props.FirstOrDefault(m => m.id.ToString() == r.Property);
                                    if (tt != null)
                                    {
                                        updateprop1 updateprop1 = new updateprop1();
                                        updateprop1 = tt;
                                        updateprop1.IsRequired = true;
                                        props.RemoveAll(m => m.id == tt.id);
                                        props.Add(updateprop1);
                                    }

                                }
                            }
                           

                        }
                        foreach (var t in hiddenlist)
                        {
                            props.RemoveAll(m => m.id == t.propId);
                        }
                        foreach (var prop in props)
                        {
                            props1.Add(prop);
                        }
                      
                        return Ok(props1);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }

                }
                else
                {
                    return BadRequest("Could not find object with that ID");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
