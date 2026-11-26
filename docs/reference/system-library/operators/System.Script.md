# System.Script

**Declarations**

```d4
create operator .System.Script(const AName : System.Name) : System.String
 class "System.SystemScriptNode" static tags { DAE.IsRemotable = "False" };
create operator .System.Script(const ASpecifier : System.String) : System.String
 class "System.SystemScriptNode" static tags { DAE.IsRemotable = "False" };
create operator .System.Script(const ASpecifier : System.String, const AIncludeDependents : System.Boolean) : System.String
 class "System.SystemScriptNode" static tags { DAE.IsRemotable = "False" };
create operator .System.Script(const ASpecifier : System.String, const AIncludeDependents : System.Boolean, const AIncludeObject : System.Boolean) : System.String
 class "System.SystemScriptNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`Script` returns a D4 script to create the specified object. Optionally the script to create the dependents of the object can be included using `AIncludeDependents`. Optionally the D4 script to create the specified object can be omitted using `AIncludeObject`.

**Using System.Script**

```d4
select Script("Users", false, true);
```

```d4
create table .System.Users in System.Catalog
{
    ID : System.UserID, 
    Name : System.String tags { Frontend.Preview.Include = "true" }, 
    key{ID} static tags { Storage.Enforced = "true" }
    
} tags { Frontend.UseList = "true" };
```

**See Also**

[Scripts and Execution](../../../developers-guide/d4-scripts-and-execution.md) \| [Frontend.ExecuteScript](../../libraries/Frontend.md#frontendexecutescript) \| [System.ScriptDrop](System.ScriptDrop.md) \| [System.ScriptCatalog](System.ScriptCatalog.md) \| [System.ScriptDropCatalog](System.ScriptDropCatalog.md) \| [System.ScriptLibrary](System.ScriptLibrary.md) \| [System.ScriptDropLibrary](System.ScriptLibrary.md) \| [System.ScriptData](System.ScriptData.md)
