using CargenDss.Models;
using ConsoleApp4.MergingFiles;
using DocumentFormat.OpenXml.Spreadsheet;
using MFilesAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using pulling_object_permission;
using readingmetaconfigjson.metaServices;
using RecentFix.models;
using RecentFix.services;
using System;
using System.Diagnostics;
using System.Security;
using Zenfiles.Models;
using Zenfiles.Models.objversions;
using Zenfiles.Models.templates;
using Zenfiles.Models.views;
using Zenfiles.PermissionService;
using ZenFiles.Controllers;
using ZenFiles.Models;
using Zenfiles_V8.MergingFiles;
using Zenfiles_V8.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplatesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<objectinstanceController> _logger;
        private readonly Zenfiles.PermissionService.IPermission _permission;
        private readonly IMFilesObjectRepository _repository;
        private readonly GetCacheObjects _cacheObjects;
        public TemplatesController(IConfiguration Configuration, ILogger<objectinstanceController> logger, 
            Zenfiles.PermissionService.IPermission permission,IMFilesObjectRepository repository, GetCacheObjects cacheObjects)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _logger = logger;
            _permission = permission;
            _repository = repository;
            _cacheObjects = cacheObjects;

        }
        [HttpGet("GetClassTemplate/{VaultGuid}/{ClassID}")]
        public IActionResult GetClassTemplate(string VaultGuid, string ClassID)
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
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, ClassID);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
                }
                //adding is template property
                {
                    // Create the "maximum" search condition.
                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "name or title" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal the search string provided.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items that are named "hello world".
                    // Note that the type must both match the property definition type, and be applicable for the
                    // supplied value.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
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
                        string fileextension = "";
                        if (objectVersion.SingleFile)
                        {
                            fileextension = objectVersion.Files[1].Extension;
                        }
                        Response.Add(new Objectsearchresponse { ClassTypeName = classname, ObjectTypeName=objecttypeid.NameSingular, VersionId = objectVersion.ObjVer.Version,  id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = new UserPermission { AttachObjectsPermission = true, DeletePermission = true, EditPermission = true, ReadPermission = true }, LastModifiedUtc = objectVersion.LastModifiedUtc, CreatedUtc= objectVersion.LastModifiedUtc, DisplayID=objectVersion.DisplayID, IsSingleFile= objectVersion.SingleFile, FileExtension=fileextension, HasRelationship=objectVersion.HasRelationshipsToThis||objectVersion.HasRelationshipsFromThis, checkoutusername = objectVersion.CheckedOutToUserName });
                    }

                    return Ok(Response);

                }
                else
                {
                    return NotFound();
                }

            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetTemplate/{VaultGuid}")]
        public IActionResult GetTemplate(string VaultGuid)
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

                //adding is template property
                {
                    // Create the "maximum" search condition.
                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "name or title" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal the search string provided.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items that are named "hello world".
                    // Note that the type must both match the property definition type, and be applicable for the
                    // supplied value.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    List<Objectsearchresponse> Response = new List<Objectsearchresponse>();
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
                        string fileextension = "";
                        if (objectVersion.SingleFile)
                        {
                            fileextension = objectVersion.Files[1].Extension;
                        }
                        Response.Add(new Objectsearchresponse { ClassTypeName = classname, ObjectTypeName = objecttypeid.NameSingular, VersionId = objectVersion.ObjVer.Version, id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type, userPermission = new UserPermission { AttachObjectsPermission = true, DeletePermission = true, EditPermission = true, ReadPermission = true }, LastModifiedUtc = objectVersion.LastModifiedUtc, CreatedUtc = objectVersion.LastModifiedUtc, DisplayID = objectVersion.DisplayID, IsSingleFile = objectVersion.SingleFile, FileExtension = fileextension, HasRelationship = objectVersion.HasRelationshipsToThis || objectVersion.HasRelationshipsFromThis, checkoutusername = objectVersion.CheckedOutToUserName });
                    }

                    return Ok(Response);

                }
                else
                {
                    return NotFound();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetClassTemplateProps/{VaultGuid}/{ClassID}/{ObjectId}/{UserID}")]
        public async Task<IActionResult> GetClassTemplateProps(string VaultGuid, string ClassID,string ObjectId, int UserID)
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
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, ClassID);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
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
                //adding is template property
                {
                    // Create the "maximum" search condition.
                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "name or title" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal the search string provided.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items that are named "hello world".
                    // Note that the type must both match the property definition type, and be applicable for the
                    // supplied value.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
              
                    if (searchResults.Count > 0)
                    {
                        forlogs objectsearchresponse = new forlogs();

                    try
                    {
                            List<properties5> props = new List<properties5>();
                            List<properties5> props1 = new List<properties5>();
                            List<property3> hiddenlist = new List<property3>();

                            foreach (ObjectVersion objectVersion in searchResults)
                            {
                            objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                            objectsearchresponse.id = objectVersion.ObjVer.ID;
                            objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                            objectsearchresponse.Title = objectVersion.Title;
                            objectsearchresponse.CreatedUtc = objectVersion.CreatedUtc;
                            objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                            objectsearchresponse.ClassID = objectVersion.Class;
                            objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                            objectsearchresponse.VaultGuid = VaultGuid;
                            objectsearchresponse.DisplayID = objectVersion.DisplayID;
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
                                var objectt = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m=>m.ObjectType.ID== objectVersion.ObjVer.Type);
                                objectsearchresponse.ClassTypeName = classname;
                                if(objectt != null)
                                objectsearchresponse.ObjectTypeName = objectt.ObjectType.NameSingular;
                            }
                            catch
                            {

                            }
                            var propertiespdf = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m => m.ID == objectVersion.Class)?.AssociatedPropertyDefs;
                                List<templateints> ints = new List<templateints>();
                                if(propertiespdf != null)
                                foreach (AssociatedPropertyDef associatedPropertyDef in propertiespdf)
                                {   
                                    ints.Add(new templateints { propid= associatedPropertyDef .PropertyDef, isrequired= associatedPropertyDef .Required});
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

                                }

                                var propspd = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);


                                foreach (PropertyValueForDisplay properties in propspd)
                                {
                                  
                                    {
                                    var property = _cacheObjects.PropTypes(vault)?.FirstOrDefault(m=>m.PropertyDef.ID== properties.PropertyDef);
                                    try
                                    {

                                        if (property != null)
                                        {
                                            if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                            {
                                                var prop = props.FirstOrDefault(m => m.propId == properties.PropertyDef);
                                                if (prop == null)
                                                {
                                                    var perm = _permission.PropPermission(vault, UserID, properties.PropertyDef);
                                                    if (perm.ReadPermission)
                                                    {
                                                        props.Add(new properties5 { propId = int.Parse(properties.PropertyDef.ToString()), propertytype = properties.DataType.ToString(), title = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = false, userPermission = perm, PropGuid = property.PropertyDef.GUID, Alias = property.SemanticAliases.Value });
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var prop = props.FirstOrDefault(m => m.propId == properties.PropertyDef);
                                                if (prop == null)
                                                {
                                                    var perm = _permission.PropPermission(vault, UserID, properties.PropertyDef);
                                                    if (perm.ReadPermission)
                                                    {

                                                        props.Add(new properties5 { propId = int.Parse(properties.PropertyDef.ToString()), propertytype = properties.DataType.ToString(), title = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = true, userPermission = perm, PropGuid = property.PropertyDef.GUID, Alias = property.SemanticAliases.Value });
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
                                    catch
                                    {
                                        props.Add(new properties5 { propId = int.Parse(properties.PropertyDef.ToString()), propertytype = properties.DataType.ToString(), title = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = false, userPermission = new UserPermission { AttachObjectsPermission = false, DeletePermission = false, EditPermission = false, IsClassDeleted = false, ReadPermission = true }, PropGuid = property.PropertyDef.GUID, Alias = property.SemanticAliases.Value });

                                    }

                                }
                                


                                };
                            IMeta meta = new MetaImplement();
                            var classsp = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m => m.ID == objectVersion.Class);
                            var tpp = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == classsp.ObjectType);
                            if (classsp != null && tpp != null)
                            {

                                var items = meta.behaveprops(objectVersion.ObjVer.Type.ToString(),tpp.SemanticAliases.Value, vault.ClassOperations.GetObjectClassAdmin(classsp.ID).SemanticAliases.Value, objectVersion.Class.ToString(), workflowstatepropbehave, VaultGuid);

                                var itemss = items.Where(m => m.IsHidden && !m.IsRequired).OrderBy(m => m.Property);

                                foreach (var hidden in itemss)
                                {
                                    var sr = props.FirstOrDefault(m => m.propId.ToString() == hidden.Property);
                                    if (sr != null)
                                    {
                                        if (!sr.IsRequired)
                                        {
                                            var t = props.Where(m => m.propId.ToString() != hidden.Property).ToList();
                                            props = t;
                                        }
                                    }

                                }

                                var isrequired = items.Where(m => m.IsRequired).OrderBy(m => m.Property);
                                foreach (var r in isrequired)
                                {
                                    var tt = props.FirstOrDefault(m => m.propId.ToString() == r.Property);
                                    if (tt != null)
                                    {
                                        properties5 updateprop1 = new properties5();
                                        updateprop1 = tt;
                                        updateprop1.IsRequired = true;
                                        props.RemoveAll(m => m.propId == tt.propId);
                                        props.Add(updateprop1);
                                    }

                                }
                                foreach (var t in ints)
                                {
                                    var propt = props.FirstOrDefault(m => m.propId == t.propid);
                                    if (propt != null)
                                    {
                                        propt.IsRequired = t.isrequired;
                                        props1.Add(propt);
                                        props.RemoveAll(m => m.propId == t.propid);
                                    }

                                }
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
                        }
                            foreach (var t in hiddenlist)
                            {
                                props.RemoveAll(m => m.propId == t.propId);
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
        [HttpPost("ObjectCreation")]
        public async Task<IActionResult> ObjectCreationAsync([FromBody] template mfilesCreate)
        {

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var filepath = Path.Combine(path, Guid.NewGuid().ToString());
           
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
                Dictionary<string, string> mergeFields = new Dictionary<string, string>();
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
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup, mfilesCreate.classID);

                    // Add it to the conditions.
                    searchConditions.Add(-1, searchCondition);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, mfilesCreate.ObjectID);
                    searchConditions.Add(-1, condition);
                }
                //adding is template property
                {
                    // Create the "maximum" search condition.
                    // Create the search condition.
                    var searchCondition = new SearchCondition();

                    // We want to search by property - in this case the built-in "name or title" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    searchCondition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal the search string provided.
                    searchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items that are named "hello world".
                    // Note that the type must both match the property definition type, and be applicable for the
                    // supplied value.
                    searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeBoolean, true);

                    searchConditions.Add(-1, searchCondition);
                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    string title = "";
                    string extension = "";
                    string classname = "";
                    string workflowname = "";
                    string statename = "";
                    // Define the property values for the new object.
                    var propertyValues = new MFilesAPI.PropertyValues();
                    foreach (ObjectVersion objectVersion1 in searchResults)
                    {
                        var propertiesfordisplay = vault.ObjectPropertyOperations.GetProperties(objectVersion1.ObjVer);
                        foreach(PropertyValue propertyValueForDisplay in propertiesfordisplay)
                        {
                            if(!string.IsNullOrEmpty(propertyValueForDisplay.Value.DisplayValue)&& propertyValueForDisplay.PropertyDef!=20&& propertyValueForDisplay.PropertyDef != 21 && propertyValueForDisplay.PropertyDef!=23 && propertyValueForDisplay.PropertyDef != 25 && propertyValueForDisplay.PropertyDef != 37)
                            {
                                if(mfilesCreate.properties!=null)
                                if(!mfilesCreate.properties.Any(m=>m.propId== propertyValueForDisplay.PropertyDef))
                                {
                                    propertyValues.Add(-1, propertyValueForDisplay);
                                    if (propertyValueForDisplay.PropertyDef == 100)
                                        classname = propertyValueForDisplay.Value.DisplayValue;
                                    if (propertyValueForDisplay.PropertyDef == 38)
                                        workflowname = propertyValueForDisplay.Value.DisplayValue;
                                    if (propertyValueForDisplay.PropertyDef == 39)
                                        statename = propertyValueForDisplay.Value.DisplayValue;
                                }                             

                            }
                          
                        }

                        title = objectVersion1.Title;
                        foreach (ObjectFile objectFile in objectVersion1.Files)
                        {
                            filepath += $".{objectFile.Extension}";
                            vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version,filepath);
                            extension = objectFile.Extension;
                        }
                    }
                    #region setting props
                    {
                        #region class prop
                        {
                           
                            if(!string.IsNullOrEmpty(classname))
                            if (extension.ToLower().Contains("xls"))
                            {
                                mergeFields.Add("MFiles_PG{CEBF9AC9-C60C-4240-9F50-723DBF3A5CA7}".Replace("-", "").Replace("{", "").Replace("}", ""), classname);

                            }
                            else
                            {
                                mergeFields.Add($"[Class]", classname);
                            }
                        }
                        #endregion
                        #region setting workflow and state properties
                        {
                            if (!string.IsNullOrEmpty(workflowname)&& !string.IsNullOrEmpty(statename))
                            if (extension.ToLower().Contains("xls"))
                            {
                                mergeFields.Add("MFiles_PG{C0FC7EB6-B9FD-4D4A-9ACA-E5B0B0F54304}".Replace("-", "").Replace("{", "").Replace("}", ""), workflowname);
                                mergeFields.Add("MFiles_PG{2A9D33A6-E923-411D-A68A-A4BBCB58033A}".Replace("-", "").Replace("{", "").Replace("}", ""), statename);
                            }
                            else
                            {
                                mergeFields.Add("[Workflow]", workflowname);
                                mergeFields.Add("[State]", statename);

                            }
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
                            if(mfilesCreate.properties !=null)
                            {
                                foreach (var item in mfilesCreate.properties)
                                {
                                    if (!string.IsNullOrEmpty(item.DisplayValue))
                                        if (extension.ToLower().Contains("xls"))
                                    {
                                        mergeFields.Add($"MFiles_PG{item.PropGuid}".Replace("-", "").Replace("{", "").Replace("}", ""), item.DisplayValue);
                                    }
                                        else
                                    {
                                        mergeFields.Add($"[{item.PropertyName}]", item.DisplayValue);
                                    }
                                    //setting properties
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

                        }
                        #endregion
                    }
                    #endregion
                    // Add one file.
                    var myFile = new MFilesAPI.SourceObjectFile();
                    myFile.SourceFilePath = filepath;
                    myFile.Title = title; // For single-file-documents this is ignored.
                    myFile.Extension = extension;
                    // Define the source files to add.
                    var sourceFiles = new MFilesAPI.SourceObjectFiles();
                    sourceFiles.Add(-1, myFile);

                    // What object type is being created?
                    var objectTypeID = mfilesCreate.objectTypeID;

                    // A "single file document" must be both a document and contain exactly one file.
                    var isSingleFileDocument =
                        objectTypeID == mfilesCreate.objectTypeID && sourceFiles.Count == 1;

                    // Create the object and check it in.
                    var objectVersion = vault.ObjectOperations.CreateNewObjectEx(
                        objectTypeID,
                        propertyValues,
                        sourceFiles,
                        SFD: isSingleFileDocument,
                        CheckIn: false);

                    if (extension.ToLower().Contains("xls"))
                    {
                        ExcelMerger excelMerger = new ExcelMerger();
                        var filepaths = excelMerger.merge(vault,filepath, objectVersion.VersionData);
                        System.IO.File.Move(filepaths,filepath,true);
                        foreach (ObjectFile file in objectVersion.VersionData.Files)
                        {
                            vault.ObjectFileOperations.UploadFile(file.ID, file.Version,filepath);
                        }
                    }
                    else if (IsWordSupportedDocument(filepath))
                    {
                        WordMerger wordMerger= new WordMerger();
                        var filepaths = wordMerger.merge(vault,filepath, objectVersion.VersionData);
                        foreach (ObjectFile file in objectVersion.VersionData.Files)
                        {
                            vault.ObjectFileOperations.UploadFile(file.ID, file.Version, filepath);
                        }
                    }
                    // Check the object back in.
                    vault.ObjectOperations.CheckIn(objectVersion.ObjVer);
                    System.IO.File.Delete(filepath);
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

                    return Ok(objectresp);
                }
                else
                {
                    return NotFound("No document found as a template with that ID in this class");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        public static bool IsWordSupportedDocument(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            string[] wordExtensions = {
        ".docx", ".dotx", ".docm", ".dotm",
        ".doc", ".dot",
        ".rtf", ".txt", ".xml", ".odt"
    };

            return wordExtensions.Contains(ext);
        }
    }
}
