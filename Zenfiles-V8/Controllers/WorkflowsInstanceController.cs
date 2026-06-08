using ConsoleApp1;
using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using pulling_object_permission;
using Syncfusion.XlsIO;
using System.Data;
using System.Numerics;
using System.Security;
using Zenfiles.Models;
using Zenfiles.Models.objversions;
using Zenfiles.Models.Workflow;
using Zenfiles.PermissionService;
using ZenFiles.Models;
using Zenfiles_V8.Models;
using Zenfiles_V8.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    public class WorkflowsInstanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Zenfiles.PermissionService.IPermission _permission;
        private readonly Gettingusersinusergroup _gettingusersinusergroup;

        public WorkflowsInstanceController(IConfiguration Configuration, Zenfiles.PermissionService.IPermission permission, Gettingusersinusergroup gettingusersinusergroup)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _permission = permission;
            _gettingusersinusergroup = gettingusersinusergroup;
        }
        // GET: api/<WorkflowsInstanceController>
        [HttpGet("GetVaultsWorkflows/{VaultGuid}/{UserID}")]
        public IActionResult GetVaultsWorkflows(string VaultGuid,int UserID)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            

            if (string.IsNullOrEmpty(VaultGuid))
            {
                return NotFound("Kindly check the vaultguid");
            }
            List<stateWorkflows> workflows = new List<stateWorkflows>();
           
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
                var workflowlist = vault.WorkflowOperations.GetWorkflowsAdmin();
                foreach (WorkflowAdmin workflow in workflowlist)
                {
                    var users = vault.UserOperations.GetLoginAccountOfUser(UserID);
                    if(users.ServerRoles.ToString()=="9")
                    {
                        var statelist = vault.WorkflowOperations.GetWorkflowStates(workflow.Workflow.ID);
                        List<stateStates> states = new List<stateStates>();

                        foreach (State state in statelist)
                        {
                            states.Add(new stateStates { StateId = state.ID, StateName = state.Name, IsSelectable = state.Selectable });
                        }
                        workflows.Add(new stateWorkflows { ClassId = workflow.Workflow.ObjectClass, States = states, WorkflowId = workflow.Workflow.ID, WorkflowName = workflow.Workflow.Name });

                    }
                    else
                    {
                        return Unauthorized("This features is only available to admins");
                    }
                }
                if (workflowlist.Count > 0)
                {
                    return Ok(workflows);
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
        [HttpGet("GetVaultsObjectClassTypeWorkflows/{VaultGuid}/{UserID}/{ObjectTypeid}/{ClassTypeId}")]
        public IActionResult GetVaultsWorkflows(string VaultGuid, int UserID,int ObjectTypeid,int ClassTypeId)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";


            if (string.IsNullOrEmpty(VaultGuid))
            {
                return NotFound("Kindly check the vaultguid");
            }
            List<stateWorkflows> workflows = new List<stateWorkflows>();

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

                var classworkflow = vault.ClassOperations.GetObjectClass(ClassTypeId);
                if (classworkflow.Workflow > 0)
                {
                    var workflowlist = vault.WorkflowOperations.GetWorkflowAdmin(classworkflow.Workflow);
                   
                    var workflowperm = _permission.WorkflowPermission(vault, UserID, classworkflow.Workflow);
                    if (workflowperm.AttachObjectsPermission)
                    {
                        var statelist = vault.WorkflowOperations.GetWorkflowStates(classworkflow.Workflow);
                        List<stateStates> states = new List<stateStates>();

                        foreach (State state in statelist)
                        {
                            states.Add(new stateStates { StateId = state.ID, StateName = state.Name, IsSelectable = state.Selectable });
                        }

                        workflows.Add(new stateWorkflows { ClassId = ClassTypeId, States = states, WorkflowId = classworkflow.Workflow, WorkflowName = workflowlist.Workflow.Name });

                    }
                }
                else
                {
                    var workflowlist = vault.WorkflowOperations.GetWorkflowsAdmin();
                    foreach (WorkflowAdmin workflow in workflowlist)
                    {
                        if (workflow.Workflow.ObjectClass == ClassTypeId | workflow.Workflow.ObjectClass == -3)
                        {

                            var workflowperm = _permission.WorkflowPermission(vault, UserID, workflow.Workflow.ID);
                            if (workflowperm.AttachObjectsPermission)
                            {
                                var statelist = vault.WorkflowOperations.GetWorkflowStates(workflow.Workflow.ID);
                                List<stateStates> states = new List<stateStates>();

                                foreach (State state in statelist)
                                {
                                    states.Add(new stateStates { StateId = state.ID, StateName = state.Name, IsSelectable = state.Selectable });
                                }

                                workflows.Add(new stateWorkflows { ClassId = workflow.Workflow.ObjectClass, States = states, WorkflowId = workflow.Workflow.ID, WorkflowName = workflow.Workflow.Name });

                            }

                        }
                    }
                }


                if (workflows.Count > 0)
                {
                    return Ok(workflows);
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
        [HttpGet("GetObjectInWorkflowStates/{VaultGuid}/{WorkflowId}/{StateId}/{UserID}")]
        public IActionResult GetObjectInWorkflowStates(string VaultGuid, int WorkflowId, int StateId,int UserID)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
           

            if (string.IsNullOrEmpty(VaultGuid))
            {
                return NotFound("Kindly check the vaultguid");
            }
           
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

                // Add a Workflow filter.
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefWorkflow,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                        WorkflowId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }
                // Add a state filter.
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefState,
                        MFParentChildBehavior.MFParentChildBehaviorNone);

                    // Set the condition.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                        StateId);

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

               
                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if(searchResults.Count > 0) 
                {
                    try
                    {
                        List<Objectsearchresponse> Response = new List<Objectsearchresponse>();

                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var perm = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                            if (perm.ReadPermission)
                            {
                                perm = _permission.ClassPermission(vault, UserID, objectVersion.Class);
                                if (perm.ReadPermission)
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

                                    Response.Add(new Objectsearchresponse { ClassTypeName = classname, ObjectTypeName=objecttypeid.NameSingular, VersionId=objectVersion.ObjVer.Version
                                                                            ,id = objectVersion.ObjVer.ID, Title = objectVersion.Title, ClassID = objectVersion.Class, ObjectID = objectVersion.ObjVer.Type
                                                                            , userPermission = perm, CreatedUtc=objectVersion.CreatedUtc, LastModifiedUtc=objectVersion.LastModifiedUtc, 
                                                                             IsSingleFile= objectVersion.SingleFile, DisplayID=objectVersion.DisplayID});

                                }

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
                    return NotFound("No objects found in the workflow");
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost("GetObjectworkflowstate")]
        public IActionResult GetObjectworkflowstate(GetObjectstates getObjectstates)
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
                var vault = mfServerApplication.LogInToVault(getObjectstates.VaultGuid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, getObjectstates.ObjectId);

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
                        getObjectstates.ObjectTypeId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                foreach (ObjectVersion objectVersion in searchResults)
                {
                     List<currentState> statespd = new List<currentState>();
                    workflowobjectstateresp workflowobjectstateresp = new workflowobjectstateresp();

                    try
                    {
                        var Workflow = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 38).Value.GetLookupID();
                        var Workflowtitle = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 38).Value.DisplayValue;
                        workflowobjectstateresp.WorkflowTitle = Workflowtitle;
                        workflowobjectstateresp.WorkflowId = Workflow;

                        var stateval = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 39).Value;

                        var stateid = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 39).Value.GetLookupID();
                        var statename = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 39).Value.DisplayValue;

                        workflowobjectstateresp.CurrentStateTitle = statename;
                        workflowobjectstateresp.CurrentStateid = stateid;
                        var allstates = vault.WorkflowOperations.GetWorkflowStateTransitionsEx(Workflow,stateval);
                        List< workflowClass> allclasses = new List<workflowClass>();
                        foreach(StateTransitionForClient stateTransitionForClient in allstates)
                        {
                            if(!allclasses.Any(m=>m.ToId== stateTransitionForClient.ToState))
                            allclasses.Add(new workflowClass { FromId=stateTransitionForClient.FromState, ToId = stateTransitionForClient.ToState, name=stateTransitionForClient.Name });
                        }

                        var assignmentdesc = "";
                        try
                        {
                            assignmentdesc = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 41).Value.DisplayValue;
                        }
                        catch
                        {

                        }

                        {
                            var workflow = vault.WorkflowOperations.GetWorkflowAdmin(Workflow);

                            foreach (var item in allclasses)
                            {
                                foreach (StateTransition stateTransition in workflow.StateTransitions)
                                {
                                    if (stateTransition.FromState == stateid)
                                    { 
                                        if (stateTransition.FromState == stateid && stateTransition.ToState == item.ToId)
                                        {
                                            bool alreadyset = false;
                                            
                                             #region setting permission
                                            {
                                                {
                                                    try
                                                    {
                                                        AccessControlList acl = stateTransition.AccessControlList; // Display the ACL details
                                                     
                                                        foreach (AccessControlEntry accessControlEntry1 in stateTransition.AccessControlList)
                                                        {
                                                            var found = false;

                                                            if (accessControlEntry1.IsGroup)
                                                            {
                                                                try
                                                                {
                                                                   
                                                                    var items = _gettingusersinusergroup.GetUsersFromGroup(vault, Convert.ToInt16(accessControlEntry1.UserOrGroupID));
                                                                    if (items.Any(m => m.Equals(getObjectstates.UserID)))
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
                                                                    if (getObjectstates.UserID == username.ID)
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
                                                                    var name = allclasses.FirstOrDefault(m => m.FromId == stateTransition.FromState && m.ToId == stateTransition.ToState);
                                                                    if (name != null)
                                                                    {
                                                                        statespd.Add(new currentState { id = stateTransition.ToState, Title = name.name });
                                                                        break;
                                                                    }

                                                                }
                                                                else
                                                                {

                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch(Exception ex)
                                                    {
                                                        var assignedto = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer,44).Value.GetValueAsLookups();
                                                        foreach(Lookup lookup in assignedto)
                                                        {
                                                            if (Convert.ToInt16(lookup.DisplayID) == getObjectstates.UserID) 
                                                            {
                                                                var name = allclasses.FirstOrDefault(m => m.FromId == stateTransition.FromState && m.ToId == stateTransition.ToState);
                                                                if (name != null)
                                                                {
                                                                    statespd.Add(new currentState { id = stateTransition.ToState, Title = name.name });
                                                                    break;
                                                                }

                                                            }

                                                        }
                                                    }
                                                    finally
                                                    {

                                                    }
                                                }
                                                if (!alreadyset)
                                                {
                                                    try
                                                    {
                                                        var username = vault.UserOperations.GetUserAccount(getObjectstates.UserID);

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
                                                                var name = allclasses.FirstOrDefault(m => m.FromId == stateTransition.FromState && m.ToId == stateTransition.ToState);
                                                                if (name != null)
                                                                {
                                                                    if(!statespd.Any(m=>m.id== stateTransition.ToState))
                                                                    {
                                                                        statespd.Add(new currentState { id = stateTransition.ToState, Title = name.name });
                                                                        break;
                                                                    }
                                                                   
                                                                }
                                                            }
                                                        }

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex.Message);
                                                    }
                                                }
                                               
                                            }
                                            #endregion
                                        }
                                    }

                                }
                            }

                        }

                        workflowobjectstateresp.NextStates = statespd;
                        workflowobjectstateresp.Assignmentdesc = assignmentdesc;
                     
                    }
                    catch (Exception ex)
                    {

                    }
                    if (!string.IsNullOrEmpty(workflowobjectstateresp.WorkflowTitle))
                    {
                        return Ok(workflowobjectstateresp);
                    }
                    else
                    {
                        return NotFound("Object does not have workflow");
                    }
                }
                return NotFound("Could not find the searched object");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("GetObjectworkflowAllstates")]
        public IActionResult GetObjectworkflowAllstates(GetObjectstates getObjectstates)
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
                var vault = mfServerApplication.LogInToVault(getObjectstates.VaultGuid);
         

                List<currentState> statespd = new List<currentState>();

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, getObjectstates.ObjectId);

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
                        getObjectstates.ObjectTypeId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if (searchResults.Count > 0)
                {
                    foreach (ObjectVersion objectVersion in searchResults)
                    {

                        try
                        {
                            var perm = _permission.ObjectPermission(vault, getObjectstates.UserID, objectVersion.ObjVer.Type);
                            if (perm.ReadPermission)
                            {
                                perm = _permission.ClassPermission(vault, getObjectstates.UserID, objectVersion.Class);
                                if (perm.ReadPermission)
                                {
                                    var Workflow = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 38).Value.GetLookupID();

                                    var states = vault.WorkflowOperations.GetWorkflowStates(Workflow);

                                    foreach (State stateTransition in states)
                                    {
                                        statespd.Add(new currentState { id = stateTransition.ID, Title = stateTransition.Name });
                                    }
                                }
                              
                            }


                        }
                        catch (Exception ex)
                        {

                        }
                        if (statespd.Count > 0)
                        {
                            return Ok(statespd);
                        }
                        else
                        {
                            return NotFound();
                        }

                    }
                }
             
                return NotFound("Could not find the searched object");
            }
            catch (Exception ex)
            {
                return NotFound("Could not find the searched object");
            }
        }
        [HttpPost("SetObjectstate")]
        public IActionResult SetObjectstate(SetObjectstates setObjectstates)
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
                var vault = mfServerApplication.LogInToVault(setObjectstates.VaultGuid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, setObjectstates.ObjectId);

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
                        setObjectstates.ObjectTypeId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                foreach (ObjectVersion objectVersion in searchResults)
                {
                   
                   

                        // We want to alter the document with ID 249.
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: setObjectstates.ObjectTypeId,
                            ID: setObjectstates.ObjectId);
                        // Check out the object.
                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                    try
                    {
                        PropertyValues propertyValues = new PropertyValues();
                      

                        // Create a property value to update.
                        var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                        {
                            PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefState
                        };
                        nameOrTitlePropertyValue.Value.SetValue(
                            MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                            setObjectstates.NextStateId
                        );
                        propertyValues.Add(-1, nameOrTitlePropertyValue);

                        // Update the property on the server.
                        vault.ObjectPropertyOperations.SetProperties(
                            ObjVer: checkedOutObjectVersion.ObjVer
                            , propertyValues);


                        #region setting last modified
                        {
                            var date = DateTime.UtcNow;
                            var lastModifiedBy = new TypedValue();
                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, setObjectstates.UserID);
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

                        // Check the object back in.
                        vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                    }
                    catch (Exception ex)
                    {
                        // Check the object back in.
                        vault.ObjectOperations.ForceUndoCheckout(checkedOutObjectVersion.ObjVer);
                        return BadRequest(ex.Message);
                    }
                }
                return Ok("Successfully Updated");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("SetObjectWorkflowstate")]
        public IActionResult SetObjectWorkflowstate(SetObjectWorkflowstates setObjectstates)
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
                var vault = mfServerApplication.LogInToVault(setObjectstates.VaultGuid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, setObjectstates.ObjectId);

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
                        setObjectstates.ObjectTypeId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }

                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                foreach (ObjectVersion objectVersion in searchResults)
                {

                    try
                    {

                        // We want to alter the document with ID 249.
                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                            ObjType: setObjectstates.ObjectTypeId,
                            ID: setObjectstates.ObjectId);
                        // Check out the object.
                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                        PropertyValues propertyValues = new PropertyValues();


                        {
                            // Create a property value to update.
                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                            {
                                PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefWorkflow
                            };
                            nameOrTitlePropertyValue.Value.SetValue(
                                MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                setObjectstates.WorkflowId
                            );
                            propertyValues.Add(-1, nameOrTitlePropertyValue);
                        }
                        {
                            // Create a property value to update.
                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                            {
                                PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefState
                            };
                            nameOrTitlePropertyValue.Value.SetValue(
                                MFDataType.MFDatatypeLookup,  // This must be correct for the property definition.
                                setObjectstates.StateId
                            );
                            propertyValues.Add(-1, nameOrTitlePropertyValue);
                        }

                        // Update the property on the server.
                        vault.ObjectPropertyOperations.SetProperties(
                            ObjVer: checkedOutObjectVersion.ObjVer
                            , propertyValues);


                        #region setting last modified
                        {
                            var date = DateTime.UtcNow;
                            var lastModifiedBy = new TypedValue();
                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, setObjectstates.UserID);
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

                        // Check the object back in.
                        vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);

                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
                return Ok("Successfully Updated");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("GetworkflowAgeingReport")]
        public IActionResult GetworkflowAgeingReport([FromBody] WorkflowAgeingReport workflowAgeingReport)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";


            if (string.IsNullOrEmpty(workflowAgeingReport.VaultGuid))
            {
                return NotFound("Kindly check the vaultguid");
            }

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
                var vault = mfServerApplication.LogInToVault(workflowAgeingReport.VaultGuid);
                var moresult = false;
                int count = 500;
                DataTable dt = new DataTable();
                while (!moresult)
                {
                    // Create our search conditions.
                    var searchConditions = new SearchConditions();

                    // Add a Workflow filter.
                    {
                        // Create the condition.
                        var condition = new SearchCondition();

                        // Set the expression.
                        condition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefWorkflow,
                            MFParentChildBehavior.MFParentChildBehaviorNone);

                        // Set the condition.
                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                        // Set the value.
                        condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeLookup,
                            workflowAgeingReport.WorkflowId);

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
                    if (workflowAgeingReport.StartDate.HasValue)
                    {
                        // Create the "minimum" search condition.
                        var searchCondition = new SearchCondition();

                        // We want to search by property.
                        searchCondition.Expression.SetPropertyValueExpression(
                            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreated, // This is our date property ID
                            PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                        // Set the condition type.
                        searchCondition.ConditionType = MFConditionType.MFConditionTypeGreaterThanOrEqual;

                        // We only want documents that are later than 1st January 2017.
                        searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeTimestamp, workflowAgeingReport.StartDate);

                        // Add it to the conditions.
                        searchConditions.Add(-1, searchCondition);
                    }
                    if (workflowAgeingReport.EndDate.HasValue)
                    {
                        // Create the "maximum" search condition.
                        var searchCondition = new SearchCondition();

                        // We want to search by property.
                        searchCondition.Expression.SetPropertyValueExpression(
                            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreated, // This is our date property ID
                            PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                        // Set the condition.
                        searchCondition.ConditionType = MFConditionType.MFConditionTypeLessThanOrEqual;

                        // We only want documents that are before 1st February 2017.
                        searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeTimestamp, workflowAgeingReport.EndDate);

                        // Add it to the conditions.
                        searchConditions.Add(-1, searchCondition);
                    }
                    // Execute the search.
                    var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                        MFSearchFlags.MFSearchFlagNone, SortResults: false, MaxResultCount: count);
                    if (searchResults.MoreResults)
                    {
                        count += 500;
                        moresult = false;
                    }
                    else
                    {
                        moresult = true;
                    }
                    if (moresult)
                    {
                        // Add a timestamp column
                        dt.Columns.Add("ID", typeof(BigInteger));
                        dt.PrimaryKey = new DataColumn[] { dt.Columns["ID"] };
                        var workflowsstate = vault.WorkflowOperations.GetWorkflowStates(workflowAgeingReport.WorkflowId);
                        foreach (State state in workflowsstate)
                        {
                            dt.Columns.Add($"{state.ID}+({state.Name})", typeof(DateTime));
                            dt.Columns.Add($"period stayed at {state.ID}+({state.Name})", typeof(string));
                        }
                        foreach (ObjectVersion objectVersion in searchResults)
                        {
                            var objID = new MFilesAPI.ObjID();
                            objID.SetIDs(
                                ObjType: objectVersion.ObjVer.Type,
                                ID: objectVersion.ObjVer.ID);
                            var history = vault.ObjectOperations.GetHistory(objID);
                            if (history != null)
                            {
                                if (history.Count > 0)
                                {
                                    foreach (ObjectVersion objectVersionFile in history)
                                    {
                                        var factory = vault.ObjectPropertyOperations.GetProperty(objectVersionFile.ObjVer, 39).Value.GetValueAsLookup();
                                        DataRow searchrow = dt.Rows.Find(objectVersionFile.ObjVer.ID);
                                        if (searchrow != null)
                                        {

                                            if (dt.Columns.Contains($"{factory.DisplayID}+({factory.DisplayValue})"))
                                            {
                                                searchrow[$"{factory.DisplayID}+({factory.DisplayValue})"] = objectVersionFile.LastModifiedUtc;
                                            }
                                            else
                                            {
                                                dt.Columns.Add($"{factory.DisplayID}+({factory.DisplayValue})", typeof(DateTime));
                                                dt.Columns.Add($"period stayed at {factory.DisplayID}+({factory.DisplayValue})", typeof(string));

                                                searchrow[$"{factory.DisplayID}+({factory.DisplayValue})"] = objectVersionFile.LastModifiedUtc;

                                            }
                                        }
                                        else
                                        {
                                            if (dt.Columns.Contains($"{factory.DisplayID}+({factory.DisplayValue})"))
                                            {

                                                // Create a new row
                                                DataRow row = dt.NewRow();
                                                row["ID"] = objectVersionFile.ObjVer.ID;   // current timestamp
                                                row[$"{factory.DisplayID}+({factory.DisplayValue})"] = objectVersionFile.LastModifiedUtc;
                                                dt.Rows.Add(row);
                                            }
                                            else
                                            {
                                                dt.Columns.Add($"{factory.DisplayID}+({factory.DisplayValue})", typeof(DateTime));
                                                dt.Columns.Add($"period stayed at {factory.DisplayID}+({factory.DisplayValue})", typeof(string));
                                                // Create a new row
                                                DataRow row = dt.NewRow();
                                                row["ID"] = objectVersionFile.ObjVer.ID;   // current timestamp
                                                row[$"{factory.DisplayID}+({factory.DisplayValue})"] = objectVersionFile.LastModifiedUtc;
                                                dt.Rows.Add(row);
                                            }
                                        }

                                    }
                                }
                            }

                        }

                        Test class1 = new Test();

                        foreach (DataRow row in dt.Rows)
                        {
                            for (int colIndex = 0; colIndex < dt.Columns.Count; colIndex++)
                            {
                                // Skip first column (no previous)
                                if (colIndex == 0) continue;

                                // Only proceed if previous column has a value
                                if (colIndex % 2 == 0)
                                {
                                    if (row[colIndex - 1] != DBNull.Value)
                                    {
                                        // Get next column value (can be next one, next two, etc.)
                                        string? daysDiff = class1.GetDaysDifference(row, colIndex - 1, new int[] { +1, +2 });

                                        if (!string.IsNullOrEmpty(daysDiff))
                                        {
                                            // Store the days difference in the current column
                                            row[colIndex] = daysDiff;
                                        }
                                    }
                                }
                            }
                        }



                    }
                }
                if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
                {
                    using (ExcelEngine excelEngine = new ExcelEngine())
                    {
                        IApplication application = excelEngine.Excel;
                        application.DefaultVersion = ExcelVersion.Excel2016;

                        // Create a workbook with one worksheet
                        IWorkbook workbook = application.Workbooks.Create(1);
                        IWorksheet sheet = workbook.Worksheets[0];

                        // Import DataTable into worksheet
                        sheet.ImportDataTable(dt, true, 1, 1);

                        // Autofit columns for neatness
                        sheet.UsedRange.AutofitColumns();
                        // Save to memory stream
                        using (MemoryStream stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            workbook.Close();

                            // Reset stream position before returning
                            stream.Position = 0;

                            // Return as FileResult
                            return File(
                                stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "Sample.xlsx"
                            );
                        }
                    }
                }
                else
                {
                    return NotFound("Could not generate the excel report");
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
    }
}
