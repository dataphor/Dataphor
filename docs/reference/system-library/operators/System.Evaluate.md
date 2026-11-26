# System.Evaluate

**Declarations**

```d4
create operator .System.Evaluate(const AExpression : System.String) : generic
 class "System.SystemEvaluateNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotabe = "False", 
        DAE.IsNilable = "True"
    };
create operator .System.Evaluate(const AExpression : System.String, const AInParams : generic row) : generic
 class "System.SystemEvaluateNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsNilable = "True"
    };
create operator .System.Evaluate(const AExpression : System.String, const AInParams : generic row, var AOutParams : generic row) : generic
 class "System.SystemEvaluateNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsNilable = "True"
    };
create operator .System.Evaluate(const AProcessID : System.Integer, const AExpression : System.String) : generic
 class "System.SystemEvaluateNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotabe = "False", 
        DAE.IsNilable = "True"
    };
create operator .System.Evaluate(const AProcessID : System.Integer, const AExpression : System.String, const AInParams : generic row) : generic
 class "System.SystemEvaluateNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsNilable = "True"
    };
create operator .System.Evaluate(const AProcessID : System.Integer, const AExpression : System.String, const AInParams : generic row, var AOutParams : generic row) : generic
 class "System.SystemEvaluateNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False", 
        DAE.IsNilable = "True"
    };
```

**Remarks**

`Evaluate` dynamically evaluates the given expression and returns the result as a generic value.

If AProcessID is specified the evaluation will be performed on the specified process, otherwise it will be performed on the current process.

By default, the inferred characteristics for the `Evaluate` operator are:

- non-literal
- functional
- deterministic
- repeatable
- nilable

**Using System.Evaluate**

```d4
select Evaluate("6 - 2");
select Evaluate("Arg1 + Arg2", row { 2 Arg1, 6 Arg2 });
```

```text
4
8
```

**See Also**

[System.Execute](System.Execute.md) \| [Dynamic Execution](../../../developers-guide/modeling-process-logic.md#dynamic-execution)
