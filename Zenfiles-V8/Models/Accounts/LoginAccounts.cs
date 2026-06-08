using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulling_users.models
{
    public class LoginAccounts
    {
        public bool InternalUser { get; set; }
        public int ID { get; set; }
        public bool Enabled { get; set; }
        public string? LoginName { get; set; }
        public string? VaultRoles { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? AccountName { get; set; }
        public string? DomainName { get; set; }
        public string? AccountType { get; set; }
        public string? EmailAddress { get; set; }
        public string? LicenseType { get; set; }
        public string? ServerRoles { get; set; }
    }
}
