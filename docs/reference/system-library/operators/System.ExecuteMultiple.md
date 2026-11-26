# System.ExecuteMultiple

**Declarations**

```d4
create operator .System.ExecuteMultiple(const AStatement : System.String, const ANumber : System.Integer, const AUserID : System.String, const APassword : System.String)
 class "System.SystemExecuteMultipleNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
create operator .System.ExecuteMultiple(const AStatement : System.String, const ANumber : System.Integer)
 class "System.SystemExecuteMultipleNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRepeatable = "False"
    };
```

**Remarks**

`ExecuteMultiple` compiles and executes the code in the string specified in `AString`. It is executed as if it were normal code and repeated `ANumber` times. The string can be typed in as literals or built and placed as a variable. Optionally a `AUserID` and `APassword` can be specified in which case the code will be executed as the specified user.

By default, the inferred characteristics for the `ExecuteMultiple` operator are:

- non-literal
- non-functional
- non-deterministic
- non-repeatable

**Using System.ExecuteMultiple**

```d4
create table Example in Temp
     { TString : String, key{} };
insert table { row { "" TString } } into Example;
ExecuteMultiple
(
 "update Example set { TString := TString + 'x' };",
 5
);
select Example;
drop table Example;
```

```text
TString 
------- 
xxxxx
```

**See Also**

[Frontend.ExecuteScript](../../libraries/Frontend.md#frontendexecutescript) \| [System.Evaluate](System.Evaluate.md) \| [System.Execute](System.Execute.md) \| [System.ExecuteAs](System.ExecuteAs.md) \| [System.ExecuteAsync](System.ExecuteAsync.md) \| [System.ExecuteWithTimeout](System.ExecuteWithTimeout.md) \| [Dynamic Execution](../../../developers-guide/modeling-process-logic.md#dynamic-execution)
