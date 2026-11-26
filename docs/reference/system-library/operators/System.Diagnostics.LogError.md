# System.Diagnostics.LogError

**Declarations**

```d4
create operator .System.Diagnostics.LogError(const AError : System.Error)
 class "System.SystemLogErrorNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`LogError` writes the specified error `AError` to the Dataphor log.

**Using System.Diagnostics.LogError**

```d4
begin
 try
  raise Error("This is a test Error message");
 except
  on LError : Error do
  begin
   LogError(LError);
  end;
 end;
end;
```

```text
Entries in log file read:

2004-05-24 10:24:29.2745 Event: LogStarted
2004-05-24 10:24:41.7590 Event: ServerStarted
2004-05-24 10:36:42.9043 Error: DataphorException: This is a test Error message
   at Alphora.Dataphor.DAE.Runtime.Instructions.PlanNode.Execute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\PlanNode.cs:line 358
   at Alphora.Dataphor.DAE.Runtime.Instructions.TryExceptNode.InternalExecute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\ImperativeNodes.cs:line 371
```

**See Also**

[System.Diagnostics.DisableErrorLogging](System.Diagnostics.DisableErrorLogging.md) \| [System.Diagnostics.EnableErrorLogging](System.Diagnostics.EnableErrorLogging.md) \| [System.Diagnostics.LogMessage](System.Diagnostics.LogMessage.md)
