# System.ScriptLibrary

**Declarations**

```d4
create operator .System.ScriptLibrary(const ALibraryName : System.Name) : System.String
 class "System.SystemScriptLibraryNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ScriptLibrary` returns a D4 script that would create all the objects currently in the specified library `ALibraryName`.

**Using System.ScriptLibrary**

```d4
select ScriptLibrary("SimpleDevice");
```

```d4
create operator .SimpleDevice.SaveTable(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.SaveTableNode" static tags { DAE.IsRemotable = "false" };
. . .
create operator .SimpleDevice.EndUpdate(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.EndUpdateNode" static tags { DAE.IsRemotable = "false" };
System.SetObjectOwner(System.Name('.SimpleDevice.SaveTable_System_Name_System_Name_'), 'Admin');
. . .
System.SetObjectOwner(System.Name('.SimpleDevice.EndUpdate_System_Name_System_Name_'), 'Admin');
begin
    System.SafeGrantRightToRole(System.Name('SimpleDevice.BeginUpdate_System_Name_System_Name_Execute'), System.Name('System.User'));
 . . .
    System.SafeGrantRightToRole(System.Name('SimpleDevice.SaveTable_System_Name_System_Name_Execute'), System.Name('System.User'));
end;
```

**See Also**

[Scripts and Execution](../../../developers-guide/d4-scripts-and-execution.md) \| [Frontend.ExecuteScript](../../libraries/Frontend.md#frontendexecutescript) \| [System.Script](System.Script.md) \| [System.ScriptCatalog](System.ScriptCatalog.md) \| [System.ScriptDrop](System.ScriptDrop.md) \| [System.ScriptDropCatalog](System.ScriptDropCatalog.md) \| [System.ScriptDropLibrary](System.ScriptDropLibrary.md) \| [System.ScriptData](System.ScriptData.md)
