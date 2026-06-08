using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Zenfiles.Models;
using Zenfiles.Models.comments;
using Zenfiles.Models.Workflow;
using ZenFiles.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public CommentsController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));

        }
        // GET: api/<CommentsController>
        [HttpGet]
        public IActionResult Get([FromQuery] getcomments postcomments)
        {
            if (postcomments.ObjectId == 0)
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
                List<Comment> comments1 = new List<Comment>();
                var vault = mfServerApplication.LogInToVault(postcomments.VaultGuid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, postcomments.ObjectId);

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
                        postcomments.ObjectTypeId);

                    // Add the condition to the collection.
                    searchConditions.Add(-1, condition);
                }
                // Execute the search.
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                    MFSearchFlags.MFSearchFlagNone, SortResults: false);
                if(searchResults.Count > 0) 
                foreach (ObjectVersion objectVersion in searchResults)
                {
                    var test = vault.ObjectPropertyOperations.GetVersionCommentHistory(objectVersion.ObjVer);
                    foreach (VersionComment comments in test)
                    {
                        var date = vault.ObjectPropertyOperations.GetProperty(comments.ObjVer, 21).Value.DisplayValue;
                        var commentedby = vault.ObjectPropertyOperations.GetProperty(comments.ObjVer, 23).Value.DisplayValue;
                            comments1.Add(new Comment { Coment = comments.VersionComment.Value.DisplayValue, ModifiedDate = date, CommentedBy=commentedby   });
                    }
                }
                if (comments1.Count > 0)
                    return Ok(comments1);
                else
                    return NotFound("Object has no coments");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // POST api/<CommentsController>
        [HttpPost]
        public IActionResult Post([FromBody] postcomments postcomments)
        {
            if (postcomments.ObjectId == 0)
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
                var vault = mfServerApplication.LogInToVault(postcomments.VaultGuid);

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
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeInteger, postcomments.ObjectId);

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
                        postcomments.ObjectTypeId);

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
                            ObjType: postcomments.ObjectTypeId,
                            ID: postcomments.ObjectId);
                        // Check out the object.
                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);
                        PropertyValues propertyValues = new PropertyValues();

                        {

                            // Create a property value to update.
                            var nameOrTitlePropertyValue = new MFilesAPI.PropertyValue
                            {
                                PropertyDef = 33
                            };
                            nameOrTitlePropertyValue.Value.SetValue(
                                MFDataType.MFDatatypeMultiLineText,  // This must be correct for the property definition.
                                postcomments.comment
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
                            lastModifiedBy.SetValue(MFDataType.MFDatatypeLookup, postcomments.UserID);
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

    }
}
