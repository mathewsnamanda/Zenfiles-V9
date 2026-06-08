using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Zenfiles_V8.Services;

namespace ConsoleApp1
{
    public class Gettingusersinusergroup
    {
        private readonly GetCacheObjects _cacheObjects;
        public Gettingusersinusergroup(GetCacheObjects cacheObjects)
        {
            _cacheObjects = cacheObjects;
        }
        public List<int> GetUsersFromGroup(Vault vault, int groupId)
        {
            List<int> users = new List<int>();

            // Get the user group object
           
            UserGroup group = _cacheObjects.UserGroups(vault)?.FirstOrDefault(m=>m.ID==groupId);
            if(group != null)
            foreach (int memberId in group.Members)
            {
                try
                {
                    if (memberId > 0)
                    {

                        users.Add(memberId);
                    }
                    else
                    {
                        // Negative IDs are nested user groups → recurse
                        users.AddRange(GetUsersFromGroup(vault, memberId * -1));
                    }
                }
                catch
                {

                }

            }


            return users;
        }

    }
}
