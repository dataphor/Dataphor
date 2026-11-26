# System.StopProcess

**Declarations**

```d4
create operator .System.StopProcess(const AProcessID : System.Integer)
 class "System.SystemStopProcessNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`StopProcess` stops a process.

**Using System.StopProcess**

```d4
create table Example in Temp
{
 PID : Integer,
 Key : String,
 key{Key}
};
insert table "
begin
 var LProcessID : Integer := StartProcess();
 insert table { row { LProcessID PID, "Cur" Key } } into Example;
 ExecuteAsync
 (
  LProcessID,
  "insert table
  {
   row
   {
    ProcessID() PID,
    'Pro' Key
   }
  } into Example;"
 );
 StopProcess(LProcessID);
end;
select Example;
drop table Example;
```

```text
PID  Key 
---- --- 
2703 Cur 
2696 New 
2696 Pro
```

**See Also**

[System.ProcessID](System.ProcessID.md) \| [System.SetMaxConcurrentProcesses](System.SetMaxConcurrentProcesses.md) \| [System.SetProcessWaitTimeout](System.SetProcessWaitTimeout.md) \| [System.StartProcess](System.StartProcess.md) \| [System.ExecuteAsync](System.ExecuteAsync.md)
