# System.ScriptDropCatalog

**Declarations**

```d4
create operator .System.ScriptDropCatalog() : System.String
 class "System.SystemScriptDropCatalogNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ScriptDropCatalog` returns a D4 script that would drop the entire catalog.

**Using System.ScriptDropCatalog**

```d4
select ScriptDropCatalog();
```

```d4
UnregisterLibrary('Security');
. . .
UnregisterLibrary('SQLDevice');
DropUser('Administrator');
DropUser('System User');
```

**See Also**

[Scripts and Execution](../../../developers-guide/d4-scripts-and-execution.md) \| Frontend.ExecuteScript \| [System.Script](System.Script.md) \| [System.ScriptCatalog](System.ScriptCatalog.md) \| [System.ScriptDrop](System.ScriptDrop.md) \| [System.ScriptLibrary](System.ScriptLibrary.md) \| [System.ScriptDropLibrary](System.ScriptDropLibrary.md) \| [System.ScriptData](System.ScriptData.md)
