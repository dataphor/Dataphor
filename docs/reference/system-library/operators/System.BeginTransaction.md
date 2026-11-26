# System.BeginTransaction

**Declarations**

```d4
create operator .System.BeginTransaction()
 class "System.SystemBeginTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.BeginTransaction(const AIsolationLevel : System.String)
 class "System.SystemBeginTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`BeginTransaction` begins a database transaction.

**Using System.BeginTransaction**

```d4
begin
    BeginTransaction();
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    TempCursor.Insert( row { 10 ID, "Juan" Name});
    TempCursor.Delete();
    PrepareTransaction();
    RollbackTransaction();
end;
```

**See Also**

[Transaction Management, Application Design](../../../developers-guide/modeling-process-logic.md#transaction-management) \| [Transaction Management, Database Management Concepts](../../../developers-guide/database-management-concepts.md#transaction-management) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.PrepareTransaction](System.PrepareTransaction.md) \| [System.CommitTransaction](System.CommitTransaction.md) \| [System.RollbackTransaction](System.RollbackTransaction.md) \| [System.TransactionCount](System.TransactionCount.md) \|[System.InTransaction](System.InTransaction.md)
