using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Zenfiles.Models.objversions;
using Zenfiles.PermissionService;
using ZenFiles.Models;

namespace ConsoleApp10.LinkedObjectService
{
    public class linkedobjectfunction
    {
        private readonly Zenfiles.PermissionService.IPermission _permission;
        public linkedobjectfunction(Zenfiles.PermissionService.IPermission permission)
        {
            _permission = permission;
        }
        public Objectsearchresponse objectsearchresponse(Vault vault, int Objecttype, string displayId,int UserID)
        {
            Objectsearchresponse objectsearchresponse = new Objectsearchresponse();
           
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
                    Objecttype);

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
            //display id
            {
                // Create the condition.
                var condition = new SearchCondition();

                // Set the expression.
                condition.Expression.DataStatusValueType = MFStatusType.MFStatusTypeExtID;

                // Set the condition type.
                condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                // Set the value.
                // In this case "MyExternalObjectId" is the ID of the object in the remote system.
                condition.TypedValue.SetValue(MFDataType.MFDatatypeText, displayId);
                searchConditions.Add( -1, condition);
            }

            // Execute the search.
            var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions,
                MFSearchFlags.MFSearchFlagNone, SortResults: false);
            if (searchResults.Count > 0)
            {
                foreach (ObjectVersion objectVersion in searchResults)
                {

                    var perm = _permission.ObjectPermission(vault, UserID, objectVersion.ObjVer.Type);
                    if (perm.ReadPermission)
                    {
                        perm = _permission.ClassPermission(vault, UserID, objectVersion.Class);

                    }
                    if (perm.ReadPermission)
                    {
                        var objecttype = vault.ObjectTypeOperations.GetObjectType(objectVersion.ObjVer.Type);
                        var classname = "";

                        var propfordisplay = vault.ObjectPropertyOperations.GetPropertiesForDisplay(objectVersion.ObjVer);
                        foreach (PropertyValueForDisplay propertyValueForDisplay in propfordisplay)
                        {
                            if (propertyValueForDisplay.PropertyDef == 100)
                            {
                                classname = propertyValueForDisplay.PropertyValue.Value.DisplayValue;
                            }
                        }
                        objectsearchresponse.VersionId = objectVersion.ObjVer.Version;
                        objectsearchresponse.Title = objectVersion.Title;
                        objectsearchresponse.CreatedUtc = DateTime.UtcNow;
                        objectsearchresponse.ObjectID = objectVersion.ObjVer.Type;
                        objectsearchresponse.ClassID = objectVersion.Class;
                        objectsearchresponse.DisplayID = objectVersion.DisplayID;
                        objectsearchresponse.IsSingleFile = objectVersion.SingleFile;
                        objectsearchresponse.LastModifiedUtc = objectVersion.LastModifiedUtc;
                        objectsearchresponse.userPermission = perm;
                        objectsearchresponse.ObjectTypeName = objecttype.NamePlural;
                        objectsearchresponse.ClassTypeName = classname;
                        objectsearchresponse.id = objectVersion.ObjVer.ID;
                    }
                    else
                    {
                        objectsearchresponse = null;
                    }
                    
                }
            }

            return objectsearchresponse;
        }
    }
}
