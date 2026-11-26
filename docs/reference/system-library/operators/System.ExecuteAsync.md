# System.ExecuteAsync

**Declarations**

```d4
create operator .System.ExecuteAsync(const AScript : System.String)
 class "System.SystemExecuteAsyncNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAsync(const AScript : System.String, const AInParams : generic row)
 class "System.SystemExecuteAsyncNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAsync(const AScript : System.String, const AInParams : generic row, var AOutParams : generic row)
 class "System.SystemExecuteAsyncNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAsync(const AProcessID : System.Integer, const AScript : System.String)
 class "System.SystemExecuteAsyncNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAsync(const AProcessID : System.Integer, const AScript : System.String, const AInParams : generic row)
 class "System.SystemExecuteAsyncNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAsync(const AProcessID : System.Integer, const AScript : System.String, const AInParams : generic row, var AOutParams : generic row)
 class "System.SystemExecuteAsyncNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
```

**Remarks**

`ExecuteAsync` compiles and executes the code in the string specified in `AString`. It is executed as if it were normal code and is run on the specified process `AProcessID`. The string can be typed in as literals or built and placed as a variable. If `AProcessID` is omitted, the current process will be used.

By default, the inferred characteristics for the `ExecuteAsync` operator are:

- non-literal
- non-functional
- non-deterministic
- non-repeatable

**Using System.ExecuteAsync**

```d4
create table Example in Temp
{
 PID : Integer,
 Key : String,
 key{Key}
};
insert table { row { ProcessID() PID, "Cur" Key } } into Example;
begin
 var LProcessID : Integer := StartProcess();
 insert table { row { LProcessID PID, "New" Key } } into Example;
 ExecuteAsync
 (
  LProcessID,
  "insert table
  {
   row { ProcessID() PID, 'Pro' Key }
  } into Example;" );
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

Frontend.ExecuteScript \| [System.Evaluate](System.Evaluate.md) \| [System.Execute](System.Execute.md) \| [System.ExecuteAs](System.ExecuteAs.md) \| [System.ExecuteMultiple](System.ExecuteMultiple.md) \| [System.ExecuteWithTimeout](System.ExecuteWithTimeout.md) \| [Dynamic Execution](../../../developers-guide/modeling-process-logic.md#dynamic-execution)
