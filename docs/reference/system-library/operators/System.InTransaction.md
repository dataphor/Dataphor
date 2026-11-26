# System.InTransaction

**Declarations**

```d4
create operator .System.InTransaction() : System.Boolean
 class "System.SystemInTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsLiteral = "False" };
```

**Remarks**

`InTransaction` performs a check to see if the code is currently being executed in a transaction. It returns a boolean value representing the state.

**Using System.InTransaction**

```d4
begin
    var Err : String := "";
    Err := Err + Unicode({13}) + InTransaction().ToString();
    CommitTransaction();
    Err := Err + Unicode({13}) + InTransaction().ToString();
    raise Error(Err);
end;
```

```text
true
false
```

**See Also**

[Transaction Management, Application Design](../../../developers-guide/modeling-process-logic.md#transaction-management) \| [Transaction Management, Database Management Concepts](../../../developers-guide/database-management-concepts.md#transaction-management) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginTransaction](System.BeginTransaction.md)
