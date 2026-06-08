using System;
using System.Collections.Generic;

[Flags]
public enum MFUserAccountVaultRole1
{
    None = 0,
    FullControl = 1,
    LogIn = 2,
    CreateObjects = 4,
    SeeAllObjects = 8,
    UndeleteObjects = 16,
    DestroyObjects = 32,
    ForceUndoCheckout = 64,
    ChangeObjectSecurity = 128,
    ChangeMetadataStructure = 256,
    ManageUserAccounts = 512,
    InternalUser = 1024,
    ManageTraditionalFolders = 2048,
    DefaultRoles = 3078,
    DefineTemplates = 4096,
    ManageCommonViews = 8192,
    ManageWorkflows = 16384,
    CannotManagePrivateViews = 32768,
    AnonymousUser = 65536
}

public class EnumDecryptor
{
    public static List<MFUserAccountVaultRole1> Decrypt(int value)
    {
        var results = new List<MFUserAccountVaultRole1>();

        // First check if the value contains DefaultRoles
        if (value >= (int)MFUserAccountVaultRole1.DefaultRoles)
        {
            results.Add(MFUserAccountVaultRole1.DefaultRoles);
            value -= (int)MFUserAccountVaultRole1.DefaultRoles;
        }

        // Check remaining flags
        foreach (MFUserAccountVaultRole1 role in Enum.GetValues(typeof(MFUserAccountVaultRole1)))
        {
            if (role == MFUserAccountVaultRole1.DefaultRoles || role == MFUserAccountVaultRole1.None)
                continue;

            if ((value & (int)role) == (int)role)
            {
                results.Add(role);
            }
        }

        return results;
    }
}

