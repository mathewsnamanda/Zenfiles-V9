using MFilesAPI;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;
using ZenFiles.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZenFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    public class objectstractureController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public objectstractureController(IConfiguration Configuration)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));

        }
        [HttpPost("CreateObjectAdmin")]
        public IActionResult CreateObjectAdmin([FromBody] MfilesObject1 mfilesObject)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            MfilesObject4 mfilesObject4 = new MfilesObject4();

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
                var vault = mfServerApplication.LogInToVault(mfilesObject.VaultGuid);
                int onjid = -1;
                var foundobj = vault.ObjectTypeOperations.GetObjectTypesAdmin();
                foreach(ObjTypeAdmin objType in  foundobj)
                {
                    if(objType.ObjectType.NameSingular == mfilesObject.objectName.Trim())
                    {
                        onjid = objType.ObjectType.ID;
                    }
                }
                if (onjid == -1)
                {
                    foreach (var item in mfilesObject.properties)
                    {
                        if(!string.IsNullOrEmpty(item.propertytype))
                        {
                            if (!item.propertytype.Contains("MFDatatypeBoolean") && !item.propertytype.Contains("MFDatatypeText") &&
                                !item.propertytype.Contains("MFDatatypeMultiLineText") && !item.propertytype.Contains("MFDatatypeDate") &&
                                !item.propertytype.Contains("MFDatatypeTime") && !item.propertytype.Contains("MFDatatypeFloating") &&
                                !item.propertytype.Contains("MFDatatypeTimestamp") && !item.propertytype.Contains("MFDatatypeInteger"))
                            {
                                return BadRequest("One of the property datatypes is wrong");
                            }
                        }
                       
                    }
                    ObjTypeAdmin objTypeAdmin= new ObjTypeAdmin();
                    objTypeAdmin.ObjectType.AllowAdding = true;
                    objTypeAdmin.ObjectType.NamePlural = mfilesObject.objectName.Trim()+"s";
                    objTypeAdmin.ObjectType.NameSingular = mfilesObject.objectName.Trim();
                    objTypeAdmin.SemanticAliases.Value = $"OT.{mfilesObject.objectName.Trim()}";
                    objTypeAdmin.ObjectType.AllowedAsGroupingLevel = true;
                    objTypeAdmin.ObjectType.CanHaveFiles = false;
                    objTypeAdmin.ObjectType.DefaultPropertyDef = 0;
                    objTypeAdmin.ObjectType.OwnerPropertyDef = 0;
                    objTypeAdmin.ObjectType.ShowCreationCommandInTaskPane = true;
                    objTypeAdmin.ObjectType.RealObjectType = true;

                    var newobj = vault.ObjectTypeOperations.AddObjectTypeAdmin(objTypeAdmin);
                    mfilesObject4.objectID = newobj.ObjectType.ID;

                    var classes = vault.ClassOperations.GetObjectClassesAdmin(newobj.ObjectType.ID);
                    foreach (ObjectClassAdmin objectClassAdmin in classes)
                    {
                        objectClassAdmin.SemanticAliases.Value = $"CL.{objectClassAdmin.Name}";
                        mfilesObject4.classID = objectClassAdmin.ID;
                        List<Property5> property4s = new List<Property5>();
                        var props = objectClassAdmin.AssociatedPropertyDefs;
                        bool found = false;
                        List<Property> newprops = new List<Property>();
                        foreach (AssociatedPropertyDef prop in objectClassAdmin.AssociatedPropertyDefs)
                        {
                            if (prop.PropertyDef == 0)
                            {
                                found = true;
                            }
                        }
                        if (found)
                        {
                            int firstproid = 0;
                            List<propreqs> proids = new List<propreqs>();

                            foreach(var item in mfilesObject.properties)
                            {
                                if(!string.IsNullOrEmpty(item.propertytype)&& !string.IsNullOrEmpty(item.title))
                                {
                                    var prop = vault.PropertyDefOperations.GetPropertyDefIDByAlias($"PD.{item.title.Trim()}");
                                    Property property = new Property();
                                    if (prop == -1)
                                    {
                                        PropertyDefAdmin propertyDefAdmin = new PropertyDefAdmin();
                                        propertyDefAdmin.SemanticAliases.Value = $"PD.{item.title.Trim()}";
                                        propertyDefAdmin.PropertyDef.ObjectsSearchableByThisProperty = true ;
                                        if (item.propertytype.ToLower() == "MFDatatypeBoolean".ToLower())
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeBoolean;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeText".ToLower())
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeText;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeMultiLineText".ToLower())
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeMultiLineText;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeDate")
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeDate;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeTime")
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeTime;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeFloating")
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeFloating;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeTimestamp")
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeTimestamp;
                                        }
                                        else if (item.propertytype.ToLower() == "MFDatatypeInteger")
                                        {
                                            propertyDefAdmin.PropertyDef.DataType = MFDataType.MFDatatypeInteger;
                                        }
                                        propertyDefAdmin.PropertyDef.Name = item.title.Trim();
                                        propertyDefAdmin.PropertyDef.AllowedAsGroupingLevel = true;
                                        propertyDefAdmin.PropertyDef.AllObjectTypes = true;
                                        var propbuild = vault.PropertyDefOperations.AddPropertyDefAdmin(propertyDefAdmin);
                                        property.title = propbuild.PropertyDef.Name;
                                        property.propertytype = propbuild.PropertyDef.DataType.ToString();
                                        property.propId = propbuild.PropertyDef.ID;
                                        newprops.Add(property);
                                        if (firstproid == 0)
                                        {
                                            firstproid = propbuild.PropertyDef.ID;
                                            property4s.Add(new Property5 { title = item.title, propId = firstproid});
                                        }
                                        else
                                        {
                                            proids.Add(new propreqs { propId= propbuild.PropertyDef.ID , IsRequired = item.IsRequired});
                                            property4s.Add(new Property5 { title = item.title, propId = firstproid });
                                        }
                                    }
                                    else
                                    {
                                        if (firstproid == 0)
                                        {
                                            firstproid = prop;
                                            property4s.Add(new Property5 { title = item.title, propId = firstproid });

                                        }
                                        else
                                        {
                                            proids.Add(new propreqs { propId= prop , IsRequired=item.IsRequired});
                                            property4s.Add(new Property5 { title = item.title, propId = firstproid });
                                        }

                                        var proppd = vault.PropertyDefOperations.GetPropertyDef(prop);
                                        if(proppd != null)
                                        {
                                            property.title = proppd.Name;
                                            property.propertytype = proppd.DataType.ToString();
                                            property.propId = proppd.ID;
                                            newprops.Add(property);
                                        }
                                      

                                    }
                                }
                               
                            }

                            if(firstproid>0)
                            {
                                AssociatedPropertyDef associatedPropertyDef = new AssociatedPropertyDef();
                                associatedPropertyDef.PropertyDef = firstproid;
                                associatedPropertyDef.Required = true;
                                objectClassAdmin.AssociatedPropertyDefs.Remove(1);
                                objectClassAdmin.AssociatedPropertyDefs.Add(-1, associatedPropertyDef);

                                foreach(var prop in proids)
                                {
                                    AssociatedPropertyDef associatedPropertyDef1 = new AssociatedPropertyDef();
                                    associatedPropertyDef1.PropertyDef = prop.propId;
                                    associatedPropertyDef1.Required = prop.IsRequired;

                                    objectClassAdmin.AssociatedPropertyDefs.Add(-1, associatedPropertyDef1);

                                }

                                objectClassAdmin.NamePropertyDef = associatedPropertyDef.PropertyDef;


                            } 
                        }
                        objectClassAdmin.Name = mfilesObject.ClassName;
                        mfilesObject4.properties = property4s;
                        vault.ClassOperations.UpdateObjectClassAdmin(objectClassAdmin);

                    }
                    return Ok(mfilesObject4);
                }
                else
                {
                    return BadRequest("Another object with same name already exists"); ;
                }
              
            }
            catch(Exception ex)
            {
                return  BadRequest(ex.Message);
            }

        }
        [HttpPost("DeleteObjectProps")]
        public IActionResult DeleteProps([FromBody] prop5 prop5)
        {
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            MfilesObject4 mfilesObject4 = new MfilesObject4();

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
                var vault = mfServerApplication.LogInToVault(prop5.VaultGuid);
                
                bool done = false;
                PropsError propsError =  new PropsError();
                List<int> properrorids= new List<int>();
                string propidswitherror = "";
                var foundobj = vault.ObjectTypeOperations.GetObjectType(prop5.objectid);
                if (foundobj!=null)
                    {
                        var classp = vault.ClassOperations.GetObjectClass(prop5.classid);
                        
                        if (classp != null)
                            {
                                List<propcheckers> list = new List<propcheckers>();
                                int titleid = classp.NamePropertyDef;
                                List<propcheckers> props = new List<propcheckers>();
                                List<int> removeprops = new List<int>();
                                List<int> cant = new List<int>();
                               
                                foreach (AssociatedPropertyDef associatedPropertyDef in classp.AssociatedPropertyDefs)
                                {
                                    if (associatedPropertyDef.PropertyDef != titleid && associatedPropertyDef.PropertyDef > 1000)
                                    {
                                        props.Add(new propcheckers { id = associatedPropertyDef.PropertyDef, required = associatedPropertyDef.Required });
                                    }
                                    else if (associatedPropertyDef.PropertyDef == titleid) 
                                    {
                                        propsError.errortitle = "Title prop cannot be removed";
                                        cant.Add(titleid);
                                        propsError.propsids = cant;
                                    };
                                }
                                if (props.Count>0)
                        {
                            foreach (var item in prop5.propIds)
                            {
                                var itemm = props.FirstOrDefault(m => m.id == item.propid);
                                if (itemm != null)
                                {
                                    props.Remove(itemm);
                                    removeprops.Add(itemm.id);
                                }

                            }
                            var objClass = vault.ClassOperations.GetObjectClassAdmin(prop5.classid);
                            var titlefoundinitems = prop5.propIds.FirstOrDefault(m => m.propid == titleid);
                            if (titlefoundinitems != null)
                            {
                                removeprops.Add(titlefoundinitems.propid);
                            }
                            if (titlefoundinitems != null)
                            {
                                propsError.errortitle = "Title prop cannot be removed";
                                cant.Add(titlefoundinitems.propid);
                                propsError.propsids = cant;
                            }
                            objClass.NamePropertyDef = titleid;
                            foreach (var itemp in props)
                            {
                                AssociatedPropertyDef associatedPropertyDef1 = new AssociatedPropertyDef();
                                associatedPropertyDef1.PropertyDef = itemp.id;
                                associatedPropertyDef1.Required = itemp.required;
                                objClass.AssociatedPropertyDefs.Add(-1, associatedPropertyDef1);
                            }

                            vault.ClassOperations.UpdateObjectClassAdmin(objClass);
                            foreach (var item in removeprops)
                            {
                                vault.PropertyDefOperations.RemovePropertyDefAdmin(item);
                            }
                        }
                               
                                 done = true;
                             }
                    }
               
                if(!done)
                {
                    return BadRequest($"could not handle your request"); 
                }
                else
                {
                    if (propsError != null)
                    {
                        propsError.errortitle = "Object props Updated except could not remove the following title prop";
                        return Ok(propsError);
                    }
                    return Ok("Object props removed successfully");
                }
             
             }

            
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("DeleteObject/{ObjectID}/{VaultGuid}")]
        public IActionResult DeleteObject(int ObjectID, string VaultGuid)
        {
            if (ObjectID <= 100)
            {
                return BadRequest("Cannot delete this object");
            }
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            MfilesObject4 mfilesObject4 = new MfilesObject4();

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

                bool done = false;
                string propidswitherror = "";
                var foundobj = vault.ObjectTypeOperations.GetObjectType(ObjectID);
               if(foundobj != null)
                {
                    List<int> classids = new List<int>();
                    List<int> propids = new List<int>();
                    var classp = vault.ClassOperations.GetObjectClasses(foundobj.ID);
                    foreach (ObjectClass objClass in classp)
                    {
                        foreach (AssociatedPropertyDef propertyDef in objClass.AssociatedPropertyDefs)
                        {
                           
                            if (propertyDef.PropertyDef > 1000)
                            {
                                propids.Add(propertyDef.PropertyDef);
                            }
                           
                        }

                    }
                    vault.ObjectTypeOperations.RemoveObjectTypeAdmin(foundobj.ID);

                    foreach (int id in propids)
                    {
                        try
                        {
                            vault.PropertyDefOperations.RemovePropertyDefAdmin(id);
                        }
                        catch
                        {

                        }

                    }
                }
                  
                
                if (!done)
                {
                    return BadRequest($"an error occured during the removal of:{propidswitherror} props");
                }
                else
                {
                    return Ok("Object props Updated");
                }

            }


            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("GetObjectName/{ObjectID}/{VaultGuid}")]
        public IActionResult GetObjectName(int ObjectID, string VaultGuid)
        {
          
            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            MfilesObject4 mfilesObject4 = new MfilesObject4();

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

                bool done = false;
                string propidswitherror = "";
                var foundobj = vault.ObjectTypeOperations.GetObjectType(ObjectID);
                if (foundobj != null)
                {
                    ObjectClassTitle objectClassTitle = new ObjectClassTitle();
                    objectClassTitle.Name = foundobj.NameSingular;
                    objectClassTitle.id = foundobj.ID;
                    return Ok(objectClassTitle);
                }
                else
                {
                    return BadRequest("Object does not exist");
                }

            }


            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("GetClassName/{ClassID}/{VaultGuid}")]
        public IActionResult GetClassName(int ClassID, string VaultGuid)
        {

            string Username = this._configuration.GetConnectionString("Username") ?? "";
            string Password = this._configuration.GetConnectionString("Password") ?? "";
            string ipaddress = this._configuration.GetConnectionString("IP") ?? "";
            string domain = this._configuration.GetConnectionString("Domain") ?? "";
            string port = this._configuration.GetConnectionString("HostPort") ?? "";
            MfilesObject4 mfilesObject4 = new MfilesObject4();

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

                bool done = false;
                string propidswitherror = "";
                var foundobj = vault.ClassOperations.GetObjectClass(ClassID);
                if (foundobj != null)
                {
                    ObjectClassTitle objectClassTitle = new ObjectClassTitle();
                    objectClassTitle.Name = foundobj.Name;
                    objectClassTitle.id = foundobj.ID;
                    return Ok(objectClassTitle);
                }
                else
                {
                    return BadRequest("Object does not exist");
                }

            }


            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }
}
