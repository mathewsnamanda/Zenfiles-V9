using MFilesAPI;
using Syncfusion.DocIO.DLS;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Zenfiles.Models.objversions;

namespace ConsoleApp4.MergingFiles
{
    public class ExcelMerger
    {
        public string merge(Vault vault,string path, ObjectVersion objectversion)
        {
            string returnedpath = "";
            string filePath = path;
            // Open existing workbook using FileStream
            using (FileStream inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                string extension = Path.GetExtension(filePath).ToLower();

                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Open(inputStream);
                    foreach (IName name in workbook.Names)
                    {
                        if (name.Name.Contains("MFiles_"))
                        {
                            var SY = objectversion.ObjVer;
                            int count = 0;
                            var propertyvalue = "";
                            var mfilestring = name.Name.Replace("MFiles_", "");
                            if (mfilestring.Length > 32)
                            {
                                if (mfilestring.Contains("PG"))
                                {
                                    foreach (var p in mfilestring.Split("_"))
                                    {
                                        try
                                        {
                                            if (count + 1 < mfilestring.Split("_").Length)
                                            {
                                                var propsd = vault.PropertyDefOperations.GetPropertyDefIDByGUID(Guid.Parse(p.Substring(2, 32)).ToString());
                                                ConsoleApp2.lookuppropservices.loopupvalueloop loopupvalueloop = new ConsoleApp2.lookuppropservices.loopupvalueloop();
                                                var prop = vault.ObjectPropertyOperations.GetProperty(SY, propsd).Value.GetValueAsLookup();
                                                SY = loopupvalueloop.response(vault, prop.ObjectType, prop.Item);

                                            }
                                            else
                                            {

                                                var propid = Guid.Parse(p.Substring(2, 32)).ToString();
                                                var propsd = vault.PropertyDefOperations.GetPropertyDefIDByGUID(propid);
                                                if (propsd >= 0)
                                                {
                                                    var prop = vault.ObjectPropertyOperations.GetProperty(SY, propsd).Value.DisplayValue;
                                                    propertyvalue = prop;

                                                }
                                            }

                                            count += 1;
                                        }
                                        catch (Exception ex)
                                        {

                                        }

                                    }
                                }
                                else
                                {
                                    foreach (var p in mfilestring.Split("_"))
                                    {
                                        try
                                        {
                                            if (count + 1 < mfilestring.Split("_").Length)
                                            {
                                                ConsoleApp2.lookuppropservices.loopupvalueloop loopupvalueloop = new ConsoleApp2.lookuppropservices.loopupvalueloop();
                                                var prop = vault.ObjectPropertyOperations.GetProperty(SY, Convert.ToInt16(p.Substring(1, 4).Trim())).Value.GetValueAsLookup();
                                                SY = loopupvalueloop.response(vault, prop.ObjectType, prop.Item);
                                            }
                                            else
                                            {
                                                var prop = vault.ObjectPropertyOperations.GetProperty(SY, Convert.ToInt16(p.Substring(1, 4).Trim())).Value.DisplayValue;
                                                propertyvalue = prop;
                                            }
                                        }
                                        catch
                                        {

                                        }

                                        count += 1;
                                    }
                                }
                                if (!string.IsNullOrEmpty(propertyvalue))
                                {
                                    workbook.Names.Remove(name.Name);
                                    IName stringConstant = workbook.Names.Add(name.Name);
                                    stringConstant.Value = $"=\"{propertyvalue}\"";
                                    stringConstant.Description = name.Description;

                                }
                            }

                        }

                    }

                    for (int i = 0; i < workbook.Worksheets.Count(); i++)
                    {
                        try
                        {
                            IWorksheet sheet = workbook.Worksheets[i];

                            if (sheet.UsedRange != null && sheet.UsedRange.Count > 1)
                            {
                                var usedrange = sheet.UsedRange.Cells[0];
                                // Loop through each cell in the used range
                                foreach (IRange cell in sheet.UsedRange.Cells)
                                {
                                    // Check the value/text of the cell
                                    if (!string.IsNullOrEmpty(cell.Text))
                                    {
                                        sheet.Calculate();
                                        // Check if the sheet has any data
                                    }
                                }
                              
                            }
                        }
                        catch
                        {

                        }
                      
                        
                    }
                   
                    returnedpath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Files",Guid.NewGuid().ToString() + extension);

                    // Save back to file using FileStream
                    using (FileStream outputStream = new FileStream(returnedpath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.SaveAs(outputStream);
                    }
                    // Recalculate all formulas in the workbook

                }
            }
            return returnedpath;
        }
       

    }
}
