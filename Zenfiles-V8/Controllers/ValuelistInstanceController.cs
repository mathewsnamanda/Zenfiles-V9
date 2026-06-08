using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Hybrid;
using Newtonsoft.Json.Linq;
using Zenfiles.Models.Valuelist;
using ZenFiles.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZenFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
     public class ValuelistInstanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public ValuelistInstanceController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
      
        }
        // GET api/<ValuelistInstanceController>/5
        [HttpGet("Search/{VaultGuid}/{Searchphrase}/{PropertyID}/{UserID}")]
        public IActionResult Search(string VaultGuid, int PropertyID, string Searchphrase,int UserID)
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
                var objects = new List<ObjtypeRespModel>();
                var vault = mfServerApplication.LogInToVault(VaultGuid);
                var t = vault.PropertyDefOperations.GetPropertyDefAdmin(PropertyID);

                if (t == null)
                {
                    return NotFound("Property with that id doesn't exist");
                }
                if (t.PropertyDef.DataType == MFDataType.MFDatatypeLookup | t.PropertyDef.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                {
                    List<Valuelistresp> Items = new List<Valuelistresp>();

                   
                    var valuelistp = vault.ValueListOperations.GetValueListAdmin(t.PropertyDef.ValueList);
                    if (valuelistp.ObjectType.RealObjectType)
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
                                valuelistp.ObjectType.ID);

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
                        //filter with title
                        {
                            // Create the search condition.
                            var searchCondition = new SearchCondition();

                            // We want to search by property - in this case the built-in "name or title" property.
                            // Alternatively we could pass the ID of the property definition if it's not built-in.
                            searchCondition.Expression.SetPropertyValueExpression(
                                valuelistp.ObjectType.TitlePropertyID,
                                MFParentChildBehavior.MFParentChildBehaviorNone);

                            // We want only items that equal the search string provided.
                            searchCondition.ConditionType = MFConditionType.MFConditionTypeContains;

                            // We want to search for items that are named "hello world".
                            // Note that the type must both match the property definition type, and be applicable for the
                            // supplied value.
                            searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, Searchphrase);
                            searchConditions.Add(-1,searchCondition);
                        }
                        // Execute the search.
                        var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                            MFSearchFlags.MFSearchFlagNone, SortResults: false);
                        foreach (ObjectVersion objectVersion in searchResults)
                        {

                            bool p = true;
                            foreach (SearchCondition expressionEx in t.PropertyDef.StaticFilter)
                            {

                                if (expressionEx.Expression.DataPropertyValuePropertyDef == 25)
                                {
                                    var prop = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 25);
                                    var found = false;
                                    if (expressionEx.ConditionType == MFConditionType.MFConditionTypeEqual)
                                    {

                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }

                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }

                                        }
                                    }
                                    else if (expressionEx.ConditionType == MFConditionType.MFConditionTypeNotEqual)
                                    {
                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }

                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }
                                            if (!found)
                                                found = true;
                                        }
                                    }
                                    p &= found;
                                }
                                else if (expressionEx.Expression.DataPropertyValuePropertyDef == 23)
                                {
                                    var prop = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 23);
                                    var found = false;
                                    if (expressionEx.ConditionType == MFConditionType.MFConditionTypeEqual)
                                    {

                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }

                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }

                                        }
                                    }
                                    else if (expressionEx.ConditionType == MFConditionType.MFConditionTypeNotEqual)
                                    {
                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }
                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }
                                            if (!found)
                                                found = true;
                                        }
                                    }
                                    p &= found;
                                }
                                else if (expressionEx.Expression.DataPropertyValuePropertyDef == 100)
                                {
                                    var found = false;
                                    if (expressionEx.ConditionType == MFConditionType.MFConditionTypeEqual)
                                    {

                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        foreach (Lookup lookup in items)
                                        {
                                            string id = lookup.DisplayID;

                                            if (id == objectVersion.Class.ToString())
                                            {
                                                found = true;
                                            }

                                        }
                                    }
                                    else if (expressionEx.ConditionType == MFConditionType.MFConditionTypeNotEqual)
                                    {
                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-101" && lookup.DisplayID == "-103")
                                            {
                                                id = "31";
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }
                                            if (id == "31")
                                            {
                                                found = true;
                                            }
                                            if (!found)
                                                found = true;
                                        }
                                    }
                                    p &= found;
                                }

                            }
                            if (p)
                            {
                                Items.Add(new Valuelistresp { id = objectVersion.ObjVer.ID, Name = objectVersion.Title, DisplayID=objectVersion.DisplayID });

                            }
                        }

                    }
                    else
                    {
                        // Create our search conditions collection.
                        var conditions = new SearchConditions();

                        // Exclude deleted items.
                        {
                            // Create the condition.
                            var condition = new SearchCondition();

                            // Set the expression.
                            condition.Expression.SetValueListItemExpression(MFValueListItemPropertyDef.MFValueListItemPropertyDefDeleted,
                                MFParentChildBehavior.MFParentChildBehaviorNone);

                            // Set the condition type.
                            condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                            // Set the value.
                            condition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

                            // Add it to the collection.
                            conditions.Add(-1, condition);
                        }

                        // Filter by name (starts with "North").
                        {
                            // Create the condition.
                            var condition = new SearchCondition();

                            // Set the expression.
                            condition.Expression.SetValueListItemExpression(MFValueListItemPropertyDef.MFValueListItemPropertyDefName,
                                MFParentChildBehavior.MFParentChildBehaviorNone);

                            // Set the condition type.
                            condition.ConditionType = MFConditionType.MFConditionTypeStartsWith;

                            // Set the value.
                            condition.TypedValue.SetValue(MFDataType.MFDatatypeText, Searchphrase);

                            // Add it to the collection.
                            conditions.Add(-1, condition);
                        }


                        // Search value list with ID 102.
                        // ref: https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~VaultValueListItemOperations~SearchForValueListItemsEx.html
                        var results = vault.ValueListItemOperations.SearchForValueListItemsEx2(
                            ValueList: t.PropertyDef.ValueList,
                            SearchConditions: conditions,
                            UpdateFromServer: false,
                            RefreshTypeIfExternalValueList: MFExternalDBRefreshType.MFExternalDBRefreshTypeNone,
                            ReplaceCurrentUserWithCallersIdentity: true,PropertyID);

                        foreach (ValueListItem value in results)
                        {
                            Items.Add(new Valuelistresp { id = value.ID, Name = value.Name, DisplayID=value.DisplayID });
                        }

                    }
                    if (Items.Count == 0)
                    {
                        return NotFound("Valuelist is empty");
                    }
                    else
                    {
                        return Ok(Items.OrderBy(m => m.Name));
                    }
                }
                else
                {
                    return BadRequest("Property with that id is not a valuelist type");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("{VaultGuid}/{propertyid}/{UserID}")]
        public async Task<IActionResult> GetAsync(string VaultGuid, int propertyid,int UserID)
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
                var objects = new List<ObjtypeRespModel>();
                var vault = mfServerApplication.LogInToVault(VaultGuid);
                var t = vault.PropertyDefOperations.GetPropertyDefAdmin(propertyid);
                if (t == null)
                    return BadRequest("prop not found");
                var itemspd = new List<Valuelistresp>();
                if (t.PropertyDef.DataType == MFDataType.MFDatatypeLookup | t.PropertyDef.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                {
                    List<Valuelistresp> Items = new List<Valuelistresp>();

                    var valuelistp = vault.ValueListOperations.GetValueListAdmin(t.PropertyDef.ValueList);
                    if (valuelistp.ObjectType.RealObjectType)
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
                                valuelistp.ObjectType.ID);

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
                        foreach (ObjectVersion objectVersion in searchResults)
                        {

                            bool p = true;
                            foreach (SearchCondition expressionEx in t.PropertyDef.StaticFilter)
                            {

                                if (expressionEx.Expression.DataPropertyValuePropertyDef == 25)
                                {
                                    var prop = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 25);
                                    var found = false;
                                    if (expressionEx.ConditionType == MFConditionType.MFConditionTypeEqual)
                                    {

                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }

                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }

                                        }
                                    }
                                    else if (expressionEx.ConditionType == MFConditionType.MFConditionTypeNotEqual)
                                    {
                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }

                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }
                                            if (!found)
                                                found = true;
                                        }
                                    }
                                    p &= found;
                                }
                                else if (expressionEx.Expression.DataPropertyValuePropertyDef == 23)
                                {
                                    var prop = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer, 23);
                                    var found = false;
                                    if (expressionEx.ConditionType == MFConditionType.MFConditionTypeEqual)
                                    {

                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }

                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }

                                        }
                                    }
                                    else if (expressionEx.ConditionType == MFConditionType.MFConditionTypeNotEqual)
                                    {
                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-100" | lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }
                                            if (id == prop.TypedValue.GetLookupID().ToString())
                                            {
                                                found = true;
                                            }
                                            if (!found)
                                                found = true;
                                        }
                                    }
                                    p &= found;
                                }
                                else if (expressionEx.Expression.DataPropertyValuePropertyDef == 100)
                                {
                                    var found = false;
                                    if (expressionEx.ConditionType == MFConditionType.MFConditionTypeEqual)
                                    {

                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        foreach (Lookup lookup in items)
                                        {
                                            string id = lookup.DisplayID;

                                            if (id == objectVersion.Class.ToString())
                                            {
                                                found = true;
                                            }

                                        }
                                    }
                                    else if (expressionEx.ConditionType == MFConditionType.MFConditionTypeNotEqual)
                                    {
                                        var items = expressionEx.TypedValue.GetValueAsLookups();
                                        string id = "0";
                                        foreach (Lookup lookup in items)
                                        {
                                            if (lookup.DisplayID == "-101" && lookup.DisplayID == "-103")
                                            {
                                                id = UserID.ToString();
                                            }
                                            else
                                            {
                                                id = lookup.DisplayID;
                                            }
                                            if (id == UserID.ToString())
                                            {
                                                found = true;
                                            }
                                            if (!found)
                                                found = true;
                                        }
                                    }
                                    p &= found;
                                }

                            }
                            if (p)
                            {
                                itemspd.Add(new Valuelistresp { id = objectVersion.ObjVer.ID, Name = objectVersion.Title, DisplayID=objectVersion.DisplayID });

                            }
                        }

                    }
                    else
                    {
                        var valuelist = vault.ValueListItemOperations.GetValueListItemsWithPermissions(t.PropertyDef.ValueList, false, MFExternalDBRefreshType.MFExternalDBRefreshTypeNone, false, propertyid);
                        foreach (ValueListItem value in valuelist.ValueListItems)
                        {
                            itemspd.Add(new Valuelistresp { id = value.ID, Name = value.Name, DisplayID=value.DisplayID });
                        }

                    }
                }

                if (itemspd.Count == 0)
                    return NotFound("Valuelist is empty or property not found");

                return Ok(itemspd.OrderBy(m => m.Name));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("AddValuelistItem")]
        public IActionResult AddValuelistItem([FromBody]ValuelistPost valuelistPost)
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
                var objects = new List<ObjtypeRespModel>();
                var vault = mfServerApplication.LogInToVault(valuelistPost.VaultGuid);
                List<Valuelistresp> Items = new List<Valuelistresp>();
                #region searching for item in the valuelist
                {
                    // Create our search conditions collection.
                    var conditions = new SearchConditions();

                    // Exclude deleted items.
                    {
                        // Create the condition.
                        var condition = new SearchCondition();

                        // Set the expression.
                        condition.Expression.SetValueListItemExpression(MFValueListItemPropertyDef.MFValueListItemPropertyDefDeleted,
                            MFParentChildBehavior.MFParentChildBehaviorNone);

                        // Set the condition type.
                        condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                        // Set the value.
                        condition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

                        // Add it to the collection.
                        conditions.Add(-1, condition);
                    }

                    // Filter by name (starts with "North").
                    {
                        // Create the condition.
                        var condition = new SearchCondition();

                        // Set the expression.
                        condition.Expression.SetValueListItemExpression(MFValueListItemPropertyDef.MFValueListItemPropertyDefName,
                            MFParentChildBehavior.MFParentChildBehaviorNone);

                        // Set the condition type.
                        condition.ConditionType = MFConditionType.MFConditionTypeStartsWith;

                        // Set the value.
                        condition.TypedValue.SetValue(MFDataType.MFDatatypeText, valuelistPost.Name.Trim());

                        // Add it to the collection.
                        conditions.Add(-1, condition);
                    }


                    // Search value list with ID 102.
                    // ref: https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~VaultValueListItemOperations~SearchForValueListItemsEx.html
                    var results = vault.ValueListItemOperations.SearchForValueListItemsEx(
                        ValueList: valuelistPost.ValuelistID,
                        SearchConditions: conditions,
                        UpdateFromServer: false,
                        RefreshTypeIfExternalValueList: MFExternalDBRefreshType.MFExternalDBRefreshTypeNone,
                        ReplaceCurrentUserWithCallersIdentity: true);
                    foreach(ValueListItem valueListItem in results)
                    {
                        Items.Add(new Valuelistresp { id=valueListItem.ID, Name=valueListItem.Name});
                    }

                }
                #endregion
                if (Items.Count == 0)
                {

                    ValueListItem valueListItem = new ValueListItem();
                    valueListItem.Name = valuelistPost.Name;
                    var Item = vault.ValueListItemOperations.AddValueListItem(valuelistPost.ValuelistID, valueListItem);
                    Items.Add(new Valuelistresp { id = Item.ID, Name = Item.Name });
                    return Ok(Items);
                }
                else
                {
                    return Ok(Items.OrderBy(m => m.Name));
                }

               
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
