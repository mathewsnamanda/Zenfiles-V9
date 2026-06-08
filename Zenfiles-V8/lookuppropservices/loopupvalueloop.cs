using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp2.lookuppropservices
{
    public class loopupvalueloop
    {
        public ObjVer response(Vault vault,int ObjecttypeID,int objectid)
        {
            // We want to alter the document with ID 249.
            var objID = new MFilesAPI.ObjID();
            objID.SetIDs(
                ObjType: ObjecttypeID,
                ID: objectid);
           return vault.ObjectOperations.GetLatestObjVer(objID,true,false);
        }
    }
}
