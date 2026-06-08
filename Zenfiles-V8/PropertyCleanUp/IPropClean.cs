using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp6.PropertyCleanUp
{
    public interface IPropClean
    {
        string cleaned(string text,Vault vault);
        string classcleaned(string text,Vault vault);
        string objectcleaned(string text, Vault vault);
    }
}
