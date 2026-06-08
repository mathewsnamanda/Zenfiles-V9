using MFilesAPI;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Hybrid;
using Newtonsoft.Json.Linq;
using readingmetaconfigjson.metaServices;
using Syncfusion.XlsIO.Parser.Biff_Records;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Security;
using System.Text.RegularExpressions;
using Zenfiles.Models.Workflow;
using Zenfiles.PermissionService;
using ZenFiles.Models;
using Zenfiles_V8.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZenFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    public class MfilesObjectsController : ControllerBase
    {
        private readonly GetCacheObjects _cacheObjects;
        private readonly IConfiguration _configuration;
        private readonly Zenfiles.PermissionService.IPermission _permission;
        public MfilesObjectsController(IConfiguration Configuration, GetCacheObjects cacheObjects, Zenfiles.PermissionService.IPermission permission)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _cacheObjects = cacheObjects;
            _permission = permission;
        }
        // GET: api/<MfilesObjectsController>
        [HttpGet("GetVaultsObjects/{VaultGuid}/{UserID}")]
        public async Task<IActionResult> GetVaultsObjectsAsync(string VaultGuid, int UserID)
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
                var results = new List<ObjtypeRespModel>();
                var vault = mfServerApplication.LogInToVault(VaultGuid);
                var mfilesobjects = _cacheObjects.GetObjectTypes(vault);

                mfilesobjects.RemoveAll(o=>o.ObjectType.External);
                foreach (var mfile in mfilesobjects)
                {
                   var permissionp = _permission.ObjectPermission(vault, UserID, mfile.ObjectType.ID);
                    if (permissionp.ReadPermission)
                    {
                       results.Add(new ObjtypeRespModel { External=false, nameplural= mfile.ObjectType.NamePlural, namesingular= mfile.ObjectType.NameSingular, objectid= mfile.ObjectType.ID, userPermission=permissionp });
                    }               
                }
                if (results.Count == 0)
                    return NotFound();
                return Ok(results);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
         
        }
        // GET api/<MfilesObjectsController>/5
        [HttpGet("GetObjectClasses/{VaultGuid}/{Objectid}/{UserID}")]
        public async Task<IActionResult> GetAsync(int Objectid,string VaultGuid,int UserID)
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
                var classGroupps = new List<ClassGroupp>();
                var ungrouped = new List<ObjectClassp>();
                var ids = new List<ObjectClassp>();

                var vault = mfServerApplication.LogInToVault(VaultGuid);

                var classes = _cacheObjects.ClassTypes(vault); 
                var classgroup = _cacheObjects.ClassGroupTypes(vault, Objectid);

                // Build grouped classes
                foreach (ClassGroup classgroupp in classgroup)
                {
                    var objectClassps = new List<ObjectClassp>();
                    foreach (var member in classgroupp.Members)
                    {
                        var objectclass = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m=>m.ID==Convert.ToInt16(member));
                        var perm = _permission.ClassPermission(vault, UserID, objectclass.ID);
                        if (perm.ReadPermission)
                        {
                            ids.Add(new ObjectClassp { classId = objectclass.ID, ClassName = objectclass.Name, userPermission = perm });
                            objectClassps.Add(new ObjectClassp { classId = objectclass.ID, ClassName = objectclass.Name, userPermission = perm });

                        }
                    }
                    if(objectClassps.Count>0)
                    classGroupps.Add(new ClassGroupp
                    {
                        ClassGroupId = classgroupp.ID,
                        ClassGroupName = classgroupp.Name,
                        members = objectClassps
                    });
                }
                var classp = _cacheObjects.ClassTypes(vault)?.Where(m => m.ObjectType == Objectid);
                if(classp!=null)
                // Build ungrouped classes
                foreach (ObjectClass item in classp)
                {
                    if (!ids.Any(m => m.classId == item.ID))
                    {
                        var perm = _permission.ClassPermission(vault, UserID, item.ID);
                        ungrouped.Add(new ObjectClassp { classId = item.ID, ClassName = item.Name, userPermission = perm });
                    }
                }

                // Remove duplicates
                foreach (var item in ids)
                    ungrouped.RemoveAll(u => u.classId == item.classId);


                return Ok(new ClassRespo
                {
                    UnGrouped = ungrouped,
                    Grouped = classGroupps,
                    ObjectId = Objectid
                });

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("ClassProps/{VaultGuid}/{ObjectTypeID}/{ClassID}/{UserID}")]
        public async Task<IActionResult> Get(string VaultGuid,int ClassID,int ObjectTypeID,int UserID)
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
                
                var classsp = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m=>m.ID== ClassID);
                if (classsp == null)
                    return NotFound(); // empty list signals not found

                var tpp = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == classsp.ObjectType);
                if(tpp == null)   return NotFound();

                var props = new List<property3>();
                var workflowstatepropbehave = new List<readingmetaconfigjson.MetaModals.workflowstatepropbehave>();
                if (tpp.ObjectType.OwnerPropertyDef >= 0&& tpp.ObjectType.OwnerType>0)
                {
                    var prop = _cacheObjects.PropTypes(vault)?.FirstOrDefault(m => m.PropertyDef.ID == tpp.ObjectType.OwnerPropertyDef);
                    var valuelisttst = vault.ValueListOperations.GetValueList(prop.PropertyDef.ValueList);

                    props.Add(new property3
                    {
                         alias=prop.SemanticAliases.Value,
                         AllowAdding = valuelisttst.AllowAdding,
                         IsAutomatic=false,
                         IsHidden=false,
                         IsRequired=true,
                         objectTypeVL= valuelisttst.RealObjectType,
                         propertytype=prop.PropertyDef.DataType.ToString(), 
                         propId=prop.PropertyDef.ID,
                         title=prop.PropertyDef.Name,
                         TypeID=valuelisttst.ID,
                         userPermission=
                         new pulling_object_permission.UserPermission { AttachObjectsPermission=true, DeletePermission=false,
                         EditPermission=true, IsClassDeleted=false, ReadPermission=true},
                                    
                    });
                }
                // Handle workflow
                if (classsp.ForceWorkflow)
                {
                    var workflow = _cacheObjects.WorkflowTypes(vault)?.FirstOrDefault(m => m.Workflow.ID == classsp.Workflow);
                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                    {
                        propertytype = MFDataType.MFDatatypeLookup.ToString(),
                        propertyvalue = workflow.Workflow.ID.ToString(),
                        propid = "38",
                        workflowstatealias = workflow.SemanticAliases.Value,
                        workflowstateguid = vault.ValueListItemOperations.GetValueListItemByID(7, workflow.Workflow.ID).ItemGUID,
                        workflowstateid = workflow.Workflow.ID.ToString()
                    });
                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                    {
                        propertytype = MFDataType.MFDatatypeLookup.ToString(),
                        propertyvalue = "",
                        propid = "39",
                        workflowstatealias = "",
                        workflowstateguid = "",
                        workflowstateid = ""
                    });
                }
                else
                {

                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                    {
                        propertytype = MFDataType.MFDatatypeLookup.ToString(),
                        propertyvalue = "",
                        propid = "38",
                        workflowstatealias = "",
                        workflowstateguid = "",
                        workflowstateid = ""
                    });
                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave
                    {
                        propertytype = MFDataType.MFDatatypeLookup.ToString(),
                        propertyvalue = "",
                        propid = "39",
                        workflowstatealias = "",
                        workflowstateguid = "",
                        workflowstateid = ""
                    });
                }

                IMeta meta = new MetaImplement();
              
                // Iterate associated properties
                foreach (AssociatedPropertyDef propertyDefAdmin in classsp.AssociatedPropertyDefs)
                {
                    var property = _cacheObjects.PropTypes(vault)?.FirstOrDefault(m => m.PropertyDef.ID == propertyDefAdmin.PropertyDef);
                     
                    if (property.PropertyDef.ID == 0 || property.PropertyDef.ID == 41 ||
                        property.PropertyDef.ID == 44 || property.PropertyDef.ID == 42 ||
                        property.PropertyDef.ID > 101)
                    {
                        var perm = _permission.PropPermission(vault, UserID, property.PropertyDef.ID);

                        if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                        {
                            if (!props.Any(m => m.propId == property.PropertyDef.ID))
                            {
                                if (property.PropertyDef.DataType == MFDataType.MFDatatypeLookup ||
                                    property.PropertyDef.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                                {
                                    var propertypd = _cacheObjects.PropTypes(vault)?.FirstOrDefault(m => m.PropertyDef.ID == property.PropertyDef.ID);
                                    var valuelistt = vault.ValueListOperations.GetValueList(propertypd.PropertyDef.ValueList);

                                    props.Add(new property3
                                    {
                                        propId = property.PropertyDef.ID,
                                        propertytype = property.PropertyDef.DataType.ToString(),
                                        title = property.PropertyDef.Name,
                                        IsHidden = false,
                                        IsRequired = propertyDefAdmin.Required,
                                        IsAutomatic = false,
                                        userPermission = perm,
                                        AllowAdding = valuelistt.AllowAdding,
                                        objectTypeVL = valuelistt.RealObjectType,
                                        TypeID = valuelistt.ID,
                                        alias = property.SemanticAliases.Value
                                    });

                                }
                                else
                                {
                                    props.Add(new property3
                                    {
                                        propertytype = property.PropertyDef.DataType.ToString(),
                                        propId = property.PropertyDef.ID,
                                        title = property.PropertyDef.Name,
                                        IsRequired = propertyDefAdmin.Required,
                                        IsHidden = false,
                                        IsAutomatic = false,
                                        userPermission = perm,
                                        alias = property.SemanticAliases.Value
                                    });
                                }
                            }
                        }
                        else
                        {
                            if (!props.Any(m => m.propId == property.PropertyDef.ID))
                            {
                                props.Add(new property3
                                {
                                    propertytype = property.PropertyDef.DataType.ToString(),
                                    propId = property.PropertyDef.ID,
                                    title = property.PropertyDef.Name,
                                    IsRequired = propertyDefAdmin.Required,
                                    IsHidden = false,
                                    IsAutomatic = true,
                                    userPermission = perm,
                                    alias = property.SemanticAliases.Value
                                });
                            }
                        }
                    }
                }

                // Apply meta behaviors
                var items = meta.behaveprops(ObjectTypeID.ToString(), tpp.SemanticAliases.Value, vault.ClassOperations.GetObjectClassAdmin(ClassID).SemanticAliases.Value, ClassID.ToString(), workflowstatepropbehave, VaultGuid);

                var hiddenprops = items.Where(m => m.IsHidden && !m.IsRequired).OrderBy(m => m.Property);
                foreach (var hidden in hiddenprops)
                {
                    var sr = props.FirstOrDefault(m => m.propId.ToString() == hidden.Property || m.alias == hidden.Property);
                    if (sr != null)
                    {
                        if (!sr.IsRequired)
                        {
                            var t = props.Where(m => m.propId.ToString() != hidden.Property && m.alias != hidden.Property).ToList();
                            props = t;
                        }
                    }

                }



                var isrequired = items.Where(m => m.IsRequired).OrderBy(m => m.Property);

                foreach (var ttp in isrequired)
                {
                    var prop = props.FirstOrDefault(m => m.propId.ToString() == ttp.Property || m.alias.ToString() == ttp.Property);
                    if (prop != null)
                    {
                        prop.IsRequired = false;
                        props.RemoveAll(m => m.propId == prop.propId);
                        props.Add(prop);
                    }
                }

                if (props.Count == 0)
                    return NotFound("Class with id doesn't exist or has no properties");

                return Ok(props);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
