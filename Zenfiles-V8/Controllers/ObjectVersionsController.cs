using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Zenfiles.Models.objversions;
using Zenfiles.Models.setprops;
using Zenfiles.PermissionService;
using ZenFiles.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectVersionsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ObjectVersionsController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
        }
        // GET: api/<ObjectVersionsController>
        [HttpGet("GetObjectVesions/{VaultGuid}/{ObjectId}/{ClassId}/{UserID}")]
        public IActionResult GetObjectVesions(string VaultGuid, string ObjectId, string ClassId,int UserID)
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
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, false);

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
                //filter class id
                {
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] { ClassId };

                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "class" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass,
                                    MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal one of the class Ids.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items whose class property is one of the supplied class Ids.
                    // This should be MFDatatypeMultiSelectLookup, even though the property is MFDatatypeLookup.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeMultiSelectLookup, classIds);
                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    List<Objectversions> objectversions= new List<Objectversions>();
                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                      
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.ObjVer.Type,
                            ID: objectVersion.ObjVer.ID);
                        var history = vault.ObjectOperations.GetHistory(objID);
                        foreach (ObjectVersion objectVersionFile in history)
                        {
                            List<updateprop1> updateproperties = new List<updateprop1>();
                            List<ObjectFileResp> objectfiles = new List<ObjectFileResp>();
                            string lastmodofed="", lastmodifiedby = "",extension="",classname="";

                            var properties = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersionFile.ObjVer);
                            foreach (PropertyValueForDisplay display in properties)
                            {
                                if (display.PropertyDef ==21)
                                {
                                    lastmodofed = display.DisplayValue;
                                }
                                else if (display.PropertyDef == 23)
                                {
                                    lastmodifiedby = display.DisplayValue;
                                }
                                else if (display.PropertyDef == 100)
                                {
                                    classname = display.DisplayValue;
                                }
                                updateproperties.Add(new updateprop1 { Datatype = display.DataType.ToString(), id = display.PropertyDef, PropName = display.PropertyDefName, Value = display.DisplayValue });
                            }
                            foreach (ObjectFile objectFile in objectVersionFile.Files)
                            {
                                if (objectVersionFile.SingleFile)
                                {
                                    extension = objectFile.Extension;
                                }
                                objectfiles.Add(new ObjectFileResp { extension = objectFile.Extension, fileID = objectFile.ID, fileTitle = objectFile.Title, fileversion = objectFile.Version });
                            }
                            objectversions.Add(new Objectversions { ObjectFiles = objectfiles, objectprops = updateproperties, title = objectVersionFile.Title, versionid = objectVersionFile.ObjVer.Version, DisplayID = objectVersionFile.DisplayID, IsSingleFile= objectVersionFile.SingleFile, LastModifiedBy=lastmodifiedby, LastModifiedUtc=lastmodofed, Extension = extension, Class= objectVersionFile.Class, ClassName=classname });
                        }
                    }
                    return Ok(objectversions);
                }
                else
                {
                    return NotFound("no object in that class with that id");
                }
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
            
        }
        [HttpGet("GetObjectFileVersion/{VaultGuid}/{ObjectId}/{ObjectVersion}/{FileId}/{ClassId}/{UserID}")]
        public IActionResult GetObjectFileVersion(string VaultGuid, string ObjectId, string ClassId, int UserID,int ObjectVersion, int FileId)
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
                var filepath = "";
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
                //filter class id
                {
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] { ClassId };

                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "class" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass,
                                    MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal one of the class Ids.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items whose class property is one of the supplied class Ids.
                    // This should be MFDatatypeMultiSelectLookup, even though the property is MFDatatypeLookup.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeMultiSelectLookup, classIds);
                    searchConditions.Add(-1, searchCondition);

                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    List<Objectversions> objectversions = new List<Objectversions>();
                    foreach (ObjectVersion objectVersion in searchResults)
                    {

                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.ObjVer.Type,
                            ID: objectVersion.ObjVer.ID);
                        var history = vault.ObjectOperations.GetHistory(objID);
                        foreach (ObjectVersion objectVersionFile in history)
                        {
                            if(objectVersionFile.ObjVer.Version == ObjectVersion)
                            {
                                foreach (ObjectFile objectFile in objectVersionFile.Files)
                                {
                                    if (objectFile.ID == FileId)
                                    {
                                        filepath = Path.Combine(Directory.GetCurrentDirectory(), "Files", Guid.NewGuid().ToString() + "." + objectFile.Extension);
                                        vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, filepath);

                                    }
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(filepath))
                    {
                        return NotFound("Could not find a file with that ID");
                    }

                    byte[] AsBytes = System.IO.File.ReadAllBytes(filepath);
                    String AsBase64String = Convert.ToBase64String(AsBytes);

                    FilepathResp filepathResp = new FilepathResp();
                    filepathResp.base64 = AsBase64String;
                    filepathResp.extension = System.IO.Path.GetExtension(filepath);
                    try
                    {
                        System.IO.File.Delete(filepath);
                    }
                    catch (Exception ex)
                    {

                    }
                    return Ok(filepathResp);
                }
                else
                {
                    return NotFound("no object in that class with that id");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // POST api/<ObjectVersionsController>
        [HttpPost("RollbackToVersion")]
        public IActionResult RollbackToVersion([FromBody] postversion postversion)
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

                var vault = mfServerApplication.LogInToVault(postversion.VaultGuid);
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
                //add search with internal id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, postversion.ObjectId);
                   
                   searchConditions.Add(-1, condition);
                }
                //filter class id
                {
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] { postversion.ClassId };

                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "class" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass,
                                    MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal one of the class Ids.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items whose class property is one of the supplied class Ids.
                    // This should be MFDatatypeMultiSelectLookup, even though the property is MFDatatypeLookup.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeMultiSelectLookup, classIds);

                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                     
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.ObjVer.Type,
                            ID: objectVersion.ObjVer.ID);

                        vault.ObjectOperations.Rollback(objID, postversion.VersionID);

                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                        ObjVerVersion objVerVersion= new ObjVerVersion();
                        objVerVersion.Version = postversion.VersionID;
                        
                     
                        var date = DateTime.UtcNow;
                        var lastModifiedBy = new TypedValue();
                        lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, postversion.UserID);
                        var lastModifiedDate = new TypedValue();
                        lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour - 3, date.Minute, date.Second));
                        vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                        (
                            checkedOutObjectVersion.ObjVer,
                            true, lastModifiedBy,
                            true, lastModifiedDate
                        );
                        // Check the object back in.
                        vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);
                    }
                    return Ok("Rolled Back successfully");
                }
                else
                {
                    return NotFound("no object in that class with that id");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        

    }
}
