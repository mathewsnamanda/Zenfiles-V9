using MFilesAPI;

namespace Zenfiles_V8.Services
{
    public interface IObjectTypeProvider
    {
        IEnumerable<ObjTypeAdmin> GetObjectTypes(Vault vault);
        IEnumerable<ObjectClassAdmin> ClassTypes(Vault vault);
        IEnumerable<PropertyDefAdmin> PropTypes(Vault vault);
        IEnumerable<WorkflowAdmin> WorkflowTypes(Vault vault);
        IEnumerable<ClassGroup> ClassGroupTypes(Vault vault,int objecttypes);
    }
}

