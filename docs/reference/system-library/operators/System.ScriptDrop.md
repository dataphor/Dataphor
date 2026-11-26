# System.ScriptDrop

**Declarations**

```d4
create operator .System.ScriptDrop(const AName : System.Name) : System.String
 class "System.SystemScriptDropNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ScriptDrop(const ASpecifier : System.String) : System.String
 class "System.SystemScriptDropNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ScriptDrop(const ASpecifier : System.String, const AIncludeDependents : System.Boolean) : System.String
 class "System.SystemScriptDropNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ScriptDrop(const ASpecifier : System.String, const AIncludeDependents : System.Boolean, const AIncludeObject : System.Boolean) : System.String
 class "System.SystemScriptDropNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ScriptDrop` returns a D4 script that would drop the specified object. Optionally the D4 script that would drop the dependents of the object can be included using `AIncludeDependents`. Optionally the D4 script that would drop the specified object can be omitted using `AIncludeObject`.

**Using System.ScriptDrop**

```d4
select ScriptDrop("Users", false, true);
```

```d4
drop table System.Users;
```

**See Also**

[Scripts and Execution](../../../developers-guide/d4-scripts-and-execution.md) \| [Frontend.ExecuteScript](../../libraries/Frontend.md#frontendexecutescript) \| [System.Script](System.Script.md) \| [System.ScriptCatalog](System.ScriptCatalog.md) \| [System.ScriptDropCatalog](System.ScriptDropCatalog.md) \| [System.ScriptLibrary](System.ScriptLibrary.md) \| [System.ScriptDropLibrary](System.ScriptDropLibrary.md) \| [System.ScriptData](System.ScriptData.md)
