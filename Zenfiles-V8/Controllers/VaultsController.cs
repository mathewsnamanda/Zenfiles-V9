using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Pulling_users.models;
using System;
using TestAuthentication.models;
using ZenFiles.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZenFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    public class VaultsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
       
        public VaultsController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
        }
        // POST api/<ValuesController>
        [HttpPost]
        public IActionResult Post([FromBody] VaultClassp value)
        {
            string Username = this._configuration.GetConnectionString("Username") ??"";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain")??"";
            string port = this._configuration.GetConnectionString("HostPort") ??"";

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
            VaultsProps vaultsProps = new VaultsProps();

            // Obtain a connection to the vault with GUID {C840BE1A-5B47-4AC0-8EF7-835C166C8E24}.
            // Note: this will except if the vault is not found.
            try
            {
                VaultProperties vaultProperties = new VaultProperties();
                vaultProperties.MainDataFolder = $"C:\\Program Files\\M-Files\\Server Vaults\\{value.VaultName}";
                vaultProperties.DisplayName = value.VaultName;
                vaultProperties.ExtendedMetadataDrivenPermissions = true;
                vaultProperties.EncryptionOfFileDataAtRest = true;
                vaultProperties.FileDataStorageType = 0;
                vaultProperties.FullTextSearchLanguage = "eng";
                var vaultguid = mfServerApplication.VaultManagementOperations.CreateNewVault(vaultProperties);
                var vault = mfServerApplication.LogInToVaultAdministrative(vaultguid);
                vaultsProps.VaultGuid = vaultguid;
                vaultsProps.VaultName = value.VaultName;
                var userAccount = new UserAccount()
                {
                    Enabled = true,
                    InternalUser = true,
                    LoginName = domain+@"\"+Username,
                    VaultRoles = MFUserAccountVaultRole.MFUserAccountVaultRoleFullControl

                };

                var user  = vault.UserOperations.AddUserAccount(userAccount);
                user.Enabled = true;
                user.InternalUser = true;
                vault.UserOperations.ModifyUserAccount(user);
                return Ok(vaultsProps);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
          

        }
        [HttpGet("{CompanyName}")]
        public async Task<IActionResult> getAsync(string CompanyName)
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

            // Obtain a connection to the vault with GUID {C840BE1A-5B47-4AC0-8EF7-835C166C8E24}.
            // Note: this will except if the vault is not found.
            try
            {

                var vaultsPropsList = new List<VaultsProps>();
                var vaults = mfServerApplication.GetOnlineVaults();

                foreach (VaultOnServer vault in vaults)
                {
                    if (vault.Name.StartsWith(CompanyName) || vault.Name.EndsWith(CompanyName))
                    {
                        var vault1 = mfServerApplication.LogInToVault(vault.GUID);
                        vaultsPropsList.Add(new VaultsProps
                        {
                            VaultGuid = vault.GUID,
                            VaultName = vault.Name
                        });
                    }
                }
                if (vaultsPropsList.Count == 0)
                    return NotFound();
                return Ok(vaultsPropsList);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("GetVaultGuid/{VaultGuid}")]
        public async Task<IActionResult> GetVaultGuidAsync(string VaultGuid)
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

            // Obtain a connection to the vault with GUID {C840BE1A-5B47-4AC0-8EF7-835C166C8E24}.
            // Note: this will except if the vault is not found.
            try
            {
                var vaultsPropsList = new List<VaultsProps>();
                var vaults = mfServerApplication.GetOnlineVaults();

                foreach (VaultOnServer vault in vaults)
                {
                    if (vault.GUID.Contains(VaultGuid))
                    {
                        var vault1 = mfServerApplication.LogInToVault(vault.GUID);
                        vaultsPropsList.Add(new VaultsProps
                        {
                            VaultGuid = vault.GUID,
                            VaultName = vault.Name
                        });
                    }
                }

                if (vaultsPropsList.Count > 0)
                    return Ok(vaultsPropsList);
                else
                    return NotFound("Vault not found");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
