using MFilesAPI;
using Zenfiles.Models.comments;

namespace Zenfiles_V8.schedule
{
    public class pullAssignments
    {
        private readonly IConfiguration _configuration;

        public pullAssignments(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Assignmentmodel> DoSomething()
        {
            List<Assignmentmodel> items= new List<Assignmentmodel>();
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
            foreach (Vault vault1 in mfServerApplication.GetVaults())
            {
                try
                {
                    var vaultguid = vault1.GetGUID();
                    var vault = mfServerApplication.LogInToVault(vaultguid);
                    
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
                            10);

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
                    //filter 
                    {
                        // Create the "minimum" search condition.
                        var searchCondition = new SearchCondition();

                        // We want to search by property.
                        searchCondition.Expression.SetPropertyValueExpression(
                            20, // This is our date property ID
                            PCBehavior: MFParentChildBehavior.MFParentChildBehaviorNone);

                        // Set the condition type.
                        searchCondition.ConditionType = MFConditionType.MFConditionTypeGreaterThanOrEqual;

                        // We only want documents that are later than 1st January 2017.
                        searchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeTimestamp, DateTime.Now.AddMinutes(-5));

                        // Add it to the conditions.
                        searchConditions.Add(-1, searchCondition);
                    }

                    // Execute the search.
                    var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                        MFSearchFlags.MFSearchFlagNone, SortResults: false);
                    if(searchResults.Count>0)
                    {
                        foreach(ObjectVersion objectVersion in searchResults)
                        {
                            var assignedto = vault.ObjectPropertyOperations.GetProperty(objectVersion.ObjVer,44).TypedValue.GetValueAsLookups();
                            foreach (Lookup lookup in assignedto)
                            {
                                items.Add(new Assignmentmodel { Title= objectVersion.Title, UserID=lookup.Item.ToString(), VaultGuid= vaultguid });
                            }
                        }
                    }

                }
                catch (Exception ex)
                {

                }
            }
            return items;
         }
    }
}
