# System.Execute

**Declarations**

```d4
create operator .System.Execute(const AScript : System.String)
 class "System.SystemExecuteNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.Execute(const AScript : System.String, const AInParams : generic row)
 class "System.SystemExecuteNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.Execute(const AScript : System.String, const AInParams : generic row, var AOutParams : generic row)
 class "System.SystemExecuteNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.Execute(const AProcessID : System.Integer, const AScript : System.String)
 class "System.SystemExecuteNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.Execute(const AProcessID : System.Integer, const AScript : System.String, const AInParams : generic row)
 class "System.SystemExecuteNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.Execute(const AProcessID : System.Integer, const AScript : System.String, const AInParams : generic row, var AOutParams : generic row)
 class "System.SystemExecuteNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
```

**Remarks**

`Execute` compiles and executes the code in the string specified in `AString`. It is executed as if it were normal code. The string can be typed in as literals or built and placed as a variable. If `AProcessID` is omitted, the current process will be used.

By default, the inferred characteristics for the `Execute` operator are:

- non-literal
- non-functional
- non-deterministic
- non-repeatable

**Using System.Execute**

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
 Execute(LProcessID, "insert table { row { ProcessID() PID, 'Pro' Key } } into Example;");
end;
select Example;
drop table Example;
```

```text
PID  Key
---- ---
2430 Cur
2013 New
2013 Pro
```

**See Also**

[Frontend.ExecuteScript](../../libraries/Frontend.md#frontendexecutescript) \| [System.Evaluate](System.Evaluate.md) \| [System.ExecuteAs](System.ExecuteAs.md) \| [System.ExecuteAsync](System.ExecuteAsync.md) \| [System.ExecuteMultiple](System.ExecuteMultiple.md) \| [System.ExecuteWithTimeout](System.ExecuteWithTimeout.md) \| [Dynamic Execution](../../../developers-guide/modeling-process-logic.md#dynamic-execution)
