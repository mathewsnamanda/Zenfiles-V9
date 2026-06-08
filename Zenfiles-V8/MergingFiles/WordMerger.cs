using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using MFilesAPI;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.XlsIO;

namespace Zenfiles_V8.MergingFiles
{
    public class WordMerger
    {
        public string merge(Vault vault, string path, ObjectVersion objectversion)
        {
            string pathy = "";
            if (File.Exists(path))
            {
                var fields = ReadCustomProperties(path);
                foreach (var field in fields)
                {
                    if (field.Key.Contains("MFiles_"))
                    {
                        var SY = objectversion.ObjVer;
                        int count = 0;
                        var propertyvalue = "";
                        var mfilestring = field.Key.Replace("MFiles_", "");
                        if (mfilestring.Contains("PG"))
                        {
                            foreach (var p in mfilestring.Split("_"))
                            {
                                try
                                {
                                    if (count + 1 < mfilestring.Split("_").Length)
                                    {
                                        var propsd = vault.PropertyDefOperations.GetPropertyDefIDByGUID(Guid.Parse(p.Substring(2)).ToString());
                                        ConsoleApp2.lookuppropservices.loopupvalueloop loopupvalueloop = new ConsoleApp2.lookuppropservices.loopupvalueloop();
                                        var prop = vault.ObjectPropertyOperations.GetProperty(SY, propsd).Value.GetValueAsLookup();
                                        SY = loopupvalueloop.response(vault, prop.ObjectType, prop.Item);

                                    }
                                    else
                                    {
                                        var propsd = vault.PropertyDefOperations.GetPropertyDefIDByGUID(Guid.Parse(p.Substring(2)).ToString());
                                        if (propsd >= 0)
                                        {
                                            var prop = vault.ObjectPropertyOperations.GetProperty(SY, propsd).Value.DisplayValue;
                                            propertyvalue = prop;

                                        }
                                    }

                                    count += 1;
                                }
                                catch
                                {

                                }

                            }
                        }
                        else
                        {
                            foreach (var p in mfilestring.Split("_"))
                            {
                                if (count + 1 < mfilestring.Split("_").Length)
                                {
                                    ConsoleApp2.lookuppropservices.loopupvalueloop loopupvalueloop = new ConsoleApp2.lookuppropservices.loopupvalueloop();
                                    var prop = vault.ObjectPropertyOperations.GetProperty(SY, Convert.ToInt16(p.Substring(1).Trim())).Value.GetValueAsLookup();
                                    SY = loopupvalueloop.response(vault, prop.ObjectType, prop.Item);
                                }
                                else
                                {
                                    var prop = vault.ObjectPropertyOperations.GetProperty(SY, Convert.ToInt16(p.Substring(1).Trim())).Value.DisplayValue;
                                    propertyvalue = prop;
                                }
                                count += 1;
                            }
                        }
                        if (!string.IsNullOrEmpty(propertyvalue))
                        {
                            UpdateCustomProperty(path, field.Key, propertyvalue);
                            SyncfusionRefreshWordFields(path);
                        }


                    }

                }
            }
            return pathy;
        }
        public static Dictionary<string, string> ReadCustomProperties(string filePath)
        {
            var properties = new Dictionary<string, string>();

            using (var doc = WordprocessingDocument.Open(filePath, false))
            {
                var customPropsPart = doc.CustomFilePropertiesPart;
                if (customPropsPart != null)
                {
                    foreach (var prop in customPropsPart.Properties.Elements<CustomDocumentProperty>())
                    {
                        string name = prop.Name?.Value ?? string.Empty;
                        string value = string.Empty;

                        // Each property has exactly one child element of a variant type
                        var variant = prop.FirstChild;
                        if (variant is VTLPWSTR lpwstr)
                            value = lpwstr.Text;
                        else if (variant is VTFileTime fileTime)
                            value = fileTime.Text;
                        else if (variant is VTBool vbool)
                            value = vbool.Text;
                        else if (variant is VTInt32 vint)
                            value = vint.Text;
                        else if (variant is VTDouble vdouble)
                            value = vdouble.Text;
                        else if (variant is VTDecimal vdec)
                            value = vdec.Text;
                        else if (variant is VTDate date)
                            value = date.Text;

                        properties[name] = value;
                    }
                }
            }

            return properties;
        }
        static void UpdateCustomProperty(string filePath, string name, string value)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, true))
            {
                var customProps = wordDoc.CustomFilePropertiesPart;
                if (customProps == null)
                {
                    customProps = wordDoc.AddCustomFilePropertiesPart();
                    customProps.Properties = new Properties();
                }

                var props = customProps.Properties;
                var existingProp = props.Elements<CustomDocumentProperty>()
                                        .FirstOrDefault(p => p.Name.Value == name);

                if (existingProp != null)
                {
                    existingProp.VTLPWSTR = new VTLPWSTR(value);
                }

                props.Save();
            }
        }
        static void SyncfusionRefreshWordFields(string filePath)
        {
            var newpath = Path.Combine(Directory.GetCurrentDirectory(),"Files",Guid.NewGuid().ToString() + Path.GetExtension(filePath));
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                WordDocument wordDocument = new WordDocument(fileStream, FormatType.Docx);
                wordDocument.UpdateDocumentFields();
                using (FileStream fileStream1 = new FileStream(newpath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    wordDocument.Save(fileStream1, FormatType.Docx);
                }
                wordDocument.Close();
            }
            System.IO.File.Move(newpath,filePath,true);
        }
    }
}
