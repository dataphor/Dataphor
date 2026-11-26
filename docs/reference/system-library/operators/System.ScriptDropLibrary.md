# System.ScriptDropLibrary

**Declarations**

```d4
create operator .System.ScriptDropLibrary(const AName : System.Name) : System.String
 class "System.SystemScriptDropLibraryNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ScriptDropLibrary` returns a D4 script that would drop all the objects currently in the specified library `ALibraryName`.

**Using System.ScriptDropLibrary**

```d4
select ScriptDropLibrary("SimpleDevice");
```

```d4
drop operator SimpleDevice.SaveTable(const System.Name, const System.Name);
drop operator SimpleDevice.LoadTable(const System.Name, const System.Name);
drop operator SimpleDevice.TruncateTable(const System.Name, const System.Name);
drop operator SimpleDevice.BeginUpdate(const System.Name, const System.Name);
drop operator SimpleDevice.EndUpdate(const System.Name, const System.Name);
```

**See Also**

[Scripts and Execution](../../../developers-guide/d4-scripts-and-execution.md) \| Frontend.ExecuteScript \| [System.Script](System.Script.md) \| [System.ScriptCatalog](System.ScriptCatalog.md) \| [System.ScriptDrop](System.ScriptDrop.md) \| [System.ScriptDropCatalog](System.ScriptDropCatalog.md) \| [System.ScriptDropLibrary](System.ScriptDropLibrary.md) \| [System.ScriptData](System.ScriptData.md)
