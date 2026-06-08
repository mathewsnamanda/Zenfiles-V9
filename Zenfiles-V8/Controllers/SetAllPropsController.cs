using DocumentFormat.OpenXml.Drawing.Charts;
using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using readingmetaconfigjson.MetaModals;
using readingmetaconfigjson.metaServices;
using System;
using System.Runtime.InteropServices;
using Zenfiles.Models.objversions;
using Zenfiles.Models.setprops;
using Zenfiles.Models.views;
using ZenFiles.Models;
using Zenfiles_V8.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SetAllPropsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly GetCacheObjects _cacheObjects;
        public SetAllPropsController(IConfiguration Configuration, GetCacheObjects cacheObjects)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _cacheObjects = cacheObjects;

        }
        // GET api/<SetAllPropsController>/5
        [HttpGet("GetNewClassProps/{VaultGuid}/{ObjectId}/{ClassId}/{NewClassID}")]
        public IActionResult GetNewClassProps(string VaultGuid, string ObjectId, string ClassId, int NewClassID)
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
                  
                    List<updateprop1> props = new List<updateprop1>();
                    List<updateprop1> props1 = new List<updateprop1>();
                    List<updateprop1> props6 = new List<updateprop1>();
                    List<workflowstatepropbehave> workflowstatepropbehave = new List<workflowstatepropbehave>();
                    List<property3> hiddenlist = new List<property3>();
                    var NewClass = vault.ClassOperations.GetObjectClass(NewClassID);
                   
                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                        var classprops = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);
                        foreach(PropertyValueForDisplay properties in classprops)
                        {
                            if (vault.ClassOperations.GetObjectClass(objectVersion.Class).NamePropertyDef != 0)
                            {
                                if (properties.PropertyDef > 0)
                                {
                                    var property = vault.PropertyDefOperations.GetPropertyDefAdmin(properties.PropertyDef);
                                    if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                    {
                                        var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                        if (prop == null)
                                        {
                                            props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = false, Alias = property.SemanticAliases.Value });

                                        }
                                    }
                                    else
                                    {
                                        var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                        if (prop == null)
                                        {
                                            props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = true, Alias = property.SemanticAliases.Value });

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
                                                workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias =  property.SemanticAliases.Value });
                                            }
                                        }
                                        else
                                        {
                                            workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = "", workflowstateid = "", workflowstatealias =  property.SemanticAliases.Value });

                                        }

                                    }
                                }

                            }
                            else
                            {
                                var property = vault.PropertyDefOperations.GetPropertyDefAdmin(properties.PropertyDef);
                                if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                {
                                    var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                    if (prop == null)
                                    {
                                        props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = false, Alias = property.SemanticAliases.Value });

                                    }
                                }
                                else
                                {
                                    var prop = props.FirstOrDefault(m => m.id == properties.PropertyDef);
                                    if (prop == null)
                                    {
                                        props.Add(new updateprop1 { id = int.Parse(properties.PropertyDef.ToString()), Datatype = properties.DataType.ToString(), PropName = properties.PropertyDefName, Value = properties.DisplayValue, IsHidden = false, IsRequired = false, IsAutomatic = true, Alias = property.SemanticAliases.Value });

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
                                            workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = lookup.ItemGUID, workflowstateid = lookup.Item.ToString(), workflowstatealias =  property.SemanticAliases.Value });
                                        }
                                    }
                                    else
                                    {
                                        workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = properties.PropertyDef.ToString(), workflowstateguid = "", workflowstateid = "", workflowstatealias = property.SemanticAliases.Value });

                                    }

                                }
                            }
                        }
                    }
                    foreach (AssociatedPropertyDef propertyDefClass in NewClass.AssociatedPropertyDefs)
                    {
                        var found = props.FirstOrDefault(m=>m.id==propertyDefClass.PropertyDef);
                        if (found != null)
                        {
                            props6.Add(found);
                        }
                        else
                        {
                            if (NewClass.NamePropertyDef != 0)
                            {
                                if (propertyDefClass.PropertyDef > 0)
                                {
                                    var property = vault.PropertyDefOperations.GetPropertyDefAdmin(propertyDefClass.PropertyDef);
                                    if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                    {
                                        var prop = props.FirstOrDefault(m => m.id == propertyDefClass.PropertyDef);
                                        if (prop == null)
                                        {
                                            props6.Add(new updateprop1 { id = int.Parse(propertyDefClass.PropertyDef.ToString()), Datatype = property.PropertyDef.DataType.ToString(), PropName = property.PropertyDef.Name, Value = "", IsHidden = false, IsRequired = false, IsAutomatic = false, Alias = property.SemanticAliases.Value });

                                        }
                                    }
                                    else
                                    {
                                        var prop = props.FirstOrDefault(m => m.id == propertyDefClass.PropertyDef);
                                        if (prop == null)
                                        {
                                            props6.Add(new updateprop1 { id = int.Parse(propertyDefClass.PropertyDef.ToString()), Datatype = property.PropertyDef.DataType.ToString(), PropName = property.PropertyDef.Name, Value = "", IsHidden = false, IsRequired = false, IsAutomatic = true, Alias = property.SemanticAliases.Value });

                                        }
                                    }
                                }

                            }
                            else
                            {
                                var property = vault.PropertyDefOperations.GetPropertyDefAdmin(propertyDefClass.PropertyDef);
                                if (property.PropertyDef.AutomaticValueType.ToString() == "MFAutomaticValueTypeNone")
                                {
                                    var prop = props.FirstOrDefault(m => m.id == propertyDefClass.PropertyDef);
                                    if (prop == null)
                                    {
                                        props6.Add(new updateprop1 { id = int.Parse(propertyDefClass.PropertyDef.ToString()), Datatype = property.PropertyDef.DataType.ToString(), PropName = property.PropertyDef.Name, Value = "", IsHidden = false, IsRequired = false, IsAutomatic = false, Alias = property.SemanticAliases.Value });

                                    }
                                }
                                else
                                {
                                    var prop = props.FirstOrDefault(m => m.id == propertyDefClass.PropertyDef);
                                    if (prop == null)
                                    {
                                        props6.Add(new updateprop1 { id = int.Parse(propertyDefClass.PropertyDef.ToString()), Datatype = property.PropertyDef.DataType.ToString(), PropName = property.PropertyDef.Name, Value = "", IsHidden = false, IsRequired = false, IsAutomatic = true, Alias = property.SemanticAliases.Value });

                                    }
                                }
                            }
                        }
                    }
                    List<string> ints = new List<string>();
                    foreach(var item in workflowstatepropbehave)
                    {
                        var foundp = props6.Any(m=>m.id.ToString()==item.propid);
                        if (!foundp)
                        {
                            ints.Add(item.propid);
                       //     
                        }
                    }
                    foreach(var t in ints)
                    {
                        workflowstatepropbehave.RemoveAll(m => m.propid == t);
                    }
                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "38", workflowstateguid = "", workflowstateid = "", workflowstatealias = "" });
                    workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = "39", workflowstateguid = "", workflowstateid = "", workflowstatealias = "" });
                    foreach(var prop in props6)
                    {
                        if (prop.Datatype == MFDataType.MFDatatypeLookup.ToString())
                        {
                            var found = workflowstatepropbehave.Any(m=>m.propid==prop.id.ToString());
                            if(!found)
                            workflowstatepropbehave.Add(new readingmetaconfigjson.MetaModals.workflowstatepropbehave { propid = prop.id.ToString(), workflowstateguid = "", workflowstateid = "", workflowstatealias = "" });
                        }
                    }
                    IMeta meta = new MetaImplement();

                    var classsp = _cacheObjects.ClassTypes(vault)?.FirstOrDefault(m => m.ID == NewClassID);                  
                    var tpp = _cacheObjects.GetObjectTypes(vault)?.FirstOrDefault(m => m.ObjectType.ID == classsp.ObjectType);
                    if (classsp != null && tpp != null)
                    {
                        var items = meta.behaveprops(NewClass.ObjectType.ToString(),tpp.SemanticAliases.Value, vault.ClassOperations.GetObjectClassAdmin(NewClassID).SemanticAliases.Value, NewClassID.ToString(), workflowstatepropbehave, VaultGuid);
                        var itemss = items.Where(m => m.IsHidden).OrderBy(m => m.Property);

                        foreach (var t in itemss)
                        {
                            var checkre = props6.FirstOrDefault(m => m.id.ToString().Trim() == t.Property || m.Alias == t.Property).IsRequired;
                            if (!checkre)
                                props6.RemoveAll(m => m.id.ToString().Trim() == t.Property);
                        }
                        var isrequired = items.Where(m => m.IsRequired).OrderBy(m => m.Property);
                        foreach (var r in isrequired)
                        {
                            var tt = props6.FirstOrDefault(m => m.id.ToString() == r.Property || m.Alias == r.Property);
                            if (tt != null)
                            {
                                updateprop1 updateprop1 = new updateprop1();
                                updateprop1 = tt;
                                updateprop1.IsRequired = true;
                                props6.RemoveAll(m => m.id == tt.id);
                                props6.Add(updateprop1);
                            }

                        }
                    }
                    return Ok(props6);
                }
                else
                {
                    return NotFound("Object with that ID and class not found");
                }
                }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // GET api/<SetAllPropsController>/5
        [HttpGet("GetTransitionClasses/{VaultGuid}/{ObjectId}/{ClassId}")]
        public IActionResult GetTransitionClasses(string VaultGuid, string ObjectId, string ClassId)
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
                    List<ClassGroupp> classGroupps = new List<ClassGroupp>();
                    List<ObjectClassp> ungrouped = new List<ObjectClassp>();
                    List<ObjectClassp> ungroupedp = new List<ObjectClassp>();
                    List<ObjectClassp> ids = new List<ObjectClassp>();
                    ClassRespo objtypeRespModel = new ClassRespo();


                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                        var getobject = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);

                        if (!getobject.External)
                        {
                            var classes = vault.ClassOperations.GetObjectClasses(objectVersion.ObjVer.Type);
                            var classgroup = vault.ClassGroupOperations.GetClassGroups(objectVersion.ObjVer.Type);
                            foreach (ClassGroup classgroupp in classgroup)
                            {
                                List<ObjectClassp> objectClassps = new List<ObjectClassp>();
                                var classesingrouo = classgroupp.Members;
                                foreach (var member in classesingrouo)
                                {
                                    var objectclass = vault.ClassOperations.GetObjectClass(int.Parse(member.ToString()));
                                    ids.Add(new ObjectClassp { classId = objectclass.ID, ClassName = objectclass.Name });
                                    objectClassps.Add(new ObjectClassp { classId = objectclass.ID, ClassName = objectclass.Name });
                                }
                                classGroupps.Add(new ClassGroupp { ClassGroupId = classgroupp.ID, ClassGroupName = classgroupp.Name, members = objectClassps });
                            }
                            foreach (ObjectClass item in classes)
                            {
                                if (!ids.Any(m => m.classId == item.ID))
                                    ungrouped.Add(new ObjectClassp { classId = item.ID, ClassName = item.Name });
                            }

                            foreach (var item in ids)
                            {
                                ungrouped.Remove(item);
                            }
                            objtypeRespModel.UnGrouped = ungrouped;
                            objtypeRespModel.Grouped = classGroupps;
                            objtypeRespModel.ObjectId = objectVersion.ObjVer.Type;
                        }
                    }
                    return Ok(objtypeRespModel);
                }
                else
                {
                    return NotFound("Object with that ID and class not found");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // POST api/<SetAllPropsController>
        [HttpPost]
        public IActionResult Post([FromBody] SetUpdateProps setUpdateProps)
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
                var vault = mfServerApplication.LogInToVault(setUpdateProps.VaultGuid);
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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, setUpdateProps.objectid);
                    searchConditions.Add(-1, condition);
                }
                //filter class id
                {
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] { setUpdateProps.classid };

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
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: objectVersion.OriginalObjID.Type,
                            ID: objectVersion.ObjVer.ID);
                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                        PropertyValues properties = new PropertyValues();

                        foreach (var property in setUpdateProps.props)
                        {
                            if (property.Datatype == "MFDatatypeMultiSelectLookup")
                            {
                                if (!string.IsNullOrEmpty(property.Value))
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
                        #region setting class id for the new class
                        {
                            // Create a property value to update.
                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                            {
                                PropertyDef = 100
                            };
                            nameOrTitlePropertyValue.Value.SetValue(
                                MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                               setUpdateProps.NewClassId
                            );
                            // Update the property on the server.
                            properties.Add(-1, nameOrTitlePropertyValue);
                        }
                        #endregion

                        vault.ObjectPropertyOperations.SetAllProperties(
                                 ObjVer: checkedOutObjectVersion.ObjVer,
                                 true,
                                 PropertyValues: properties);

                        var date = DateTime.UtcNow;
                        var lastModifiedBy = new TypedValue();
                        lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, setUpdateProps.UserID);
                        var lastModifiedDate = new TypedValue();
                        lastModifiedDate.SetValue(MFDataType.MFDatatypeTimestamp, new DateTime(date.Year, date.Month, date.Day, date.Hour - 3, date.Minute, date.Second));
                        vault.ObjectPropertyOperations.SetLastModificationInfoAdmin
                        (
                            checkedOutObjectVersion.ObjVer,
                            true, lastModifiedBy,
                            true, lastModifiedDate
                        );


                      
                        vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);
                    }
                    return Ok($"Successfully updated object : {setUpdateProps.objectid}");
                }
                else
                {
                    return NotFound("Object with that ID and class not found");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }
}
