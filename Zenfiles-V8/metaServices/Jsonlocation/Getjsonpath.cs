using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace readingmetaconfigjson.metaServices.Jsonlocation
{
    public class Getjsonpath
    {
        public string getpath(string vaultguid)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(),"getpath");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var filepath = Path.Combine(path, $"{vaultguid}.json");
            if (File.Exists(filepath))
                return filepath;
            else
                return "";
        }
    }
}
