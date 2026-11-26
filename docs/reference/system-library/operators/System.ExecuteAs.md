# System.ExecuteAs

**Declarations**

```d4
create operator .System.ExecuteAs(const AScript : System.String, const AUserID : System.UserID, const APassword : System.String)
 class "System.SystemExecuteAsNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAs(const AScript : System.String, const AUserID : System.UserID, const APassword : System.String, const AInParams : generic row)
 class "System.SystemExecuteAsNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteAs(const AScript : System.String, const AUserID : System.UserID, const APassword : System.String, const AInParams : generic row, var AOutParams : generic row)
 class "System.SystemExecuteAsNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
```

**Remarks**

`ExecuteAs` compiles and executes the code in the string specified in `AString`. It is executed as if it were normal code and is run as the user specified in `AUserID`. The string can be typed in as literals or built and placed as a variable.

By default, the inferred characteristics for the `ExecuteAs` operator are:

- non-literal
- non-functional
- non-deterministic
- non-repeatable

**Using System.ExecuteAs**

```d4
create table Example in Temp
     { PID : Integer, Key : String, key{Key} };
begin
 insert table { row { ProcessID() PID, "Cur" Key } } into Example;
 ExecuteAs
 (
  "insert table
  {
   row { ProcessID() PID, 'Pro' Key }
  } into Example;",
  "Admin",
  ""
 );
end;
select Example;
drop table Example;
```

```text
PID  Key
---- ---
2800 Cur
2753 Pro
```

**See Also**

Frontend.ExecuteScript \| [System.Evaluate](System.Evaluate.md) \| [System.Execute](System.Execute.md) \| [System.ExecuteAsync](System.ExecuteAsync.md) \| [System.ExecuteMultiple](System.ExecuteMultiple.md) \| [System.ExecuteWithTimeout](System.ExecuteWithTimeout.md) \| [Dynamic Execution](../../../developers-guide/modeling-process-logic.md#dynamic-execution)
