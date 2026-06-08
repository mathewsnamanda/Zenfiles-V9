using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using Pulling_users.models;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Security.Principal;
using TestAuthentication.models;
using TestAuthentication.Services;
using Zenfiles.Models;
using Zenfiles.Models.login;
using Zenfiles.Models.users;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zenfiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public UserAccountsController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
        }
        // POST api/<UserAccountsController>
        [HttpPost("AddUser")]
        public IActionResult AddUser([FromBody] CreateLogin login)
        {
          
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
           
            try
            {
                var serverApplication = new MFilesServerApplication();
                serverApplication.Connect(MFAuthType.MFAuthTypeSpecificWindowsUser, Username, Password, domain, ProtocolSequence: "ncacn_ip_tcp", NetworkAddress: ipaddress, Endpoint: "2266", LocalComputerName: "", AllowAnonymousConnection: false);
                UserAvailable userAvailable = new UserAvailable();
                var loginname = "";

                if (!string.IsNullOrEmpty(login.FullName)&& login.UserID>0&& !string.IsNullOrEmpty(login.Password))
                {
                    loginname = login.FullName.Split(" ")[0].Trim().Substring(0, 1).ToUpper() + login.FullName.Split(" ")[1].Trim().ToLower() + login.UserID;
                    var loginaccounts = serverApplication.LoginAccountOperations.GetLoginAccounts();
                    foreach (LoginAccount loginAccount1 in loginaccounts)
                    {
                        if (loginAccount1.FullName.ToLower().Trim() == login.FullName.ToLower().Trim() && loginAccount1.EmailAddress.ToLower().Trim() == login.EmailAddress.ToLower().Trim())
                        {
                            userAvailable.email = loginAccount1.EmailAddress;
                            userAvailable.fullname = loginAccount1.FullName;
                            userAvailable.email = loginAccount1.EmailAddress;
                            userAvailable.foundonserver = true;
                            userAvailable.loginname = loginAccount1.UserName;
                        }
                        
                    }
                    if (userAvailable.foundonserver)
                    {
                        Error error = new Error();
                        error.explanation = "User Already exists in the server";
                        return BadRequest(error);
                    }
                    else
                    {
                        //serverApplication.ConnectAdministrativeEx();
                        var loginAccount = new LoginAccount
                        {
                            AccountType = MFLoginAccountType.MFLoginAccountTypeMFiles,
                            EmailAddress = login.EmailAddress,
                            FullName = login.FullName,
                            Enabled = true,
                            UserName = loginname,
                            LicenseType = MFLicenseType.MFLicenseTypeNone
                        };
                        serverApplication.LoginAccountOperations.AddLoginAccount(loginAccount, login.Password);
                        // Add the user to the vault.
                        var vault = serverApplication.LogInToVaultAdministrative(login.VaultGuid);
                        if (login.IsAdmin)
                        {
                            var userAccount = new UserAccount()
                            {
                                Enabled = true,
                                InternalUser = true,
                                LoginName = loginAccount.UserName,
                                VaultRoles = MFUserAccountVaultRole.MFUserAccountVaultRoleFullControl

                            };
                            var user = vault.UserOperations.AddUserAccount(userAccount);
                            user.Enabled = true;
                            user.InternalUser = true;
                            vault.UserOperations.ModifyUserAccount(user);
                        }
                        else
                        {
                            var userAccount = new UserAccount()
                            {
                                Enabled = true,
                                InternalUser = true,
                                LoginName = loginAccount.UserName,
                                VaultRoles = MFUserAccountVaultRole.MFUserAccountVaultRoleDefaultRoles

                            };
                            var user = vault.UserOperations.AddUserAccount(userAccount);
                            user.Enabled = true;
                            user.InternalUser = true;
                            vault.UserOperations.ModifyUserAccount(user);
                        }

                        vault.LogOutSilent();
                        serverApplication.Disconnect();
                    }
                }
                else
                {
                    var loginaccounts = serverApplication.LoginAccountOperations.GetLoginAccounts();
                    var vault = serverApplication.LogInToVaultAdministrative(login.VaultGuid);
                    var userAccounts = vault.UserOperations.GetUserAccounts();
                    userAvailable = new UserAvailable();
                   foreach (LoginAccount loginAccount in loginaccounts)
                    {
                        foreach (UserAccount userAccount in userAccounts)
                        {
                            if(loginAccount.EmailAddress==login.EmailAddress)
                            {
                                userAvailable.email = loginAccount.EmailAddress;
                                userAvailable.fullname = loginAccount.FullName;
                                userAvailable.email = loginAccount.EmailAddress;
                                userAvailable.loginname = loginAccount.UserName;
                                userAvailable.admin = loginAccount.ServerRoles.ToString();

                                if (userAccount.LoginName.EndsWith(loginAccount.UserName))
                                {
                                    userAvailable.foundonvault = true;
                                  }
                            }
                          
                        }
                    }
                    if (userAvailable.foundonvault)
                    {
                        var error = new Error();
                        error.explanation = "User already exists on the vault";
                        return BadRequest(error);
                    }
                    else
                    {
                        if(string.IsNullOrEmpty(userAvailable.admin))
                        {
                            Error error = new Error();
                            error.explanation = "internal server error";
                            return BadRequest(error); 
                        }
                        if (userAvailable.admin=="9")
                        {
                            var userAccount = new UserAccount()
                            {
                                Enabled = true,
                                InternalUser = true,
                                LoginName = userAvailable.loginname,
                                VaultRoles = MFUserAccountVaultRole.MFUserAccountVaultRoleFullControl

                            };
                            var user = vault.UserOperations.AddUserAccount(userAccount);
                            user.Enabled = true;
                            user.InternalUser = true;
                            vault.UserOperations.ModifyUserAccount(user);
                        }
                        else
                        {
                            var userAccount = new UserAccount()
                            {
                                Enabled = true,
                                InternalUser = true,
                                LoginName = userAvailable.loginname,
                                VaultRoles = MFUserAccountVaultRole.MFUserAccountVaultRoleDefaultRoles

                            };
                            var user = vault.UserOperations.AddUserAccount(userAccount);
                            user.Enabled = true;
                            user.InternalUser = true;
                            vault.UserOperations.ModifyUserAccount(user);
                        }
                    }
                }
                
                return Ok("Successfully Created");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
      
        [HttpPost("RemoveVaultUser")]
        public IActionResult RemoveUser([FromBody] remoaveuser login)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            try
            {
                var serverApplication = new MFilesServerApplication();
                serverApplication.Connect(MFAuthType.MFAuthTypeSpecificWindowsUser, Username, Password, domain, ProtocolSequence: "ncacn_ip_tcp", NetworkAddress: ipaddress, Endpoint: "2266", LocalComputerName: "", AllowAnonymousConnection: false);
                var vault = serverApplication.LogInToVaultAdministrative(login.VaultGuid);
                var useraccounts = vault.UserOperations.GetUserAccounts();
                var loginaccounts = serverApplication.LoginAccountOperations.GetLoginAccounts();
                bool found = false;
                foreach (LoginAccount loginAccount in loginaccounts)
                {
                    foreach (UserAccount userAccount in useraccounts)
                    {
                        if(userAccount.LoginName== loginAccount.UserName && loginAccount.EmailAddress == login.EmailAddress)
                        {
                            vault.UserOperations.RemoveUserAccount(userAccount.ID);
                            found = true;
                        }
                    }
                }
                if (found)
                    return Ok("Successfully Removed");
                else
                    return BadRequest("Could not find the user with that email in the vault");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("UpdateUser")]
        public IActionResult UpdateUser([FromBody] remoaveuser login)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            try
            {
                var serverApplication = new MFilesServerApplication();
                serverApplication.Connect(MFAuthType.MFAuthTypeSpecificWindowsUser, Username, Password, domain, ProtocolSequence: "ncacn_ip_tcp", NetworkAddress: ipaddress, Endpoint: "2266", LocalComputerName: "", AllowAnonymousConnection: false);
                var vault = serverApplication.LogInToVaultAdministrative(login.VaultGuid);
                var loginaccounts = serverApplication.LoginAccountOperations.GetLoginAccounts();
                bool found = false;
                foreach (LoginAccount loginAccount in loginaccounts)
                {
                    if (loginAccount.EmailAddress == login.EmailAddress)
                    {
                        loginAccount.EmailAddress = "";
                        serverApplication.LoginAccountOperations.ModifyLoginAccount(loginAccount);
                        found = true;
                    }
                }
                if (found)
                    return Ok("Successfully Removed");
                else
                    return BadRequest("Could not find or remove a user with that email from the server");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("SyncAccounts/{VaultGuid}")]
        public IActionResult SyncAccounts(string VaultGuid)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            var mfServerApplication = new MFilesServerApplication();

            mfServerApplication.Connect(
                MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                Endpoint: port);

            // Obtain a connection to the vault with GUID {C840BE1A-5B47-4AC0-8EF7-835C166C8E24}.
            // Note: this will except if the vault is not found.

            try
            {
                var vault = mfServerApplication.LogInToVault(VaultGuid);
                List<Pulling_users.models.LoginAccounts> loginAccounts = new List<Pulling_users.models.LoginAccounts>();
                var users = vault.UserOperations.GetUserAccounts();
                foreach (UserAccount userAccount in users)
                {
                    if(userAccount.ID!=vault.CurrentLoggedInUserID)
                    {
                        try
                        {
                            var login = vault.UserOperations.GetLoginAccountOfUser(userAccount.ID);
                            Console.WriteLine(login.ServerRoles);
                            loginAccounts.Add(
                                new Pulling_users.models.LoginAccounts
                                {
                                    AccountName = login.AccountName,
                                    AccountType = login.AccountType.ToString(),
                                    DomainName = login.DomainName,
                                    EmailAddress = login.EmailAddress,
                                    Enabled = login.Enabled,
                                    FullName = login.FullName,
                                    ID = userAccount.ID,
                                    InternalUser = userAccount.InternalUser,
                                    LicenseType = login.LicenseType.ToString(),
                                    LoginName = userAccount.LoginName,
                                    ServerRoles = login.ServerRoles.ToString(),
                                    UserName = login.UserName,
                                    VaultRoles = userAccount.VaultRoles.ToString()
                                });
                        }
                        catch(Exception ex)
                        {

                        }
                       
                    }
                  
                }
                if (loginAccounts.Count > 0)
                {
                    return Ok(loginAccounts);
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
        [HttpPost("DomainAuth")]
        public IActionResult DomainAuth([FromBody] login login)
        {
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";

            if (ModelState.IsValid)
            {

                var mfServerApplication = new MFilesServerApplication();

                // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
                // https://developer.m-files.com/APIs/COM-API/Reference/index.html#MFilesAPI~MFilesServerApplication~Connect.html
                try
                {
                    mfServerApplication.Connect(
                            MFAuthType.MFAuthTypeSpecificWindowsUser,
                            UserName: login.Username,
                            Password: login.Password,
                            Domain: login.Domain,
                            ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                            NetworkAddress: ipaddress, // Connect to m-files.mycompany.com
                            Endpoint: port
                                    );
                        return Ok(true);
                    }
                    catch (Exception ex)
                    {
                        return Unauthorized("Invalid Username or Password");
                    }
            }
            else
            {
                return BadRequest("Kindly provide all the required fields");
            }
        }
    }
}
