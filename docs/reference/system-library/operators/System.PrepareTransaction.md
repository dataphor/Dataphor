# System.PrepareTransaction

**Declarations**

```d4
create operator .System.PrepareTransaction()
 class "System.SystemPrepareTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`PrepareTransaction` prepares the transaction for the final commit. Executing the `PrepareTransaction` operator checks all of the integrity constraints and ensures that everything is ready in this transaction to commit.

**Using System.PrepareApplicationTransaction**

```d4
begin
    BeginTransaction();
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    TempCursor.Insert( row { 10 ID, "Juan" Name});
    PrepareTransaction();
    RollbackTransaction();
end;
```

**See Also**

[Transaction Management, Application Design](../../../developers-guide/modeling-process-logic.md#transaction-management) \| [Transaction Management, Database Management Concepts](../../../developers-guide/database-management-concepts.md#transaction-management) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginTransaction](System.BeginTransaction.md)
