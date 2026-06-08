using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Zenfiles.Models;
using ZenFiles.Controllers;
using ZenFiles.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectCheckoutController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<objectinstanceController> _logger;
        public ObjectCheckoutController(IConfiguration Configuration, ILogger<objectinstanceController> logger)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _logger = logger;
        }

        // POST api/<ObjectCheckoutController>
        [HttpPost("Checkout")]
        public IActionResult Checkout([FromBody] ObjectCheckoutmodel objectCheckoutmodel)
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
                var vault = mfServerApplication.LogInToVault(objectCheckoutmodel.VaultGuid);
                // We want to alter the document with ID 249.
                var objID = new MFilesAPI.ObjID();
                objID.SetIDs(
                    ObjType: objectCheckoutmodel.Objecttypeid,
                    ID: objectCheckoutmodel.objectid);

                // Check out the object.

                var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);

                var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var filepath = Path.Combine(path, objectCheckoutmodel.VaultGuid+"_" + objectCheckoutmodel.Objecttypeid.ToString() + "_" + objectCheckoutmodel.objectid.ToString() + "_" + objectCheckoutmodel.UserID.ToString()+".txt");
               
                string content = objectCheckoutmodel.VaultGuid + "_" + objectCheckoutmodel.Objecttypeid + "_" + objectCheckoutmodel.objectid + "_" + objectCheckoutmodel.UserID;
                System.IO.File.WriteAllText(filepath, content);

                return Ok("Object sucessfully checked out");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // POST api/<ObjectCheckoutController>
        [HttpPost("UndoCheckout")]
        public IActionResult UndoCheckout([FromBody] ObjectCheckoutmodel objectCheckoutmodel)
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
                var vault = mfServerApplication.LogInToVault(objectCheckoutmodel.VaultGuid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeLookup,
                        objectCheckoutmodel.Objecttypeid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, objectCheckoutmodel.objectid);
                    searchConditions.Add(-1,condition);
                }

                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                foreach (ObjectVersion objectVersion in searchResults)
                {
                    var objID = new MFilesAPI.ObjID();
                    objID.SetIDs(
                        ObjType: objectVersion.ObjVer.Type,
                        ID: objectVersion.ObjVer.ID);

                    // Check out the object.
                    if (vault.ObjectOperations.IsObjectCheckedOut(objID))
                    {
                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Checkouts");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        var filepath = Path.Combine(path, objectCheckoutmodel.VaultGuid + "_" + objectCheckoutmodel.Objecttypeid.ToString() + "_" + objectCheckoutmodel.objectid.ToString() + "_" + objectCheckoutmodel.UserID.ToString() + ".txt");

                        if (System.IO.File.Exists(filepath))
                        {
                            string content = objectCheckoutmodel.VaultGuid + "_" + objectCheckoutmodel.Objecttypeid + "_" + objectCheckoutmodel.objectid + "_" + objectCheckoutmodel.UserID;
                            string content1 = System.IO.File.ReadAllText(filepath);
                            bool allowed = false;
                            var username = vault.UserOperations.GetUserAccount(objectCheckoutmodel.UserID);
                            var loginaccount = vault.UserOperations.GetLoginAccountOfUser(objectCheckoutmodel.UserID);
                            if (username.VaultRoles.ToString() == "3079" | loginaccount.ServerRoles.ToString() == "9" | username.VaultRoles.ToString() == "27974")
                            {
                                allowed = true;
                            }

                            if (content.Trim() == content1.Trim()|allowed)
                            {
                                vault.ObjectOperations.ForceUndoCheckout(objectVersion.ObjVer);
                            }
                                System.IO.File.Delete(filepath);
                        }
                        else
                        {
                            bool allowed = false;
                            var username = vault.UserOperations.GetUserAccount(objectCheckoutmodel.UserID);
                            var loginaccount = vault.UserOperations.GetLoginAccountOfUser(objectCheckoutmodel.UserID);
                            if (username.VaultRoles.ToString() == "3079" | loginaccount.ServerRoles.ToString() == "9" | username.VaultRoles.ToString() == "27974")
                            {
                                allowed = true;
                            }

                            if (objectVersion.CheckedOutTo == objectCheckoutmodel.UserID|allowed)
                            {
                                vault.ObjectOperations.ForceUndoCheckout(objectVersion.ObjVer);
                            }
                        }

                    }

                }

                return Ok("Object sucessfully checked in");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
