using ConsoleApp1;
using Newtonsoft.Json;
using readingmetaconfigjson.MetaModals;
using readingmetaconfigjson.metaServices.Jsonlocation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace readingmetaconfigjson.metaServices
{
    public class MetaImplement : IMeta
    {   
        public List<behaveprop> behaveprops(string objecttypeid, string objectalias, string classalias, string classtypeid, List<workflowstatepropbehave> workflowstatepropbehaves, string vaultguid)
        {
            List<behaveprop> behaveprops = new List<behaveprop>();

            Getjsonpath getjsonpath = new Getjsonpath();
            string filepath = getjsonpath.getpath(vaultguid);
            if (File.Exists(filepath))
            {
                // Read the JSON file
                string jsonString = File.ReadAllText(filepath);
                MetadataConfig config = JsonConvert.DeserializeObject<MetadataConfig>(jsonString);

                foreach (var rule in config.Rules)
                {
                    var found = false;

                    if (rule.Filter?.Properties != null && rule.Filter?.Class != null && rule.Filter?.ObjectType != null)
                    {
                       
                        found = rule.Filter.ObjectType.Any(m => m.Equals(objecttypeid)|| m.Equals(objectalias));
                        found &= rule.Filter.Class.Any(m => m.Equals(classtypeid)|| m.Equals(classalias));
                        foreach (var prop in rule.Filter.Properties)
                        {
                            var propsd = workflowstatepropbehaves
                                          .FirstOrDefault(m => m.propid == prop.Property
                                          || m.workflowstatealias == prop.Property);

                            if (propsd == null)
                            {
                                found &= false;
                            }
                            else
                            {
                                if (propsd.propertytype == "MFDatatypeLookup")
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {
                                            if (!string.IsNullOrEmpty(prop.SSLU.Item.id))
                                            {
                                                if (prop.SSLU.Item.id != null)
                                                {

                                                    if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstatealias || prop.SSLU.Item.id != propsd.workflowstateguid)
                                                    {
                                                        found &= false;
                                                    }

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.State))
                                            {
                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstatealias && prop.SSLU.Item.State != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.workflow))
                                            {
                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstatealias && prop.SSLU.Item.workflow != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.valueListItem))
                                            {
                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstatealias && prop.SSLU.Item.valueListItem != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {
                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {
                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {
                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }

                                        }

                                    }

                                }
                                else
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {

                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstateguid && prop.SSLU.Item.id != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstateguid && prop.SSLU.Item.State != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstateguid && prop.SSLU.Item.workflow != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstateguid && prop.SSLU.Item.valueListItem != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid || prop.SSLU.Item.id == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid || prop.SSLU.Item.State == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid || prop.SSLU.Item.workflow == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid || prop.SSLU.Item.valueListItem == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }

                                    }
                                }
                            }

                        }
                        if (found)
                        {
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired??false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden??false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else if (rule.Filter?.Class != null && rule.Filter?.ObjectType != null)
                    {
                        found = rule.Filter.ObjectType.Any(m => m.Equals(objecttypeid)|| m.Equals(objectalias));
                        found &= rule.Filter.Class.Any(m => m.Equals(classtypeid)|| m.Equals(classalias));
                       
                        if (found)
                        {
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired ?? false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden ?? false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else if (rule.Filter?.Properties != null && rule.Filter?.Class != null)
                    {
                        var ensurecheck = false;
                        found = rule.Filter.Class.Any(m=>m.Equals(classtypeid)|| m.Equals(classalias));
                       
                        foreach (var prop in rule.Filter.Properties)
                        {
                            var propsd = workflowstatepropbehaves
                                          .FirstOrDefault(m => m.propid == prop.Property
                                          || m.workflowstatealias == prop.Property);

                            if (propsd == null)
                            {
                                found &= false;
                            }
                            else
                            {
                                if (propsd.propertytype == "MFDatatypeLookup")
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {
                                            if (!string.IsNullOrEmpty(prop.SSLU.Item.id))
                                            {
                                                if (prop.SSLU.Item.id != null)
                                                {

                                                    if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstatealias || prop.SSLU.Item.id != propsd.workflowstateguid)
                                                    {
                                                        found &= false;
                                                    }

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.State))
                                            {
                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstatealias && prop.SSLU.Item.State != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.workflow))
                                            {
                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstatealias && prop.SSLU.Item.workflow != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.valueListItem))
                                            {
                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstatealias && prop.SSLU.Item.valueListItem != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {
                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {
                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {
                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }

                                        }

                                    }

                                }
                                else
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {

                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstateguid && prop.SSLU.Item.id != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstateguid && prop.SSLU.Item.State != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstateguid && prop.SSLU.Item.workflow != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstateguid && prop.SSLU.Item.valueListItem != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid || prop.SSLU.Item.id == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid || prop.SSLU.Item.State == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid || prop.SSLU.Item.workflow == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid || prop.SSLU.Item.valueListItem == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }

                                    }
                                }
                            }
                            
                        }

                       
                            if (found)
                        {
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired??false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden??false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else if (rule.Filter?.Properties != null && rule.Filter?.ObjectType != null)
                    {
                        found = rule.Filter.ObjectType.Any(m => m.Equals(objecttypeid) || m.Equals(objectalias));
                        foreach (var prop in rule.Filter.Properties)
                        {
                            var propsd = workflowstatepropbehaves
                                          .FirstOrDefault(m => m.propid == prop.Property
                                          || m.workflowstatealias == prop.Property);

                            if (propsd == null)
                            {
                                found &= false;
                            }
                            else
                            {
                                if (propsd.propertytype == "MFDatatypeLookup")
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {
                                            if (!string.IsNullOrEmpty(prop.SSLU.Item.id))
                                            {
                                                if (prop.SSLU.Item.id != null)
                                                {

                                                    if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstatealias || prop.SSLU.Item.id != propsd.workflowstateguid)
                                                    {
                                                        found &= false;
                                                    }

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.State))
                                            {
                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstatealias && prop.SSLU.Item.State != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.workflow))
                                            {
                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstatealias && prop.SSLU.Item.workflow != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.valueListItem))
                                            {
                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstatealias && prop.SSLU.Item.valueListItem != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {
                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {
                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {
                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }

                                        }

                                    }

                                }
                                else
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {

                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstateguid && prop.SSLU.Item.id != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstateguid && prop.SSLU.Item.State != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstateguid && prop.SSLU.Item.workflow != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstateguid && prop.SSLU.Item.valueListItem != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid || prop.SSLU.Item.id == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid || prop.SSLU.Item.State == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid || prop.SSLU.Item.workflow == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid || prop.SSLU.Item.valueListItem == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }

                                    }
                                }
                            }

                        }
                        if (found)
                        {
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired ?? false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden ?? false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else if (rule.Filter?.ObjectType != null)
                    {
                        found = rule.Filter.ObjectType.Any(m => m.Equals(objecttypeid) || m.Equals(objectalias));
                        if (found)
                        {
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired ?? false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden ?? false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else if (rule.Filter?.Class != null)
                    {
                        found = rule.Filter.Class.Any(m => m.Equals(classtypeid) || m.Equals(classalias));
                        if (found)
                        {
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired ?? false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden ?? false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else if (rule.Filter?.Properties != null)
                    {
                        found = true;
                        foreach (var prop in rule.Filter.Properties)
                        {
                            var propsd = workflowstatepropbehaves
                                          .FirstOrDefault(m => m.propid == prop.Property
                                          || m.workflowstatealias == prop.Property);

                            if (propsd == null)
                            {
                                found &= false;
                            }
                            else
                            {
                                if (propsd.propertytype == "MFDatatypeLookup")
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {
                                            if (!string.IsNullOrEmpty(prop.SSLU.Item.id))
                                            {
                                                if (prop.SSLU.Item.id != null)
                                                {

                                                    if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstatealias || prop.SSLU.Item.id != propsd.workflowstateguid)
                                                    {
                                                        found &= false;
                                                    }

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.State))
                                            {
                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstatealias && prop.SSLU.Item.State != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.workflow))
                                            {
                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstatealias && prop.SSLU.Item.workflow != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(prop.SSLU.Item.valueListItem))
                                            {
                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstatealias && prop.SSLU.Item.valueListItem != propsd.workflowstateguid)
                                                {
                                                    found &= false;

                                                }
                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {
                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {
                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {
                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid)
                                                    found &= false;

                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }

                                        }

                                    }

                                }
                                else
                                {
                                    if (prop.Operator == "IsNULL" && !string.IsNullOrEmpty(propsd.propertyvalue))
                                    {
                                        found &= false;
                                    }
                                    else if (prop.SSLU != null)
                                    {
                                        if (prop.Operator == "Is")
                                        {

                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id != propsd.propertyvalue && prop.SSLU.Item.id != propsd.workflowstateguid && prop.SSLU.Item.id != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State != propsd.propertyvalue && prop.SSLU.Item.State != propsd.workflowstateguid && prop.SSLU.Item.State != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow != propsd.propertyvalue && prop.SSLU.Item.workflow != propsd.workflowstateguid && prop.SSLU.Item.workflow != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem != propsd.propertyvalue && prop.SSLU.Item.valueListItem != propsd.workflowstateguid && prop.SSLU.Item.valueListItem != propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }

                                            }
                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            if (prop.SSLU.Item.id != null)
                                            {

                                                if (prop.SSLU.Item.id == propsd.propertyvalue || prop.SSLU.Item.id == propsd.workflowstateguid || prop.SSLU.Item.id == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.State != null)
                                            {

                                                if (prop.SSLU.Item.State == propsd.propertyvalue || prop.SSLU.Item.State == propsd.workflowstateguid || prop.SSLU.Item.State == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.workflow != null)
                                            {

                                                if (prop.SSLU.Item.workflow == propsd.propertyvalue || prop.SSLU.Item.workflow == propsd.workflowstateguid || prop.SSLU.Item.workflow == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                            else if (prop.SSLU.Item.valueListItem != null)
                                            {

                                                if (prop.SSLU.Item.valueListItem == propsd.propertyvalue || prop.SSLU.Item.valueListItem == propsd.workflowstateguid || prop.SSLU.Item.valueListItem == propsd.workflowstatealias)
                                                {
                                                    found &= false;
                                                }
                                            }
                                        }
                                    }
                                    else if (prop.MSLU != null)
                                    {

                                        if (prop.Operator == "Is")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }
                                        else if (prop.Operator == "IsNot")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }
                                            founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                            if (founds != null)
                                            {
                                                found &= false;
                                            }

                                        }
                                        else if (prop.Operator == "HasAny")
                                        {
                                            var founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.propertyvalue || m.Item.State == propsd.propertyvalue || m.Item.workflow == propsd.propertyvalue || m.Item.valueListItem == propsd.propertyvalue);
                                            if (founds == null)
                                            {
                                                founds = prop.MSLU.FirstOrDefault(m => m.Item.id == propsd.workflowstateguid || m.Item.State == propsd.workflowstateguid || m.Item.workflow == propsd.workflowstateguid || m.Item.valueListItem == propsd.workflowstateguid);
                                                if (founds == null)
                                                {
                                                    found &= false;
                                                }
                                            }


                                        }

                                    }
                                }
                            }

                        }
                        if (found)
                        {
                           
                            foreach (var behaviorProp in rule.Behavior.Properties)
                            {
                                behaveprop behaveprop = new behaveprop();
                                if (behaviorProp.IsRequired != null)
                                    behaveprop.IsRequired = behaviorProp.IsRequired??false;
                                if (behaviorProp.IsHidden != null)
                                    behaveprop.IsHidden = behaviorProp.IsHidden ?? false;
                                behaveprop.Property = behaviorProp.Property.ToString();
                                behaveprop.Priority = behaviorProp.Priority;
                                var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                                if (propfoundp == null)
                                    behaveprops.Add(behaveprop);
                            }
                        }
                    }
                    else
                    {
                        foreach (var behaviorProp in rule.Behavior.Properties)
                        {
                            behaveprop behaveprop = new behaveprop();
                            if (behaviorProp.IsRequired != null)
                                behaveprop.IsRequired = behaviorProp.IsRequired ?? false;
                            if (behaviorProp.IsHidden != null)
                                behaveprop.IsHidden = behaviorProp.IsHidden ?? false;
                            behaveprop.Property = behaviorProp.Property.ToString();
                            behaveprop.Priority = behaviorProp.Priority;
                            var propfoundp = behaveprops.FirstOrDefault(m => m.Property == behaviorProp.Property.ToString());
                            if (propfoundp == null)
                                behaveprops.Add(behaveprop);
                        }
                    }
                }

            }
            return behaveprops;
        }
    }
}
