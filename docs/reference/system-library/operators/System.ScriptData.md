# System.ScriptData

**Declarations**

```d4
create operator .System.ScriptData(const AExpression : System.String) : System.String
 class "System.SystemScriptDataNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ScriptData` returns a table selector (D4 script) that represents all the data in the specified table `AExpression`.

**Using System.ScriptData**

```d4
select ScriptData("Users");
```

```d4
table of
{
 ID : System.UserID,
 Name : System.String
}
{
    row
 {
  System.UserID('Admin') ID,
  'Administrator' Name
 }, 
    row
 {
  System.UserID('System') ID,
  'System User' Name
 }
}
```

**See Also**

[Scripts and Execution](../../../developers-guide/d4-scripts-and-execution.md) \| [Frontend.ExecuteScript](../../libraries/Frontend.md#frontendexecutescript) \| [System.Script](System.Script.md) \| [System.ScriptCatalog](System.ScriptCatalog.md) \| [System.ScriptDrop](System.ScriptDrop.md) \| [System.ScriptDropCatalog](System.ScriptDropCatalog.md) \| [System.ScriptLibrary](System.ScriptLibrary.md) \| [System.ScriptDropLibrary](System.ScriptDropLibrary.md) \| [System.ScriptData](System.ScriptData.md)
