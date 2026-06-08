using MFilesAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Zenfiles.Models.Workflow;
using Zenfiles_V8.Models.objecttypes;

namespace Zenfiles_V8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalObjectsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public ExternalObjectsController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
        }
        // GET: api/<WorkflowsInstanceController>
        [HttpGet("GetExternalObjects/{VaultGuid}")]
        public IActionResult GetExternalObjects(string VaultGuid)
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
            List<objecttyperesp> objects = new List<objecttyperesp>();

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
                var objectstypes = vault.ObjectTypeOperations.GetObjectTypes();
                foreach (ObjType objType in objectstypes)
                {
                    if (objType.External&&!objType.DisableExternalConnection)
                    {
                        objects.Add(new objecttyperesp { ObjectType=objType.ID, ObjectName=objType.NameSingular});
                    }
                }
                if (objects.Count == 0)
                    return NotFound();
                return Ok(objects);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetExternalObjects/{VaultGuid}/{ObjectTypeID}")]
        public IActionResult GetExternalObjects(string VaultGuid,int ObjectTypeID)
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
                vault.ObjectTypeOperations.RefreshExternalObjectType(ObjectTypeID, MFExternalDBRefreshType.MFExternalDBRefreshTypeQuick);
                return Ok("Started");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
