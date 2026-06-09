using CargenDss.Models;
using ConsoleApp1;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using EdoState_DSS.Models;
using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using pulling_object_permission;
using readingmetaconfigjson.MetaModals;
using readingmetaconfigjson.metaServices;
using RecentFix.models;
using RecentFix.services;
using RestSharp;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Uon_DSS.Models;
using Zenfiles.Models;
using Zenfiles.Models.comments;
using Zenfiles.Models.linked;
using Zenfiles.Models.objversions;
using Zenfiles.Models.views;
using Zenfiles.Models.Workflow;
using Zenfiles.PermissionService;
using ZenFiles.Models;
using Zenfiles_V7.Models;
using Zenfiles_V8.Models;
using Zenfiles_V8.Services;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZenFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    public class objectinstanceController : ControllerBase
    {
        private readonly IMFilesObjectRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly Zenfiles.PermissionService.IPermission _permission;
        private readonly GetCacheObjects _cacheObjects;
        private readonly Gettingusersinusergroup _gettingusersinusergroup;
        public objectinstanceController(GetCacheObjects cacheObjects, IConfiguration Configuration,
            Zenfiles.PermissionService.IPermission permission,
            IMFilesObjectRepository repository, Gettingusersinusergroup gettingusersinusergroup)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
          
            _cacheObjects = cacheObjects;
            _permission = permission;
            _repository = repository;
            _gettingusersinusergroup = gettingusersinusergroup;
        }
        [HttpPut("UpdateObjectFile")]
        public async Task<IActionResult> UpdateObjectFileAsync([FromForm] UpdateFile updateFile, IFormFile formFile, CancellationToken cancellationToken)
        {
            var respo = await WriteFile(formFile);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var files = Directory.GetFiles(path);
            List<string> filesp = new List<string>();
            foreach (var file in files)
            {
                if (System.IO.Path.GetFileName(file).StartsWith(respo))
                {
                    filesp.Add(file);
                }
                else
                {
                    if (DateTime.UtcNow.Subtract(System.IO.File.GetCreationTime(file)).Minutes >= 30)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }

            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";

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
                var vault = mfServerApplication.LogInToVault(updateFile.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, updateFile.objectid);
                    searchConditions.Add(-1, condition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        Zenfiles.Models.Error error = new Zenfiles.Models.Error();
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: objectVersion.OriginalObjID.Type,
                                ID: objectVersion.ObjVer.ID);
                            var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                         
                            foreach (ObjectFile objectFile in checkedOutObjectVersion.Files)
                            {
                                if(objectFile.ID==updateFile.fileid)
                                {
                                    if (objectFile.ID == updateFile.fileid && objectFile.Version == updateFile.versionID)
                                    {
                                        foreach (var filer in filesp)
                                        {
                                            string fileName = filer;
                                            string extension = Path.GetExtension(fileName);
                                            var filename = fileName.Substring(0, fileName.Length - extension.Length).TrimEnd('.');
                                            vault.ObjectFileOperations.UploadFile(objectFile.ID, objectFile.Version, filename + extension);
                                            vault.ObjectFileOperations.RenameFile(checkedOutObjectVersion.ObjVer, objectFile.FileVer, objectFile.Title, System.IO.Path.GetExtension(filer).Replace(".", ""), true);
                                        }
                                    }
                                    else if (objectFile.ID == updateFile.fileid && 0 == updateFile.versionID)
                                    {
                                        foreach (var filer in filesp)
                                        {
                                            string fileName = filer;

                                            string extension = Path.GetExtension(fileName);
                                            var filename = fileName.Substring(0, fileName.Length - extension.Length).TrimEnd('.');

                                            vault.ObjectFileOperations.UploadFile(objectFile.ID, objectFile.Version, filename + extension);
                                            vault.ObjectFileOperations.RenameFile(checkedOutObjectVersion.ObjVer, objectFile.FileVer, objectFile.Title, System.IO.Path.GetExtension(filer).Replace(".", ""), true);
                                        }
                                    }
                                    else
                                    {
                                        error.explanation = "Error: Changes not saved. The Object Version in the server is higher than your current version";
                                    }
                                }
                               
                            }
                            if (string.IsNullOrEmpty(error.explanation))
                            {
                                var date = DateTime.UtcNow;
                                var lastModifiedBy = new TypedValue();
                                lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, updateFile.UserID);
                                var lastModifiedDate = new TypedValue();
                                lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                                vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                                (
                                    checkedOutObjectVersion.ObjVer,
                                    true, lastModifiedBy,
                                    true, lastModifiedDate
                                );


                                vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                                Reports reports = new Reports();
                                reports.classID = checkedOutObjectVersion.Class;
                                reports.objectID = objectVersion.ObjVer.ID;
                                reports.objectTypeID = checkedOutObjectVersion.ObjVer.Type;
                                reports.VaultGuid = updateFile.VaultGuid;
                                string json = JsonConvert.SerializeObject(reports);

                                var options112 = new RestClientOptions(ReportsUrl)
                               ;
                                var client112 = new RestClient(options112);
                                var request1112 = new RestRequest("UpdateRecord", Method.Post);
                                request1112.AddHeader("Content-Type", "application/json");
                                var body = json;
                                request1112.AddStringBody(body, RestSharp.DataFormat.Json);
                                RestResponse response1112 = await client112.ExecuteAsync(request1112);
                                if (!response1112.IsSuccessful)
                                {
                                    Console.WriteLine(response1112.Content);
                                }


                                forlogs objectsearchresponse = new forlogs();

                                objectsearchresponse.id = objectVersion.ObjVer.ID;
                                objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                                objectsearchresponse.Title = objectVersion.Title;
                                objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                                objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                                objectsearchresponse.ClassID = objectVersion.Class;
                                objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                                objectsearchresponse.VaultGuid = updateFile.VaultGuid;
                                objectsearchresponse.DisplayID = objectVersion.DisplayID;
                                objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                                var perm = _permission.ObjectPermission(vault, updateFile.UserID, objectVersion.ObjVer.Type);
                                if (perm.EditPermission)
                                {
                                    perm = _permission.ClassPermission(vault, updateFile.UserID, objectVersion.Class);

                                }

                                objectsearchresponse.userPermission = perm;



                                try
                                {
                                    var classty = vault.ClassOperations.GetObjectClass(updateFile.ClassId);
                                    var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                    objectsearchresponse.ClassTypeName = classty.Name;
                                    objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                                }
                                catch
                                {

                                }
                            }
                            else
                            {
                                try
                                {
                                    foreach (var file in files)
                                    {
                                        System.IO.File.Delete(file);
                                    }
                                }
                                catch (Exception)
                                {
                                
                                }

                                return BadRequest(error);
                            }
                        }
                        try
                        {
                            foreach (var file in files)
                            {
                                System.IO.File.Delete(file);
                            }
                        }
                        catch (Exception) { }
                      
                        return Ok("Updated file successfully");
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
        [HttpGet("DSSfileupdateBackend")]
        public async Task<IActionResult> DSSfileupdateBackend([FromQuery] UpdateFile updateFile)
        {
           
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";

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
                var vault = mfServerApplication.LogInToVault(updateFile.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, updateFile.objectid);
                    searchConditions.Add(-1, condition);
                }
                //filter pdf only
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetFileValueExpression(MFFileValueType.MFFileValueTypeFileName);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeContains;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, ".pdf");

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
                        updateFile.ClassId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);

                            var filepath = Path.Combine(path, updateFile.VaultGuid + "_" + 0 + "_" + updateFile.objectid.ToString() + "_" + updateFile.UserID.ToString() + ".txt");

                            if (System.IO.File.Exists(filepath))
                            {
                                string content = updateFile.VaultGuid + "_" + 0 + "_" + updateFile.objectid + "_" + updateFile.UserID;
                                string content1 = System.IO.File.ReadAllText(filepath);
                                if (content.Trim() == content1.Trim())
                                {
                                    vault.ObjectOperations.ForceUndoCheckout(objectVersion.ObjVer);
                                }
                                System.IO.File.Delete(filepath);
                            }

                                var objID = new MFilesAPI.ObjID();
                                objID.SetIDs(
                                    ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                                    ID: objectVersion.ObjVer.ID);
                                var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                                if (vault.ObjectOperations.IsObjectCheckedOutToThisUserOnThisComputer(objID))
                                {
                                    vault.ObjectOperations.UndoCheckout(objectVersion.ObjVer);
                                }
                                if (!checkout)
                                {
                                    // Check out the object.
                                    var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                                    bool signed = false;
                                    bool declined = false;
                                    string docguid = "";
                                    path = "";


                                    foreach (ObjectFile objectFile in checkedOutObjectVersion.Files)
                                    {
                                        if (objectFile.Version == updateFile.versionID+1)
                                        {
                                            if (objectFile.ID == updateFile.fileid)
                                            {
                                                var options = new RestClientOptions($"https://dsscall.alignsys.tech/api/Docs/Getdocument/{objectFile.FileGUID.TrimStart('{').TrimEnd('}')}")
                                                ;
                                                var client = new RestClient(options);
                                                var request = new RestRequest("", Method.Get);
                                                RestResponse response = await client.ExecuteAsync(request);

                                                if (response.IsSuccessful)
                                                {
                                                    CargenDss.Models.Root myDeserializedClass = JsonConvert.DeserializeObject<CargenDss.Models.Root>(response.Content);
                                                    if (myDeserializedClass != null)
                                                    {
                                                        signed = myDeserializedClass.signedComplete;
                                                        declined = myDeserializedClass.declined;
                                                        docguid = myDeserializedClass.docGuid;
                                                    }

                                                }
                                                else
                                                {
                                                    return BadRequest("The document is still being signed");
                                                }

                                                if (signed || declined)
                                                {
                                                    var options1 = new RestClientOptions($"https://dsscall.alignsys.tech/api/Docs/FileDownload/{docguid}");
                                                    var client1 = new RestClient(options1);
                                                    var request1 = new RestRequest("", Method.Get);
                                                    RestResponse response1 = await client1.ExecuteAsync(request1);

                                                    if (response1.IsSuccessful)
                                                    {
                                                        using (WebClient webClient = new WebClient())
                                                        {
                                                            path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Files", Guid.NewGuid().ToString() + ".pdf");
                                                            webClient.DownloadFile($"https://dsscall.alignsys.tech/api/Docs/FileDownload/{docguid}", path);
                                                            vault.ObjectFileOperations.UploadFile(objectFile.ID, objectFile.Version, path);
                                                            var date = DateTime.UtcNow;
                                                            var lastModifiedBy = new TypedValue();
                                                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, updateFile.UserID);
                                                            var lastModifiedDate = new TypedValue();
                                                            lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                                                            vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                                                            (
                                                                checkedOutObjectVersion.ObjVer,
                                                                true, lastModifiedBy,
                                                                true, lastModifiedDate
                                                            );
                                                        }

                                                    }



                                                }


                                            }
                                        }
                                        else
                                        {
                                            return BadRequest("There is another higher version in the server");
                                        }
                                    }
                                    try
                                    {
                                        var propertytoset = _cacheObjects.PropTypes(vault)?.FirstOrDefault(m=>m.SemanticAliases.Value== "PD.Signed");
                                        if (propertytoset != null)
                                        {
                                          var propertyonobject = propertytoset.PropertyDef.ID;
                                            if (propertytoset.PropertyDef.ID > -1)
                                            {
                                                // Create a property value to update.
                                                var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                                {
                                                    PropertyDef = propertytoset.PropertyDef.ID
                                                };
                                                nameOrTitlePropertyValue.Value.SetValue(
                                                    MFDataType.MFDatatypeBoolean,  // This must be correct for the property definition.
                                                    true
                                                );
                                                // Update the property on the server.
                                                vault.ObjectPropertyOperations.SetProperty(
                                                    ObjVer: checkedOutObjectVersion.ObjVer,
                                                    PropertyValue: nameOrTitlePropertyValue);
                                            }

                                        }
                                    }
                                    catch
                                    {

                                    }

                                    vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);


                                    Reports reports = new Reports();
                                    reports.classID = checkedOutObjectVersion.Class;
                                    reports.objectID = objectVersion.ObjVer.ID;
                                    reports.objectTypeID = checkedOutObjectVersion.ObjVer.Type;
                                    reports.VaultGuid = updateFile.VaultGuid;
                                    string json = JsonConvert.SerializeObject(reports);

                                    var options112 = new RestClientOptions(ReportsUrl);
                                    var client112 = new RestClient(options112);
                                    var request1112 = new RestRequest("UpdateRecord", Method.Post);
                                    request1112.AddHeader("Content-Type", "application/json");
                                    var body = json;
                                    request1112.AddStringBody(body, RestSharp.DataFormat.Json);
                                    RestResponse response1112 = await client112.ExecuteAsync(request1112);
                                    if (!response1112.IsSuccessful)
                                    {
                                        Console.WriteLine(response1112.Content);
                                    }

                                    try
                                    {
                                        System.IO.File.Delete(path);

                                    }
                                    catch (Exception) { }
                                }
                          

                        }
                  
                        return Ok("Updated file successfully");
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
        [HttpPost("DSSSelfSignPostObjectFile")]
        public async Task<IActionResult> DSSSelfSignPostObjectFile([FromBody] UpdateFile1 updateFile)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReplyLink = this._configuration.GetConnectionString($"ReplyUrl")+$"?objectid={updateFile.objectid}&fileid={updateFile.fileid}&VaultGuid={updateFile.VaultGuid}&UserID={updateFile.UserID}&ClassId={updateFile.ClassId}";
            var dssprofileid = Convert.ToInt64(this._configuration.GetConnectionString("ID") ?? "0");

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
                var vault = mfServerApplication.LogInToVault(updateFile.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, updateFile.objectid);
                    searchConditions.Add(-1, condition);
                }
                //filter pdf only
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetFileValueExpression(MFFileValueType.MFFileValueTypeFileName);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeContains;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, ".pdf");

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
                        updateFile.ClassId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        var pathy = "";
                        var filepath = "";
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                                ID: objectVersion.ObjVer.ID);

                            var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                            if (!checkout)
                            {
                               
                                foreach (ObjectFile objectFile in objectVersion.Files)
                                {
                                    if (objectFile.ID == updateFile.fileid)
                                    {
                                        ReplyLink = ReplyLink + "&versionID=" + objectFile.Version;
                                        pathy = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                                        if (!Directory.Exists(pathy))
                                        {
                                            Directory.CreateDirectory(pathy);
                                        }
                                        var fullpath = Path.Combine(pathy, Guid.NewGuid().ToString());
                                        if (!Directory.Exists(fullpath))
                                        {
                                            Directory.CreateDirectory(fullpath);
                                        }
                                        filepath = Path.Combine(fullpath, DateTime.UtcNow.Ticks.ToString() + "." + objectFile.Extension);
                                        vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, filepath);
                                        var options = new RestClientOptions("https://dsscall.alignsys.tech/api/Docs"); 
                                        var client = new RestClient(options);
                                        var request = new RestRequest("", Method.Post);
                                        request.AlwaysMultipartFormData = true;
                                        request.AddParameter("title", objectVersion.Title);
                                        request.AddParameter("AssignmentDescription", "self sign from Zenfiles");
                                        request.AddParameter("username", "Zenfiles");
                                        if (!updateFile.SignerEmail.Contains(","))
                                        {
                                            request.AddParameter("email", updateFile.SignerEmail);
                                        }
                                      
                                        request.AddParameter("userid", dssprofileid);
                                        request.AddParameter("DocumentGuid", objectFile.FileGUID.TrimStart('{').TrimEnd('}'));
                                        request.AddFile("formFile", filepath);
                                        request.AddParameter("ReplyLink", ReplyLink);
                                        request.AddParameter("VaultGuid", updateFile.VaultGuid);
                                        request.AddParameter("IP", ipaddress);
                                        RestResponse response = await client.ExecuteAsync(request);
                                        if (response.Content == "")
                                        {

                                        }
                                        if (response.IsSuccessful)
                                        {
                                            var myDeserializedClass = JsonConvert.DeserializeObject<PostResponse>(response.Content);


                                            if (myDeserializedClass != null)
                                            {
                                                if (!string.IsNullOrEmpty(myDeserializedClass.docGuid))
                                                {
                                                    List<signers> signer = new List<signers>();
                                                    if(updateFile.SignerEmail.Contains(","))
                                                    {
                                                        foreach (var signerd in updateFile.SignerEmail.Split(','))
                                                        {
                                                            if(!string.IsNullOrEmpty(signerd.Trim()))
                                                            signer.Add(new CargenDss.Models.signers { email = signerd.Trim(), Authenticate = false });
                                                        }
                                                    }
                                                    else
                                                    {
                                                        signer.Add(new CargenDss.Models.signers { email = updateFile.SignerEmail, Authenticate = false });
                                                    }
                                                    SignersClass signers = new SignersClass();

                                                    signers.signers = signer;
                                                    signers.documentid = myDeserializedClass.docGuid;
                                                   
                                                    var signersjson = JsonConvert.SerializeObject(signers);


                                                    var options2 = new RestClientOptions("https://dsscall.alignsys.tech");
                                                    var client2 = new RestClient(options2);
                                                    var request2 = new RestRequest("/api/SelfSign/AddSelfSign", Method.Post);
                                                    request2.AddHeader("Content-Type", "application/json");
                                                    var body = signersjson;
                                                    request2.AddStringBody(body, RestSharp.DataFormat.Json);
                                                    RestResponse response2 = await client2.ExecuteAsync(request2);
                                                    if (response2.IsSuccessful)
                                                    {
                                                        string signersguid = "";
                                                        var t = response2.Content;
                                                        var myDeserializedClassp = JsonConvert.DeserializeObject<selfsign_respo>(response2.Content);
                                                     
                                                        var userguid = signersguid;
                                                        List<string> items = new List<string>();
                                                        var newurl = $"https://dss.alignsys.tech/mail/sign/{myDeserializedClassp.fileName}/{myDeserializedClassp.fileName.Split("_")[0]}";

                                                        FileLink fileLink = new FileLink();
                                                        fileLink.Filelink = newurl;


                                                        try
                                                        {
                                                            System.IO.File.Delete(filepath);
                                                            System.IO.Directory.Delete(fullpath);
                                                        }
                                                        catch (Exception ex)
                                                        {

                                                        }
                                                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                                                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                                                        if (!Directory.Exists(path))
                                                            Directory.CreateDirectory(path);

                                                        var filepathd = Path.Combine(path, updateFile.VaultGuid + "_" + 0 + "_" + updateFile.objectid.ToString() + "_" + updateFile.UserID.ToString() + ".txt");

                                                        string content = updateFile.VaultGuid + "_" + 0 + "_" + updateFile.objectid + "_" + updateFile.UserID;
                                                        System.IO.File.WriteAllText(filepathd, content);

                                                        return Ok(fileLink);
                                                    }
                                                    else
                                                    {
                                                        return BadRequest("Failed to retrieve data");
                                                    }
                                                }
                                            }
                                        }


                                    }
                                }

                            }

                        }
                        return BadRequest("Server could not post");
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
        [HttpPost("DSSPostObjectFile")]
        public async Task<IActionResult> DSSPostObjectFile([FromBody] UpdateFile1 updateFile)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReplyLink = this._configuration.GetConnectionString($"ReplyUrl") + $"?objectid={updateFile.objectid}&fileid={updateFile.fileid}&VaultGuid={updateFile.VaultGuid}&UserID={updateFile.UserID}&ClassId={updateFile.ClassId}";
            var dssprofileid = Convert.ToInt64(this._configuration.GetConnectionString("ID") ?? "0");

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
                var vault = mfServerApplication.LogInToVault(updateFile.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, updateFile.objectid);
                    searchConditions.Add(-1, condition);
                }
                //filter pdf only
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetFileValueExpression(MFFileValueType.MFFileValueTypeFileName);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeContains;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, ".pdf");

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
                        updateFile.ClassId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        var pathy = "";
                        var filepath = "";
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                                ID: objectVersion.ObjVer.ID);

                            var checkout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                            if (!checkout)
                            {

                                foreach (ObjectFile objectFile in objectVersion.Files)
                                {
                                    if (objectFile.ID == updateFile.fileid)
                                    {
                                        ReplyLink = ReplyLink + "&versionID=" + objectFile.Version;
                                        pathy = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                                        if (!Directory.Exists(pathy))
                                        {
                                            Directory.CreateDirectory(pathy);
                                        }
                                        var fullpath = Path.Combine(pathy, Guid.NewGuid().ToString());
                                        if (!Directory.Exists(fullpath))
                                        {
                                            Directory.CreateDirectory(fullpath);
                                        }
                                        filepath = Path.Combine(fullpath, DateTime.UtcNow.Ticks.ToString() + "." + objectFile.Extension);
                                        vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, filepath);
                                        var options = new RestClientOptions("https://dsscall.alignsys.tech/api/Docs")
                                        ;
                                        var client = new RestClient(options);
                                        var request = new RestRequest("", Method.Post);
                                        request.AlwaysMultipartFormData = true;
                                        request.AddParameter("title", objectVersion.Title);
                                        request.AddParameter("AssignmentDescription", "self sign from Zenfiles");
                                        request.AddParameter("username", "Zenfiles");
                                        if (!updateFile.SignerEmail.Contains(","))
                                        {
                                            request.AddParameter("email", updateFile.SignerEmail);
                                        }

                                        request.AddParameter("userid", dssprofileid);
                                        request.AddParameter("DocumentGuid", objectFile.FileGUID.TrimStart('{').TrimEnd('}'));
                                        request.AddFile("formFile", filepath);
                                        request.AddParameter("ReplyLink", ReplyLink);
                                        request.AddParameter("VaultGuid", updateFile.VaultGuid);
                                        request.AddParameter("IP", ipaddress);
                                        RestResponse response = await client.ExecuteAsync(request);
                                        if (response.Content == "")
                                        {

                                        }
                                        if (response.IsSuccessful)
                                        {
                                            var myDeserializedClass = JsonConvert.DeserializeObject<PostResponse>(response.Content);


                                            if (myDeserializedClass != null)
                                            {
                                                if (!string.IsNullOrEmpty(myDeserializedClass.docGuid))
                                                {
                                                    List<signers> signer = new List<signers>();
                                                    if (updateFile.SignerEmail.Contains(","))
                                                    {
                                                        foreach (var signerd in updateFile.SignerEmail.Split(','))
                                                        {
                                                            if (!string.IsNullOrEmpty(signerd.Trim()))
                                                                signer.Add(new CargenDss.Models.signers { email = signerd.Trim(), Authenticate = false });
                                                        }
                                                    }
                                                    else
                                                    {
                                                        signer.Add(new CargenDss.Models.signers { email = updateFile.SignerEmail, Authenticate = false });
                                                    }
                                                    SignersClass signers = new SignersClass();

                                                    signers.signers = signer;
                                                    signers.documentid = myDeserializedClass.docGuid;

                                                    var signersjson = JsonConvert.SerializeObject(signers);


                                                    var options1 = new RestClientOptions("https://dsscall.alignsys.tech/api/signers");

                                                    var client1 = new RestClient(options1);
                                                    var request1 = new RestRequest("", Method.Post);
                                                    request1.AddHeader("Content-Type", "application/json");
                                                    var body = signersjson;
                                                    request1.AddStringBody(body, RestSharp.DataFormat.Json);
                                                    RestResponse response1 = await client1.ExecuteAsync(request1);
                                                    if (response1.Content == "")
                                                    {

                                                    }
                                                    if (response1.IsSuccessful)
                                                    {

                                                        var options2 = new RestClientOptions("https://dsscall.alignsys.tech")
                                                       ;
                                                        var client2 = new RestClient(options2);
                                                        var request2 = new RestRequest("/api/signers/" + myDeserializedClass.docGuid, Method.Get);
                                                        RestResponse response2 = await client2.ExecuteAsync(request2);
                                                        if (response2.IsSuccessful)
                                                        {
                                                            string signersguid = "";
                                                            var myDeserializedClassp = JsonConvert.DeserializeObject<List<DSSPostResp>>(response2.Content);
                                                            foreach (var item in myDeserializedClassp)
                                                            {
                                                                if (item.currentsigner)
                                                                {
                                                                    signersguid = item.uid;
                                                                }
                                                            }

                                                            var userguid = signersguid;
                                                            List<string> items = new List<string>();
                                                            var newurl = $"https://dss.alignsys.tech/mail/sign/{userguid + "_" + myDeserializedClass.docGuid + ".pdf"}/{userguid}";

                                                            FileLink fileLink = new FileLink();
                                                            fileLink.Filelink = newurl;


                                                            try
                                                            {
                                                                System.IO.File.Delete(filepath);
                                                                System.IO.Directory.Delete(fullpath);
                                                            }
                                                            catch (Exception ex)
                                                            {

                                                            }
                                                            try
                                                            {
                                                                System.IO.File.Delete(filepath);
                                                                System.IO.Directory.Delete(fullpath);
                                                            }
                                                            catch (Exception ex)
                                                            {

                                                            }
                                                            var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                                                            var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                                                            if (!Directory.Exists(path))
                                                                Directory.CreateDirectory(path);

                                                            var filepathd = Path.Combine(path, updateFile.VaultGuid + "_" + 0 + "_" + updateFile.objectid.ToString() + "_" + updateFile.UserID.ToString() + ".txt");

                                                            string content = updateFile.VaultGuid + "_" + 0 + "_" + updateFile.objectid + "_" + updateFile.UserID;
                                                            System.IO.File.WriteAllText(filepathd, content);

                                                            return Ok(fileLink);
                                                        }
                                                        else
                                                        {
                                                            return BadRequest("Failed to retrieve data");
                                                        }


                                                    }
                                                }
                                            }
                                        }


                                    }
                                }

                            }

                        }
                        return BadRequest("Server could not post");
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
        [HttpGet("GetObjectFiles/{VaultGuid}/{ObjectId}/{ClassId}")]
        public async Task<IActionResult> GetObjectFilesAsync(int ObjectId,string VaultGuid,int ClassId)
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
                    var classIds = new[] {ClassId };

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
                    try
                    {
                        List<ObjectFileResp> objectFileResp = new List<ObjectFileResp>();

                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            foreach (ObjectFile objectFile in objectVersion.Files)
                            {
                                var options = new RestClientOptions($"https://dsscall.alignsys.tech/api/Docs/Getdocument/{objectFile.FileGUID.TrimStart('{').TrimEnd('}')}")
                                       ;
                                var client = new RestClient(options);
                                var request = new RestRequest("", Method.Get);
                                RestResponse response = await client.ExecuteAsync(request);
                                var reportguid = "";
                                if (response.IsSuccessful)
                                {
                                    CargenDss.Models.Root myDeserializedClass = JsonConvert.DeserializeObject<CargenDss.Models.Root>(response.Content);
                                    if (myDeserializedClass != null)
                                    {
                                        if (myDeserializedClass.signedComplete)
                                        {
                                            reportguid = myDeserializedClass.docGuid;
                                        }
                                    }

                                }
                                objectFileResp.Add(new ObjectFileResp { extension = objectFile .Extension, fileID = objectFile .ID, fileTitle= objectFile .Title, fileversion= objectFile .Version, reportGuid=reportguid});
                            }

                        }
                        if(objectFileResp.Count <= 0)
                        {
                            return NotFound("Object does not contain any file");
                        }
                        return Ok(objectFileResp);
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
        [HttpGet("DownloadFile/{VaultGuid}/{ObjectId}/{ClassId}/{fileID}")]
        public IActionResult DownloadFile(int ObjectId, string VaultGuid,int fileID,int ClassId)
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
                    try
                    {
                        var filepath = "";
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            foreach (ObjectFile objectFile in objectVersion.Files)
                            {
                                if (objectFile.ID == fileID)
                                {
                                    filepath = Path.Combine(Directory.GetCurrentDirectory(), "Files", Guid.NewGuid().ToString() + "." + objectFile.Extension);
                                    //vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, filepath);

                                }
                            }

                        }
                        if (string.IsNullOrEmpty(filepath))
                        {
                            return NotFound("Could not find a file with that ID");
                        }

                        //byte[] AsBytes =System.IO.File.ReadAllBytes(filepath);
                        //String AsBase64String = Convert.ToBase64String(AsBytes);

                        FilepathResp filepathResp = new FilepathResp();
                       // filepathResp.base64 = AsBase64String;
                        filepathResp.extension = System.IO.Path.GetExtension(filepath);
                        try
                        {
                            System.IO.File.Delete(filepath);
                        }
                        catch(Exception ex)
                        {

                        }
                        return Ok(filepathResp);
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
        [HttpGet("DownloadActualFile/{VaultGuid}/{ObjectId}/{ClassId}/{fileID}")]
        public IActionResult DownloadActualFile(int ObjectId, string VaultGuid, int fileID, int ClassId)
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
                    try
                    {
                        var filepath = "";
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            foreach (ObjectFile objectFile in objectVersion.Files)
                            {
                                if (objectFile.ID == fileID)
                                {
                                    filepath = Path.Combine(Directory.GetCurrentDirectory(), "Files", Guid.NewGuid().ToString() + "." + objectFile.Extension);
                                    vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, filepath);

                                }
                            }

                        }
                        if (string.IsNullOrEmpty(filepath))
                        {
                            return NotFound("Could not find a file with that ID");
                        }

                        //Load the PDF document
                        FileStream docStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                        PdfLoadedDocument doc = new PdfLoadedDocument(docStream);
                       
                        MemoryStream stream = new MemoryStream();
                        //Save the document as stream
                        doc.Save(stream);
                        //Close the document
                        doc.Close(true);
                        //If the position is not set to '0' then the PDF will be empty.
                        stream.Position = 0;
                        //Download the PDF document in the browser.
                        FileStreamResult fileStreamResult = new FileStreamResult(stream, "application/pdf");
                        fileStreamResult.FileDownloadName = "Sample.pdf";
                        return fileStreamResult;
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
        [HttpGet("DownloadOtherFiles")]
        public async Task<IActionResult> DownloadOtherFilesAsync([FromQuery]otherfile otherfile)
        {
            if (string.IsNullOrEmpty( otherfile.VaultGuid) | otherfile.ClassId < 0 | otherfile.fileID <= 0 | otherfile.ObjectId <= 0)
            {
                return BadRequest("Kindly provide the correct data");
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
               
                var vault = mfServerApplication.LogInToVault(otherfile.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, otherfile.ObjectId);
                    searchConditions.Add(-1, condition);
                }
                //filter class id
                {
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] { otherfile.ClassId };

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
                    searchConditions.Add(-1,searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        var filepath = "";
                        string filename = "";
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            foreach (ObjectFile objectFile in objectVersion.Files)
                            {
                                if (objectFile.ID == otherfile.fileID)
                                {
                                    filepath = Path.Combine(Directory.GetCurrentDirectory(), "Files", Guid.NewGuid().ToString() + "." + objectFile.Extension);
                                  
                                    vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, filepath);
                                    filename = CleanFilename(objectFile.Title)+"."+objectFile.Extension;
                                }
                            }

                        }
                        if (string.IsNullOrEmpty(filepath))
                        {
                            return NotFound("Could not find a file with that ID");
                        }
                        var filePath = filepath;
                        var memory = new MemoryStream();
                        using (var stream = new FileStream(filePath, FileMode.Open))
                        {
                            await stream.CopyToAsync(memory);
                        }
                        memory.Position = 0;
                        try
                        {
                            System.IO.File.Delete(filepath);
                        }
                        catch (Exception ex)
                        {

                        }
                        return File(memory, "application/octet-stream", Path.GetFileName(filename));

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
        [HttpPost("ConvertToPdf")]
        public IActionResult ConvertToPdf( Convertmodal convertmodal)
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
                var vault = mfServerApplication.LogInToVault(convertmodal.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, convertmodal.ObjectId);
                    searchConditions.Add(-1, condition);
                }
                //filter class id
                {
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] { convertmodal.ClassId };

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
                    foreach(ObjectVersion objectVersion in searchResults)
                    {
                        try
                        {
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: objectVersion.OriginalObjID.Type,
                                ID: objectVersion.ObjVer.ID);
                            var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                            vault.ObjectFileOperations.ConvertToPDF(checkedOutObjectVersion.ObjVer, convertmodal.fileID, convertmodal.SeparateFile, convertmodal.OverWriteOriginal, true, true);
                            
                            var date = DateTime.UtcNow;
                            var lastModifiedBy = new TypedValue();
                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, convertmodal.UserID);
                            var lastModifiedDate = new TypedValue();
                            lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                            vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                            (
                                checkedOutObjectVersion.ObjVer,
                                true, lastModifiedBy,
                                true, lastModifiedDate
                            );


                            vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                            return Ok("Successfully Replaced");
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(ex.Message);
                        }

                    }
                    return NotFound("Could not find any object or file with those ids");
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
        [HttpGet("LinkedObjects/{VaultGuid}/{ObjectTypeId}/{ObjectId}/{ClassId}/{UserID}")]
        public IActionResult LinkedObjects(int ObjectId, string VaultGuid, int ObjectTypeId, int UserID, int ClassId)
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
                        ObjectTypeId);

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
                // Add a condition that the customer must be based in the UK.
                {


                    // // Create the condition.
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
                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    List<LinkedObjectmodel> linkedObjectmodel = new List<LinkedObjectmodel>();

                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                        List<Class1> class1 = new List<Class1>();
                        var props = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);
                        foreach (PropertyValueForDisplay property in props)
                        {
                            if (property.PropertyDef > 101)
                            {
                                if (property.DataType == MFDataType.MFDatatypeLookup)
                                {
                                    var propname = "";
                                    var defaultprop = -1;

                                    Lookups lookups = new Lookups();
                                    foreach (Lookup lookup in property.PropertyValue.Value.GetValueAsLookups())
                                    {
                                        var objecttype1 = _cacheObjects.GetObjectTypes(vault);
                                        if (objecttype1.Count > 0)
                                        {
                                            var objecttype = objecttype1.FirstOrDefault(m => m.ObjectType.ID == lookup.ObjectType);
                                            if (objecttype != null)
                                                if (!lookup.Deleted && objecttype.ObjectType.RealObjectType)
                                                {
                                                    propname = objecttype.ObjectType.NamePlural;
                                                    lookups.Add(-1, lookup);
                                                    defaultprop = objecttype.ObjectType.DefaultPropertyDef;
                                                }
                                        }

                                       
                                    }
                                    if (property.PropertyDef == defaultprop)
                                    {
                                        if (lookups.Count > 0)
                                            class1.Add(new Class1 { proname = propname, propid = property.PropertyDef, values = lookups, type = property.DataType });

                                    }
                                    else
                                    {
                                        if (lookups.Count > 0)
                                            class1.Add(new Class1 { proname = property.PropertyDefName, propid = property.PropertyDef, values = lookups, type = property.DataType });

                                    }
                                }
                                else if (property.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                                {
                                    var propname = "";
                                    var defaultprop = -1;

                                    Lookups lookups = new Lookups();
                                    foreach (Lookup lookup in property.PropertyValue.Value.GetValueAsLookups())
                                    {
                                        var objecttype1 = _cacheObjects.GetObjectTypes(vault);
                                        if (objecttype1.Count > 0)
                                        {
                                            var objecttype = objecttype1.FirstOrDefault(m => m.ObjectType.ID == lookup.ObjectType);
                                            if (objecttype != null)
                                                if (!lookup.Deleted && objecttype.ObjectType.RealObjectType)
                                                {
                                                    propname = objecttype.ObjectType.NamePlural;
                                                    lookups.Add(-1, lookup);
                                                    defaultprop = objecttype.ObjectType.DefaultPropertyDef;
                                                }
                                        }

                                    }
                                    if (property.PropertyDef == defaultprop)
                                    {
                                        if (lookups.Count > 0)
                                            class1.Add(new Class1 { proname = propname, propid = property.PropertyDef, values = lookups, type = property.DataType });

                                    }
                                    else
                                    {
                                        if (lookups.Count > 0)
                                            class1.Add(new Class1 { proname = property.PropertyDefName, propid = property.PropertyDef, values = lookups, type = property.DataType });

                                    }
                                }
                            }
                        }
                        HashSet<string> strings = new HashSet<string>();

                        var st = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);
                        List<Objectsearchresponse> objectsearchresponsed = new List<Objectsearchresponse>();
                        if (st.Count > 0)
                        {
                            foreach (ObjectVersion objectVersion1 in st)
                            {
                                Objectsearchresponse objectsearchresponseps = new Objectsearchresponse();
                                UserPermission userPermission = new UserPermission();
                              
                                #region setting permission
                                {
                                    var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion1.ObjVer);

                                    if (perm.CustomACL)
                                    {
                                        try
                                        {
                                            userPermission = _permission.ObjectPermission(vault, UserID, objectVersion1.ObjVer.Type);
                                            if (userPermission.ReadPermission)
                                            {
                                                userPermission = _permission.ClassPermission(vault, UserID, objectVersion1.Class);
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
                                                            if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                            {
                                                                userPermission.ReadPermission = true;
                                                            }
                                                            if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                            {
                                                                userPermission.EditPermission = true;
                                                            }
                                                            if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                            {
                                                                userPermission.AttachObjectsPermission = true;
                                                            }
                                                            if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
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
                                            var username = _cacheObjects.UserAccounts(vault)?.FirstOrDefault(m=>m.ID==UserID);
                                            if(username!= null)
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
                                if(userPermission!=null)
                                if (userPermission.ReadPermission)
                                {
                                    var classname = "";

                                    var propfordisplay = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion1.ObjVer);
                                    foreach (PropertyValueForDisplay propertyValueForDisplay in propfordisplay)
                                    {
                                        if (propertyValueForDisplay.PropertyDef == 100)
                                        {
                                            classname = propertyValueForDisplay.PropertyValue.Value.DisplayValue;
                                        }
                                    }

                                    var objecttype = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == objectVersion1.ObjVer.Type);
                                    if(objecttype!=null)
                                        {
                                            string fileextension = "";
                                            int fileid = 0;
                                            if (objectVersion1.SingleFile)
                                            {
                                                fileextension = objectVersion1.Files[1].Extension;
                                                fileid = objectVersion1.Files[1].ID;
                                            }
                                            var stpr = vault.ObjectOperations.GetRelationshipsEx(objectVersion1.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                                            strings.Add(objecttype.ObjectType.NamePlural);
                                            objectsearchresponseps.ClassID = objectVersion1.Class;
                                            objectsearchresponseps.VersionId = objectVersion1.ObjVer.Version;
                                            objectsearchresponseps.Title = objectVersion1.Title;
                                            objectsearchresponseps.CreatedUtc = objectVersion1.CreatedUtc;
                                            objectsearchresponseps.ObjectID = objectVersion1.ObjVer.Type;
                                            objectsearchresponseps.ClassID = objectVersion1.Class;
                                            objectsearchresponseps.DisplayID = objectVersion1.DisplayID;
                                            objectsearchresponseps.IsSingleFile = objectVersion1.SingleFile;
                                            objectsearchresponseps.LastModifiedUtc = objectVersion1.LastModifiedUtc;
                                            objectsearchresponseps.userPermission = userPermission;
                                            objectsearchresponseps.ObjectTypeName = objecttype.ObjectType.NamePlural;
                                            objectsearchresponseps.ClassTypeName = classname;
                                            objectsearchresponseps.id = objectVersion1.ObjVer.ID;
                                            objectsearchresponseps.IsSingleFile = objectVersion1.SingleFile;
                                            objectsearchresponseps.IsDeleted = objectVersion1.Deleted;
                                            objectsearchresponseps.FileExtension = fileextension;
                                            objectsearchresponseps.FileId = fileid;
                                            objectsearchresponseps.HasRelationship = stpr.Count > 0;
                                            objectsearchresponseps.checkoutusername = objectVersion1.CheckedOutToUserName;
                                            objectsearchresponseps.IsCheckedOut = objectVersion1.CheckedOutTo > 0;
                                        }
                                       
                                }
                                else
                                {
                                    objectsearchresponseps = new Objectsearchresponse();
                                }
                                if (objectsearchresponseps != null)
                                    objectsearchresponsed.Add(objectsearchresponseps);
                            }
                            if (objectsearchresponsed.Count > 0)
                            {
                                foreach (var item in strings)
                                {
                                    linkedObjectmodel.Add(new LinkedObjectmodel { PropertyName = item, items = objectsearchresponsed.Where(m => m.ObjectTypeName == item).ToList() });
                                }
                            }
                        }
                    }
                    if (linkedObjectmodel.Count <= 0)
                    {
                        return NotFound();
                    }
                    else
                    {
                        return Ok(linkedObjectmodel);
                    }
                }
                else
                {
                    return BadRequest("Can't find the object");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetSpecificObject/{VaultGuid}/{ObjectTypeId}/{ObjectId}/{ClassId}/{UserID}")]
        public IActionResult GetSpecificObject(int ObjectId, string VaultGuid, int ObjectTypeId, int UserID, int ClassId)
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
                        ObjectTypeId);

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
                // Add a condition that the customer must be based in the UK.
                {


                    // // Create the condition.
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
                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
                        if (searchResults.Count > 0)
                        foreach (ObjectVersion objectVersion in searchResults)
                            {
                                try
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
                                    string fileextension = "";
                                    int fileid = 0;
                                    if (objectVersion.SingleFile)
                                    {
                                        fileextension = objectVersion.Files[1].Extension;
                                        fileid = objectVersion.Files[1].ID;
                                    }
                                    var st = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);

                                    Response.Add(new Objectsearchresponse { DisplayID = objectVersion.DisplayID, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = userPermission, ClassTypeName = classname, VersionId = objectVersion.ObjVer.Version, ObjectTypeName = objecttype.NameSingular, CreatedUtc = objectVersion.CreatedUtc, LastModifiedUtc = objectVersion.LastModifiedUtc, IsSingleFile = objectVersion.SingleFile, IsCheckedOut = objectVersion.CheckedOutTo > 0, checkoutuserid = objectVersion.CheckedOutTo, IsDeleted = objectVersion.Deleted, HasRelationship = st.Count > 0, FileExtension = fileextension, FileId = fileid, checkoutusername = objectVersion.CheckedOutToUserName });
                                }
                                catch
                                {

                                }
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
                    return BadRequest("Can't find the object");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("RemoveFile/{VaultGuid}/{ObjectId}/{fileID}/{UserID}")]
        public IActionResult RemoveFile(int ObjectId, string VaultGuid, int fileID,int UserID)
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
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        var filepath = "";
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: objectVersion.OriginalObjID.Type,
                                ID: objectVersion.ObjVer.ID);
                            var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                            foreach (ObjectFile objectFile in checkedOutObjectVersion.Files)
                            {
                                if (objectFile.ID == fileID)
                                {
                                   vault.ObjectFileOperations.RemoveFile(checkedOutObjectVersion.ObjVer, objectFile.FileVer);
                               
                                }
                            }

                            var date = DateTime.UtcNow;
                            var lastModifiedBy = new TypedValue();
                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, UserID);
                            var lastModifiedDate = new TypedValue();
                            lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                            vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                            (
                                checkedOutObjectVersion.ObjVer,
                                true, lastModifiedBy,
                                true, lastModifiedDate
                            );

                            vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);
                           
                        }
                        return Ok("Successfully removed file from the object");
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
        [HttpGet("GetObjectViewProps/{VaultGuid}/{ObjectId}/{ObjectTypeId}/{UserID}")]
        public async Task<IActionResult> GetObjectViewProps(int ObjectId, string VaultGuid,int ObjectTypeId, int UserID)
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
                        ObjectTypeId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    forlogs objectsearchresponse = new forlogs();
                    try
                    {
                        List<updateprop1> props = new List<updateprop1>();
                        List<updateprop1> props1 = new List<updateprop1>();
                        List<property3> hiddenlist = new List<property3>();
                        var ischeckout = false;
                        var objecttypeid = -1;
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            List<int> ints = new List<int>();
                            var objecttype = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == objectVersion.ObjVer.Type)?.ObjectType;
                            if(objecttype!=null)                            
                            if(objecttype.External)
                            {
                                    var Test = objecttype.ReadOnlyPropertiesDuringInsert;
                                    foreach( var property in Test)
                                    {
                                        ints.Add((int)property);
                                    }
                                    ints.Add(100);
                            }
                          
                                List<Associatedclassidentity> associatedclassids = new List<Associatedclassidentity>();
                                var propsssd = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m => m.ID == objectVersion.Class).AssociatedPropertyDefs;

                                foreach (AssociatedPropertyDef associatedPropertyDef in propsssd)
                                {
                                    if (associatedPropertyDef.Required)
                                    {
                                        associatedclassids.Add(new Associatedclassidentity { id = associatedPropertyDef.PropertyDef, isrequired = associatedPropertyDef.Required });
                                    }
                                }
                                objecttypeid = objectVersion.ObjVer.Type;
                                var objID = new MFilesAPI.ObjID();
                                objID.SetIDs(
                                    ObjType: objectVersion.ObjVer.Type,
                                    ID: objectVersion.ObjVer.ID);
                                ischeckout = vault.ObjectOperations.IsObjectCheckedOut(objID);
                                objectsearchresponse.id = objectVersion.ObjVer.ID;
                                objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                                objectsearchresponse.Title = objectVersion.Title;
                                objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                                objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                                objectsearchresponse.ClassID = objectVersion.Class;
                                objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                                objectsearchresponse.VaultGuid = VaultGuid;
                                objectsearchresponse.DisplayID = objectVersion.DisplayID;
                                objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                                UserPermission userPermission = new UserPermission();
                                var found = false;
                                #region setting permission
                            {
                                    var perm = vault.ObjectOperations.GetObjectPermissions(objectVersion.ObjVer);
                                    try
                                    {
                                        AccessControlList acl = perm.AccessControlList; // Display the ACL details
                                        foreach (AccessControlEntry accessControlEntry1 in perm.AccessControlList)
                                        {
                                           

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
                                                    if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow")) || (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                    {
                                                        userPermission.ReadPermission = true;
                                                    }
                                                    if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                    {
                                                        userPermission.EditPermission = true;
                                                    }
                                                    if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                    {

                                                        userPermission.AttachObjectsPermission = true;
                                                    }
                                                    if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
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
                                objectsearchresponse.userPermission = userPermission;
                                try
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
                                    var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                    objectsearchresponse.ClassTypeName = classname;
                                    objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                                }
                                catch
                                {

                                }

                                List<workflowstatepropbehave> workflowstatepropbehave = new List<workflowstatepropbehave>();

                                try
                                {
                                    var workflow = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 38);
                                    var workflowstate = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 39);
                                    foreach (Lookup lookup in workflow.Value.GetValueAsLookups())
                                    {
                                        workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "38", workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias = "", propertyvalue = lookup.Item.ToString(), propertytype = workflow.Value.DataType.ToString() });
                                    }
                                    foreach (Lookup lookup in workflowstate.Value.GetValueAsLookups())
                                    {
                                        workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "39", workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias = "", propertyvalue = lookup.Item.ToString(), propertytype = workflow.Value.DataType.ToString() });

                                    }
                                }
                                catch (Exception ex)
                                {
                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "38", workflowstateguid = "", workflowstateid = "", workflowstatealias = "", propertytype = MFDataType.MFDatatypeLookup.ToString(), propertyvalue = "" });
                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "39", workflowstateguid = "", workflowstateid = "", workflowstatealias = "", propertytype = MFDataType.MFDatatypeLookup.ToString(), propertyvalue = "" });
                                }
                                var propspd = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);

                                foreach (PropertyValueForDisplay properties in propspd)
                                {
                                    if (vault.ClassOperations.GetObjectClass(objectVersion.Class).NamePropertyDef != 0)
                                    {

                                        if (properties.PropertyDef > 0)
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
                                                        bool allowadding = false;
                                                        bool realobject = false;
                                                        int objectorvaluelistid = 0;
                                                        List<lookupvalues> values = new List<lookupvalues>();
                                                        var valuesd = properties.PropertyValue.Value.GetValueAsLookups();
                                                        int id = 0;
                                                        foreach (Lookup lookup in valuesd)
                                                        {
                                                            values.Add(new lookupvalues { ID = lookup.Item.ToString(), Title = properties.DisplayValue.Split(";")[id] });
                                                            id += 1;
                                                        }
                                                        var propertypd = vault.PropertyDefOperations.GetPropertyDefAdmin(properties.PropertyDef);

                                                        var valuelistt = vault.ValueListOperations.GetValueList(propertypd.PropertyDef.ValueList);
                                                        if (valuelistt.AllowAdding)
                                                        {
                                                            allowadding = true;
                                                            var permd = _permission.Valuelist(vault, UserID, valuelistt.ID);
                                                            if (permd.EditPermission)
                                                            {
                                                                if (valuelistt.RealObjectType)
                                                                {
                                                                    realobject = true;
                                                                    objectorvaluelistid = valuelistt.OwnerType;
                                                                }
                                                                else
                                                                {

                                                                }
                                                            }

                                                        }
                                                        try
                                                        {
                                                            if (properties.DataType == MFDataType.MFDatatypeLookup)
                                                            {
                                                                if (!string.IsNullOrEmpty(properties.DisplayValue))
                                                                {
                                                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                                                                    {
                                                                        propertytype = properties.DataType.ToString(),
                                                                        propertyvalue = properties.PropertyValue.Value.GetLookupID().ToString(),
                                                                        propid = properties.PropertyDef.ToString(),
                                                                        workflowstatealias = "",
                                                                        workflowstateguid = properties.PropertyValue.Value.GetValueAsLookup().ItemGUID,

                                                                    });
                                                                }
                                                                else
                                                                {
                                                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                                                                    {
                                                                        propertytype = properties.DataType.ToString(),
                                                                        propertyvalue = "".ToString(),
                                                                        propid = properties.PropertyDef.ToString(),
                                                                        workflowstatealias = "",
                                                                        workflowstateguid = "",

                                                                    });
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {

                                                        }

                                                        // Try to find item
                                                        var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);

                                                        props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = false, userPermission = perm, AllowAdding = allowadding, objectTypeVL = valuelistt.RealObjectType, TypeID = valuelistt.ID, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

                                                    }
                                                    else
                                                    {
                                                        var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);

                                                        props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = false, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

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
                                                            values.Add(new lookupvalues { ID = lookup.Item.ToString(), Title = lookup.DisplayValue });
                                                        }
                                                        try
                                                        {
                                                            if (properties.DataType == MFDataType.MFDatatypeLookup)
                                                            {
                                                                if (!string.IsNullOrEmpty(properties.DisplayValue))
                                                                {
                                                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                                                                    {
                                                                        propertytype = properties.DataType.ToString(),
                                                                        propertyvalue = properties.PropertyValue.Value.GetValueAsLookup().ToString(),
                                                                        propid = properties.PropertyDef.ToString(),
                                                                        workflowstatealias = "",
                                                                        workflowstateguid = properties.PropertyValue.Value.GetValueAsLookup().ItemGUID,

                                                                    });
                                                                }
                                                                else
                                                                {
                                                                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                                                                    {
                                                                        propertytype = properties.DataType.ToString(),
                                                                        propertyvalue = "".ToString(),
                                                                        propid = properties.PropertyDef.ToString(),
                                                                        workflowstatealias = "",
                                                                        workflowstateguid = "",

                                                                    });
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {

                                                        }
                                                        var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);

                                                        props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = true, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

                                                    }
                                                    else
                                                    {
                                                        var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);
                                                        props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = true, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

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
                                                    var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);

                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = false, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

                                                }
                                                else
                                                {
                                                    var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);

                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = false, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

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
                                                    var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);

                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = values, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = true, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

                                                }
                                                else
                                                {
                                                    var result = associatedclassids.FirstOrDefault(x => x.id == properties.PropertyDef);
                                                    props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = result != null ? result.isrequired : properties.PropertyDef == 100, IsAutomatic = false, userPermission = perm, DisplayValue = properties.DisplayValue, Alias = property.SemanticAliases.Value, PropGuid = property.PropertyDef.GUID });

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
                                IMeta meta = new MetaImplement();
                            var classsp = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m => m.ID == objectVersion.Class);
                            var tpp = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == classsp.ObjectType);
                            if (classsp != null && tpp != null)
                            {
                                var items = meta.behaveprops(objectVersion.ObjVer.Type.ToString(), tpp.SemanticAliases.Value, vault.ClassOperations.GetObjectClassAdmin(classsp.ID).SemanticAliases.Value, objectVersion.Class.ToString(), workflowstatepropbehave, VaultGuid);

                                var itemss = items.Where(m => m.IsHidden && !m.IsRequired).OrderBy(m => m.Property);

                                foreach (var hidden in itemss)
                                {
                                    var t = props.Where(m => m.id.ToString() != hidden.Property && m.Alias != hidden.Property).ToList();
                                    props = t;
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
                                foreach (var item in ints)
                                {
                                    // Option 1: Update the property directly
                                    var itemToUpdate = props.FirstOrDefault(i => i.id == item);
                                    if (itemToUpdate != null)
                                    {
                                        itemToUpdate.IsAutomatic = true;
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


                        RecentModel recentModel = new RecentModel();
                        recentModel.Title = objectsearchresponse.Title;
                        recentModel.VaultGuid = objectsearchresponse.VaultGuid;
                        recentModel.TimeStamp = DateTime.Now;
                        recentModel.ObjectID = objectsearchresponse.ObjectID;
                        recentModel.ObjectTypeName = objectsearchresponse.ObjectTypeName;
                        recentModel.Id = objectsearchresponse.id;
                        recentModel.ClassID = objectsearchresponse.ClassID;
                        recentModel.ClassTypeName = objectsearchresponse.ClassTypeName;
                        recentModel.UserID = UserID;
                        recentModel.DisplayID = objectsearchresponse.DisplayID;

                        await _repository.AddOrUpdateAsync(recentModel);

                        if (ischeckout)
                        {
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);

                            DirectoryInfo dir = new DirectoryInfo(path);
                            FileInfo[] files = dir.GetFiles(VaultGuid + "_" + objecttypeid + "_" + ObjectId + "*.txt");

                            foreach (FileInfo file in files)
                            {
                                string content = System.IO.File.ReadAllText(file.FullName);
                                if (!string.IsNullOrEmpty(content.Trim()))
                                {
                                    var itds = content.Split("_");
                                    int userid = Convert.ToInt32(itds[3]);
                                    var user = vault.UserOperations.GetUserAccount(userid).LoginName;
                                    var person = props1.FirstOrDefault(p => p.id == 23);
                                    if (person != null)
                                    {
                                        person.Value = user; // Updates Bob's age
                                    }
                                }
                            }

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
        [HttpPut("UpdateObjectProps")]
        public async Task<IActionResult> UpdateObjectPropsAsync([FromBody] UpdateObjectProps updateObjectProps)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";


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
                var vault = mfServerApplication.LogInToVault(updateObjectProps.VaultGuid);
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
                        updateObjectProps.Objectypeid);

                    // Add the condition to the collection.
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
                        updateObjectProps.classid);

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
                //add search with internal id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, updateObjectProps.objectid);
                    searchConditions.Add(-1, condition);
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
                            var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                        try
                        {

                            MFilesAPI.PropertyValues properties = new MFilesAPI.PropertyValues();
                            foreach (var property in updateObjectProps.props)
                            {
                                if (property.Datatype == "MFDatatypeMultiSelectLookup")
                                {
                                    if(!string.IsNullOrEmpty(property.Value))
                                    {
                                        List<Int64> longs = new List<long>();
                                        foreach (var item in property.Value.Split(','))
                                        {
                                            longs.Add(Int64.Parse(item));
                                        }
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeMultiSelectLookup,  // This must be correct for the property definition.
                                           longs.ToArray()
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeMultiSelectLookup,  // This must be correct for the property definition.
                                           null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                   
                                }
                                else if (property.Datatype == "MFDatatypeLookup")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {

                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                           Convert.ToInt64(property.Value)
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                          null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeBoolean")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeBoolean,  // This must be correct for the property definition.
                                           property.Value
                                        );
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeBoolean,  // This must be correct for the property definition.
                                           null
                                        );
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeMultiLineText")
                                {

                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeMultiLineText,  // This must be correct for the property definition.
                                           property.Value
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeMultiLineText,  // This must be correct for the property definition.
                                           null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeText")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeText,  // This must be correct for the property definition.
                                           property.Value
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeText,  // This must be correct for the property definition.
                                          null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeDate")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeDate,  // This must be correct for the property definition.
                                           DateTime.Parse(property.Value)
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeDate,  // This must be correct for the property definition.
                                           null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeTime")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeTime,  // This must be correct for the property definition.
                                          DateTime.Parse(property.Value)
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeTime,  // This must be correct for the property definition.
                                         null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeFloating")
                                {
                                    // Create a property value to update.
                                    var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                    {
                                        PropertyDef = property.id
                                    };
                                    nameOrTitlePropertyValue.Value.SetValue(
                                        MFDataType.MFDatatypeFloating,  // This must be correct for the property definition.
                                       Double.Parse(property.Value)
                                    );
                                    // Update the property on the server.
                                    properties.Add(-1, nameOrTitlePropertyValue);
                                }
                                else if (property.Datatype == "MFDatatypeTimestamp")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeTimestamp,  // This must be correct for the property definition.
                                           DateTime.Parse(property.Value)
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeTimestamp,  // This must be correct for the property definition.
                                           null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                                else if (property.Datatype == "MFDatatypeInteger")
                                {
                                    if (!string.IsNullOrEmpty(property.Value))
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeInteger,  // This must be correct for the property definition.
                                           Int64.Parse(property.Value)
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else
                                    {
                                        // Create a property value to update.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                        {
                                            PropertyDef = property.id
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeInteger,  // This must be correct for the property definition.
                                          null
                                        );
                                        // Update the property on the server.
                                        properties.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                            }
                            vault.ObjectPropertyOperations.SetProperties(
                                       ObjVer: checkedOutObjectVersion.ObjVer,
                                       PropertyValues: properties);
                            #region setting last modified
                            {
                                var date = DateTime.UtcNow;
                                var lastModifiedBy = new TypedValue();
                                lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, updateObjectProps.UserID);
                                var lastModifiedDate = new TypedValue();
                                lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                                vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                                (
                                    checkedOutObjectVersion.ObjVer,
                                    true, lastModifiedBy,
                                    true, lastModifiedDate
                                );

                            }
                            #endregion

                            vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                            Reports reports = new Reports();
                            reports.classID = updateObjectProps.classid;
                            reports.objectID = objectVersion.ObjVer.ID;
                            reports.objectTypeID = objectVersion.ObjVer.Type;
                            reports.VaultGuid = updateObjectProps.VaultGuid;
                            string json = JsonConvert.SerializeObject(reports);

                            var options = new RestClientOptions(ReportsUrl)
                            ;
                            var client = new RestClient(options);
                            var request = new RestRequest("UpdateRecord", Method.Post);
                            request.AddHeader("Content-Type", "application/json");
                            var body = json;
                            request.AddStringBody(body, RestSharp.DataFormat.Json);
                            RestResponse response = await client.ExecuteAsync(request);
                            if (!response.IsSuccessful)
                            {
                                Console.WriteLine(response.Content);
                            }

                        }
                        catch (Exception ex)
                        {
                            vault.ObjectOperations.ForceUndoCheckout(checkedOutObjectVersion.ObjVer);

                            return BadRequest(ex.Message);
                        }
                    }
                  
                    return Ok("Object successfully Updated");
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
        [HttpPost("ApproveAssignment")]
        public async Task<IActionResult> ApproveAssignmentAsync([FromBody] ApproveAssignment approveAsignment)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";

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

                var vault = mfServerApplication.LogInToVault(approveAsignment.VaultGuid);
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
                        10);

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
                //add search with internal id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, approveAsignment.ObjectId);
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
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, approveAsignment.ClassId);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            // We want to alter the document with ID 249.
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: objectVersion.ObjVer.Type,
                                ID: objectVersion.ObjVer.ID);

                            // Check out the object.
                            var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                            vault.ObjectPropertyOperations.ApproveOrRejectAssignmentByUser(checkedOutObjectVersion.ObjVer, approveAsignment.Approve, approveAsignment.UserID);
                          

                            vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                            Reports reports = new Reports();
                            reports.classID = checkedOutObjectVersion.Class;
                            reports.objectID = objectVersion.ObjVer.ID;
                            reports.objectTypeID = checkedOutObjectVersion.ObjVer.Type;
                            reports.VaultGuid = approveAsignment.VaultGuid;
                            string json = JsonConvert.SerializeObject(reports);

                            var options112 = new RestClientOptions(ReportsUrl)
                           ;
                            var client112 = new RestClient(options112);
                            var request1112 = new RestRequest("UpdateRecord", Method.Post);
                            request1112.AddHeader("Content-Type", "application/json");
                            var body = json;
                            request1112.AddStringBody(body, RestSharp.DataFormat.Json);
                            RestResponse response1112 = await client112.ExecuteAsync(request1112);
                            if (!response1112.IsSuccessful)
                            {
                                Console.WriteLine(response1112.Content);
                            }

                        }

                        return Ok("Successfully Marked as Completed");
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }

                }
                else
                {
                    return BadRequest("Could not find an assignment object with that ID");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("ObjectCreation")]
        public async Task<IActionResult> ObjectCreation([FromBody] MfilesCreate mfilesCreate)
        {
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var files = Directory.GetFiles(path);
            List<string> filesp = new List<string>();
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(mfilesCreate.UploadId))
                {
                    if (System.IO.Path.GetFileName(file).StartsWith(mfilesCreate.UploadId))
                    {
                        filesp.Add(file);
                    }
                    else
                    {
                        if (DateTime.UtcNow.Subtract(System.IO.File.GetCreationTime(file)).Minutes >= 30)
                        {
                            try
                            {
                                System.IO.File.Delete(file);
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }
               
            }
           
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";

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

                var vault = mfServerApplication.LogInToVault(mfilesCreate.VaultGuid);

                {
                    // Define the property values for the new object.
                    var propertyValues = new MFilesAPI.PropertyValues();
                    #region class prop
                    {
                        // Class.
                        var classPropertyValue = new MFilesAPI.PropertyValue()
                        {
                            PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass
                        };
                        classPropertyValue.Value.SetValue(
                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                            mfilesCreate.classID // This must be the ID of a class within the object type specified below.
                            );
                        propertyValues.Add(-1, classPropertyValue);
                    }
                    #endregion
                    #region created by
                    {
                        // Class.
                        var classPropertyValue = new MFilesAPI.PropertyValue()
                        {
                            PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreatedBy
                        };
                        classPropertyValue.Value.SetValue(
                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                            mfilesCreate.UserID // This must be the ID of a class within the object type specified below.
                            );
                        propertyValues.Add(-1, classPropertyValue);
                    }

                    #endregion
                    #region last modified
                    {
                        // Class.
                        var classPropertyValue = new MFilesAPI.PropertyValue()
                        {
                            PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModifiedBy
                        };
                        classPropertyValue.Value.SetValue(
                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                            mfilesCreate.UserID // This must be the ID of a class within the object type specified below.
                            );
                        propertyValues.Add(-1, classPropertyValue);
                    }
                    #endregion
                    #region setting state and workflow
                    {
                        int state = 0;
                        var workflow = vault.ClassOperations.GetObjectClass(mfilesCreate.classID).Workflow;
                        if (workflow > 0)
                        {
                            var stateone = vault.WorkflowOperations.GetWorkflowAdmin(workflow).StateTransitions;
                            foreach (StateTransition stated in stateone)
                            {
                                if (stated.FromState == 0)
                                {
                                    state = stated.ToState;
                                }
                            }

                        }
                        if (mfilesCreate.properties != null)
                        {
                            foreach (var item in mfilesCreate.properties)
                            {
                                if (!string.IsNullOrEmpty(item.propertytype) && !string.IsNullOrEmpty(item.value) && item.propId >= 0)
                                {
                                    if (item.propertytype == "MFDatatypeBoolean")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeBoolean,  // This must be correct for the property definition.
                                            item.value
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeText")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeText,  // This must be correct for the property definition.
                                            item.value
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeMultiLineText")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeMultiLineText,  // This must be correct for the property definition.
                                            item.value
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeMultiSelectLookup")
                                    {
                                        List<Int64> id = new List<Int64>();
                                        foreach (var itemp in item.value.Split(","))
                                        {
                                            id.Add(Int64.Parse(itemp));
                                        }
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeMultiSelectLookup,  // This must be correct for the property definition.
                                            id.ToArray()
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeLookup")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                            Int64.Parse(item.value)
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeDate")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeDate,  // This must be correct for the property definition.
                                            DateTime.Parse(item.value)
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeTime")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeTime,  // This must be correct for the property definition.
                                            DateTime.Parse(item.value)
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeFloating")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeFloating,  // This must be correct for the property definition.
                                            Double.Parse(item.value)
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeTimestamp")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeTimestamp,  // This must be correct for the property definition.
                                            DateTime.Parse(item.value)
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    else if (item.propertytype == "MFDatatypeInteger")
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = item.propId
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeInteger,  // This must be correct for the property definition.
                                            Int64.Parse(item.value)
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }
                              
                            }
                        }
                        if (workflow > 0 && state > 0)
                        {
                            {
                                // Name or title.
                                var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                {
                                    PropertyDef = 38
                                };
                                nameOrTitlePropertyValue.Value.SetValue(
                                    MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                    workflow
                                );
                                propertyValues.Add(-1, nameOrTitlePropertyValue);
                            }
                            {
                                // Name or title.
                                var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                {
                                    PropertyDef = 39
                                };
                                nameOrTitlePropertyValue.Value.SetValue(
                                    MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                    state
                                );
                                propertyValues.Add(-1, nameOrTitlePropertyValue);
                            }
                        }
                    }
                    #endregion

                    // Define the source files to add (none, in this case).
                    var sourceFiles = new MFilesAPI.SourceObjectFiles();

                    if (filesp.Count >= 1)
                    {
                       
                        foreach (var file in filesp)
                        {
                            var filename = System.IO.Path.GetFileName(file).Split(".")[0].Trim();
                            var sharednames = filesp.Where(m => m.EndsWith(filename));
                          
                            filename = filename.Split("_")[1];
                            // Add one file.
                            var myFile = new MFilesAPI.SourceObjectFile();
                            myFile.SourceFilePath = file;
                            myFile.Title = $"{filename}"; // For single-file-documents this is ignored.
                            myFile.Extension = System.IO.Path.GetExtension(file).TrimStart('.'); 
                            sourceFiles.Add(-1, myFile);
                        }
                    }


                    // What object type is being created (Employee)
                    var objectTypeID = mfilesCreate.objectID; // Employee object type ID.

                    // A "single file document" must be both a document and contain exactly one file.
                    var isSingleFileDocument =
                        objectTypeID == (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument && sourceFiles.Count == 1;

                    // Create the object and check it in.
                    var objectVersion = vault.ObjectOperations.CreateNewObjectEx(
                        objectTypeID,
                        propertyValues,
                        sourceFiles,
                        SFD: isSingleFileDocument,
                        CheckIn: true);

                    Objectresp objectresp = new Objectresp();
                    forlogs objectsearchresponse = new forlogs();

                    objectsearchresponse.id = objectVersion.ObjVer.ID;
                    objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                    objectsearchresponse.Title = objectVersion.VersionData.Title;
                    objectsearchresponse.CreatedUtc = objectVersion.VersionData.CreatedUtc;
                    objectsearchresponse.LastModifiedUtc = objectVersion.VersionData.LastModifiedUtc;
                    objectsearchresponse.ClassID = mfilesCreate.classID;
                    objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                    objectsearchresponse.VaultGuid = mfilesCreate.VaultGuid;
                    objectsearchresponse.DisplayID = objectVersion.VersionData.DisplayID;
                    objectsearchresponse.IsSingleFile = objectVersion.VersionData.SingleFile;
                    var perm = _permission.ObjectPermission(vault, mfilesCreate.UserID, objectVersion.VersionData.ObjVer.Type);
                    if (perm.EditPermission)
                    {
                        perm = _permission.ClassPermission(vault, mfilesCreate.UserID, objectVersion.VersionData.Class);

                    }

                    objectsearchresponse.userPermission = perm;
                    


                    try
                    {
                        var classty = vault.ClassOperations.GetObjectClass(mfilesCreate.classID);
                        var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                        objectsearchresponse.ClassTypeName = classty.Name;
                        objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                    }
                    catch
                    {

                    }

                    RecentModel recentModel = new RecentModel();
                    recentModel.Title = objectsearchresponse.Title;
                    recentModel.VaultGuid = objectsearchresponse.VaultGuid;
                    recentModel.TimeStamp = DateTime.Now;
                    recentModel.ObjectID = objectsearchresponse.ObjectID;
                    recentModel.ObjectTypeName = objectsearchresponse.ObjectTypeName;
                    recentModel.Id = objectsearchresponse.id;
                    recentModel.ClassID = objectsearchresponse.ClassID;
                    recentModel.ClassTypeName = objectsearchresponse.ClassTypeName;
                    recentModel.UserID = mfilesCreate.UserID;
                    recentModel.DisplayID = objectsearchresponse.DisplayID;

                    await _repository.AddOrUpdateAsync(recentModel);

                    objectresp.ObjID = objectVersion.ObjVer.ID;
                    try
                    {
                        foreach (var file in filesp)
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    Reports reports= new Reports();
                    reports.classID = mfilesCreate.classID;
                    reports.objectID = objectVersion.ObjVer.ID;
                    reports.objectTypeID= objectVersion.ObjVer.Type;
                    reports.VaultGuid= mfilesCreate.VaultGuid;
                    string json = JsonConvert.SerializeObject(reports);

                    var options = new RestClientOptions(ReportsUrl)
                    ;
                    var client = new RestClient(options);
                    var request = new RestRequest("PostNew", Method.Post);
                    request.AddHeader("Content-Type", "application/json");
                    var body = json;
                    request.AddStringBody(body, RestSharp.DataFormat.Json);
                    RestResponse response = await client.ExecuteAsync(request);
                    if (!response.IsSuccessful)
                    {
                        Console.WriteLine(response.Content);
                    }

                    return Ok(objectresp);
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("CombinePdfObjectFiles")]
        public IActionResult CombinePdfObjectFiles([FromBody] CombinedMfilesCreate mfilesCreate)
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
                var vault = mfServerApplication.LogInToVault(mfilesCreate.VaultGuid);
                List<string> filesp = new List<string>();
                var newpath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Files", Guid.NewGuid().ToString());
                if (!Directory.Exists(newpath))
                    Directory.CreateDirectory(newpath);
                int propid = 0;
                var filename = "merged docs from " + mfilesCreate.Title;

                #region searching for object files
                {
                    // Create our search conditions.
                    var searchConditions = new SearchConditions();
                    //Filter with objecttype
                    {
                        // Create the condition.
                        var condition = new SearchCondition();

                        // Set the expression.
                        condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                        // Set the condition type.
                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                        // Set the value (this excludes all objects with ID 478 - in all object types!).
                        condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, mfilesCreate.objectId);
                        searchConditions.Add(-1,condition);
                    }
                    //filter with id
                    {
                        // Create the condition.
                        var condition = new SearchCondition();

                        // Set the expression.
                        condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);

                        // Set the condition.
                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                        // Set the value.
                        condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                            mfilesCreate.OldObjectTypeID);

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
                            mfilesCreate.OldClassID);

                        // Add the condition to the collection.
                        searchConditions.Add(-1, condition);
                    }
                    //filter with user id
                    {
                        // Create the condition.
                        var condition = new SearchCondition();

                        // Set the expression (must be visible to the provided user).
                        condition.Expression.SetPermissionExpression(MFPermissionsExpressionType.MFVisibleTo);

                        // Set the condition type.
                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                        // Set a lookup representing the user.
                        var lookup = new MFilesAPI.Lookup()
                        {
                            ObjectType = (int)MFBuiltInValueList.MFBuiltInValueListUsers,
                            Item = mfilesCreate.UserID // User ID = 25
                        };

                        // Set the value.
                        condition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, lookup);
                        searchConditions.Add(-1,condition);
                    }
                    var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                        MFSearchFlags.MFSearchFlagNone, SortResults: false);
                    if (searchResults.Count > 0)
                    {
                        var classCombine = vault.ClassOperations.GetObjectClassIDByAlias("CL.AlignsysCombined");
                        foreach(ObjectVersion objectVersion1 in searchResults)
                        {
                            var v = vault.ObjectOperations.GetRelationshipsEx(objectVersion1.ObjVer,MFRelationshipsMode.MFRelationshipsModeToThisObject,true);
                            if(v.Count>1)
                            foreach (ObjectVersion objectVersion in v)
                            {
                                    if (!objectVersion.ThisVersionCheckedOut)
                                    {
                                        if (classCombine > 0)
                                        {
                                            if (objectVersion.Class != classCombine)
                                            {
                                                var valueslist = vault.ValueListOperations.GetValueList(mfilesCreate.OldObjectTypeID);
                                                propid = valueslist.DefaultPropertyDef;
                                                foreach (ObjectFile objectFile in objectVersion.Files)
                                                {
                                                    if (objectFile.Extension.ToLower() == "pdf")
                                                    {
                                                        var newpathx = System.IO.Path.Combine(newpath, Guid.NewGuid().ToString() + "." + objectFile.Extension);
                                                        vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, newpathx);
                                                        filesp.Add(newpathx);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var valueslist = vault.ValueListOperations.GetValueList(mfilesCreate.OldObjectTypeID);
                                            propid = valueslist.DefaultPropertyDef;
                                            foreach (ObjectFile objectFile in objectVersion.Files)
                                            {
                                                if (objectFile.Extension.ToLower() == "pdf")
                                                {
                                                    var newpathx = System.IO.Path.Combine(newpath, Guid.NewGuid().ToString() + "." + objectFile.Extension);
                                                    vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, newpathx);
                                                    filesp.Add(newpathx);
                                                }
                                            }
                                        }
                                    }

                             
                            }
                        }
                       
                     
                    }
                }
                #endregion
                var completedfilepath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Files",Guid.NewGuid().ToString()+".pdf");
                FileStream firstStream = new FileStream(completedfilepath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                #region merging files
                {
                    //Get the folder path into DirectoryInfo
                    DirectoryInfo directoryInfo = new DirectoryInfo(newpath);

                    //Get the PDF files in folder path into FileInfo
                    FileInfo[] files = directoryInfo.GetFiles("*.pdf");

                    //Create a new PDF document 
                    PdfDocument document = new PdfDocument();

                    //Set enable memory optimization as true 
                    document.EnableMemoryOptimization = true;

                    foreach (FileInfo file in files)
                    {
                        //Load the PDF document 
                        FileStream fileStream = new FileStream(file.FullName, FileMode.Open);
                        PdfLoadedDocument loadedDocument = new PdfLoadedDocument(fileStream);

                        //Merge PDF file
                        PdfDocumentBase.Merge(document, loadedDocument);

                        //Close the existing PDF document 
                        loadedDocument.Close(true);
                    }

                    //Save the PDF document
                    document.Save(firstStream);

                    //Close the instance of PdfDocument
                    document.Close(true);

                    PdfLoadedDocument pdfLoadedDocument = new PdfLoadedDocument(firstStream);
                    PdfCompressionOptions options = new PdfCompressionOptions();
                    options.CompressImages = true;
                    options.ImageQuality = 50;
                    options.OptimizeFont = true;
                    options.OptimizePageContents = true;
                    options.RemoveMetadata = true;
                    pdfLoadedDocument.Compress(options);
                    var filepath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Files", "output.pdf");
                    using (FileStream output = new FileStream(filepath, FileMode.OpenOrCreate))
                    {
                        pdfLoadedDocument.Save(output);
                    }
                    pdfLoadedDocument.Close(true);
                    firstStream.Close();
                    try
                    {
                        System.IO.File.Move(filepath, completedfilepath,true);
                    }
                    catch
                    {

                    }
                }
                #endregion
                if (filesp.Count>1&&System.IO.File.Exists(completedfilepath))
                {

                    var classid = 0;

                    var classCombine = vault.ClassOperations.GetObjectClassIDByAlias("CL.AlignsysCombined");
                    if (classCombine == -1)
                    {
                        ObjectClassAdmin objectClassAdmin = new ObjectClassAdmin();
                        objectClassAdmin.Name = "Alignsys Combined";
                        objectClassAdmin.NamePropertyDef = 0;
                        // objectClassAdmin.AssociatedPropertyDefs.Add(-1, associatedPropertyDefClass);
                        objectClassAdmin.ObjectType = 0;
                        objectClassAdmin.SemanticAliases.Value = "CL.AlignsysCombined";
                        var classp = vault.ClassOperations.AddObjectClassAdmin(objectClassAdmin);
                        classid= classp.ID;
                    }
                    else
                    {
                        classid = classCombine;
                    }

                    #region checking if combined document exists
                    {
                        var objectp = vault.ObjectTypeOperations.GetObjectType(mfilesCreate.OldObjectTypeID).DefaultPropertyDef;

                        // Create our search conditions.
                        var searchConditions = new SearchConditions();

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
                                classid);

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
                                0);

                            // Add the condition to the collection.
                            searchConditions.Add(-1, condition);
                        }
                        //filter by prop
                        {
                            // Create the "minimum" search condition.
                            var searchCondition = new SearchCondition();

                            // We want to search by property.
                            searchCondition.Expression.SetPropertyValueExpression(
                                objectp, // This is our date property ID
                                PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                            // Set the condition type.
                            searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                            // We only want documents that are later than 1st January 2017.
                            searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeMultiSelectLookup, mfilesCreate.objectId);

                            // Add it to the conditions.
                            searchConditions.Add(-1, searchCondition);
                        }
                        // Execute the search.
                        var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                            MFSearchFlags.MFSearchFlagNone, SortResults: false);
                        if (searchResults.Count > 0)
                        {
                            foreach(ObjectVersion objectVersion in searchResults.ObjectVersions)
                            {
                                var objID = new MFilesAPI.ObjID();
                                objID.SetIDs(
                                    ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                                    ID: objectVersion.ObjVer.ID);

                                // Check out the object.
                                var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                                foreach(ObjectFile objectFile in checkedOutObjectVersion.Files)
                                {
                                    vault.ObjectFileOperations.UploadFile(objectFile.ID, objectFile.Version, completedfilepath);
                                }

                                // Check the object back in.
                                vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                                forlogs objectsearchresponse = new forlogs();

                                objectsearchresponse.id = objectVersion.ObjVer.ID;
                                objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                                objectsearchresponse.Title = objectVersion.Title;
                                objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                                objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                                objectsearchresponse.ClassID = objectVersion.Class;
                                objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                                objectsearchresponse.VaultGuid = mfilesCreate.VaultGuid;
                                objectsearchresponse.DisplayID = objectVersion.DisplayID;
                                objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                                var perm = _permission.ObjectPermission(vault, mfilesCreate.UserID, objectVersion.ObjVer.Type);
                                if (perm.EditPermission)
                                {
                                    perm = _permission.ClassPermission(vault, mfilesCreate.UserID, objectVersion.Class);

                                }

                                objectsearchresponse.userPermission = perm;



                                try
                                {
                                    var classty = vault.ClassOperations.GetObjectClass(objectVersion.Class);
                                    var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                    objectsearchresponse.ClassTypeName = classty.Name;
                                    objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                                }
                                catch
                                {

                                }
                              }
                            try
                            {
                                foreach (var file in filesp)
                                {
                                    System.IO.File.Delete(file);
                                }
                                Directory.Delete(newpath);
                                System.IO.File.Delete(completedfilepath);
                            }
                            catch (Exception ex)
                            {

                            }
                            return Ok("Successfully Updated existing merged document");
                        }
                        else
                        {

                            // Define the property values for the new object.
                            var propertyValues = new MFilesAPI.PropertyValues();
                            #region class prop
                            {
                                // Class.
                                var classPropertyValue = new MFilesAPI.PropertyValue()
                                {
                                    PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass
                                };
                                classPropertyValue.Value.SetValue(
                                    MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                    classid // This must be the ID of a class within the object type specified below.
                                    );
                                propertyValues.Add(-1, classPropertyValue);
                            }
                            #endregion
                            #region created by
                            {
                                // Class.
                                var classPropertyValue = new MFilesAPI.PropertyValue()
                                {
                                    PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreatedBy
                                };
                                classPropertyValue.Value.SetValue(
                                    MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                    mfilesCreate.UserID // This must be the ID of a class within the object type specified below.
                                    );
                                propertyValues.Add(-1, classPropertyValue);
                            }

                            #endregion
                            #region last modified
                            {
                                // Class.
                                var classPropertyValue = new MFilesAPI.PropertyValue()
                                {
                                    PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModifiedBy
                                };
                                classPropertyValue.Value.SetValue(
                                    MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                    mfilesCreate.UserID // This must be the ID of a class within the object type specified below.
                                    );
                                propertyValues.Add(-1, classPropertyValue);
                            }
                            #endregion
                            #region setting state and workflow
                            {
                                int state = 0;
                                var workflow = vault.ClassOperations.GetObjectClass(classid).Workflow;
                                if (workflow > 0)
                                {
                                    var stateone = vault.WorkflowOperations.GetWorkflowAdmin(workflow).StateTransitions;
                                    foreach (StateTransition stated in stateone)
                                    {
                                        if (stated.FromState == 0)
                                        {
                                            state = stated.ToState;
                                        }
                                    }

                                }
                                if (workflow > 0 && state > 0)
                                {
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = 38
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                            workflow
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                    {
                                        // Name or title.
                                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                        {
                                            PropertyDef = 39
                                        };
                                        nameOrTitlePropertyValue.Value.SetValue(
                                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                            state
                                        );
                                        propertyValues.Add(-1, nameOrTitlePropertyValue);
                                    }
                                }


                            }
                            #endregion
                            #region setting props
                            {

                                {
                                    // Name or title.
                                    var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                    {
                                        PropertyDef = 0
                                    };
                                    nameOrTitlePropertyValue.Value.SetValue(
                                        MFDataType.MFDatatypeText,  // This must be correct for the property definition.
                                        filename
                                    );
                                    propertyValues.Add(-1, nameOrTitlePropertyValue);
                                }
                                {
                                    // Name or title.
                                    var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                    {
                                        PropertyDef = objectp
                                    };
                                    nameOrTitlePropertyValue.Value.SetValue(
                                        MFDataType.MFDatatypeMultiSelectLookup,  // This must be correct for the property definition.
                                       mfilesCreate.objectId
                                    );
                                    propertyValues.Add(-1, nameOrTitlePropertyValue);
                                }
                            }
                            #endregion

                            // Define the source files to add (none, in this case).
                            var sourceFiles = new MFilesAPI.SourceObjectFiles();

                            // Add one file.
                            var myFile = new MFilesAPI.SourceObjectFile();
                            myFile.SourceFilePath = completedfilepath;
                            myFile.Title = $"{filename}"; // For single-file-documents this is ignored.
                            myFile.Extension = "pdf";
                            sourceFiles.Add(-1, myFile);


                            // What object type is being created (Employee)
                            var objectTypeID = 0; // Employee object type ID.

                            // A "single file document" must be both a document and contain exactly one file.
                            var isSingleFileDocument =
                                objectTypeID == (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument && sourceFiles.Count == 1;

                            // Create the object and check it in.
                            var objectVersion = vault.ObjectOperations.CreateNewObjectEx(
                                objectTypeID,
                                propertyValues,
                                sourceFiles,
                                SFD: isSingleFileDocument,
                                CheckIn: true);


                            forlogs objectsearchresponse = new forlogs();

                            objectsearchresponse.id = objectVersion.VersionData.ObjVer.ID;
                            objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                            objectsearchresponse.Title = objectVersion.VersionData.Title;
                            objectsearchresponse.CreatedUtc = objectVersion.VersionData.CreatedUtc;
                            objectsearchresponse.LastModifiedUtc = objectVersion.VersionData.LastModifiedUtc;
                            objectsearchresponse.ClassID = objectVersion.VersionData.Class;
                            objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                            objectsearchresponse.VaultGuid = mfilesCreate.VaultGuid;
                            objectsearchresponse.DisplayID = objectVersion.VersionData.DisplayID;
                            objectsearchresponse.IsSingleFile = objectVersion.VersionData.SingleFile;
                            var perm = _permission.ObjectPermission(vault, mfilesCreate.UserID, objectVersion.ObjVer.Type);
                            if (perm.EditPermission)
                            {
                                perm = _permission.ClassPermission(vault, mfilesCreate.UserID, objectVersion.VersionData.Class);

                            }

                            objectsearchresponse.userPermission = perm;



                            try
                            {
                                var classty = vault.ClassOperations.GetObjectClass(objectVersion.VersionData.Class);
                                var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                objectsearchresponse.ClassTypeName = classty.Name;
                                objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                            }
                            catch
                            {

                            }
                          

                            Objectresp objectresp = new Objectresp();
                            objectresp.ObjID = objectVersion.ObjVer.ID;
                            filesp.Add(completedfilepath);
                            try
                            {
                                foreach (var file in filesp)
                                {
                                    System.IO.File.Delete(file);
                                }
                                Directory.Delete(newpath);
                            }
                            catch (Exception ex)
                            {

                            }

                            return Ok(objectresp);
                        }
                    }

                    #endregion

                }
                else
                {
                    return BadRequest("cannot combine a single pdf file");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("FilesUploadAsync")]
        public async Task<IActionResult> FilesUploadAsync(List<IFormFile> formFiles, CancellationToken cancellationToken)
        {
            if (formFiles.Count == null)
            {
                return BadRequest("Cannot upload empty file");
            }

            var filestitle = new List<string>();
            foreach (var file in formFiles)
            {
                filestitle.Add(file.FileName);
            }
            var hashset =  new HashSet<string>(filestitle);
            if(hashset.Count != formFiles.Count)
            {
                return BadRequest("Certain files are repeated in this list");
            }

            UploadRespo uploadRespo = new UploadRespo();
            uploadRespo.UploadID = await WriteFiles(formFiles);
            return Ok(uploadRespo);
        }
        [HttpPut("AddObjectFiles/{VaultGuid}/{ObjectId}/{UserID}")]
        public async Task<IActionResult> AddObjectFilesAsync(int ObjectId, string VaultGuid, int UserID, List<IFormFile> formFiles, CancellationToken cancellationToken)
        {
            List<string> erros = new List<string>();
            if (formFiles.Count == null)
            {
                return BadRequest("Cannot upload empty file");
            }

            var filestitle = new List<string>();
            foreach (var file in formFiles)
            {
                filestitle.Add(file.FileName);
            }
            var hashset = new HashSet<string>(filestitle);
            if (hashset.Count != formFiles.Count)
            {
                return BadRequest("Certain files are repeated in this list");
            }
            UploadRespo uploadRespo = new UploadRespo();
            uploadRespo.UploadID = await WriteFiles(formFiles);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var files = Directory.GetFiles(path);
            List<string> filesp = new List<string>();
            foreach (var file in files)
            {
                if (System.IO.Path.GetFileName(file).StartsWith(uploadRespo.UploadID))
                {
                    filesp.Add(file);
                }
                else
                {
                    if (DateTime.UtcNow.Subtract(System.IO.File.GetCreationTime(file)).Minutes >= 30)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }

            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";

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
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    try
                    {
                        foreach(ObjectVersion objectVersion in searchResults)
                        {
                            if (objectVersion.Files.Count == 1)
                            {
                                var objID = new MFilesAPI.ObjID();
                                objID.SetIDs(
                                    ObjType: objectVersion.OriginalObjID.Type,
                                    ID: objectVersion.ObjVer.ID);
                                var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                                MFilesAPI.PropertyValues properties = new MFilesAPI.PropertyValues();


                                foreach (var file in filesp)
                                {
                                    foreach(ObjectFile objectFile in checkedOutObjectVersion.Files)
                                    {
                                        if (Path.GetFileName(file).Split("_")[1].Trim().Split(".")[0].Trim() == objectFile.Title)
                                        {
                                            erros.Add(Path.GetFileName(file));
                                        }
                                    }
                                }
                                if (erros.Count <= 0)
                                {
                                    foreach (var file in filesp)
                                    {
                                        
                                            {
                                            //is single file
                                            {
                                                // Create a property value to update.
                                                var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                                                {
                                                    PropertyDef = 22
                                                };
                                                nameOrTitlePropertyValue.Value.SetValue(
                                                    MFDataType.MFDatatypeBoolean,  // This must be correct for the property definition.
                                                     false
                                                );
                                                // Update the property on the server.
                                                properties.Add(-1, nameOrTitlePropertyValue);
                                            }

                                        }
                                            vault.ObjectPropertyOperations.SetProperties(
                                                                      ObjVer: checkedOutObjectVersion.ObjVer,
                                                                      PropertyValues: properties);
                                            var id = vault.ObjectFileOperations.AddFile(checkedOutObjectVersion.ObjVer, Path.GetFileName(file).Split("_")[1].Trim(), Path.GetExtension(file), file);
                                        
                                    }
                                }

                                var date = DateTime.UtcNow;
                                var lastModifiedBy = new TypedValue();
                                lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, UserID);
                                var lastModifiedDate = new TypedValue();
                                lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                                vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                                (
                                    checkedOutObjectVersion.ObjVer,
                                    true, lastModifiedBy,
                                    true, lastModifiedDate
                                );

                                vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                                forlogs objectsearchresponse = new forlogs();

                                objectsearchresponse.id = objectVersion.ObjVer.ID;
                                objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                                objectsearchresponse.Title = objectVersion.Title;
                                objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                                objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                                objectsearchresponse.ClassID = objectVersion.Class;
                                objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                                objectsearchresponse.VaultGuid = VaultGuid;
                                objectsearchresponse.DisplayID = objectVersion.DisplayID;
                                objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                                var perm = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                                if (perm.EditPermission)
                                {
                                    perm = _permission.ClassPermission(vault, UserID, objectVersion.Class);

                                }

                                objectsearchresponse.userPermission = perm;



                                try
                                {
                                    var classty = vault.ClassOperations.GetObjectClass(objectVersion.Class);
                                    var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                    objectsearchresponse.ClassTypeName = classty.Name;
                                    objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                                }
                                catch
                                {

                                }
                                 
                            }
                            else
                            {
                                var objID = new MFilesAPI.ObjID();
                                objID.SetIDs(
                                    ObjType: objectVersion.OriginalObjID.Type,
                                    ID: objectVersion.ObjVer.ID);
                                var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                                MFilesAPI.PropertyValues properties = new MFilesAPI.PropertyValues();

                                foreach (var file in filesp)
                                {

                                    foreach (ObjectFile objectFile in checkedOutObjectVersion.Files)
                                    {
                                        if (Path.GetFileName(file).Split("_")[1].Trim() == objectFile.Title)
                                        {
                                            erros.Add(Path.GetFileName(file));
                                        }
                                    }
                                }
                                if (erros.Count <= 0)
                                {
                                    foreach (var file in filesp)
                                    {
                                      var id = vault.ObjectFileOperations.AddFile(checkedOutObjectVersion.ObjVer, Path.GetFileName(file).Split("_")[1].Trim().Split(".")[0].Trim(), Path.GetExtension(file), file); 
                                    }
                                }
                                var date = DateTime.UtcNow;
                                var lastModifiedBy = new TypedValue();
                                lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, UserID);
                                var lastModifiedDate = new TypedValue();
                                lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                                vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                                (
                                    checkedOutObjectVersion.ObjVer,
                                    true, lastModifiedBy,
                                    true, lastModifiedDate
                                );

                                vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                                Reports reports = new Reports();
                                reports.classID = checkedOutObjectVersion.Class;
                                reports.objectID = objectVersion.ObjVer.ID;
                                reports.objectTypeID = checkedOutObjectVersion.ObjVer.Type;
                                reports.VaultGuid = VaultGuid;
                                string json = JsonConvert.SerializeObject(reports);

                                var options112 = new RestClientOptions(ReportsUrl)
                               ;
                                var client112 = new RestClient(options112);
                                var request1112 = new RestRequest("UpdateRecord", Method.Post);
                                request1112.AddHeader("Content-Type", "application/json");
                                var body = json;
                                request1112.AddStringBody(body, RestSharp.DataFormat.Json);
                                RestResponse response1112 = await client112.ExecuteAsync(request1112);
                                if (!response1112.IsSuccessful)
                                {
                                    Console.WriteLine(response1112.Content);
                                }

                                forlogs objectsearchresponse = new forlogs();

                                objectsearchresponse.id = objectVersion.ObjVer.ID;
                                objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                                objectsearchresponse.Title = objectVersion.Title;
                                objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                                objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                                objectsearchresponse.ClassID = objectVersion.Class;
                                objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                                objectsearchresponse.VaultGuid = VaultGuid;
                                objectsearchresponse.DisplayID = objectVersion.DisplayID;
                                objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                                var perm = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                                if (perm.EditPermission)
                                {
                                    perm = _permission.ClassPermission(vault, UserID, objectVersion.Class);

                                }

                                objectsearchresponse.userPermission = perm;



                                try
                                {
                                    var classty = vault.ClassOperations.GetObjectClass(objectVersion.Class);
                                    var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                                    objectsearchresponse.ClassTypeName = classty.Name;
                                    objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                                }
                                catch
                                {

                                }
                             
                            }
                        }
                        if (erros.Count > 0)
                        {
                          
                         return BadRequest(erros);
                           
                        }
                        try
                        {
                            foreach(var file in  filesp)
                            {
                                System.IO.File.Delete(file);
                            }
                         
                        }
                        catch (Exception)
                        {

                        }
                        return Ok("Files successfully added to the object");
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
        [HttpGet("Search/{VaultGuid}/{SearchPhrase}/{UserID}")]
        public IActionResult Search(string SearchPhrase, string VaultGuid,int UserID)
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
               
                //add a text
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression (search in file data and metadata).
                    condition.Expression.SetAnyFieldExpression(MFFullTextSearchFlags.MFFullTextSearchFlagsLookInFileData
                            | MFFullTextSearchFlags.MFFullTextSearchFlagsLookInMetaData
                            |MFFullTextSearchFlags.MFFullTextSearchFlagsTypeAllWords
                            );

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeContains;

                    // Set the value.
                    // In this case "ESTT" is the text to search for.
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeText, SearchPhrase);
                    searchConditions.Add(-1, condition);
                }
              
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false, MaxResultCount:500);
                if (searchResults.Count > 0)
                {
                  
                    try
                    {
                        List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
                        if(searchResults.Count>0)
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                                try
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
																if ((!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == "MFPermissionAllow"))|| (!userPermission.ReadPermission && (aceData.ReadPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
																{
																	userPermission.ReadPermission = true;
																}
                                                                if ((!userPermission.EditPermission && (aceData.EditPermission.ToString() == "MFPermissionAllow")) || (!userPermission.EditPermission && (aceData.EditPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                                {
                                                                    userPermission.EditPermission = true;
                                                                }
                                                                if ((!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == "MFPermissionAllow")) || (!userPermission.AttachObjectsPermission && (aceData.AttachObjectsPermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
                                                                {
                                                                    userPermission.AttachObjectsPermission = true;
                                                                }
                                                                if ((!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == "MFPermissionAllow")) || (!userPermission.DeletePermission && (aceData.DeletePermission.ToString() == MFilesAPI.MFPermission.MFPermissionNotSet.ToString())))
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
                                    string fileextension = "";
                                    int fileid = 0;
                                    if (objectVersion.SingleFile)
                                    {
                                        fileextension = objectVersion.Files[1].Extension;
                                        fileid = objectVersion.Files[1].ID;
                                    }
                                    var st = vault.ObjectOperations.GetRelationshipsEx(objectVersion.ObjVer, MFRelationshipsMode.MFRelationshipsModeAll, true);
                                   
                                    Response.Add(new Objectsearchresponse { DisplayID=objectVersion.DisplayID,id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = userPermission, ClassTypeName = classname, VersionId = objectVersion.ObjVer.Version, ObjectTypeName = objecttype.NameSingular, CreatedUtc = objectVersion.CreatedUtc, LastModifiedUtc= objectVersion.LastModifiedUtc, IsSingleFile= objectVersion.SingleFile, IsCheckedOut= objectVersion.CheckedOutTo>0, checkoutuserid = objectVersion.CheckedOutTo, IsDeleted= objectVersion .Deleted, HasRelationship = st.Count>0, FileExtension= fileextension, FileId=fileid, checkoutusername = objectVersion.CheckedOutToUserName });
                                }
                                catch
                                {

                                }
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
        [HttpPost("ModifyObjectClass")]
        public async Task<IActionResult> ModifyObjectClassAsync([FromBody] ModifyClass modifyClass)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            string ReportsUrl = this._configuration.GetConnectionString("ReportsUrl") ?? "";

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

                var vault = mfServerApplication.LogInToVault(modifyClass.VaultGuid);

                var newclass = vault.ClassOperations.GetObjectClass(modifyClass.NewClassID);
                if (newclass.ObjectType != modifyClass.ObjectTypeID)
                {
                    return BadRequest("Only classes in the same object type can be modified");
                }
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
                        modifyClass.ObjectTypeID);

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
                // Add a class filter.
                {

                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] {
                                          modifyClass.OldClassID // Bulletin or Press Release
                                    };

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
                // Add a objectid filter.
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectID);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value (this excludes all objects with ID 478 - in all object types!).
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, modifyClass.objectID);
                    searchConditions.Add(-1, condition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone,SortResults:false);
                if (searchResults.Count>0)
                {
                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                        var properties = new MFilesAPI.PropertyValues();

                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                            ID: objectVersion.ObjVer.ID);
                        // Check out the object.
                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                        //class property
                        {
                            // Create a property value to update.
                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                            {
                                PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass
                            };
                            nameOrTitlePropertyValue.Value.SetValue(
                                MFDataType.MFDatatypeLookup, modifyClass.NewClassID   // This must be correct for the property definition.

                            );
                            properties.Add(-1, nameOrTitlePropertyValue);
                        }
                     
                        #region setting state and workflow
                        {
                            int state = 0;
                            var workflow = newclass.Workflow;
                            if (workflow > 0)
                            {
                                var stateone = vault.WorkflowOperations.GetWorkflowAdmin(workflow).StateTransitions;
                                foreach (StateTransition stated in stateone)
                                {
                                    if (stated.FromState == 0)
                                    {
                                        state = stated.ToState;
                                    }
                                }

                            }
                           
                            if (workflow > 0 && state > 0)
                            {
                                {
                                    // Name or title.
                                    var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                    {
                                        PropertyDef = 38
                                    };
                                    nameOrTitlePropertyValue.Value.SetValue(
                                        MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                        workflow
                                    );
                                    properties.Add(-1, nameOrTitlePropertyValue);
                                }
                                {
                                    // Name or title.
                                    var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                    {
                                        PropertyDef = 39
                                    };
                                    nameOrTitlePropertyValue.Value.SetValue(
                                        MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                        state
                                    );
                                    properties.Add(-1, nameOrTitlePropertyValue);
                                }
                            }
                        }
                        #endregion
                        #region setting other props
                        {
                            if (modifyClass.properties != null)
                            {
                                foreach (var item in modifyClass.properties)
                                {
                                    if (!string.IsNullOrEmpty(item.propertytype) && !string.IsNullOrEmpty(item.value) && item.propId >= 0&&item.propId!=100)
                                    {
                                        if (item.propertytype == "MFDatatypeBoolean")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeBoolean,  // This must be correct for the property definition.
                                                item.value
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeText")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeText,  // This must be correct for the property definition.
                                                item.value
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeMultiLineText")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeMultiLineText,  // This must be correct for the property definition.
                                                item.value
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeMultiSelectLookup")
                                        {
                                            List<Int64> id = new List<Int64>();
                                            foreach (var itemp in item.value.Split(","))
                                            {
                                                id.Add(Int64.Parse(itemp));
                                            }
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeMultiSelectLookup,  // This must be correct for the property definition.
                                                id.ToArray()
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeLookup")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                                Int64.Parse(item.value)
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeDate")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeDate,  // This must be correct for the property definition.
                                                DateTime.Parse(item.value)
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeTime")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeTime,  // This must be correct for the property definition.
                                                DateTime.Parse(item.value)
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeFloating")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeFloating,  // This must be correct for the property definition.
                                                Double.Parse(item.value)
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeTimestamp")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeTimestamp,  // This must be correct for the property definition.
                                                DateTime.Parse(item.value)
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                        else if (item.propertytype == "MFDatatypeInteger")
                                        {
                                            // Name or title.
                                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue()
                                            {
                                                PropertyDef = item.propId
                                            };
                                            nameOrTitlePropertyValue.Value.SetValue(
                                                MFDataType.MFDatatypeInteger,  // This must be correct for the property definition.
                                                Int64.Parse(item.value)
                                            );
                                            properties.Add(-1, nameOrTitlePropertyValue);
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        //is single file
                        {
                            // Create a property value to update.
                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                            {
                                PropertyDef = 22
                            };
                            nameOrTitlePropertyValue.Value.SetValue(
                                MFDataType.MFDatatypeBoolean, objectVersion.SingleFile  // This must be correct for the property definition.

                            );
                            properties.Add(-1, nameOrTitlePropertyValue);
                        }
                        // Update the property on the server.
                        vault.ObjectPropertyOperations.SetAllProperties(
                            ObjVer: checkedOutObjectVersion.ObjVer, true,
                            PropertyValues: properties);

                        #region setting last modified
                        {
                            var date = DateTime.UtcNow;
                            var lastModifiedBy = new TypedValue();
                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, modifyClass.UserID);
                            var lastModifiedDate = new TypedValue();
                            lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));
                            vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                            (
                                checkedOutObjectVersion.ObjVer,
                                true, lastModifiedBy,
                                true, lastModifiedDate
                            );

                        }
                        #endregion

                        vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);


                        Objectresp objectresp = new Objectresp();
                        forlogs objectsearchresponse = new forlogs();

                        objectsearchresponse.id = objectVersion.ObjVer.ID;
                        objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                        objectsearchresponse.Title = objectVersion.Title;
                        objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                        objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                        objectsearchresponse.ClassID = modifyClass.NewClassID;
                        objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                        objectsearchresponse.VaultGuid = modifyClass.VaultGuid;
                        objectsearchresponse.DisplayID = objectVersion.DisplayID;
                        objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                        var perm = _permission.ObjectPermission(vault, modifyClass.UserID, objectVersion.ObjVer.Type);
                        if (perm.EditPermission)
                        {
                            perm = _permission.ClassPermission(vault, modifyClass.UserID, objectVersion.Class);

                        }

                        objectsearchresponse.userPermission = perm;

                        var newobjectversion = vault.ObjectOperations.GetLatestObjectVersionAndProperties(objID,true);

                        try
                        {
                            var classty = vault.ClassOperations.GetObjectClass(modifyClass.NewClassID);
                            var objectt = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                            objectsearchresponse.ClassTypeName = classty.Name;
                            objectsearchresponse.ObjectTypeName = objectt.NameSingular;
                        }
                        catch
                        {

                        }
                        objectresp.ObjID = newobjectversion.ObjVer.ID;


                        Reports reports = new Reports();
                        reports.classID = modifyClass.NewClassID;
                        reports.objectID = objectVersion.ObjVer.ID;
                        reports.objectTypeID = objectVersion.ObjVer.Type;
                        reports.VaultGuid = modifyClass.VaultGuid;
                        string json = JsonConvert.SerializeObject(reports);

                        var options = new RestClientOptions(ReportsUrl)
                       ;
                        var client = new RestClient(options);
                        var request = new RestRequest("PostNew", Method.Post);
                        request.AddHeader("Content-Type", "application/json");
                        var body = json;
                        request.AddStringBody(body, RestSharp.DataFormat.Json);
                        RestResponse response = await client.ExecuteAsync(request);
                        if (!response.IsSuccessful)
                        {
                            Console.WriteLine(response.Content);
                        }

                        return Ok(objectresp);
                    }
                    return Ok();
                }
                else
                {
                    return NotFound("No items were found");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private int GetObjectversion(Vault vault, string Objectid, int classid,int objecttypeid )
        {
            int id = 0;
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
                    objecttypeid);

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
                    classid);

                // Add the condition to the collection.
                searchConditions.Add(-1, condition);
            }
            // add a object id filter
            {
                // Create the condition.
                var condition = new SearchCondition();

                // Set the expression.
                condition.Expression.DataStatusValueType = MFStatusType.MFStatusTypeExtID;

                // Set the condition type.
                condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                // Set the value.
                // In this case "MyExternalObjectId" is the ID of the object in the remote system.
                condition.TypedValue.SetValue(MFDataType.MFDatatypeText, Objectid);

                searchConditions.Add(-1,condition);
            }
            // Execute the search.
            var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                MFSearchFlags.MFSearchFlagNone, SortResults: false);
            foreach(ObjectVersion objectVersion in searchResults)
            {
                id = objectVersion.ObjVer.ID;
            }

            return id;

        }
        private async Task<string> WriteFiles(List<IFormFile> filesp)
        {
            string UploadID = Guid.NewGuid().ToString();
            int count = 0;
            var pathy = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            if (!Directory.Exists(pathy))
            {
                Directory.CreateDirectory(pathy);
            }
            bool isSaveSuccess = true;

            foreach (var f in filesp)
            {
                string docname = f.FileName.Split(".")[0];
                string filename;
                try
                {
                    var extension = "." + f.FileName.Split('.')[f.FileName.Split('.').Length - 1]; //Get file extension
                  
                        filename = UploadID + count + "_"+docname; //File is renamed
                        var path = Path.Combine(pathy, filename + extension);

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await f.CopyToAsync(stream);
                        }
                        isSaveSuccess = isSaveSuccess && true;
                    
                }
                catch (Exception)
                {

                }
                count += 1;
            }

            return UploadID;
        }
        private async Task<string> WriteFile(IFormFile file)
        {
            string UploadID = Guid.NewGuid().ToString();
            int count = 0;
            var pathy = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            if (!Directory.Exists(pathy))
            {
                Directory.CreateDirectory(pathy);
            }
            string docname = file.FileName.Split(".")[0];
            string filename;
            try
            {

                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1]; //Get file extension

                filename = UploadID + count + "_" + docname; //File is renamed
                var path = Path.Combine(pathy, filename + extension);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
               

            }
            catch (Exception)
            {

            }

            return UploadID;
        }
        public static string CleanFilename(string filename)
        {
            // Define a regex pattern for invalid characters
            string pattern = @"[\\/:*?""<>|]";
            // Replace invalid characters with an empty string
            return Regex.Replace(filename, pattern, "");
        }
    }
    public class lookupvalues
    {
        public string? ID { get; set; }
        public string? Title { get; set; }
    }
}